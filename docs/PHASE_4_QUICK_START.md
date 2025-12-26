# ?? Phase 4 Implementation - Quick Start

## ?? Overview

This guide will help you implement Phase 4 features step-by-step. We'll start with the highest-impact, easiest features first.

---

## ?? Recommended Implementation Order

### 1?? **Conversation History** (Priority: HIGH, Difficulty: MEDIUM)
- **Impact:** Users can resume conversations, build knowledge base
- **Time:** 4-6 hours
- **Dependencies:** None

### 2?? **Streaming Responses** (Priority: HIGH, Difficulty: MEDIUM)
- **Impact:** Much better UX, feels faster
- **Time:** 3-4 hours
- **Dependencies:** None

### 3?? **Multi-File Context** (Priority: HIGH, Difficulty: LOW)
- **Impact:** Better code understanding, more accurate suggestions
- **Time:** 2-3 hours
- **Dependencies:** None

### 4?? **Code Templates** (Priority: MEDIUM, Difficulty: LOW)
- **Impact:** Faster common tasks
- **Time:** 2-3 hours
- **Dependencies:** None

### 5?? **Solution Analysis** (Priority: LOW, Difficulty: HIGH)
- **Impact:** Advanced feature, fewer users need it
- **Time:** 8-10 hours
- **Dependencies:** Project parser

### 6?? **Inline Suggestions** (Priority: LOW, Difficulty: VERY HIGH)
- **Impact:** Copilot-like, but very complex
- **Time:** 20+ hours
- **Dependencies:** VS IntelliSense integration

---

## ?? Let's Start with Conversation History!

### Step 1: Create the Models

**Models/Conversation.cs:**
```csharp
using System;
using System.Collections.Generic;

namespace OllamaLocalHostIntergration.Models
{
    public class Conversation
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = "New Conversation";
        public DateTime Created { get; set; } = DateTime.Now;
        public DateTime LastModified { get; set; } = DateTime.Now;
        public string ModelUsed { get; set; }
        public InteractionMode Mode { get; set; }
        public List<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
        public int TokensUsed { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
    }
}
```

### Step 2: Create the Service

**Services/ConversationHistoryService.cs:**
```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using OllamaLocalHostIntergration.Models;

namespace OllamaLocalHostIntergration.Services
{
    public class ConversationHistoryService
    {
        private readonly string _historyPath;

        public ConversationHistoryService()
        {
            _historyPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "OllamaVSExtension",
                "History"
            );
            Directory.CreateDirectory(_historyPath);
        }

        public async Task SaveConversationAsync(Conversation conversation)
        {
            conversation.LastModified = DateTime.Now;
            string filename = $"{conversation.Id}.json";
            string filePath = Path.Combine(_historyPath, filename);
            
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(conversation, options);
            
            await File.WriteAllTextAsync(filePath, json);
        }

        public async Task<List<Conversation>> LoadAllConversationsAsync()
        {
            var conversations = new List<Conversation>();
            
            if (!Directory.Exists(_historyPath))
                return conversations;

            foreach (var file in Directory.GetFiles(_historyPath, "*.json"))
            {
                try
                {
                    string json = await File.ReadAllTextAsync(file);
                    var conversation = JsonSerializer.Deserialize<Conversation>(json);
                    if (conversation != null)
                    {
                        conversations.Add(conversation);
                    }
                }
                catch
                {
                    // Skip corrupted files
                }
            }

            return conversations.OrderByDescending(c => c.LastModified).ToList();
        }

        public async Task<Conversation> LoadConversationAsync(Guid id)
        {
            string filename = $"{id}.json";
            string filePath = Path.Combine(_historyPath, filename);
            
            if (!File.Exists(filePath))
                return null;

            try
            {
                string json = await File.ReadAllTextAsync(filePath);
                return JsonSerializer.Deserialize<Conversation>(json);
            }
            catch
            {
                return null;
            }
        }

        public async Task DeleteConversationAsync(Guid id)
        {
            string filename = $"{id}.json";
            string filePath = Path.Combine(_historyPath, filename);
            
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }

        public async Task ExportToMarkdownAsync(Conversation conversation, string outputPath)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"# {conversation.Title}");
            sb.AppendLine($"*Created: {conversation.Created:yyyy-MM-dd HH:mm}*");
            sb.AppendLine($"*Model: {conversation.ModelUsed}*");
            sb.AppendLine($"*Mode: {conversation.Mode}*");
            sb.AppendLine();

            foreach (var message in conversation.Messages)
            {
                sb.AppendLine($"## {(message.IsUser ? "You" : "Ollama")}");
                sb.AppendLine(message.Content);
                sb.AppendLine();
            }

            await File.WriteAllTextAsync(outputPath, sb.ToString());
        }

        public string GetHistoryPath() => _historyPath;
    }
}
```

### Step 3: Update MyToolWindowControl

Add conversation tracking to `MyToolWindowControl.xaml.cs`:

```csharp
// Add these fields
private readonly ConversationHistoryService _conversationHistory;
private Conversation _currentConversation;

// In constructor, add:
_conversationHistory = new ConversationHistoryService();
_currentConversation = new Conversation
{
    ModelUsed = "Not selected",
    Mode = InteractionMode.Ask
};

// Update SendUserMessage to save conversation
private async Task SendUserMessage()
{
    // ... existing code ...
    
    // After adding user message
    _currentConversation.Messages.Add(userChatMessage);
    
    // After receiving response
    _currentConversation.Messages.Add(responseChatMessage);
    _currentConversation.ModelUsed = comboModels.SelectedItem as string ?? "Unknown";
    _currentConversation.Mode = _modeManager.CurrentMode;
    
    // Auto-save after each exchange
    await _conversationHistory.SaveConversationAsync(_currentConversation);
}

// Add method to start new conversation
private void NewConversationClick(object sender, RoutedEventArgs e)
{
    _currentConversation = new Conversation
    {
        Title = $"Conversation {DateTime.Now:yyyy-MM-dd HH:mm}",
        ModelUsed = comboModels.SelectedItem as string ?? "Not selected",
        Mode = _modeManager.CurrentMode
    };
    _chatMessages.Clear();
    txtStatusBar.Text = "New conversation started";
}
```

### Step 4: Update XAML

Add a "New Conversation" button to the toolbar:

```xaml
<!-- Add after Clear Chat button -->
<Button Grid.Column="4"
        Content="&#xE8F4;"
        FontFamily="Segoe MDL2 Assets"
        Click="NewConversationClick"
        Width="28"
        Height="28"
        Margin="4,0,0,0"
        VerticalAlignment="Center"
        ToolTip="New conversation"
        FontSize="12"
        Padding="0"
        VerticalContentAlignment="Center"
        HorizontalContentAlignment="Center"
        Background="{DynamicResource {x:Static vsshell:VsBrushes.ButtonFaceKey}}"
        Foreground="{DynamicResource {x:Static vsshell:VsBrushes.ButtonTextKey}}"/>
```

### Step 5: Test It!

1. Build the project
2. Start debugging (F5)
3. Have a conversation with Ollama
4. Check `%APPDATA%\OllamaVSExtension\History\` for saved JSON files
5. Restart VS and verify conversations persist

---

## ?? Quick Wins You Can Implement Right Now

### 1. Auto-save current conversation
? Already included above!

### 2. Generate conversation title from first message
```csharp
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

// Use when starting conversation:
_currentConversation.Title = GenerateTitleFromMessage(userMessage);
```

### 3. Show conversation info in status bar
```csharp
private void UpdateConversationStatus()
{
    int messageCount = _currentConversation.Messages.Count;
    string status = $"{messageCount} messages • {_currentConversation.ModelUsed}";
    // Display somewhere in UI
}
```

---

## ?? Next Steps After Conversation History

Once you have conversation history working, you can:

1. **Add a history browser dialog** - Show list of past conversations
2. **Implement streaming** - Make responses feel faster
3. **Add multi-file context** - Include multiple files
4. **Create templates** - Quick common tasks

---

## ?? Testing Checklist

- [ ] Conversation saves after each message
- [ ] Conversation loads correctly from disk
- [ ] Multiple conversations don't interfere
- [ ] Export to Markdown works
- [ ] Old conversations can be deleted
- [ ] Corrupted JSON files don't crash extension

---

## ?? You're Ready!

Start with conversation history, then move to streaming responses. Both are high-impact features that users will love!

**Need help?** Check `PHASE_4_PLAN.md` for detailed implementations of all features.

**Ready to code?** Let's implement conversation history first! ??
