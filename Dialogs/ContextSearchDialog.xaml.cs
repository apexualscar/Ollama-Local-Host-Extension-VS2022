using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualStudio.Shell;
using OllamaLocalHostIntergration.Models;
using OllamaLocalHostIntergration.Services;
using WpfSelectionChangedEventArgs = System.Windows.Controls.SelectionChangedEventArgs;

namespace OllamaLocalHostIntergration.Dialogs
{
    public partial class ContextSearchDialog : UserControl
    {
        private readonly CodeSearchService _searchService;
        private readonly PromptBuilder _promptBuilder;
        private ObservableCollection<SearchResultViewModel> _searchResults;
        private System.Threading.CancellationTokenSource _searchCts;
        private string _filterType; // Phase 6.5: Filter by type

        public event EventHandler<ContextReference> ContextSelected;

        public ContextSearchDialog(string filterType = null)
        {
            InitializeComponent();
            
            _searchService = new CodeSearchService();
            _promptBuilder = new PromptBuilder();
            _searchResults = new ObservableCollection<SearchResultViewModel>();
            _filterType = filterType; // Phase 6.5: Store filter
            
            resultsPanel.ItemsSource = _searchResults;
            
            // Phase 6.5: Update placeholder based on filter
            UpdatePlaceholder();
            
            System.Diagnostics.Debug.WriteLine($"[ContextSearch] Initialized with filter: {filterType ?? "None"}");
        }

        /// <summary>
        /// Phase 6.5: Update placeholder text based on filter
        /// </summary>
        private void UpdatePlaceholder()
        {
            string placeholder = _filterType switch
            {
                "File" => "Type at least 2 characters to search files...",
                "Class" => "Type at least 2 characters to search classes...",
                "Method" => "Type at least 2 characters to search methods...",
                _ => "Type at least 2 characters to search files, classes, methods..."
            };
            
            txtPlaceholder.Text = placeholder;
        }

        /// <summary>
        /// Load initial search results - Phase 6.3 FIX: Show helpful message, don't load anything
        /// </summary>
        private async Task LoadInitialResultsAsync()
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                
                // Phase 6.3 FIX: Don't load anything - just show placeholder
                _searchResults.Clear();
                
                System.Diagnostics.Debug.WriteLine($"[ContextSearch] Ready - waiting for user input");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ContextSearch] Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Handle search text changes
        /// </summary>
        private async void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Update placeholder visibility
            txtPlaceholder.Visibility = string.IsNullOrEmpty(txtSearch.Text) 
                ? Visibility.Visible 
                : Visibility.Collapsed;

            // Cancel previous search
            _searchCts?.Cancel();
            _searchCts = new System.Threading.CancellationTokenSource();
            var token = _searchCts.Token;

            var searchTerm = txtSearch.Text?.Trim();
            
            // Phase 6.3 FIX: Require at least 2 characters to search
            if (string.IsNullOrEmpty(searchTerm) || searchTerm.Length < 2)
            {
                _searchResults.Clear();
                return;
            }

            // Debounce search - wait for user to stop typing
            try
            {
                await Task.Delay(400, token); // Slightly longer delay for better debouncing
            }
            catch (System.Threading.Tasks.TaskCanceledException)
            {
                return;
            }
            catch (System.OperationCanceledException)
            {
                return;
            }
            
            if (token.IsCancellationRequested)
                return;

            await PerformSearchAsync(searchTerm);
        }

        /// <summary>
        /// Perform search - Phase 6.3 FIX: Async on background thread with filtering
        /// </summary>
        private async Task PerformSearchAsync(string searchTerm)
        {
            try
            {
                ShowLoading(true);
                
                System.Diagnostics.Debug.WriteLine($"[ContextSearch] Searching for: {searchTerm} (Filter: {_filterType ?? "None"})");
                
                // Phase 6.5: Run search on background thread with filter
                var results = await Task.Run(async () =>
                {
                    var allResults = await _searchService.SearchSolutionAsync(searchTerm);
                    
                    // Phase 6.5: Filter results by type if specified
                    if (!string.IsNullOrEmpty(_filterType))
                    {
                        allResults = allResults.Where(r => 
                        {
                            return _filterType switch
                            {
                                "File" => r.Type == CodeSearchService.SearchResultType.File,
                                "Class" => r.Type == CodeSearchService.SearchResultType.Class || 
                                          r.Type == CodeSearchService.SearchResultType.Interface,
                                "Method" => r.Type == CodeSearchService.SearchResultType.Method || 
                                           r.Type == CodeSearchService.SearchResultType.Property,
                                _ => true
                            };
                        }).ToList();
                    }
                    
                    return allResults;
                });
                
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                
                _searchResults.Clear();
                
                // Phase 6.5: Limit results to 100 for performance
                int count = 0;
                foreach (var result in results)
                {
                    if (count >= 100) break;
                    _searchResults.Add(new SearchResultViewModel(result));
                    count++;
                }
                
                ShowLoading(false);
                
                System.Diagnostics.Debug.WriteLine($"[ContextSearch] Found {_searchResults.Count} results (filtered by {_filterType ?? "None"})");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ContextSearch] Search error: {ex.Message}");
                ShowLoading(false);
            }
        }

        /// <summary>
        /// Phase 6.3: Handle ListBox selection changed (replaces button click)
        /// </summary>
        private async void ResultsPanel_SelectionChanged(object sender, WpfSelectionChangedEventArgs e)
        {
            if (resultsPanel.SelectedItem is SearchResultViewModel viewModel)
            {
                // Deselect immediately to allow reselecting same item
                resultsPanel.SelectedItem = null;
                
                try
                {
                    ShowLoading(true);
                    
                    // Get content for the selected item
                    var content = await _searchService.GetElementContentAsync(viewModel.SearchResult);
                    var tokenCount = _promptBuilder.EstimateTokenCount(content);

                    // Create context reference
                    var contextRef = new ContextReference
                    {
                        Type = MapSearchResultType(viewModel.SearchResult.Type),
                        DisplayText = viewModel.DisplayName,
                        FilePath = viewModel.SearchResult.FilePath,
                        ClassName = viewModel.SearchResult.ClassName,
                        MethodName = viewModel.SearchResult.MethodName,
                        ProjectName = viewModel.SearchResult.ProjectName,
                        Content = content,
                        TokenCount = tokenCount
                    };

                    // Raise event
                    ContextSelected?.Invoke(this, contextRef);
                    
                    ShowLoading(false);
                    
                    System.Diagnostics.Debug.WriteLine($"[ContextSearch] Selected: {contextRef.DisplayText}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[ContextSearch] Error adding context: {ex.Message}");
                    ShowLoading(false);
                }
            }
        }

        /// <summary>
        /// Handle active document quick action
        /// </summary>
        private void ActiveDocument_Click(object sender, RoutedEventArgs e)
        {
            // Close and signal active document selection
            var contextRef = new ContextReference
            {
                Type = ContextReferenceType.File,
                DisplayText = "Active Document"
            };
            
            ContextSelected?.Invoke(this, contextRef);
        }

        /// <summary>
        /// Handle selection quick action
        /// </summary>
        private void Selection_Click(object sender, RoutedEventArgs e)
        {
            // Close and signal selection
            var contextRef = new ContextReference
            {
                Type = ContextReferenceType.Selection,
                DisplayText = "Selection"
            };
            
            ContextSelected?.Invoke(this, contextRef);
        }

        /// <summary>
        /// Close dialog
        /// </summary>
        private void Close_Click(object sender, RoutedEventArgs e)
        {
            // Find parent window and close
            var window = Window.GetWindow(this);
            window?.Close();
        }

        /// <summary>
        /// Show/hide loading indicator
        /// </summary>
        private void ShowLoading(bool show)
        {
            txtLoading.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// Map search result type to context reference type
        /// </summary>
        private ContextReferenceType MapSearchResultType(CodeSearchService.SearchResultType type)
        {
            switch (type)
            {
                case CodeSearchService.SearchResultType.File:
                    return ContextReferenceType.File;
                case CodeSearchService.SearchResultType.Class:
                case CodeSearchService.SearchResultType.Interface:
                    return ContextReferenceType.Class;
                case CodeSearchService.SearchResultType.Method:
                case CodeSearchService.SearchResultType.Property:
                    return ContextReferenceType.Method;
                case CodeSearchService.SearchResultType.Project:
                    return ContextReferenceType.Project;
                default:
                    return ContextReferenceType.File;
            }
        }
    }

    /// <summary>
    /// View model for search results
    /// </summary>
    public class SearchResultViewModel
    {
        public CodeSearchService.SearchResult SearchResult { get; }

        public SearchResultViewModel(CodeSearchService.SearchResult result)
        {
            SearchResult = result;
        }

        public string DisplayName => SearchResult.DisplayName;

        public string Details
        {
            get
            {
                if (!string.IsNullOrEmpty(SearchResult.ProjectName) && !string.IsNullOrEmpty(SearchResult.FilePath))
                {
                    var fileName = System.IO.Path.GetFileName(SearchResult.FilePath);
                    return $"{SearchResult.ProjectName} • {fileName}";
                }
                else if (!string.IsNullOrEmpty(SearchResult.ProjectName))
                {
                    return SearchResult.ProjectName;
                }
                else if (!string.IsNullOrEmpty(SearchResult.FilePath))
                {
                    return SearchResult.FilePath;
                }
                return "";
            }
        }

        public string TypeLabel
        {
            get
            {
                switch (SearchResult.Type)
                {
                    case CodeSearchService.SearchResultType.File:
                        return "FILE";
                    case CodeSearchService.SearchResultType.Class:
                        return "CLASS";
                    case CodeSearchService.SearchResultType.Interface:
                        return "INTERFACE";
                    case CodeSearchService.SearchResultType.Method:
                        return "METHOD";
                    case CodeSearchService.SearchResultType.Property:
                        return "PROPERTY";
                    case CodeSearchService.SearchResultType.Project:
                        return "PROJECT";
                    default:
                        return ""; // Phase 6.3: Return empty string instead of unknown
                }
            }
        }

        public string Icon
        {
            get
            {
                switch (SearchResult.Type)
                {
                    case CodeSearchService.SearchResultType.File:
                        return "\uE8A5"; // Document
                    case CodeSearchService.SearchResultType.Class:
                        return "\uE8D3"; // Class
                    case CodeSearchService.SearchResultType.Interface:
                        return "\uE8D4"; // Interface
                    case CodeSearchService.SearchResultType.Method:
                        return "\uE8E3"; // Method
                    case CodeSearchService.SearchResultType.Property:
                        return "\uE8B9"; // Property
                    case CodeSearchService.SearchResultType.Project:
                        return "\uE8F1"; // Project
                    default:
                        return "\uE8A5";
                }
            }
        }
    }
}
