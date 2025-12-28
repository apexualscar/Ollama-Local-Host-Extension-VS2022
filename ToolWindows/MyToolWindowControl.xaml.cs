using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation; // Phase 6.1+: For Storyboard animation
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
        private readonly TemplateService _templateService;
        private readonly VSDiffService _vsDiffService; // Phase 5.7
        private readonly ChangePersistenceService _changePersistence; // Phase 5.7
        private ObservableCollection<ChatMessage> _chatMessages;
        private ObservableCollection<FileContextItem> _contextFileItems;
        private ObservableCollection<Conversation> _savedConversations;
        private ObservableCollection<ContextReference> _contextReferences; // NEW
        private Conversation _currentConversation;
        private string _currentCodeContext = string.Empty;
        private List<string> _availableModels = new List<string>();
        private bool _isInitializing = true;

        public MyToolWindowControl()
        {
            
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
            _templateService = new TemplateService();
            _vsDiffService = new VSDiffService(); // Phase 5.7
            _changePersistence = new ChangePersistenceService(); // Phase 5.7
            _chatMessages = new ObservableCollection<ChatMessage>();
            _contextFileItems = new ObservableCollection<FileContextItem>();
            _savedConversations = new ObservableCollection<Conversation>();
            _contextReferences = new ObservableCollection<ContextReference>(); // NEW
            
            // Initialize component FIRST to create UI elements
            InitializeComponent();
            
            // Then bind collections to UI elements
            chatMessagesPanel.ItemsSource = _chatMessages;
            // lstContextFiles.ItemsSource = _contextFileItems;  // REMOVED - old system replaced by context references
            comboConversations.ItemsSource = _savedConversations;
            contextChipsPanel.ItemsSource = _contextReferences; // Phase 5.5.2 Context References

            // Wire up context chip removal via routed event
            contextChipsPanel.AddHandler(Controls.ContextChipControl.RemoveContextEvent, new RoutedEventHandler(ContextChip_RemoveContext));
            
            // Update summary whenever collection changes
            _contextReferences.CollectionChanged += (s, e) => UpdateContextSummary();

            // NEW: Phase 5.5.3 - Wire up pending changes
            _modeManager.OnPendingEditsChanged += UpdatePendingChangesDisplay;
            pendingChangesItemsControl.AddHandler(Controls.PendingChangeControl.ViewDiffEvent, new RoutedEventHandler(PendingChange_ViewDiff));
            pendingChangesItemsControl.AddHandler(Controls.PendingChangeControl.KeepChangeEvent, new RoutedEventHandler(PendingChange_Keep));
            pendingChangesItemsControl.AddHandler(Controls.PendingChangeControl.UndoChangeEvent, new RoutedEventHandler(PendingChange_Undo));

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
            // Load saved conversations
            _ = LoadSavedConversationsAsync();
            // Phase 5.7: Load saved pending changes
            _ = LoadSavedPendingChangesAsync();
        }
        
        /// <summary>
        /// Phase 6.1+: Show/hide spinning loading animation
        /// </summary>
        private void ShowLoadingSpinner(bool show, string message = "AI is thinking...")
        {
            Dispatcher.Invoke(() =>
            {
                loadingSpinnerPanel.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
                txtLoadingMessage.Text = message;
                
                if (show)
                {
                    // Start the spinning animation
                    var storyboard = (Storyboard)FindResource("SpinAnimation");
                    storyboard.Begin();
                }
                else
                {
                    // Stop the spinning animation
                    var storyboard = (Storyboard)FindResource("SpinAnimation");
                    storyboard.Stop();
                }
            });
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
            // Phase 5.5.4: Standard chat behavior
            // ENTER = Send message
            // SHIFT+ENTER = New line (default TextBox behavior)
            if (e.Key == Key.Enter && !Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                e.Handled = true;
                await SendUserMessage();
            }
            // If SHIFT+ENTER, let default behavior add new line (do nothing)
        }

        /// <summary>
        /// Handles text changes to show/hide placeholder (Phase 5.5.4)
        /// </summary>
        private void TxtUserInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Show placeholder only when textbox is empty
            if (txtPlaceholder != null)
            {
                txtPlaceholder.Visibility = string.IsNullOrEmpty(txtUserInput.Text) 
                    ? Visibility.Visible 
                    : Visibility.Collapsed;
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

                // Build context from new context references system
                string codeContext = BuildContextFromReferences();
                
                // If no context references, fall back to old system for backward compatibility
                if (string.IsNullOrEmpty(codeContext))
                {
                    codeContext = _currentCodeContext;
                    
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
                }
                
                string language = await _codeEditorService.GetActiveDocumentLanguageAsync();

                // Phase 6.1+: Show spinning loading animation ONLY (no chat messages)
                ShowLoadingSpinner(true, "💭 AI is thinking...");
                
                // Update status bar with progressive steps
                txtStatusBar.Text = "🔄 Preparing request...";
                await Task.Delay(100);
                
                if (!string.IsNullOrEmpty(codeContext))
                {
                    txtStatusBar.Text = "📝 Analyzing code context...";
                    await Task.Delay(100);
                }
                
                if (_modeManager.IsAgentMode)
                {
                    txtStatusBar.Text = "🤖 Agent mode: Planning code modifications...";
                    ShowLoadingSpinner(true, "🤖 Planning modifications...");
                }
                else
                {
                    txtStatusBar.Text = "💬 Ask mode: Preparing explanation...";
                    ShowLoadingSpinner(true, "💬 Preparing explanation...");
                }
                await Task.Delay(100);
                
                txtStatusBar.Text = "🔄 Sending to AI model...";
                await Task.Delay(100);
                
                ShowLoadingSpinner(true, "💭 AI is thinking...");
                txtStatusBar.Text = "💭 Receiving response...";
                
                // Create streaming message that will appear word-by-word
                var streamingMessage = new ChatMessage("", false);
                _chatMessages.Add(streamingMessage);
                
                int tokenCount = 0;
                string fullResponse = "";
                
                // Stream the response with real-time updates (word-by-word)
                fullResponse = await _ollamaService.GenerateStreamingChatResponseAsync(
                    userMessage, 
                    token => 
                    {
                        tokenCount++;
                        // Update UI on each token received
                        Dispatcher.Invoke(() =>
                        {
                            // Phase 6.1+: Hide spinner on first token
                            if (tokenCount == 1)
                            {
                                ShowLoadingSpinner(false);
                            }
                            
                            streamingMessage.Content += token;
                            
                            // Phase 6.1: Show token count in status bar
                            if (tokenCount % 10 == 0) // Update every 10 tokens
                            {
                                txtStatusBar.Text = $"Streaming... ({tokenCount} tokens)";
                            }
                            
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
                
                // Phase 6.1+: Hide spinning loader (in case no tokens received)
                ShowLoadingSpinner(false);
                
                // Remove the streaming placeholder and replace with parsed message
                _chatMessages.Remove(streamingMessage);
                
                // Phase 6.1: Get current model name for display
                string currentModel = comboModels.SelectedItem as string ?? "Ollama";
                
                // Parse complete response for code blocks and create rich message
                var responseChatMessage = _messageParser.ParseMessage(fullResponse, false);
                responseChatMessage.ModelName = currentModel;
                
                // Phase 6.1: Show processing status for agent mode
                bool processingCodeEdit = false;
                
                // For Agent mode, check if we can create a CodeEdit
                if (_modeManager.IsAgentMode && responseChatMessage.HasCodeBlocks)
                {
                    try
                    {
                        // Phase 6.1: Show that we're processing the code
                        processingCodeEdit = true;
                        txtStatusBar.Text = "🔍 Analyzing code changes...";
                        
                        var codeEdit = await _codeModService.CreateCodeEditFromResponseAsync(fullResponse, codeContext);
                        if (codeEdit != null)
                        {
                            responseChatMessage.AssociatedCodeEdit = codeEdit;
                            responseChatMessage.IsApplicable = true;
                            _modeManager.AddPendingEdit(codeEdit);
                            
                            // Phase 6.1: Success feedback
                            txtStatusBar.Text = "✅ Code changes ready to apply";
                        }
                    }
                    catch
                    {
                        // Failed to create CodeEdit, but still show the message
                        processingCodeEdit = false;
                    }
                }
                
                _chatMessages.Add(responseChatMessage);
                
                // Add to conversation history
                _currentConversation.Messages.Add(responseChatMessage);
                _currentConversation.ModelUsed = comboModels.SelectedItem as string ?? "Unknown";
                _currentConversation.Mode = _modeManager.CurrentMode;
                
                // Auto-save conversation
                await _conversationHistory.SaveConversationAsync(_currentConversation);
                
                // Refresh dropdown if this was the first message
                if (_currentConversation.Messages.Count == 2) // User message + AI response
                {
                    await LoadSavedConversationsAsync();
                }
                
                // Phase 6.1: Enhanced status message with more information
                if (!processingCodeEdit)
                {
                    txtStatusBar.Text = responseChatMessage.HasCodeBlocks 
                        ? $"✅ Done! ({responseChatMessage.CodeBlocks.Count} code block(s), {tokenCount} tokens)" 
                        : $"✅ Done! ({tokenCount} tokens)";
                }
                
                // Update token count
                UpdateTokenCount();
            }
            catch (Exception ex)
            {
                // Phase 6.1+: Hide spinner on error
                ShowLoadingSpinner(false);
                
                // Phase 6.1+: Remove any empty streaming messages on error
                for (int i = _chatMessages.Count - 1; i >= 0; i--)
                {
                    if (string.IsNullOrEmpty(_chatMessages[i].Content))
                    {
                        _chatMessages.RemoveAt(i);
                    }
                }
                
                // Phase 6.1: Better error message with emoji
                var errorMessage = new ChatMessage($"❌ Error: {ex.Message}", false);
                _chatMessages.Add(errorMessage);
                _currentConversation.Messages.Add(errorMessage);
                txtStatusBar.Text = $"❌ Error: {ex.Message}";
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
            
            // Reload conversations dropdown
            await LoadSavedConversationsAsync();
            
            txtStatusBar.Text = "New conversation started";
        }
        
        private async void ClearChatClick(object sender, RoutedEventArgs e)
        {
            // This method is now deprecated - use DeleteConversationClick instead
            // But keep for backward compatibility with old button references
            DeleteConversationClick(sender, e);
        }

        private async void DeleteConversationClick(object sender, RoutedEventArgs e)
        {
            if (_currentConversation == null || _currentConversation.Messages.Count == 0)
            {
                txtStatusBar.Text = "No conversation to delete";
                return;
            }

            // Confirm deletion
            var result = System.Windows.MessageBox.Show(
                $"Delete conversation \"{_currentConversation.Title}\"?\n\nThis action cannot be undone.",
                "Delete Conversation",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning
            );

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                try
                {
                    // Delete from disk
                    await _conversationHistory.DeleteConversationAsync(_currentConversation.Id);
                    
                    // Remove from dropdown
                    _savedConversations.Remove(_currentConversation);
                    
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
                    
                    // Reload conversations
                    await LoadSavedConversationsAsync();
                    
                    txtStatusBar.Text = "Conversation deleted";
                }
                catch (Exception ex)
                {
                    txtStatusBar.Text = $"Failed to delete conversation: {ex.Message}";
                }
            }
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

            // txtCodeContext.Text = _currentCodeContext;  // TEMPORARILY COMMENTED - XAML removed during phase 5.5.2

            // Update token count
            UpdateTokenCount();

            int contextLength = _currentCodeContext.Length;
            txtStatusBar.Text = contextLength > 0 ? $"Code context updated ({contextLength} characters)" : "No code context";
        }

        private void UpdateTokenCount()
        {
            // Calculate token count for current context + multi-file context
            int currentContextTokens = _promptBuilder.EstimateTokenCount(_currentCodeContext);
            int multiFileTokens = _fileContextService.GetFileCount() > 0 ? _fileContextService.GetTotalTokenCount() : 0;
            int totalTokens = currentContextTokens + multiFileTokens;
            
            // TEMPORARILY COMMENTED - txtTokenCount removed in phase 5.5.2
            /*
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
            */
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

                // Phase 6.1+: Show ONLY spinner (no chat messages)
                ShowLoadingSpinner(true, "🔄 Analyzing code structure...");
                txtStatusBar.Text = "Analyzing code structure...";
                await Task.Delay(150);
                
                ShowLoadingSpinner(true, "📝 Understanding code logic...");
                txtStatusBar.Text = "Understanding code logic...";
                await Task.Delay(150);
                
                ShowLoadingSpinner(true, "💭 Preparing explanation...");
                txtStatusBar.Text = "Preparing explanation...";
                chatMessagesScroll?.ScrollToBottom();

                // Get explanation
                string response = await _ollamaService.ExplainCodeAsync(code, language);

                // Phase 6.1+: Hide spinner
                ShowLoadingSpinner(false);
                
                // Add response directly (no intermediate messages)
                var responseChatMessage = _messageParser.ParseMessage(response, false);
                responseChatMessage.ModelName = comboModels.SelectedItem as string ?? "Ollama";
                _chatMessages.Add(responseChatMessage);
                _currentConversation.Messages.Add(responseChatMessage);
                
                // Save conversation
                await _conversationHistory.SaveConversationAsync(_currentConversation);
                
                txtStatusBar.Text = "✅ Explanation complete";
                chatMessagesScroll.ScrollToBottom();
            }
            catch (Exception ex)
            {
                // Phase 6.1+: Hide spinner on error
                ShowLoadingSpinner(false);
                
                txtStatusBar.Text = $"❌ Error: {ex.Message}";
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

                // Phase 6.1+: Show ONLY spinner (no chat messages)
                ShowLoadingSpinner(true, "🔄 Analyzing code patterns...");
                txtStatusBar.Text = "Analyzing code patterns...";
                await Task.Delay(150);
                
                ShowLoadingSpinner(true, "🤖 Planning improvements...");
                txtStatusBar.Text = "Planning improvements...";
                await Task.Delay(150);
                
                ShowLoadingSpinner(true, "⚡ Generating refactored code...");
                txtStatusBar.Text = "Generating refactored code...";
                chatMessagesScroll?.ScrollToBottom();

                // Get refactoring suggestions
                string response = await _ollamaService.SuggestRefactoringAsync(code, language);

                // Phase 6.1+: Hide spinner
                ShowLoadingSpinner(false);
                
                // Add response directly (no intermediate messages)
                var responseChatMessage = _messageParser.ParseMessage(response, false);
                responseChatMessage.ModelName = comboModels.SelectedItem as string ?? "Ollama";
                
                // Check if we can create a CodeEdit
                if (_modeManager.IsAgentMode && responseChatMessage.HasCodeBlocks)
                {
                    try
                    {
                        // Show processing status
                        txtStatusBar.Text = "🔍 Analyzing code changes...";
                        
                        var codeEdit = await _codeModService.CreateCodeEditFromResponseAsync(response, code);
                        if (codeEdit != null)
                        {
                            responseChatMessage.AssociatedCodeEdit = codeEdit;
                            responseChatMessage.IsApplicable = true;
                            _modeManager.AddPendingEdit(codeEdit);
                            txtStatusBar.Text = "✅ Refactoring ready to apply";
                        }
                    }
                    catch { }
                }
                
                _chatMessages.Add(responseChatMessage);
                _currentConversation.Messages.Add(responseChatMessage);
                
                // Save conversation
                await _conversationHistory.SaveConversationAsync(_currentConversation);
                
                if (!responseChatMessage.IsApplicable)
                {
                    txtStatusBar.Text = "✅ Refactoring suggestions ready";
                }
                chatMessagesScroll.ScrollToBottom();
            }
            catch (Exception ex)
            {
                // Phase 6.1+: Hide spinner on error
                ShowLoadingSpinner(false);
                
                txtStatusBar.Text = $"❌ Error: {ex.Message}";
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

                // Phase 6.1+: Show ONLY spinner (no chat messages)
                ShowLoadingSpinner(true, "🔄 Scanning code for issues...");
                txtStatusBar.Text = "Scanning code for issues...";
                await Task.Delay(150);
                
                ShowLoadingSpinner(true, "🐛 Checking for bugs...");
                txtStatusBar.Text = "Checking for bugs and vulnerabilities...";
                await Task.Delay(150);
                
                ShowLoadingSpinner(true, "💡 Analyzing best practices...");
                txtStatusBar.Text = "Analyzing best practices...";
                chatMessagesScroll?.ScrollToBottom();

                // Find issues
                string response = await _ollamaService.FindCodeIssuesAsync(code, language);

                // Phase 6.1+: Hide spinner
                ShowLoadingSpinner(false);
                
                // Add response directly (no intermediate messages)
                var responseChatMessage = _messageParser.ParseMessage(response, false);
                responseChatMessage.ModelName = comboModels.SelectedItem as string ?? "Ollama";
                _chatMessages.Add(responseChatMessage);
                _currentConversation.Messages.Add(responseChatMessage);
                
                // Save conversation
                await _conversationHistory.SaveConversationAsync(_currentConversation);
                
                txtStatusBar.Text = "✅ Analysis complete";
                chatMessagesScroll.ScrollToBottom();
            }
            catch (Exception ex)
            {
                // Phase 6.1+: Hide spinner on error
                ShowLoadingSpinner(false);
                
                txtStatusBar.Text = $"❌ Error: {ex.Message}";
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
            
            // TEMPORARILY COMMENTED - txtContextFilesSummary removed in phase 5.5.2
            /*
            if (fileCount == 0)
            {
                txtContextFilesSummary.Text = "No files in context";
            }
            else
            {
                txtContextFilesSummary.Text = $"{fileCount} file(s), ~{totalTokens} total tokens";
            }
            */
        }

        /// <summary>
        /// Loads all saved conversations into the dropdown
        /// </summary>
        private async Task LoadSavedConversationsAsync()
        {
            try
            {
                var conversations = await _conversationHistory.LoadAllConversationsAsync();
                
                _savedConversations.Clear();
                
                // Add "Current" conversation at top if it has messages
                if (_currentConversation != null && _currentConversation.Messages.Count > 0)
                {
                    _savedConversations.Add(_currentConversation);
                }
                
                // Add saved conversations
                foreach (var conversation in conversations)
                {
                    if (conversation.Id != _currentConversation?.Id)
                    {
                        _savedConversations.Add(conversation);
                    }
                }
                
                // Select current conversation
                if (_currentConversation != null && _savedConversations.Contains(_currentConversation))
                {
                    comboConversations.SelectedItem = _currentConversation;
                }
                else if (_savedConversations.Count > 0)
                {
                    comboConversations.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load conversations: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles conversation selection from dropdown
        /// </summary>
        private async void ComboConversations_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing || comboConversations.SelectedItem == null)
                return;

            if (comboConversations.SelectedItem is Conversation selectedConversation)
            {
                // Save current conversation if it has messages
                if (_currentConversation != null && _currentConversation.Messages.Count > 0)
                {
                    await _conversationHistory.SaveConversationAsync(_currentConversation);
                }
                
                // Load selected conversation
                _currentConversation = selectedConversation;
                
                // Clear and load messages
                _chatMessages.Clear();
                foreach (var message in _currentConversation.Messages)
                {
                    _chatMessages.Add(message);
                }
                
                // Update UI
                txtStatusBar.Text = $"Loaded conversation: {_currentConversation.Title}";
                
                // Scroll to bottom
                if (chatMessagesScroll != null)
                {
                    chatMessagesScroll.ScrollToBottom();
                }
            }
        }

        #region Context References (Phase 5.5.2)

        /// <summary>
        /// Shows context search dialog (Phase 5.6 - Copilot-style)
        /// </summary>
        private async void AddContextClick(object sender, RoutedEventArgs e)
        {
            try
            {
                // Create and show search dialog
                var dialog = new Dialogs.ContextSearchDialog();
                
                // Subscribe to context selected event
                dialog.ContextSelected += async (s, contextRef) =>
                {
                    // Handle special cases (active document, selection)
                    if (contextRef.DisplayText == "Active Document")
                    {
                        await AddActiveDocumentContextAsync();
                    }
                    else if (contextRef.DisplayText == "Selection")
                    {
                        await AddSelectionContextAsync();
                    }
                    else
                    {
                        // Add the selected context reference
                        _contextReferences.Add(contextRef);
                        UpdateContextSummary();
                        txtStatusBar.Text = $"Added {contextRef.DisplayText} to context";
                    }
                    
                    // Close the dialog window
                    var window = System.Windows.Window.GetWindow(dialog);
                    window?.Close();
                };

                // Show dialog in a window
                var window = new System.Windows.Window
                {
                    Content = dialog,
                    Title = "Add Context",
                    Width = 600,
                    Height = 500,
                    WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner,
                    Owner = System.Windows.Window.GetWindow(this),
                    ResizeMode = System.Windows.ResizeMode.CanResize,
                    Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Transparent),
                    WindowStyle = System.Windows.WindowStyle.ToolWindow
                };
                
                window.ShowDialog();
            }
            catch (Exception ex)
            {
                txtStatusBar.Text = $"Error opening context search: {ex.Message}";
            }
        }

        /// <summary>
        /// Adds file(s) to context - DEPRECATED, replaced by unified search dialog
        /// </summary>
        private async Task AddFileContextAsync()
        {
            // This method is now handled by the unified search dialog
            // Keeping for backward compatibility but not used
        }

        /// <summary>
        /// Adds a specific file to context references
        /// </summary>
        private async Task AddFileToContextAsync(string filePath)
        {
            try
            {
                var content = System.IO.File.ReadAllText(filePath);
                var fileName = System.IO.Path.GetFileName(filePath);
                var tokenCount = _promptBuilder.EstimateTokenCount(content);

                var contextRef = new ContextReference
                {
                    Type = ContextReferenceType.File,
                    DisplayText = fileName,
                    FilePath = filePath,
                    Content = content,
                    TokenCount = tokenCount
                };

                _contextReferences.Add(contextRef);
                UpdateContextSummary();
                txtStatusBar.Text = $"Added {fileName} to context";
            }
            catch (Exception ex)
            {
                txtStatusBar.Text = $"Error reading file: {ex.Message}";
            }
        }

        /// <summary>
        /// Adds current editor selection to context
        /// </summary>
        private async Task AddSelectionContextAsync()
        {
            try
            {
                var selectedText = await _codeEditorService.GetSelectedTextAsync();
                if (string.IsNullOrWhiteSpace(selectedText))
                {
                    txtStatusBar.Text = "No text selected";
                    return;
                }

                var language = await _codeEditorService.GetActiveDocumentLanguageAsync();
                var tokenCount = _promptBuilder.EstimateTokenCount(selectedText);

                var contextRef = new ContextReference
                {
                    Type = ContextReferenceType.Selection,
                    DisplayText = $"Selection ({selectedText.Length} chars)",
                    Content = selectedText,
                    TokenCount = tokenCount
                };

                _contextReferences.Add(contextRef);
                UpdateContextSummary();
                txtStatusBar.Text = "Added selection to context";
            }
            catch (Exception ex)
            {
                txtStatusBar.Text = $"Error adding selection: {ex.Message}";
            }
        }

        /// <summary>
        /// Adds active document to context
        /// </summary>
        private async Task AddActiveDocumentContextAsync()
        {
            try
            {
                var documentText = await _codeEditorService.GetActiveDocumentTextAsync();
                if (string.IsNullOrWhiteSpace(documentText))
                {
                    txtStatusBar.Text = "No active document";
                    return;
                }

                var documentPath = await _codeEditorService.GetActiveDocumentPathAsync();
                var documentName = System.IO.Path.GetFileName(documentPath) ?? "Active Document";
                var tokenCount = _promptBuilder.EstimateTokenCount(documentText);

                var contextRef = new ContextReference
                {
                    Type = ContextReferenceType.File,
                    DisplayText = documentName,
                    FilePath = documentPath,
                    Content = documentText,
                    TokenCount = tokenCount
                };

                _contextReferences.Add(contextRef);
                UpdateContextSummary();
                txtStatusBar.Text = $"Added {contextRef.DisplayText} to context";
            }
            catch (Exception ex)
            {
                txtStatusBar.Text = $"Error adding document: {ex.Message}";
            }
        }

        /// <summary>
        /// Updates context summary text
        /// </summary>
        private void UpdateContextSummary()
        {
            if (_contextReferences.Count == 0)
            {
                txtContextSummary.Text = "No context added";
            }
            else
            {
                int totalTokens = _contextReferences.Sum(c => c.TokenCount);
                txtContextSummary.Text = $"{_contextReferences.Count} item(s), ~{totalTokens} tokens";
            }
        }

        /// <summary>
        /// Builds context prompt from all references
        /// </summary>
        private string BuildContextFromReferences()
        {
            if (_contextReferences.Count == 0)
                return string.Empty;

            var context = new System.Text.StringBuilder();
            context.AppendLine("=== Context References ===");
            context.AppendLine();

            foreach (var reference in _contextReferences)
            {
                context.AppendLine($"### {reference.Type}: {reference.DisplayText}");
                context.AppendLine("```");
                context.AppendLine(reference.Content);
                context.AppendLine("```");
                context.AppendLine();
            }

            return context.ToString();
        }

        /// <summary>
        /// Handles removal of a context chip (routed event)
        /// </summary>
        private void ContextChip_RemoveContext(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is ContextReference contextRef)
            {
                _contextReferences.Remove(contextRef);
                txtStatusBar.Text = $"Removed {contextRef.DisplayText} from context";
            }
        }

        #endregion

        #region Pending Changes (Phase 5.5.3 + 5.7)

        /// <summary>
        /// Load saved pending changes on startup (Phase 5.7)
        /// </summary>
        private async Task LoadSavedPendingChangesAsync()
        {
            try
            {
                if (_changePersistence.HasPendingChanges())
                {
                    var savedChanges = await _changePersistence.LoadPendingChangesAsync();
                    
                    foreach (var change in savedChanges)
                    {
                        if (!change.Applied)
                        {
                            _modeManager.AddPendingEdit(change);
                        }
                    }
                    
                    if (savedChanges.Any(c => !c.Applied))
                    {
                        txtStatusBar.Text = $"Restored {savedChanges.Count(c => !c.Applied)} pending change(s) from previous session";
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load pending changes: {ex.Message}");
            }
        }

        /// <summary>
        /// Save pending changes (Phase 5.7)
        /// </summary>
        private async Task SavePendingChangesAsync()
        {
            try
            {
                await _changePersistence.SavePendingChangesAsync(_modeManager.PendingEdits);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save pending changes: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates pending changes display
        /// </summary>
        private void UpdatePendingChangesDisplay()
        {
            Dispatcher.Invoke(() =>
            {
                var pendingEdits = _modeManager.PendingEdits.ToList();
                var count = pendingEdits.Count;
                
                if (count == 0)
                {
                    pendingChangesPanel.Visibility = Visibility.Collapsed;
                }
                else
                {
                    pendingChangesPanel.Visibility = Visibility.Visible;
                    txtPendingChangesCount.Text = count == 1 
                        ? "1 change pending" 
                        : $"{count} changes pending";
                    
                    // Bind to observable collection for display
                    pendingChangesItemsControl.ItemsSource = pendingEdits;
                }
                
                // Phase 5.7: Auto-save pending changes
                _ = SavePendingChangesAsync();
            });
        }

        /// <summary>
        /// Handles View Diff routed event (Phase 5.7: Enhanced with VS diff)
        /// </summary>
        private async void PendingChange_ViewDiff(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is CodeEdit codeEdit)
            {
                try
                {
                    // Phase 5.7: Try to use VS diff service first
                    await _vsDiffService.ShowDiffAsync(codeEdit);
                    codeEdit.DiffWindowOpen = true;
                    
                    txtStatusBar.Text = $"Showing diff for {System.IO.Path.GetFileName(codeEdit.FilePath)}";
                }
                catch (Exception ex)
                {
                    txtStatusBar.Text = $"Failed to show diff: {ex.Message}";
                }
            }
        }

        /// <summary>
        /// Handles Keep Change routed event (Phase 5.7: Enhanced with cleanup)
        /// </summary>
        private async void PendingChange_Keep(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is CodeEdit codeEdit)
            {
                try
                {
                    // Apply the change
                    await _codeModService.ApplyCodeEditAsync(codeEdit);
                    
                    // Mark as applied
                    _modeManager.MarkEditApplied(codeEdit);
                    
                    // Remove from pending
                    _modeManager.RemovePendingEdit(codeEdit);
                    
                    // Phase 5.7: Cleanup temp files
                    _vsDiffService.CleanupTempFiles(codeEdit);
                    
                    // Phase 5.7: Remove from persistence
                    await _changePersistence.RemoveSingleChangeAsync(codeEdit);
                    
                    txtStatusBar.Text = $"Applied change to {System.IO.Path.GetFileName(codeEdit.FilePath)}";
                }
                catch (Exception ex)
                {
                    txtStatusBar.Text = $"Failed to apply change: {ex.Message}";
                }
            }
        }

        /// <summary>
        /// Handles Undo Change routed event (Phase 5.7: Enhanced with cleanup)
        /// </summary>
        private void PendingChange_Undo(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is CodeEdit codeEdit)
            {
                // Remove from pending
                _modeManager.RemovePendingEdit(codeEdit);
                
                // Phase 5.7: Cleanup temp files
                _vsDiffService.CleanupTempFiles(codeEdit);
                
                // Phase 5.7: Remove from persistence
                _ = _changePersistence.RemoveSingleChangeAsync(codeEdit);
                
                txtStatusBar.Text = $"Discarded change to {System.IO.Path.GetFileName(codeEdit.FilePath)}";
            }
        }

        /// <summary>
        /// Keeps all pending changes (Phase 5.7: Enhanced with better error handling)
        /// </summary>
        private async void KeepAllChangesClick(object sender, RoutedEventArgs e)
        {
            var edits = _modeManager.PendingEdits.ToList();
            int successCount = 0;
            int failedCount = 0;
            
            foreach (var edit in edits)
            {
                try
                {
                    await _codeModService.ApplyCodeEditAsync(edit);
                    _modeManager.RemovePendingEdit(edit);
                    _modeManager.MarkEditApplied(edit);
                    
                    // Phase 5.7: Cleanup
                    _vsDiffService.CleanupTempFiles(edit);
                    
                    successCount++;
                }
                catch (Exception ex)
                {
                    failedCount++;
                    System.Diagnostics.Debug.WriteLine($"Failed to apply change: {ex.Message}");
                }
            }
            
            // Phase 5.7: Clear all from persistence
            await _changePersistence.ClearPendingChangesAsync();
            
            if (failedCount == 0)
            {
                txtStatusBar.Text = $"Applied {successCount} change(s) successfully";
            }
            else
            {
                txtStatusBar.Text = $"Applied {successCount}, failed {failedCount} change(s)";
            }
        }

        /// <summary>
        /// Undoes all pending changes (Phase 5.7: Enhanced with cleanup)
        /// </summary>
        private async void UndoAllChangesClick(object sender, RoutedEventArgs e)
        {
            int count = _modeManager.PendingEdits.Count;
            
            // Phase 5.7: Cleanup temp files for all edits
            foreach (var edit in _modeManager.PendingEdits.ToList())
            {
                _vsDiffService.CleanupTempFiles(edit);
            }
            
            _modeManager.ClearPendingEdits();
            
            // Phase 5.7: Clear from persistence
            await _changePersistence.ClearPendingChangesAsync();
            
            txtStatusBar.Text = $"Discarded {count} pending change(s)";
        }

        #endregion
    }
}