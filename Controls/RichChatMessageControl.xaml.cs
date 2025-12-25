using System;
using System.Windows;
using System.Windows.Controls;
using OllamaLocalHostIntergration.Models;
using OllamaLocalHostIntergration.Services;
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
                    // Visual feedback
                    button.Content = "? Copied!";
                    var timer = new System.Windows.Threading.DispatcherTimer
                    {
                        Interval = TimeSpan.FromSeconds(2)
                    };
                    timer.Tick += (s, args) =>
                    {
                        button.Content = "?? Copy";
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
                if (_codeModService == null || !(DataContext is ChatMessage message))
                    return;
                
                try
                {
                    // If there's an associated CodeEdit, use it
                    if (message.AssociatedCodeEdit != null)
                    {
                        // Show confirmation dialog
                        var result = WpfMessageBox.Show(
                            "Apply this code change to the active editor?",
                            "Confirm Code Application",
                            WpfMessageBoxButton.YesNo,
                            WpfMessageBoxImage.Question
                        );
                        
                        if (result == WpfMessageBoxResult.Yes)
                        {
                            bool success = await _codeModService.ApplyCodeEditAsync(message.AssociatedCodeEdit);
                            
                            if (success)
                            {
                                button.Content = "? Applied!";
                                button.IsEnabled = false;
                                WpfMessageBox.Show("Code successfully applied!", "Success", WpfMessageBoxButton.OK, WpfMessageBoxImage.Information);
                            }
                            else
                            {
                                WpfMessageBox.Show("Failed to apply code. Please check the active editor.", "Error", WpfMessageBoxButton.OK, WpfMessageBoxImage.Error);
                            }
                        }
                    }
                    else
                    {
                        // Direct code application without CodeEdit
                        var result = WpfMessageBox.Show(
                            "Apply this code to the active editor? This will replace the current selection or document.",
                            "Confirm Code Application",
                            WpfMessageBoxButton.YesNo,
                            WpfMessageBoxImage.Question
                        );
                        
                        if (result == WpfMessageBoxResult.Yes)
                        {
                            var codeEditorService = new CodeEditorService();
                            bool success = await codeEditorService.ReplaceSelectedTextAsync(code);
                            
                            if (!success)
                            {
                                success = await codeEditorService.ReplaceDocumentTextAsync(code);
                            }
                            
                            if (success)
                            {
                                button.Content = "? Applied!";
                                button.IsEnabled = false;
                                WpfMessageBox.Show("Code successfully applied!", "Success", WpfMessageBoxButton.OK, WpfMessageBoxImage.Information);
                            }
                            else
                            {
                                WpfMessageBox.Show("Failed to apply code. Please check the active editor.", "Error", WpfMessageBoxButton.OK, WpfMessageBoxImage.Error);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    WpfMessageBox.Show($"Error applying code: {ex.Message}", "Error", WpfMessageBoxButton.OK, WpfMessageBoxImage.Error);
                }
            }
        }
    }
}
