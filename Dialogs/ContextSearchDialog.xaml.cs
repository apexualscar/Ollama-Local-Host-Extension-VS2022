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

namespace OllamaLocalHostIntergration.Dialogs
{
    public partial class ContextSearchDialog : UserControl
    {
        private readonly CodeSearchService _searchService;
        private readonly PromptBuilder _promptBuilder;
        private ObservableCollection<SearchResultViewModel> _searchResults;
        private System.Threading.CancellationTokenSource _searchCts;

        public event EventHandler<ContextReference> ContextSelected;

        public ContextSearchDialog()
        {
            InitializeComponent();
            
            _searchService = new CodeSearchService();
            _promptBuilder = new PromptBuilder();
            _searchResults = new ObservableCollection<SearchResultViewModel>();
            
            resultsPanel.ItemsSource = _searchResults;
            
            // Load initial results (all files)
            _ = LoadInitialResultsAsync();
        }

        /// <summary>
        /// Load initial search results (all files in solution)
        /// </summary>
        private async Task LoadInitialResultsAsync()
        {
            try
            {
                ShowLoading(true);
                
                var results = await _searchService.GetAllFilesAsync();
                
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                
                _searchResults.Clear();
                foreach (var result in results.Take(50)) // Limit to 50 initially
                {
                    _searchResults.Add(new SearchResultViewModel(result));
                }
                
                ShowLoading(false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading initial results: {ex.Message}");
                ShowLoading(false);
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
            
            if (string.IsNullOrEmpty(searchTerm))
            {
                // Show all files if search is empty
                await LoadInitialResultsAsync();
                return;
            }

            // Debounce search
            try
            {
                await Task.Delay(300, token);
            }
            catch (System.Threading.Tasks.TaskCanceledException)
            {
                // Expected when user types quickly - exit gracefully
                return;
            }
            catch (System.OperationCanceledException)
            {
                // Also handle OperationCanceledException
                return;
            }
            
            if (token.IsCancellationRequested)
                return;

            await PerformSearchAsync(searchTerm);
        }

        /// <summary>
        /// Perform search
        /// </summary>
        private async Task PerformSearchAsync(string searchTerm)
        {
            try
            {
                ShowLoading(true);
                
                var results = await _searchService.SearchSolutionAsync(searchTerm);
                
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                
                _searchResults.Clear();
                foreach (var result in results.Take(100)) // Limit results
                {
                    _searchResults.Add(new SearchResultViewModel(result));
                }
                
                ShowLoading(false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error performing search: {ex.Message}");
                ShowLoading(false);
            }
        }

        /// <summary>
        /// Handle result item click
        /// </summary>
        private async void ResultItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is SearchResultViewModel viewModel)
            {
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
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error adding context: {ex.Message}");
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
                        return "";
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
