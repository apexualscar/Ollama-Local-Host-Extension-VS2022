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
            Unloaded += OnUnloaded;
        }
        
        public void SetCodeModificationService(CodeModificationService service)
        {
            _codeModService = service;
        }

        /// <summary>
        /// Public method to update display text during streaming
        /// </summary>
        public void UpdateStreamingText(string text)
        {
            txtContent.Text = text;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // Unsubscribe from old message
            if (e.OldValue is ChatMessage oldMessage)
            {
                oldMessage.ContentUpdated -= OnContentUpdated;
            }

            if (DataContext is ChatMessage message)
            {
                // Subscribe to live content updates (for streaming)
                message.ContentUpdated += OnContentUpdated;
                RenderMessage(message);
            }
        }

        private void OnContentUpdated()
        {
            if (DataContext is ChatMessage message)
            {
                // Update text directly for streaming (no code block parsing)
                txtContent.Text = message.Content;
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            // Prevent memory leaks by unsubscribing
            if (DataContext is ChatMessage message)
            {
                message.ContentUpdated -= OnContentUpdated;
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
                    // Phase 6.3 FIX: Direct insertion using CodeEditorService
                    var codeEditorService = new CodeEditorService();
                    
                    // Check if there's a selection
                    var selectionInfo = await codeEditorService.GetSelectionInfoAsync();
                    
                    bool success = false;
                    
                    if (!string.IsNullOrEmpty(selectionInfo.text))
                    {
                        // Replace the selected text
                        success = await codeEditorService.ReplaceSelectedTextAsync(code);
                    }
                    else
                    {
                        // Insert at cursor position
                        success = await codeEditorService.InsertTextAtCursorAsync(code);
                    }
                    
                    if (success)
                    {
                        // Visual feedback - update button
                        var originalContent = button.Content;
                        
                        var stackPanel = new StackPanel { Orientation = Orientation.Horizontal };
                        var icon = new TextBlock 
                        { 
                            Text = "\uE73E", // Checkmark icon
                            FontFamily = new System.Windows.Media.FontFamily("Segoe MDL2 Assets"), 
                            Margin = new Thickness(0, 0, 4, 0) 
                        };
                        var text = new TextBlock { Text = "Applied!" };
                        stackPanel.Children.Add(icon);
                        stackPanel.Children.Add(text);
                        button.Content = stackPanel;
                        
                        // Reset button after 2 seconds
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
                            "Failed to insert code. Please ensure you have an active document open.",
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
