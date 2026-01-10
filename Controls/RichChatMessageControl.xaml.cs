using System;
using System.Windows;
using System.Windows.Controls;
using OllamaLocalHostIntergration.Models;
using OllamaLocalHostIntergration.Services;
using OllamaLocalHostIntergration.Dialogs;
using WpfMessageBox = System.Windows.MessageBox;
using WpfMessageBoxButton = System.Windows.MessageBoxButton;
using WpfMessageBoxImage = System.Windows.MessageBoxImage;
using WpfMessageBoxResult = System.Windows.MessageBoxResult;

namespace OllamaLocalHostIntergration.Controls
{
    public partial class RichChatMessageControl : UserControl
    {
        private CodeModificationService _codeModService;
        
        public RichChatMessageControl()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }
        
        public void SetCodeModificationService(CodeModificationService service)
        {
            _codeModService = service;
        }
        
        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (DataContext is ChatMessage message)
            {
                RenderMessage(message);
            }
        }
        
        private void RenderMessage(ChatMessage message)
        {
            // Set text content (without code blocks)
            if (message.HasCodeBlocks)
            {
                var parser = new MessageParserService();
                txtContent.Text = parser.PrepareDisplayContent(message);
            }
            else
            {
                txtContent.Text = message.Content;
            }
            
            // Bind code blocks
            if (message.HasCodeBlocks)
            {
                codeBlocksPanel.ItemsSource = message.CodeBlocks;
            }
        }
        
        private void CopyCode_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string code)
            {
                try
                {
                    Clipboard.SetText(code);
                    // Visual feedback - update the button content
                    var originalContent = button.Content;
                    
                    var stackPanel = new StackPanel { Orientation = Orientation.Horizontal };
                    var icon = new TextBlock { Text = "\uE8FB", FontFamily = new System.Windows.Media.FontFamily("Segoe MDL2 Assets"), Margin = new Thickness(0, 0, 4, 0) };
                    var text = new TextBlock { Text = "Copied!" };
                    stackPanel.Children.Add(icon);
                    stackPanel.Children.Add(text);
                    button.Content = stackPanel;
                    
                    var timer = new System.Windows.Threading.DispatcherTimer
                    {
                        Interval = TimeSpan.FromSeconds(2)
                    };
                    timer.Tick += (s, args) =>
                    {
                        button.Content = originalContent;
                        timer.Stop();
                    };
                    timer.Start();
                }
                catch
                {
                    // Silently fail
                }
            }
        }
        
        private async void ApplyCode_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string code)
            {
                try
                {
                    // Phase 6.3: Always insert at cursor position, regardless of mode
                    var codeEditorService = new CodeEditorService();
                    
                    // Get current cursor position info
                    var selectionInfo = await codeEditorService.GetSelectionInfoAsync();
                    
                    // If there's a selection, replace it; otherwise insert at cursor
                    bool success;
                    if (!string.IsNullOrEmpty(selectionInfo.text))
                    {
                        // Replace selection
                        success = await codeEditorService.ReplaceSelectedTextAsync(code);
                    }
                    else
                    {
                        // Insert at cursor position
                        success = await codeEditorService.InsertTextAtCursorAsync(code);
                    }
                    
                    if (success)
                    {
                        // Visual feedback
                        var originalContent = button.Content;
                        
                        var stackPanel = new StackPanel { Orientation = Orientation.Horizontal };
                        var icon = new TextBlock { Text = "\uE8FB", FontFamily = new System.Windows.Media.FontFamily("Segoe MDL2 Assets"), Margin = new Thickness(0, 0, 4, 0) };
                        var text = new TextBlock { Text = "Applied!" };
                        stackPanel.Children.Add(icon);
                        stackPanel.Children.Add(text);
                        button.Content = stackPanel;
                        
                        // Reset after 2 seconds
                        var timer = new System.Windows.Threading.DispatcherTimer
                        {
                            Interval = TimeSpan.FromSeconds(2)
                        };
                        timer.Tick += (s, args) =>
                        {
                            button.Content = originalContent;
                            timer.Stop();
                        };
                        timer.Start();
                    }
                    else
                    {
                        WpfMessageBox.Show(
                            "Failed to insert code. Please ensure you have an active document open with a cursor position.",
                            "Error",
                            WpfMessageBoxButton.OK,
                            WpfMessageBoxImage.Error
                        );
                    }
                }
                catch (Exception ex)
                {
                    WpfMessageBox.Show(
                        $"Error applying code: {ex.Message}",
                        "Error",
                        WpfMessageBoxButton.OK,
                        WpfMessageBoxImage.Error
                    );
                }
            }
        }
    }
}
