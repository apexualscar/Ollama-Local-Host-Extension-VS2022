using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using OllamaLocalHostIntergration.Services;
using OllamaLocalHostIntergration.Models;
using System.Threading.Tasks;
using EnvDTE;
using System.Collections.Generic;
using System.Linq;
using SelectionChangedEventArgs = System.Windows.Controls.SelectionChangedEventArgs;
using System;

namespace OllamaLocalHostIntergration
{
    public partial class MyToolWindowControl : UserControl
    {
        private readonly OllamaService _ollamaService;
        private readonly CodeEditorService _codeEditorService;
        private readonly ModeManager _modeManager;
        private readonly CodeModificationService _codeModService;
        private readonly MessageParserService _messageParser;
        private readonly PromptBuilder _promptBuilder;
        private readonly SettingsService _settingsService;
        private readonly ConversationHistoryService _conversationHistory;
        private readonly FileContextService _fileContextService;
        private ObservableCollection<ChatMessage> _chatMessages;
        private ObservableCollection<FileContextItem> _contextFileItems;
        private Conversation _currentConversation;
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
            _codeModService = new CodeModificationService(_codeEditorService);
            _messageParser = new MessageParserService();
            _promptBuilder = new PromptBuilder();
            _settingsService = new SettingsService();
            _conversationHistory = new ConversationHistoryService();
            _fileContextService = new FileContextService();
            _chatMessages = new ObservableCollection<ChatMessage>();
            _contextFileItems = new ObservableCollection<FileContextItem>();
            chatMessagesPanel.ItemsSource = _chatMessages;
            lstContextFiles.ItemsSource = _contextFileItems;

            // Initialize current conversation
            _currentConversation = new Conversation
            {
                Title = $"Conversation {DateTime.Now:yyyy-MM-dd HH:mm}",
                ModelUsed = "Not selected",
                Mode = InteractionMode.Ask
            };

            // Load saved server address or use default
            string savedServerAddress = _settingsService.GetServerAddress();
            txtServerAddress.Text = savedServerAddress;
            _ollamaService.UpdateServerAddress(savedServerAddress);
            
            // Handle Enter key in the input box
            txtUserInput.KeyDown += TxtUserInputKeyDown;

            // Subscribe to mode changes
            _modeManager.OnModeChanged += OnModeChanged;

            // Handle server address text changes to auto-save
            txtServerAddress.LostFocus += TxtServerAddress_LostFocus;
            txtServerAddress.KeyDown += TxtServerAddress_KeyDown;

            // Initialize mode
            _modeManager.SwitchToAskMode();

            _isInitializing = false;

            // Load initial code context
            _ = RefreshCodeContextAsync();
            // Load models from Ollama server
            _ = RefreshModelsAsync();
        }

        private void TxtServerAddress_LostFocus(object sender, RoutedEventArgs e)
        {
            // Auto-save when user leaves the text box
            SaveServerAddress();
        }

        private void TxtServerAddress_KeyDown(object sender, KeyEventArgs e)
        {
            // Auto-save when user presses Enter
            if (e.Key == Key.Enter)
            {
                SaveServerAddress();
                e.Handled = true;
            }
        }

        private void SaveServerAddress()
        {
            string serverAddress = txtServerAddress.Text;
            if (!string.IsNullOrWhiteSpace(serverAddress))
            {
                _settingsService.SaveServerAddress(serverAddress);
                _ollamaService.UpdateServerAddress(serverAddress);
                txtStatusBar.Text = $"Server address saved: {serverAddress}";
            }
        }

        private void OnModeChanged(InteractionMode mode)
        {
            // Update current conversation mode
            if (_currentConversation != null)
            {
                _currentConversation.Mode = mode;
            }
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

            // Add user message to chat (parse for consistency)
            var userChatMessage = _messageParser.ParseMessage(userMessage, true);
            _chatMessages.Add(userChatMessage);
            
            // Add to conversation history
            _currentConversation.Messages.Add(userChatMessage);
            
            // Set conversation title from first message if still default
            if (_currentConversation.Messages.Count == 1 && _currentConversation.Title.Contains("Conversation "))
            {
                _currentConversation.Title = GenerateTitleFromMessage(userMessage);
            }

            // Clear input
            txtUserInput.Clear();

            try
            {
                // Get system prompt based on current mode
                string systemPrompt = _modeManager.GetSystemPrompt();

                // Build combined context from active document + multi-file context
                string codeContext = _currentCodeContext;
                
                // Add multi-file context if files are selected
                if (_fileContextService.GetFileCount() > 0)
                {
                    string multiFileContext = await _fileContextService.BuildMultiFileContextAsync();
                    
                    if (!string.IsNullOrEmpty(multiFileContext))
                    {
                        // Combine current document context with multi-file context
                        if (!string.IsNullOrEmpty(codeContext))
                        {
                            codeContext = $"=== Active Document ===\n{codeContext}\n\n=== Additional Context Files ===\n{multiFileContext}";
                        }
                        else
                        {
                            codeContext = multiFileContext;
                        }
                    }
                }
                
                string language = await _codeEditorService.GetActiveDocumentLanguageAsync();

                // Create streaming message placeholder
                var streamingMessage = new ChatMessage("", false);
                _chatMessages.Add(streamingMessage);
                txtStatusBar.Text = "Receiving response...";
                
                // Stream the response with real-time updates
                string fullResponse = await _ollamaService.GenerateStreamingChatResponseAsync(
                    userMessage, 
                    token => 
                    {
                        // Update UI on each token received
                        Dispatcher.Invoke(() =>
                        {
                            streamingMessage.Content += token;
                            
                            // Auto-scroll to bottom
                            if (chatMessagesScroll != null)
                            {
                                chatMessagesScroll.ScrollToBottom();
                            }
                        });
                    },
                    systemPrompt, 
                    codeContext
                );
                
                // Remove streaming placeholder
                _chatMessages.Remove(streamingMessage);
                
                // Parse complete response for code blocks and create rich message
                var responseChatMessage = _messageParser.ParseMessage(fullResponse, false);
                
                // For Agent mode, check if we can create a CodeEdit
                if (_modeManager.IsAgentMode && responseChatMessage.HasCodeBlocks)
                {
                    try
                    {
                        var codeEdit = await _codeModService.CreateCodeEditFromResponseAsync(fullResponse, codeContext);
                        if (codeEdit != null)
                        {
                            responseChatMessage.AssociatedCodeEdit = codeEdit;
                            responseChatMessage.IsApplicable = true;
                            _modeManager.AddPendingEdit(codeEdit);
                        }
                    }
                    catch
                    {
                        // Failed to create CodeEdit, but still show the message
                    }
                }
                
                _chatMessages.Add(responseChatMessage);
                
                // Add to conversation history
                _currentConversation.Messages.Add(responseChatMessage);
                _currentConversation.ModelUsed = comboModels.SelectedItem as string ?? "Unknown";
                _currentConversation.Mode = _modeManager.CurrentMode;
                
                // Auto-save conversation
                await _conversationHistory.SaveConversationAsync(_currentConversation);
                
                txtStatusBar.Text = responseChatMessage.HasCodeBlocks 
                    ? $"Ready ({responseChatMessage.CodeBlocks.Count} code block(s))" 
                    : "Ready";
                
                // Update token count
                UpdateTokenCount();
            }
            catch (Exception ex)
            {
                // Remove streaming message if it exists
                for (int i = _chatMessages.Count - 1; i >= 0; i--)
                {
                    if (_chatMessages[i].Content == "" || _chatMessages[i].Content == "Thinking...")
                    {
                        _chatMessages.RemoveAt(i);
                        break;
                    }
                }
                
                var errorMessage = new ChatMessage($"Error: {ex.Message}", false);
                _chatMessages.Add(errorMessage);
                _currentConversation.Messages.Add(errorMessage);
                txtStatusBar.Text = $"Error: {ex.Message}";
            }

            // Scroll to bottom
            if (chatMessagesScroll != null)
            {
                chatMessagesScroll.ScrollToBottom();
            }
        }

        /// <summary>
        /// Generates a conversation title from the first message
        /// </summary>
        private string GenerateTitleFromMessage(string message)
        {
            // Take first 50 chars or until newline
            string title = message.Split('\n')[0];
            if (title.Length > 50)
            {
                title = title.Substring(0, 47) + "...";
            }
            return title;
        }

        /// <summary>
        /// Starts a new conversation
        /// </summary>
        private async void NewConversationClick(object sender, RoutedEventArgs e)
        {
            // Save current conversation if it has messages
            if (_currentConversation.Messages.Count > 0)
            {
                await _conversationHistory.SaveConversationAsync(_currentConversation);
            }
            
            // Create new conversation
            _currentConversation = new Conversation
            {
                Title = $"Conversation {DateTime.Now:yyyy-MM-dd HH:mm}",
                ModelUsed = comboModels.SelectedItem as string ?? "Not selected",
                Mode = _modeManager.CurrentMode
            };
            
            // Clear chat UI
            _chatMessages.Clear();
            _ollamaService.ClearConversationHistory();
            
            txtStatusBar.Text = "New conversation started";
        }
        
        private async void ClearChatClick(object sender, RoutedEventArgs e)
        {
            // Save current conversation before clearing if it has messages
            if (_currentConversation != null && _currentConversation.Messages.Count > 0)
            {
                await _conversationHistory.SaveConversationAsync(_currentConversation);
            }
            
            // Start fresh conversation
            _currentConversation = new Conversation
            {
                Title = $"Conversation {DateTime.Now:yyyy-MM-dd HH:mm}",
                ModelUsed = comboModels.SelectedItem as string ?? "Not selected",
                Mode = _modeManager.CurrentMode
            };
            
            _chatMessages.Clear();
            _ollamaService.ClearConversationHistory();
            txtStatusBar.Text = "Chat cleared - conversation saved";
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

            // Update token count
            UpdateTokenCount();

            int contextLength = _currentCodeContext.Length;
            txtStatusBar.Text = contextLength > 0 ? $"Code context updated ({contextLength} characters)" : "No code context";
        }

        private void UpdateTokenCount()
        {
            // Calculate token count for current context + multi-file context
            int currentContextTokens = _promptBuilder.EstimateTokenCount(_currentCodeContext);
            int multiFileTokens = _fileContextService.GetTotalTokenCount();
            int totalTokens = currentContextTokens + multiFileTokens;
            
            // Use theme-aware foreground color
            txtTokenCount.Foreground = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromArgb(
                    255,
                    ((System.Windows.Media.SolidColorBrush)FindResource(
                        Microsoft.VisualStudio.Shell.VsBrushes.ToolWindowTextKey)).Color.R,
                    ((System.Windows.Media.SolidColorBrush)FindResource(
                        Microsoft.VisualStudio.Shell.VsBrushes.ToolWindowTextKey)).Color.G,
                    ((System.Windows.Media.SolidColorBrush)FindResource(
                        Microsoft.VisualStudio.Shell.VsBrushes.ToolWindowTextKey)).Color.B
                )
            );
            
            // Show breakdown if multi-file context is present
            if (multiFileTokens > 0)
            {
                txtTokenCount.Text = $"Tokens: ~{totalTokens} (Active: ~{currentContextTokens}, Files: ~{multiFileTokens})";
            }
            else
            {
                txtTokenCount.Text = $"Tokens: ~{totalTokens}";
            }
        }

        private async Task RefreshModelsAsync()
        {
            try
            {
                txtStatusBar.Text = "Fetching models from Ollama server...";
                txtSelectedModel.Text = "Loading...";
                
                // Update server address from UI
                string serverAddress = txtServerAddress.Text;
                _ollamaService.UpdateServerAddress(serverAddress);
                
                // Get all available models from the Ollama server
                _availableModels = await _ollamaService.GetAvailableModelsAsync();
                
                comboModels.ItemsSource = _availableModels;
                
                if (_availableModels.Count > 0)
                {
                    // Try to restore previously selected model
                    string savedModel = _settingsService.GetSelectedModel();
                    int selectedIndex = 0;
                    
                    if (!string.IsNullOrEmpty(savedModel) && _availableModels.Contains(savedModel))
                    {
                        selectedIndex = _availableModels.IndexOf(savedModel);
                    }
                    
                    comboModels.SelectedIndex = selectedIndex;
                    _ollamaService.SetModel(_availableModels[selectedIndex]);
                    txtSelectedModel.Text = _availableModels[selectedIndex];
                    txtStatusBar.Text = $"Loaded {_availableModels.Count} model(s) from server";
                }
                else
                {
                    txtSelectedModel.Text = "No models";
                    txtStatusBar.Text = "No models found. Make sure Ollama is running and has models installed (run: ollama pull codellama)";
                }
            }
            catch (Exception ex)
            {
                txtSelectedModel.Text = "Error";
                txtStatusBar.Text = $"Failed to fetch models: {ex.Message}";
                _availableModels.Clear();
                
                // Show detailed error message to user
                System.Windows.MessageBox.Show(
                    $"Could not connect to Ollama server.\n\n" +
                    $"Server: {txtServerAddress.Text}\n\n" +
                    $"Error: {ex.Message}\n\n" +
                    $"Troubleshooting:\n" +
                    $"1. Make sure Ollama is running (ollama serve)\n" +
                    $"2. Check that models are installed (ollama list)\n" +
                    $"3. Verify server address is correct\n" +
                    $"4. Test with: curl {txtServerAddress.Text}/api/tags",
                    "Ollama Connection Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error
                );
            }
        }

        private void ComboModels_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing)
                return;

            if (comboModels.SelectedItem is string selectedModel)
            {
                _ollamaService.SetModel(selectedModel);
                txtSelectedModel.Text = selectedModel;
                txtStatusBar.Text = $"Using {selectedModel}";
                
                // Save the selected model for next session
                _settingsService.SaveSelectedModel(selectedModel);
                
                // Update current conversation model
                if (_currentConversation != null)
                {
                    _currentConversation.ModelUsed = selectedModel;
                }
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

        private async void RefreshModelsClick(object sender, RoutedEventArgs e)
        {
            await RefreshModelsAsync();
        }

        /// <summary>
        /// Explains selected code (called from context menu command)
        /// </summary>
        public async Task ExplainCodeAsync(string code, string language)
        {
            try
            {
                // Switch to Ask mode
                _modeManager.SwitchToAskMode();
                comboMode.SelectedIndex = 0;

                // Add user message
                var userMessage = $"Please explain this {language} code";
                var userChatMessage = _messageParser.ParseMessage(userMessage, true);
                _chatMessages.Add(userChatMessage);
                _currentConversation.Messages.Add(userChatMessage);

                // Show loading
                var loadingMessage = new ChatMessage("Analyzing code...", false);
                _chatMessages.Add(loadingMessage);
                txtStatusBar.Text = "Analyzing code...";

                // Get explanation
                string response = await _ollamaService.ExplainCodeAsync(code, language);

                // Remove loading and add response
                _chatMessages.Remove(loadingMessage);
                var responseChatMessage = _messageParser.ParseMessage(response, false);
                _chatMessages.Add(responseChatMessage);
                _currentConversation.Messages.Add(responseChatMessage);
                
                // Save conversation
                await _conversationHistory.SaveConversationAsync(_currentConversation);
                
                txtStatusBar.Text = "Explanation complete";
                chatMessagesScroll.ScrollToBottom();
            }
            catch (Exception ex)
            {
                txtStatusBar.Text = $"Error: {ex.Message}";
            }
        }

        /// <summary>
        /// Refactors selected code (called from context menu command)
        /// </summary>
        public async Task RefactorCodeAsync(string code, string language)
        {
            try
            {
                // Switch to Agent mode
                _modeManager.SwitchToAgentMode();
                comboMode.SelectedIndex = 1;

                // Add user message
                var userMessage = $"Please refactor this {language} code to improve readability and performance";
                var userChatMessage = _messageParser.ParseMessage(userMessage, true);
                _chatMessages.Add(userChatMessage);
                _currentConversation.Messages.Add(userChatMessage);

                // Show loading
                var loadingMessage = new ChatMessage("Refactoring code...", false);
                _chatMessages.Add(loadingMessage);
                txtStatusBar.Text = "Refactoring code...";

                // Get refactoring suggestions
                string response = await _ollamaService.SuggestRefactoringAsync(code, language);

                // Remove loading and add response
                _chatMessages.Remove(loadingMessage);
                var responseChatMessage = _messageParser.ParseMessage(response, false);
                
                // Check if we can create a CodeEdit
                if (_modeManager.IsAgentMode && responseChatMessage.HasCodeBlocks)
                {
                    try
                    {
                        var codeEdit = await _codeModService.CreateCodeEditFromResponseAsync(response, code);
                        if (codeEdit != null)
                        {
                            responseChatMessage.AssociatedCodeEdit = codeEdit;
                            responseChatMessage.IsApplicable = true;
                            _modeManager.AddPendingEdit(codeEdit);
                        }
                    }
                    catch { }
                }
                
                _chatMessages.Add(responseChatMessage);
                _currentConversation.Messages.Add(responseChatMessage);
                
                // Save conversation
                await _conversationHistory.SaveConversationAsync(_currentConversation);
                
                txtStatusBar.Text = "Refactoring complete";
                chatMessagesScroll.ScrollToBottom();
            }
            catch (Exception ex)
            {
                txtStatusBar.Text = $"Error: {ex.Message}";
            }
        }

        /// <summary>
        /// Finds issues in selected code (called from context menu command)
        /// </summary>
        public async Task FindIssuesAsync(string code, string language)
        {
            try
            {
                // Switch to Ask mode
                _modeManager.SwitchToAskMode();
                comboMode.SelectedIndex = 0;

                // Add user message
                var userMessage = $"Please find potential issues, bugs, or improvements in this {language} code";
                var userChatMessage = _messageParser.ParseMessage(userMessage, true);
                _chatMessages.Add(userChatMessage);
                _currentConversation.Messages.Add(userChatMessage);

                // Show loading
                var loadingMessage = new ChatMessage("Analyzing for issues...", false);
                _chatMessages.Add(loadingMessage);
                txtStatusBar.Text = "Analyzing for issues...";

                // Find issues
                string response = await _ollamaService.FindCodeIssuesAsync(code, language);

                // Remove loading and add response
                _chatMessages.Remove(loadingMessage);
                var responseChatMessage = _messageParser.ParseMessage(response, false);
                _chatMessages.Add(responseChatMessage);
                _currentConversation.Messages.Add(responseChatMessage);
                
                // Save conversation
                await _conversationHistory.SaveConversationAsync(_currentConversation);
                
                txtStatusBar.Text = "Analysis complete";
                chatMessagesScroll.ScrollToBottom();
            }
            catch (Exception ex)
            {
                txtStatusBar.Text = $"Error: {ex.Message}";
            }
        }
        
        /// <summary>
        /// Adds a file to the multi-file context
        /// </summary>
        private async void AddContextFileClick(object sender, RoutedEventArgs e)
        {
            try
            {
                // Use OpenFileDialog
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "Select File to Add to Context",
                    Filter = "Code Files|*.cs;*.vb;*.fs;*.cpp;*.h;*.hpp;*.c;*.java;*.py;*.js;*.ts;*.xml;*.xaml;*.json|All Files|*.*",
                    Multiselect = true
                };

                if (dialog.ShowDialog() == true)
                {
                    foreach (var filePath in dialog.FileNames)
                    {
                        await _fileContextService.AddFileAsync(filePath);
                        
                        var fileName = System.IO.Path.GetFileName(filePath);
                        var tokenCount = _fileContextService.GetFileTokenCount(filePath);
                        
                        _contextFileItems.Add(new FileContextItem
                        {
                            FilePath = filePath,
                            FileName = fileName,
                            TokenCount = tokenCount
                        });
                    }
                    
                    UpdateFileContextSummary();
                    UpdateTokenCount();
                    txtStatusBar.Text = $"Added {dialog.FileNames.Length} file(s) to context";
                }
            }
            catch (Exception ex)
            {
                txtStatusBar.Text = $"Error adding file: {ex.Message}";
            }
        }

        /// <summary>
        /// Removes a file from the context
        /// </summary>
        private void RemoveContextFileClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is System.Windows.Controls.Button button && button.Tag is string filePath)
                {
                    _fileContextService.RemoveFile(filePath);
                    
                    var itemToRemove = _contextFileItems.FirstOrDefault(i => i.FilePath == filePath);
                    if (itemToRemove != null)
                    {
                        _contextFileItems.Remove(itemToRemove);
                    }
                    
                    UpdateFileContextSummary();
                    UpdateTokenCount();
                    txtStatusBar.Text = "File removed from context";
                }
            }
            catch (Exception ex)
            {
                txtStatusBar.Text = $"Error removing file: {ex.Message}";
            }
        }

        /// <summary>
        /// Clears all context files
        /// </summary>
        private void ClearContextFilesClick(object sender, RoutedEventArgs e)
        {
            _fileContextService.ClearFiles();
            _contextFileItems.Clear();
            UpdateFileContextSummary();
            UpdateTokenCount();
            txtStatusBar.Text = "Context files cleared";
        }

        /// <summary>
        /// Updates the file context summary display
        /// </summary>
        private void UpdateFileContextSummary()
        {
            int fileCount = _fileContextService.GetFileCount();
            int totalTokens = _fileContextService.GetTotalTokenCount();
            
            if (fileCount == 0)
            {
                txtContextFilesSummary.Text = "No files in context";
            }
            else
            {
                txtContextFilesSummary.Text = $"{fileCount} file(s), ~{totalTokens} total tokens";
            }
        }
    }
}