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
        private readonly SolutionCacheService _cache;
        private readonly CodeSearchService _searchService;   // still used for GetElementContentAsync
        private readonly PromptBuilder _promptBuilder;
        private ObservableCollection<CachedItemViewModel> _searchResults;
        private System.Threading.CancellationTokenSource _searchCts;
        private string _filterType;

        public event EventHandler<ContextReference> ContextSelected;

        public ContextSearchDialog(string filterType = null)
        {
            InitializeComponent();

            _cache = SolutionCacheService.Instance;
            _searchService = new CodeSearchService();
            _promptBuilder = new PromptBuilder();
            _searchResults = new ObservableCollection<CachedItemViewModel>();
            _filterType = filterType;

            resultsPanel.ItemsSource = _searchResults;

            UpdatePlaceholder();

            // Purple accent focus ring on search box
            txtSearch.GotFocus += (s, ev) => {
                searchBorder.BorderBrush = (System.Windows.Media.Brush)FindResource(Microsoft.VisualStudio.Shell.VsBrushes.AccentMediumKey);
                searchBorder.BorderThickness = new Thickness(1.5);
            };
            txtSearch.LostFocus += (s, ev) => {
                searchBorder.BorderBrush = (System.Windows.Media.Brush)FindResource(Microsoft.VisualStudio.Shell.VsBrushes.ComboBoxBorderKey);
                searchBorder.BorderThickness = new Thickness(1);
            };

            System.Diagnostics.Debug.WriteLine($"[ContextSearch] Initialized with filter: {filterType ?? "None"}, cache has items: {!_cache.IsBuilding}");
        }

        private void UpdatePlaceholder()
        {
            string placeholder = _filterType switch
            {
                "File" => "Search files...",
                "Class" => "Search classes...",
                "Method" => "Search methods...",
                _ => "Search files, classes, methods..."
            };
            txtPlaceholder.Text = placeholder;
        }

        /// <summary>
        /// Handle search text changes — queries the in-memory cache (instant).
        /// </summary>
        private async void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            txtPlaceholder.Visibility = string.IsNullOrEmpty(txtSearch.Text)
                ? Visibility.Visible
                : Visibility.Collapsed;

            _searchCts?.Cancel();
            _searchCts = new System.Threading.CancellationTokenSource();
            var token = _searchCts.Token;

            var searchTerm = txtSearch.Text?.Trim();

            if (string.IsNullOrEmpty(searchTerm) || searchTerm.Length < 2)
            {
                _searchResults.Clear();
                return;
            }

            // Small debounce so we don't refresh on every keystroke
            try { await Task.Delay(150, token); }
            catch (OperationCanceledException) { return; }
            if (token.IsCancellationRequested) return;

            PerformCachedSearch(searchTerm);
        }

        /// <summary>
        /// Pure in-memory search against the SolutionCacheService.
        /// </summary>
        private void PerformCachedSearch(string searchTerm)
        {
            try
            {
                var results = _cache.Search(searchTerm, _filterType, 100);

                _searchResults.Clear();
                foreach (var item in results)
                {
                    _searchResults.Add(new CachedItemViewModel(item));
                }

                System.Diagnostics.Debug.WriteLine(
                    $"[ContextSearch] Cache search '{searchTerm}' => {results.Count} results");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ContextSearch] Cache search error: {ex.Message}");
            }
        }

        /// <summary>
        /// Handle selection — loads content then raises ContextSelected.
        /// </summary>
        private async void ResultsPanel_SelectionChanged(object sender, WpfSelectionChangedEventArgs e)
        {
            if (resultsPanel.SelectedItem is CachedItemViewModel viewModel)
            {
                resultsPanel.SelectedItem = null;

                try
                {
                    ShowLoading(true);

                    // Build a lightweight CodeSearchService.SearchResult for content extraction
                    var sr = new CodeSearchService.SearchResult
                    {
                        DisplayName = viewModel.Item.DisplayName,
                        FilePath = viewModel.Item.FilePath,
                        ProjectName = viewModel.Item.ProjectName,
                        ClassName = viewModel.Item.ClassName,
                        MethodName = viewModel.Item.MethodName,
                        LineNumber = viewModel.Item.LineNumber,
                        Type = MapCachedType(viewModel.Item.Type)
                    };

                    var content = await _searchService.GetElementContentAsync(sr);
                    var tokenCount = _promptBuilder.EstimateTokenCount(content);

                    var contextRef = new ContextReference
                    {
                        Type = MapToContextType(viewModel.Item.Type),
                        DisplayText = viewModel.DisplayName,
                        FilePath = viewModel.Item.FilePath,
                        ClassName = viewModel.Item.ClassName,
                        MethodName = viewModel.Item.MethodName,
                        ProjectName = viewModel.Item.ProjectName,
                        Content = content,
                        TokenCount = tokenCount
                    };

                    ContextSelected?.Invoke(this, contextRef);
                    ShowLoading(false);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[ContextSearch] Error adding context: {ex.Message}");
                    ShowLoading(false);
                }
            }
        }

        private void ActiveDocument_Click(object sender, RoutedEventArgs e)
        {
            ContextSelected?.Invoke(this, new ContextReference
            {
                Type = ContextReferenceType.File,
                DisplayText = "Active Document"
            });
        }

        private void Selection_Click(object sender, RoutedEventArgs e)
        {
            ContextSelected?.Invoke(this, new ContextReference
            {
                Type = ContextReferenceType.Selection,
                DisplayText = "Selection"
            });
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Window.GetWindow(this)?.Close();
        }

        private void ShowLoading(bool show)
        {
            txtLoading.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        }

        // ── Mapping helpers ────────────────────────────────────────

        private static CodeSearchService.SearchResultType MapCachedType(SolutionCacheService.CachedItemType t) => t switch
        {
            SolutionCacheService.CachedItemType.File => CodeSearchService.SearchResultType.File,
            SolutionCacheService.CachedItemType.Class => CodeSearchService.SearchResultType.Class,
            SolutionCacheService.CachedItemType.Interface => CodeSearchService.SearchResultType.Interface,
            SolutionCacheService.CachedItemType.Method => CodeSearchService.SearchResultType.Method,
            SolutionCacheService.CachedItemType.Property => CodeSearchService.SearchResultType.Property,
            _ => CodeSearchService.SearchResultType.File
        };

        private static ContextReferenceType MapToContextType(SolutionCacheService.CachedItemType t) => t switch
        {
            SolutionCacheService.CachedItemType.File => ContextReferenceType.File,
            SolutionCacheService.CachedItemType.Class => ContextReferenceType.Class,
            SolutionCacheService.CachedItemType.Interface => ContextReferenceType.Class,
            SolutionCacheService.CachedItemType.Method => ContextReferenceType.Method,
            SolutionCacheService.CachedItemType.Property => ContextReferenceType.Method,
            _ => ContextReferenceType.File
        };
    }

    /// <summary>
    /// Thin view model wrapping a CachedItem for data binding in the search ListBox.
    /// Exposes the same property names the XAML DataTemplate expects.
    /// </summary>
    public class CachedItemViewModel
    {
        public SolutionCacheService.CachedItem Item { get; }

        public CachedItemViewModel(SolutionCacheService.CachedItem item) => Item = item;

        public string DisplayName => Item.DisplayName;
        public string Details => Item.Details;
        public string TypeLabel => Item.TypeLabel;
        public string Icon => Item.Icon;
    }
}
