using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using OllamaLocalHostIntergration.Services;
using OllamaLocalHostIntergration.Models;
using System.Threading.Tasks;
using EnvDTE;
using System.Collections.Generic;
using SelectionChangedEventArgs = System.Windows.Controls.SelectionChangedEventArgs;

namespace OllamaLocalHostIntergration
{
    public partial class MyToolWindowControl : UserControl
    {
        private readonly OllamaService _ollamaService;
        private readonly CodeEditorService _codeEditorService;
        private readonly ModeManager _modeManager;
        private ObservableCollection<ChatMessage> _chatMessages;
        private string _currentCodeContext = string.Empty;
        private List<string> _availableModels = new List<string>();

        public MyToolWindowControl()
        {
            InitializeComponent();
            _ollamaService = new OllamaService();
            _codeEditorService = new CodeEditorService();
            _modeManager = new ModeManager();
            _chatMessages = new ObservableCollection<ChatMessage>();
            chatMessagesPanel.ItemsSource = _chatMessages;

            // Set default server address
            txtServerAddress.Text = "http://localhost:11434";
            
            // Handle Enter key in the input box
            txtUserInput.KeyDown += TxtUserInputKeyDown;

            // Subscribe to mode changes
            _modeManager.OnModeChanged += OnModeChanged;

            // Load initial code context
            _ = RefreshCodeContextAsync();
            // Load models
            _ = RefreshModelsAsync();
        }

        private void OnModeChanged(InteractionMode mode)
        {
            if (mode == InteractionMode.Ask)
            {
                txtModeDescription.Text = "(Read-only Q&A)";
            }
            else
            {
                txtModeDescription.Text = "(Active code editing)";
            }
        }

        private void ModeChanged(object sender, RoutedEventArgs e)
        {
            if (radioAskMode.IsChecked == true)
            {
                _modeManager.SwitchToAskMode();
            }
            else if (radioAgentMode.IsChecked == true)
            {
                _modeManager.SwitchToAgentMode();
            }
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
                // Get system prompt based on current mode
                string systemPrompt = _modeManager.GetSystemPrompt();

                // Use the latest code context
                string codeContext = _currentCodeContext;

                // Show loading message
                _chatMessages.Add(new ChatMessage("Thinking...", false));
                txtStatusBar.Text = "Waiting for Ollama response...";
                
                // Get response from Ollama using the chat API with system prompt
                string response = await _ollamaService.GenerateChatResponseAsync(userMessage, systemPrompt, codeContext);
                
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
            return await _codeEditorService.GetActiveDocumentTextAsync();
        }

        private async void RefreshContextClick(object sender, RoutedEventArgs e)
        {
            await RefreshCodeContextAsync();
        }

        private async Task RefreshCodeContextAsync()
        {
            txtStatusBar.Text = "Refreshing code context...";
            
            // Get active document text
            _currentCodeContext = await GetActiveDocumentTextAsync() ?? string.Empty;
            
            // Also get selection if available
            var selectedText = await _codeEditorService.GetSelectedTextAsync();
            if (!string.IsNullOrEmpty(selectedText))
            {
                var language = await _codeEditorService.GetActiveDocumentLanguageAsync();
                _currentCodeContext = $"Selected {language} code:\n{selectedText}";
            }
            
            txtCodeContext.Text = _currentCodeContext;
            
            int contextLength = _currentCodeContext.Length;
            txtStatusBar.Text = $"Code context updated ({contextLength} characters).";
        }

        private void ClearChatClick(object sender, RoutedEventArgs e)
        {
            _chatMessages.Clear();
            _ollamaService.ClearConversationHistory();
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
            txtStatusBar.Text = _availableModels.Count > 0 ? "Models loaded." : "No models found. Make sure Ollama is running.";
        }

        private void ComboModels_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (comboModels.SelectedItem is string selectedModel)
            {
                _ollamaService.SetModel(selectedModel);
                txtStatusBar.Text = $"Model set to: {selectedModel}";
            }
        }
    }
}