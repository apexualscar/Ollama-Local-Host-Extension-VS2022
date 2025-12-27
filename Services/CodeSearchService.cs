using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using DteProject = EnvDTE.Project;

namespace OllamaLocalHostIntergration.Services
{
    /// <summary>
    /// Service for searching code elements within the solution (Copilot-style)
    /// </summary>
    public class CodeSearchService
    {
        private readonly DTE2 _dte;

        public CodeSearchService()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            _dte = Package.GetGlobalService(typeof(DTE)) as DTE2;
        }

        /// <summary>
        /// Search result item
        /// </summary>
        public class SearchResult
        {
            public string DisplayName { get; set; }
            public string FilePath { get; set; }
            public string Content { get; set; }
            public SearchResultType Type { get; set; }
            public string ClassName { get; set; }
            public string MethodName { get; set; }
            public string ProjectName { get; set; }
            public int LineNumber { get; set; }
        }

        public enum SearchResultType
        {
            File,
            Class,
            Method,
            Property,
            Interface,
            Project
        }

        /// <summary>
        /// Search all projects and files in solution
        /// </summary>
        public async Task<List<SearchResult>> SearchSolutionAsync(string searchTerm)
        {
            return await Task.Run(() =>
            {
                var results = new List<SearchResult>();
                
                ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    
                    if (_dte?.Solution == null)
                        return;

                    // Search through all projects
                    foreach (DteProject project in _dte.Solution.Projects)
                    {
                        try
                        {
                            SearchProject(project, searchTerm, results);
                        }
                        catch { }
                    }
                });

                return results;
            });
        }

        /// <summary>
        /// Get all files in solution
        /// </summary>
        public async Task<List<SearchResult>> GetAllFilesAsync()
        {
            return await Task.Run(() =>
            {
                var results = new List<SearchResult>();
                
                ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    
                    if (_dte?.Solution == null)
                        return;

                    foreach (DteProject project in _dte.Solution.Projects)
                    {
                        try
                        {
                            // CHANGED: Use search without filter to get files AND code elements
                            SearchProject(project, string.Empty, results);
                        }
                        catch { }
                    }
                });

                return results;
            });
        }

        /// <summary>
        /// Search within a specific project
        /// </summary>
        private void SearchProject(DteProject project, string searchTerm, List<SearchResult> results)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            
            if (project == null || project.ProjectItems == null)
                return;

            // Check project name - if searchTerm is empty, skip this check
            if (!string.IsNullOrEmpty(searchTerm) && 
                project.Name.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                results.Add(new SearchResult
                {
                    DisplayName = project.Name,
                    ProjectName = project.Name,
                    Type = SearchResultType.Project
                });
            }

            // Search project items
            SearchProjectItems(project.ProjectItems, searchTerm, project.Name, results);
        }

        /// <summary>
        /// Recursively search project items
        /// </summary>
        private void SearchProjectItems(ProjectItems items, string searchTerm, string projectName, List<SearchResult> results)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            
            if (items == null)
                return;

            foreach (ProjectItem item in items)
            {
                try
                {
                    // Check file name
                    var filePath = GetItemPath(item);
                    bool matchesSearch = string.IsNullOrEmpty(searchTerm) || 
                                       item.Name.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0;
                    
                    if (matchesSearch && !string.IsNullOrEmpty(filePath) && IsCodeFile(filePath))
                    {
                        results.Add(new SearchResult
                        {
                            DisplayName = item.Name,
                            FilePath = filePath,
                            ProjectName = projectName,
                            Type = SearchResultType.File
                        });
                    }

                    // Search code elements within file
                    if (item.FileCodeModel != null)
                    {
                        SearchCodeElements(item.FileCodeModel.CodeElements, searchTerm, projectName, GetItemPath(item), results);
                    }

                    // Recursively search sub-items
                    if (item.ProjectItems != null && item.ProjectItems.Count > 0)
                    {
                        SearchProjectItems(item.ProjectItems, searchTerm, projectName, results);
                    }
                }
                catch { }
            }
        }

        /// <summary>
        /// Get all files in solution
        /// </summary>
        private void GetProjectFiles(DteProject project, List<SearchResult> results)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            
            if (project == null || project.ProjectItems == null)
                return;

            GetProjectItemFiles(project.ProjectItems, project.Name, results);
        }

        /// <summary>
        /// Recursively get all files
        /// </summary>
        private void GetProjectItemFiles(ProjectItems items, string projectName, List<SearchResult> results)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            
            if (items == null)
                return;

            foreach (ProjectItem item in items)
            {
                try
                {
                    var filePath = GetItemPath(item);
                    if (!string.IsNullOrEmpty(filePath) && IsCodeFile(filePath))
                    {
                        results.Add(new SearchResult
                        {
                            DisplayName = item.Name,
                            FilePath = filePath,
                            ProjectName = projectName,
                            Type = SearchResultType.File
                        });
                    }

                    // Recursively search sub-items
                    if (item.ProjectItems != null && item.ProjectItems.Count > 0)
                    {
                        GetProjectItemFiles(item.ProjectItems, projectName, results);
                    }
                }
                catch { }
            }
        }

        /// <summary>
        /// Search code elements (classes, methods, properties)
        /// </summary>
        private void SearchCodeElements(CodeElements elements, string searchTerm, string projectName, string filePath, List<SearchResult> results)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            
            if (elements == null)
                return;

            foreach (CodeElement element in elements)
            {
                try
                {
                    // Check element name - if searchTerm is empty, include all elements
                    bool matchesSearch = string.IsNullOrEmpty(searchTerm) || 
                                       element.Name.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0;
                    
                    if (matchesSearch)
                    {
                        var result = new SearchResult
                        {
                            DisplayName = GetElementDisplayName(element),
                            FilePath = filePath,
                            ProjectName = projectName,
                            LineNumber = element.StartPoint.Line
                        };

                        switch (element.Kind)
                        {
                            case vsCMElement.vsCMElementClass:
                                result.Type = SearchResultType.Class;
                                result.ClassName = element.Name;
                                results.Add(result);
                                break;

                            case vsCMElement.vsCMElementInterface:
                                result.Type = SearchResultType.Interface;
                                result.ClassName = element.Name;
                                results.Add(result);
                                break;

                            case vsCMElement.vsCMElementFunction:
                                result.Type = SearchResultType.Method;
                                result.MethodName = element.Name;
                                // Get parent class name
                                try
                                {
                                    var parent = element.Collection?.Parent as CodeElement;
                                    if (parent != null)
                                    {
                                        result.ClassName = parent.Name;
                                    }
                                }
                                catch { }
                                results.Add(result);
                                break;

                            case vsCMElement.vsCMElementProperty:
                                result.Type = SearchResultType.Property;
                                result.MethodName = element.Name;
                                // Get parent class name
                                try
                                {
                                    var parent = element.Collection?.Parent as CodeElement;
                                    if (parent != null)
                                    {
                                        result.ClassName = parent.Name;
                                    }
                                }
                                catch { }
                                results.Add(result);
                                break;
                        }
                    }

                    // Recursively search child elements
                    if (element.Children != null && element.Children.Count > 0)
                    {
                        SearchCodeElements(element.Children, searchTerm, projectName, filePath, results);
                    }
                }
                catch { }
            }
        }

        /// <summary>
        /// Get display name for code element
        /// </summary>
        private string GetElementDisplayName(CodeElement element)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            
            try
            {
                switch (element.Kind)
                {
                    case vsCMElement.vsCMElementClass:
                        return $"class {element.Name}";
                    
                    case vsCMElement.vsCMElementInterface:
                        return $"interface {element.Name}";
                    
                    case vsCMElement.vsCMElementFunction:
                        var func = element as CodeFunction;
                        if (func != null)
                        {
                            string parentName = "";
                            try
                            {
                                var parent = element.Collection?.Parent as CodeElement;
                                if (parent != null)
                                {
                                    parentName = parent.Name + ".";
                                }
                            }
                            catch { }
                            return $"{parentName}{func.Name}()";
                        }
                        return element.Name + "()";
                    
                    case vsCMElement.vsCMElementProperty:
                        string parentName2 = "";
                        try
                        {
                            var parent = element.Collection?.Parent as CodeElement;
                            if (parent != null)
                            {
                                parentName2 = parent.Name + ".";
                            }
                        }
                        catch { }
                        return $"{parentName2}{element.Name}";
                    
                    default:
                        return element.Name;
                }
            }
            catch
            {
                return element.Name;
            }
        }

        /// <summary>
        /// Get file path from project item
        /// </summary>
        private string GetItemPath(ProjectItem item)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            
            try
            {
                if (item.FileCount > 0)
                {
                    return item.FileNames[0];
                }
            }
            catch { }
            
            return null;
        }

        /// <summary>
        /// Check if file is a code file
        /// </summary>
        private bool IsCodeFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return false;

            var ext = System.IO.Path.GetExtension(filePath).ToLower();
            return ext == ".cs" || ext == ".vb" || ext == ".fs" || 
                   ext == ".cpp" || ext == ".h" || ext == ".hpp" || 
                   ext == ".c" || ext == ".java" || ext == ".py" || 
                   ext == ".js" || ext == ".ts" || ext == ".xml" || 
                   ext == ".xaml" || ext == ".json";
        }

        /// <summary>
        /// Get content of a code element
        /// </summary>
        public async Task<string> GetElementContentAsync(SearchResult result)
        {
            return await Task.Run(() =>
            {
                string content = "";
                
                ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    
                    if (result.Type == SearchResultType.File)
                    {
                        // Read entire file
                        if (System.IO.File.Exists(result.FilePath))
                        {
                            content = System.IO.File.ReadAllText(result.FilePath);
                        }
                    }
                    else
                    {
                        // Extract specific code element
                        content = await ExtractCodeElementAsync(result);
                    }
                });

                return content;
            });
        }

        /// <summary>
        /// Extract code for a specific element (class, method, etc.)
        /// </summary>
        private async Task<string> ExtractCodeElementAsync(SearchResult result)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            
            if (!System.IO.File.Exists(result.FilePath))
                return "";

            try
            {
                var lines = System.IO.File.ReadAllLines(result.FilePath);
                
                // Find the element and extract its code
                var startLine = Math.Max(0, result.LineNumber - 1);
                var endLine = FindElementEnd(lines, startLine);
                
                var elementLines = lines.Skip(startLine).Take(endLine - startLine + 1);
                return string.Join(Environment.NewLine, elementLines);
            }
            catch
            {
                // Fallback: read entire file
                return System.IO.File.ReadAllText(result.FilePath);
            }
        }

        /// <summary>
        /// Find the end line of a code element (simplified)
        /// </summary>
        private int FindElementEnd(string[] lines, int startLine)
        {
            int braceCount = 0;
            bool foundOpenBrace = false;

            for (int i = startLine; i < lines.Length; i++)
            {
                var line = lines[i];
                
                foreach (char c in line)
                {
                    if (c == '{')
                    {
                        braceCount++;
                        foundOpenBrace = true;
                    }
                    else if (c == '}')
                    {
                        braceCount--;
                        if (foundOpenBrace && braceCount == 0)
                        {
                            return i;
                        }
                    }
                }
            }

            return Math.Min(startLine + 50, lines.Length - 1); // Default: 50 lines
        }
    }
}
