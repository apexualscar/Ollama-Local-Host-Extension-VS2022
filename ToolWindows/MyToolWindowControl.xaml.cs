using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using OllamaLocalHostIntergration.Services;
using OllamaLocalHostIntergration.Models;
using System.Threading.Tasks;
using EnvDTE;
using System.Collections.Generic;
using SelectionChangedEventArgs = Community.VisualStudio.Toolkit.SelectionChangedEventArgs;

namespace OllamaLocalHostIntergration
{
    public partial class MyToolWindowControl : UserControl
    {
        private readonly OllamaService _ollamaService;
        private ObservableCollection<ChatMessage> _chatMessages;
        private string _currentCodeContext = string.Empty;
        private List<string> _availableModels = new List<string>();

        public MyToolWindowControl()
        {
            InitializeComponent();
            _ollamaService = new OllamaService();
            _chatMessages = new ObservableCollection<ChatMessage>();
            chatMessagesPanel.ItemsSource = _chatMessages;

            // Set default server address
            txtServerAddress.Text = "http://localhost:11434";
            
            // Handle Enter key in the input box
            txtUserInput.KeyDown += TxtUserInputKeyDown;

            // Load initial code context
            _ = RefreshCodeContextAsync();
            // Load models
            _ = RefreshModelsAsync();
        }

        private async void SendMessageClick(object sender, RoutedEventArgs e)
        {
            await SendUserMessage();
        }

        private async void TxtUserInputKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                e.Handled = true;
                await SendUserMessage();
            }
        }

        private async Task SendUserMessage()
        {
            string serverAddress = txtServerAddress.Text;
            string userMessage = txtUserInput.Text;
            
            if (string.IsNullOrWhiteSpace(userMessage))
                return;

            // Update server address if changed
            _ollamaService.UpdateServerAddress(serverAddress);

            // Set selected model
            if (comboModels.SelectedItem is string selectedModel)
                _ollamaService.SetModel(selectedModel);

            // Add user message to chat
            _chatMessages.Add(new ChatMessage(userMessage, true));

            // Clear input
            txtUserInput.Clear();

            try
            {
                // Use the latest code context
                string codeContext = _currentCodeContext;

                // Show loading message
                _chatMessages.Add(new ChatMessage("Thinking...", false));
                txtStatusBar.Text = "Waiting for Ollama response...";
                
                // Get response from Ollama
                string response = await _ollamaService.GenerateResponse(userMessage, codeContext);
                
                // Remove loading message and add actual response
                _chatMessages.RemoveAt(_chatMessages.Count - 1);
                _chatMessages.Add(new ChatMessage(response, false));
                txtStatusBar.Text = "Response received.";
            }
            catch (Exception ex)
            {
                _chatMessages.Add(new ChatMessage($"Error: {ex.Message}", false));
                txtStatusBar.Text = $"Error: {ex.Message}";
            }

            // Scroll to bottom
            if (chatMessagesScroll != null)
            {
                chatMessagesScroll.ScrollToBottom();
            }
        }

        private async Task<string> GetActiveDocumentTextAsync()
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                var dte = Package.GetGlobalService(typeof(DTE)) as DTE;
                if (dte?.ActiveDocument?.Object("TextDocument") is TextDocument textDoc)
                {
                    EditPoint editPoint = textDoc.StartPoint.CreateEditPoint();
                    return editPoint.GetText(textDoc.EndPoint);
                }
            }
            catch (Exception)
            {
                // Silently fail if we can't get the document text
            }
            return null;
        }

        private async void RefreshContextClick(object sender, RoutedEventArgs e)
        {
            await RefreshCodeContextAsync();
        }

        private async Task RefreshCodeContextAsync()
        {
            txtStatusBar.Text = "Refreshing code context...";
            _currentCodeContext = await GetActiveDocumentTextAsync() ?? string.Empty;
            txtCodeContext.Text = _currentCodeContext;
            txtStatusBar.Text = "Code context updated.";
        }

        private void ClearChatClick(object sender, RoutedEventArgs e)
        {
            _chatMessages.Clear();
            txtStatusBar.Text = "Chat cleared.";
        }

        private async void RefreshModelsClick(object sender, RoutedEventArgs e)
        {
            await RefreshModelsAsync();
        }

        private async Task RefreshModelsAsync()
        {
            txtStatusBar.Text = "Fetching models...";
            _availableModels = await _ollamaService.GetAvailableModelsAsync();
            comboModels.ItemsSource = _availableModels;
            if (_availableModels.Count > 0)
            {
                comboModels.SelectedIndex = 0;
                _ollamaService.SetModel(_availableModels[0]);
            }
            txtStatusBar.Text = _availableModels.Count > 0 ? "Models loaded." : "No models found.";
        }

        private void ComboModelsSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (comboModels.SelectedItem is string selectedModel)
            {
                _ollamaService.SetModel(selectedModel);
                txtStatusBar.Text = $"Model set to: {selectedModel}";
            }
        }
    }
}