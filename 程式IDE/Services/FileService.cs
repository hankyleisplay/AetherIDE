using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using 程式IDE.Models;

namespace 程式IDE.Services
{
    public class FileService
    {
        private static readonly HashSet<string> IgnoredDirectories = new(StringComparer.OrdinalIgnoreCase)
        {
            ".git", ".vs", "bin", "obj", "node_modules", ".idea", ".vscode", "packages"
        };

        public async Task<string> ReadFileAsync(string path)
        {
            using var reader = new StreamReader(path);
            return await reader.ReadToEndAsync();
        }

        public async Task WriteFileAsync(string path, string content)
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            await File.WriteAllTextAsync(path, content);
        }

        public ObservableCollection<FileItem> LoadWorkspace(string rootPath)
        {
            var items = new ObservableCollection<FileItem>();
            if (!Directory.Exists(rootPath)) return items;

            try
            {
                var rootDir = new DirectoryInfo(rootPath);
                var rootItem = FileItem.FromDirectory(rootDir);
                rootItem.IsExpanded = true;
                PopulateChildren(rootItem, maxDepth: 2, currentDepth: 0);
                items.Add(rootItem);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading workspace: {ex.Message}");
            }

            return items;
        }

        public void PopulateChildren(FileItem parent, int maxDepth = 2, int currentDepth = 0)
        {
            if (!parent.IsDirectory || currentDepth >= maxDepth) return;

            parent.Children.Clear();
            try
            {
                var dirInfo = new DirectoryInfo(parent.FullPath);

                // Directories first
                foreach (var dir in dirInfo.GetDirectories().OrderBy(d => d.Name))
                {
                    if (IgnoredDirectories.Contains(dir.Name) || dir.Attributes.HasFlag(FileAttributes.Hidden))
                        continue;

                    var dirItem = FileItem.FromDirectory(dir);
                    if (currentDepth + 1 < maxDepth)
                    {
                        PopulateChildren(dirItem, maxDepth, currentDepth + 1);
                    }
                    parent.Children.Add(dirItem);
                }

                // Files next
                foreach (var file in dirInfo.GetFiles().OrderBy(f => f.Name))
                {
                    if (file.Attributes.HasFlag(FileAttributes.Hidden))
                        continue;

                    parent.Children.Add(FileItem.FromFile(file));
                }
            }
            catch (UnauthorizedAccessException) { }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading children for {parent.FullPath}: {ex.Message}");
            }
        }

        public async Task<List<SearchResultItem>> SearchWorkspaceAsync(string rootPath, string query, bool matchCase, bool wholeWord)
        {
            return await Task.Run(() =>
            {
                var results = new List<SearchResultItem>();
                if (string.IsNullOrWhiteSpace(query) || !Directory.Exists(rootPath))
                    return results;

                var comparison = matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
                string pattern = wholeWord ? $@"\b{Regex.Escape(query)}\b" : Regex.Escape(query);
                var regex = new Regex(pattern, matchCase ? RegexOptions.None : RegexOptions.IgnoreCase);

                try
                {
                    var files = Directory.EnumerateFiles(rootPath, "*.*", SearchOption.AllDirectories)
                        .Where(f => !IgnoredDirectories.Any(ign => f.Contains(Path.DirectorySeparatorChar + ign + Path.DirectorySeparatorChar)));

                    foreach (var file in files)
                    {
                        var ext = Path.GetExtension(file).ToLowerInvariant();
                        // Search in common text and code files
                        if (ext is ".exe" or ".dll" or ".png" or ".jpg" or ".jpeg" or ".gif" or ".ico" or ".zip" or ".pdb")
                            continue;

                        try
                        {
                            var lines = File.ReadLines(file);
                            int lineNum = 1;
                            foreach (var line in lines)
                            {
                                if (regex.IsMatch(line))
                                {
                                    results.Add(new SearchResultItem
                                    {
                                        FilePath = file,
                                        FileName = Path.GetFileName(file),
                                        LineNumber = lineNum,
                                        LineContent = line.Trim(),
                                        MatchText = query
                                    });

                                    if (results.Count >= 500) return results; // Cap results for responsiveness
                                }
                                lineNum++;
                            }
                        }
                        catch { }
                    }
                }
                catch { }

                return results;
            });
        }
    }
}

