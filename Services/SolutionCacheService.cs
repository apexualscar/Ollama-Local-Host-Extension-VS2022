using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using DteProject = EnvDTE.Project;

namespace OllamaLocalHostIntergration.Services
{
    /// <summary>
    /// Caches the solution's files, classes, and methods so that the context search
    /// dialog can filter instantly instead of walking the DTE tree on every keystroke.
    /// Rebuilds automatically when a file is saved.
    /// </summary>
    public sealed class SolutionCacheService : IVsRunningDocTableEvents, IDisposable
    {
        // ── Singleton ──────────────────────────────────────────────
        private static readonly Lazy<SolutionCacheService> _instance =
            new Lazy<SolutionCacheService>(() => new SolutionCacheService());

        public static SolutionCacheService Instance => _instance.Value;

        // ── State ──────────────────────────────────────────────────
        private readonly DTE2 _dte;
        private List<CachedItem> _cache = new List<CachedItem>();
        private readonly object _lock = new object();
        private bool _isBuilding;
        private uint _rdtCookie;
        private IVsRunningDocumentTable _rdt;
        private DateTime _lastBuildTime = DateTime.MinValue;
        private bool _initialized;

        /// <summary>True while the cache is being rebuilt in the background.</summary>
        public bool IsBuilding => _isBuilding;

        /// <summary>Fires when the cache finishes rebuilding.</summary>
        public event EventHandler CacheUpdated;

        // ── Constructor ────────────────────────────────────────────
        private SolutionCacheService()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            _dte = Package.GetGlobalService(typeof(DTE)) as DTE2;
        }

        // ── Public API ─────────────────────────────────────────────

        /// <summary>
        /// Call once from the tool window constructor (on the UI thread).
        /// Subscribes to save events and kicks off the first background index.
        /// </summary>
        public void Initialize()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (_initialized) return;
            _initialized = true;

            // Subscribe to the Running Document Table so we know when a file is saved
            _rdt = Package.GetGlobalService(typeof(SVsRunningDocumentTable)) as IVsRunningDocumentTable;
            if (_rdt != null)
            {
                _rdt.AdviseRunningDocTableEvents(this, out _rdtCookie);
            }

            // Build the cache for the first time
            _ = RebuildCacheAsync();
        }

        /// <summary>
        /// Returns a snapshot of cached items that match <paramref name="searchTerm"/>
        /// and optionally filtered by <paramref name="typeFilter"/> ("File", "Class", "Method").
        /// This is pure in-memory work — no DTE calls.
        /// </summary>
        public List<CachedItem> Search(string searchTerm, string typeFilter = null, int maxResults = 100)
        {
            List<CachedItem> snapshot;
            lock (_lock)
            {
                snapshot = _cache;
            }

            IEnumerable<CachedItem> query = snapshot;

            // Filter by type
            if (!string.IsNullOrEmpty(typeFilter))
            {
                query = typeFilter switch
                {
                    "File" => query.Where(c => c.Type == CachedItemType.File),
                    "Class" => query.Where(c => c.Type == CachedItemType.Class || c.Type == CachedItemType.Interface),
                    "Method" => query.Where(c => c.Type == CachedItemType.Method || c.Type == CachedItemType.Property),
                    _ => query
                };
            }

            // Filter by search term
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(c =>
                    c.DisplayName.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            return query.Take(maxResults).ToList();
        }

        /// <summary>
        /// Forces a full cache rebuild. Safe to call from any thread.
        /// </summary>
        public async Task RebuildCacheAsync()
        {
            if (_isBuilding) return;

            try
            {
                _isBuilding = true;
                System.Diagnostics.Debug.WriteLine("[SolutionCache] Rebuilding cache...");

                var items = await Task.Run(() =>
                {
                    var results = new List<CachedItem>();

                    ThreadHelper.JoinableTaskFactory.Run(async () =>
                    {
                        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                        if (_dte?.Solution == null) return;

                        foreach (DteProject project in _dte.Solution.Projects)
                        {
                            try
                            {
                                IndexProject(project, results);
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine(
                                    $"[SolutionCache] Error indexing project {project?.Name}: {ex.Message}");
                            }
                        }
                    });

                    return results;
                });

                lock (_lock)
                {
                    _cache = items;
                }

                _lastBuildTime = DateTime.UtcNow;

                System.Diagnostics.Debug.WriteLine(
                    $"[SolutionCache] Cache built: {items.Count} items " +
                    $"(Files={items.Count(i => i.Type == CachedItemType.File)}, " +
                    $"Classes={items.Count(i => i.Type == CachedItemType.Class)}, " +
                    $"Methods={items.Count(i => i.Type == CachedItemType.Method)})");

                CacheUpdated?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SolutionCache] Rebuild error: {ex.Message}");
            }
            finally
            {
                _isBuilding = false;
            }
        }

        // ── DTE traversal (runs on UI thread) ─────────────────────

        private void IndexProject(DteProject project, List<CachedItem> results)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (project?.ProjectItems == null) return;
            IndexProjectItems(project.ProjectItems, project.Name, results);
        }

        private void IndexProjectItems(ProjectItems items, string projectName, List<CachedItem> results)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (items == null) return;

            foreach (ProjectItem item in items)
            {
                try
                {
                    var filePath = GetItemPath(item);
                    if (!string.IsNullOrEmpty(filePath) && IsCodeFile(filePath))
                    {
                        results.Add(new CachedItem
                        {
                            DisplayName = item.Name,
                            FilePath = filePath,
                            ProjectName = projectName,
                            Type = CachedItemType.File
                        });

                        // Parse code elements
                        if (item.FileCodeModel?.CodeElements != null)
                        {
                            try
                            {
                                IndexCodeElements(item.FileCodeModel.CodeElements, projectName, filePath, results);
                            }
                            catch { /* FileCodeModel can throw for some file types */ }
                        }
                    }

                    // Recurse into sub-items (folders, nested items)
                    if (item.ProjectItems?.Count > 0)
                    {
                        IndexProjectItems(item.ProjectItems, projectName, results);
                    }
                }
                catch { }
            }
        }

        private void IndexCodeElements(CodeElements elements, string projectName, string filePath, List<CachedItem> results)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (elements == null) return;

            foreach (CodeElement el in elements)
            {
                try
                {
                    switch (el.Kind)
                    {
                        case vsCMElement.vsCMElementClass:
                            results.Add(new CachedItem
                            {
                                DisplayName = $"class {el.Name}",
                                ClassName = el.Name,
                                FilePath = filePath,
                                ProjectName = projectName,
                                Type = CachedItemType.Class,
                                LineNumber = el.StartPoint.Line
                            });
                            break;

                        case vsCMElement.vsCMElementInterface:
                            results.Add(new CachedItem
                            {
                                DisplayName = $"interface {el.Name}",
                                ClassName = el.Name,
                                FilePath = filePath,
                                ProjectName = projectName,
                                Type = CachedItemType.Interface,
                                LineNumber = el.StartPoint.Line
                            });
                            break;

                        case vsCMElement.vsCMElementFunction:
                            string parentName = GetParentName(el);
                            results.Add(new CachedItem
                            {
                                DisplayName = $"{parentName}{el.Name}()",
                                MethodName = el.Name,
                                ClassName = parentName.TrimEnd('.'),
                                FilePath = filePath,
                                ProjectName = projectName,
                                Type = CachedItemType.Method,
                                LineNumber = el.StartPoint.Line
                            });
                            break;

                        case vsCMElement.vsCMElementProperty:
                            string parentName2 = GetParentName(el);
                            results.Add(new CachedItem
                            {
                                DisplayName = $"{parentName2}{el.Name}",
                                MethodName = el.Name,
                                ClassName = parentName2.TrimEnd('.'),
                                FilePath = filePath,
                                ProjectName = projectName,
                                Type = CachedItemType.Property,
                                LineNumber = el.StartPoint.Line
                            });
                            break;
                    }

                    // Recurse into children (e.g. methods inside classes)
                    if (el.Children?.Count > 0)
                    {
                        IndexCodeElements(el.Children, projectName, filePath, results);
                    }
                }
                catch { }
            }
        }

        private string GetParentName(CodeElement el)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            try
            {
                var parent = el.Collection?.Parent as CodeElement;
                if (parent != null) return parent.Name + ".";
            }
            catch { }
            return "";
        }

        // ── IVsRunningDocTableEvents ──────────────────────────────
        // We only care about AfterSave; the rest are no-ops.

        public int OnAfterSave(uint docCookie)
        {
            // Debounce: don't rebuild more than once every 3 seconds
            if ((DateTime.UtcNow - _lastBuildTime).TotalSeconds < 3)
                return VSConstants.S_OK;

            _ = RebuildCacheAsync();
            return VSConstants.S_OK;
        }

        public int OnAfterFirstDocumentLock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining) => VSConstants.S_OK;
        public int OnBeforeLastDocumentUnlock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining) => VSConstants.S_OK;
        public int OnAfterAttributeChange(uint docCookie, uint grfAttribs) => VSConstants.S_OK;
        public int OnBeforeDocumentWindowShow(uint docCookie, int fFirstShow, IVsWindowFrame pFrame) => VSConstants.S_OK;
        public int OnAfterDocumentWindowHide(uint docCookie, IVsWindowFrame pFrame) => VSConstants.S_OK;

        // ── Helpers ───────────────────────────────────────────────

        private string GetItemPath(ProjectItem item)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            try { return item.FileCount > 0 ? item.FileNames[0] : null; }
            catch { return null; }
        }

        private static bool IsCodeFile(string path)
        {
            var ext = System.IO.Path.GetExtension(path)?.ToLowerInvariant();
            return ext == ".cs" || ext == ".vb" || ext == ".fs" ||
                   ext == ".cpp" || ext == ".h" || ext == ".hpp" || ext == ".c" ||
                   ext == ".java" || ext == ".py" || ext == ".js" || ext == ".ts" ||
                   ext == ".xml" || ext == ".xaml" || ext == ".json";
        }

        public void Dispose()
        {
            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                if (_rdt != null && _rdtCookie != 0)
                {
                    _rdt.UnadviseRunningDocTableEvents(_rdtCookie);
                }
            }
            catch { }
        }

        // ── Cached item model ─────────────────────────────────────

        public enum CachedItemType
        {
            File,
            Class,
            Interface,
            Method,
            Property,
            Project
        }

        public class CachedItem
        {
            public string DisplayName { get; set; }
            public string FilePath { get; set; }
            public string ProjectName { get; set; }
            public string ClassName { get; set; }
            public string MethodName { get; set; }
            public CachedItemType Type { get; set; }
            public int LineNumber { get; set; }

            /// <summary>Details line shown under display name.</summary>
            public string Details
            {
                get
                {
                    if (!string.IsNullOrEmpty(ProjectName) && !string.IsNullOrEmpty(FilePath))
                        return $"{ProjectName} \u2022 {System.IO.Path.GetFileName(FilePath)}";
                    if (!string.IsNullOrEmpty(ProjectName))
                        return ProjectName;
                    if (!string.IsNullOrEmpty(FilePath))
                        return FilePath;
                    return "";
                }
            }

            /// <summary>Type label for the badge.</summary>
            public string TypeLabel => Type switch
            {
                CachedItemType.File => "FILE",
                CachedItemType.Class => "CLASS",
                CachedItemType.Interface => "INTERFACE",
                CachedItemType.Method => "METHOD",
                CachedItemType.Property => "PROPERTY",
                _ => ""
            };

            /// <summary>Segoe MDL2 icon glyph.</summary>
            public string Icon => Type switch
            {
                CachedItemType.File => "\uE8A5",
                CachedItemType.Class => "\uE8D3",
                CachedItemType.Interface => "\uE8D4",
                CachedItemType.Method => "\uE8E3",
                CachedItemType.Property => "\uE8B9",
                CachedItemType.Project => "\uE8F1",
                _ => "\uE8A5"
            };
        }
    }
}
