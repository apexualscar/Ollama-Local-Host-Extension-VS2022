using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OllamaLocalHostIntergration.Services
{
    /// <summary>
    /// Service for managing multiple files in context
    /// </summary>
    public class FileContextService
    {
        private List<string> _contextFiles = new List<string>();
        private Dictionary<string, string> _fileContents = new Dictionary<string, string>();
        private Dictionary<string, int> _fileTokenCounts = new Dictionary<string, int>();

        /// <summary>
        /// Adds a file to the context
        /// </summary>
        public async Task AddFileAsync(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || _contextFiles.Contains(filePath))
                return;

            await Task.Run(() =>
            {
                try
                {
                    if (File.Exists(filePath))
                    {
                        string content = File.ReadAllText(filePath);
                        _contextFiles.Add(filePath);
                        _fileContents[filePath] = content;
                        _fileTokenCounts[filePath] = EstimateTokenCount(content);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to add file: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Removes a file from the context
        /// </summary>
        public void RemoveFile(string filePath)
        {
            _contextFiles.Remove(filePath);
            _fileContents.Remove(filePath);
            _fileTokenCounts.Remove(filePath);
        }

        /// <summary>
        /// Clears all context files
        /// </summary>
        public void ClearFiles()
        {
            _contextFiles.Clear();
            _fileContents.Clear();
            _fileTokenCounts.Clear();
        }

        /// <summary>
        /// Gets all context file paths
        /// </summary>
        public List<string> GetContextFiles()
        {
            return new List<string>(_contextFiles);
        }

        /// <summary>
        /// Builds a combined context from all files
        /// </summary>
        public async Task<string> BuildMultiFileContextAsync()
        {
            if (_contextFiles.Count == 0)
                return string.Empty;

            return await Task.Run(() =>
            {
                var sb = new StringBuilder();

                foreach (var filePath in _contextFiles)
                {
                    if (_fileContents.TryGetValue(filePath, out string content))
                    {
                        string relativePath = GetRelativePath(filePath);
                        string filename = Path.GetFileName(filePath);

                        sb.AppendLine($"// File: {relativePath}");
                        sb.AppendLine($"// Tokens: ~{_fileTokenCounts[filePath]}");
                        sb.AppendLine(content);
                        sb.AppendLine();
                        sb.AppendLine("// ?????????????????????????????????");
                        sb.AppendLine();
                    }
                }

                return sb.ToString();
            });
        }

        /// <summary>
        /// Gets the total estimated token count for all files
        /// </summary>
        public int GetTotalTokenCount()
        {
            return _fileTokenCounts.Values.Sum();
        }

        /// <summary>
        /// Gets token count for a specific file
        /// </summary>
        public int GetFileTokenCount(string filePath)
        {
            return _fileTokenCounts.TryGetValue(filePath, out int count) ? count : 0;
        }

        /// <summary>
        /// Gets the number of files in context
        /// </summary>
        public int GetFileCount()
        {
            return _contextFiles.Count;
        }

        /// <summary>
        /// Checks if a file is in context
        /// </summary>
        public bool HasFile(string filePath)
        {
            return _contextFiles.Contains(filePath);
        }

        /// <summary>
        /// Refreshes content for all files
        /// </summary>
        public async Task RefreshAllFilesAsync()
        {
            var filesToRefresh = new List<string>(_contextFiles);
            _contextFiles.Clear();
            _fileContents.Clear();
            _fileTokenCounts.Clear();

            foreach (var filePath in filesToRefresh)
            {
                await AddFileAsync(filePath);
            }
        }

        /// <summary>
        /// Gets a summary of context files
        /// </summary>
        public string GetContextSummary()
        {
            if (_contextFiles.Count == 0)
                return "No files in context";

            var sb = new StringBuilder();
            sb.AppendLine($"Files: {_contextFiles.Count}");
            sb.AppendLine($"Total tokens: ~{GetTotalTokenCount()}");
            sb.AppendLine();

            foreach (var filePath in _contextFiles)
            {
                string filename = Path.GetFileName(filePath);
                int tokens = _fileTokenCounts[filePath];
                sb.AppendLine($"  ?? {filename} (~{tokens} tokens)");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Estimates token count for content (rough approximation)
        /// </summary>
        private int EstimateTokenCount(string content)
        {
            if (string.IsNullOrEmpty(content))
                return 0;

            // Rough estimate: 1 token ? 4 characters
            // This is a simplification but works for rough estimates
            return content.Length / 4;
        }

        /// <summary>
        /// Gets relative path for display
        /// </summary>
        private string GetRelativePath(string filePath)
        {
            try
            {
                // Try to get workspace-relative path
                string currentDir = Directory.GetCurrentDirectory();
                Uri fileUri = new Uri(filePath);
                Uri currentUri = new Uri(currentDir + Path.DirectorySeparatorChar);
                
                if (fileUri.IsAbsoluteUri && currentUri.IsAbsoluteUri)
                {
                    Uri relativeUri = currentUri.MakeRelativeUri(fileUri);
                    return Uri.UnescapeDataString(relativeUri.ToString().Replace('/', Path.DirectorySeparatorChar));
                }
            }
            catch
            {
                // Fall back to filename
            }

            return Path.GetFileName(filePath);
        }
    }
}
