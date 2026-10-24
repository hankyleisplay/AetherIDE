using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace 程式IDE.Services
{
    public class ExecutionService
    {
        private Process? _currentProcess;
        private CancellationTokenSource? _cancellationTokenSource;

        public event Action<string>? OutputReceived;
        public event Action<string>? ErrorReceived;
        public event Action<int>? ProcessExited;
        public event Action<string>? ProcessStarted;

        public bool IsRunning => _currentProcess != null && !_currentProcess.HasExited;

        public async Task RunFileAsync(string filePath, string? workingDirectory = null)
        {
            if (IsRunning)
            {
                OutputReceived?.Invoke("[系統] 已有正在執行的程式，請先停止目前的執行。\n");
                return;
            }

            if (!File.Exists(filePath))
            {
                ErrorReceived?.Invoke($"[錯誤] 找不到檔案: {filePath}\n");
                return;
            }

            var ext = Path.GetExtension(filePath).ToLowerInvariant();
            string fileName = "";
            string arguments = "";

            workingDirectory ??= Path.GetDirectoryName(filePath) ?? Environment.CurrentDirectory;

            switch (ext)
            {
                case ".py":
                    fileName = "python";
                    arguments = $"\"{filePath}\"";
                    break;
                case ".js":
                    fileName = "node";
                    arguments = $"\"{filePath}\"";
                    break;
                case ".ts":
                    fileName = "npx";
                    arguments = $"ts-node \"{filePath}\"";
                    break;
                case ".ps1":
                    fileName = "powershell.exe";
                    arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{filePath}\"";
                    break;
                case ".bat" or ".cmd":
                    fileName = "cmd.exe";
                    arguments = $"/c \"{filePath}\"";
                    break;
                case ".cs":
                    // Check if in a dotnet project folder
                    var projDir = FindProjectDirectory(workingDirectory);
                    if (projDir != null)
                    {
                        fileName = "dotnet";
                        arguments = "run";
                        workingDirectory = projDir;
                    }
                    else
                    {
                        fileName = "dotnet";
                        arguments = $"run --file \"{filePath}\"";
                    }
                    break;
                case ".html":
                    fileName = "explorer.exe";
                    arguments = $"\"{filePath}\"";
                    break;
                default:
                    // Fallback to powershell execution
                    fileName = "powershell.exe";
                    arguments = $"-NoProfile -Command \"& '{filePath}'\"";
                    break;
            }

            await ExecuteCommandAsync(fileName, arguments, workingDirectory, $"[開始執行] {Path.GetFileName(filePath)} ({fileName} {arguments})\n");
        }

        public async Task ExecuteCommandAsync(string command, string? workingDirectory = null)
        {
            if (string.IsNullOrWhiteSpace(command)) return;

            string fileName = "powershell.exe";
            string arguments = $"-NoProfile -Command \"{command}\"";
            workingDirectory ??= Environment.CurrentDirectory;

            await ExecuteCommandAsync(fileName, arguments, workingDirectory, $"> {command}\n");
        }

        private async Task ExecuteCommandAsync(string fileName, string arguments, string workingDirectory, string startMessage)
        {
            if (IsRunning)
            {
                OutputReceived?.Invoke("[系統] 已有程式正在執行。\n");
                return;
            }

            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                ProcessStarted?.Invoke(startMessage);

                var startInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    WorkingDirectory = workingDirectory,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };

                _currentProcess = new Process { StartInfo = startInfo, EnableRaisingEvents = true };

                _currentProcess.OutputDataReceived += (s, e) =>
                {
                    if (e.Data != null)
                    {
                        OutputReceived?.Invoke(e.Data + "\n");
                    }
                };

                _currentProcess.ErrorDataReceived += (s, e) =>
                {
                    if (e.Data != null)
                    {
                        ErrorReceived?.Invoke(e.Data + "\n");
                    }
                };

                var tcs = new TaskCompletionSource<int>();
                _currentProcess.Exited += (s, e) =>
                {
                    int exitCode = _currentProcess.ExitCode;
                    ProcessExited?.Invoke(exitCode);
                    tcs.TrySetResult(exitCode);
                };

                _currentProcess.Start();
                _currentProcess.BeginOutputReadLine();
                _currentProcess.BeginErrorReadLine();

                using (_cancellationTokenSource.Token.Register(() =>
                {
                    try
                    {
                        if (_currentProcess != null && !_currentProcess.HasExited)
                        {
                            _currentProcess.Kill(entireProcessTree: true);
                        }
                    }
                    catch { }
                }))
                {
                    await tcs.Task;
                }
            }
            catch (Exception ex)
            {
                ErrorReceived?.Invoke($"[執行失敗] {ex.Message}\n");
                ProcessExited?.Invoke(-1);
            }
            finally
            {
                _currentProcess?.Dispose();
                _currentProcess = null;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        public void Stop()
        {
            try
            {
                if (IsRunning && _currentProcess != null)
                {
                    _cancellationTokenSource?.Cancel();
                    OutputReceived?.Invoke("[系統] 已終止執行程式。\n");
                }
            }
            catch (Exception ex)
            {
                ErrorReceived?.Invoke($"[終止錯誤] {ex.Message}\n");
            }
        }

        private static string? FindProjectDirectory(string startDir)
        {
            var dir = new DirectoryInfo(startDir);
            while (dir != null)
            {
                if (dir.GetFiles("*.csproj").Length > 0 || dir.GetFiles("*.sln").Length > 0 || dir.GetFiles("*.slnx").Length > 0)
                {
                    return dir.FullName;
                }
                dir = dir.Parent;
            }
            return null;
        }
    }
}

