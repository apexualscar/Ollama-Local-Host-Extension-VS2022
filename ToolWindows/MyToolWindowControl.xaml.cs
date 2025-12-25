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
        private bool _isInitializing = true;

        public MyToolWindowControl()
        {
            InitializeComponent();
            
            // Initialize services first
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

            // Initialize mode
            _modeManager.SwitchToAskMode();

            _isInitializing = false;

            // Load initial code context
            _ = RefreshCodeContextAsync();
            // Load models from Ollama server
            _ = RefreshModelsAsync();
        }

        private void OnModeChanged(InteractionMode mode)
        {
            // Mode description can be shown in status bar if needed
        }

        private void ComboMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing || _modeManager == null)
                return;

            if (comboMode.SelectedItem is ComboBoxItem selectedItem)
            {
                string mode = selectedItem.Tag?.ToString();
                if (mode == "Ask")
                {
                    _modeManager.SwitchToAskMode();
                    txtStatusBar.Text = "Mode: Ask";
                }
                else if (mode == "Agent")
                {
                    _modeManager.SwitchToAgentMode();
                    txtStatusBar.Text = "Mode: Agent";
                }
            }
        }

        private void SettingsClick(object sender, RoutedEventArgs e)
        {
            // Toggle settings panel visibility
            if (settingsPanel.Visibility == Visibility.Collapsed)
            {
                settingsPanel.Visibility = Visibility.Visible;
            }
            else
            {
                settingsPanel.Visibility = Visibility.Collapsed;
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
                txtStatusBar.Text = "Waiting for response...";
                
                // Get response from Ollama using the chat API with system prompt
                string response = await _ollamaService.GenerateChatResponseAsync(userMessage, systemPrompt, codeContext);
                
                // Remove loading message and add actual response
                _chatMessages.RemoveAt(_chatMessages.Count - 1);
                _chatMessages.Add(new ChatMessage(response, false));
                txtStatusBar.Text = "Ready";
            }
            catch (Exception ex)
            {
                // Remove loading message
                if (_chatMessages.Count > 0 && _chatMessages[_chatMessages.Count - 1].Content == "Thinking...")
                {
                    _chatMessages.RemoveAt(_chatMessages.Count - 1);
                }
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
            txtStatusBar.Text = contextLength > 0 ? $"Code context updated ({contextLength} characters)" : "No code context";
        }

        private void ClearChatClick(object sender, RoutedEventArgs e)
        {
            _chatMessages.Clear();
            _ollamaService.ClearConversationHistory();
            txtStatusBar.Text = "Chat cleared";
        }

        private async void RefreshModelsClick(object sender, RoutedEventArgs e)
        {
            await RefreshModelsAsync();
        }

        private async Task RefreshModelsAsync()
        {
            try
            {
                txtStatusBar.Text = "Fetching models from Ollama server...";
                
                // Get all available models from the Ollama server
                _availableModels = await _ollamaService.GetAvailableModelsAsync();
                
                comboModels.ItemsSource = _availableModels;
                
                if (_availableModels.Count > 0)
                {
                    comboModels.SelectedIndex = 0;
                    _ollamaService.SetModel(_availableModels[0]);
                    txtStatusBar.Text = $"Loaded {_availableModels.Count} model(s) from server";
                }
                else
                {
                    txtStatusBar.Text = "No models found. Make sure Ollama is running.";
                }
            }
            catch (Exception ex)
            {
                txtStatusBar.Text = $"Failed to fetch models: {ex.Message}";
                _availableModels.Clear();
            }
        }

        private void ComboModels_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing)
                return;

            if (comboModels.SelectedItem is string selectedModel)
            {
                _ollamaService.SetModel(selectedModel);
                txtStatusBar.Text = $"Using {selectedModel}";
            }
        }
    }
}