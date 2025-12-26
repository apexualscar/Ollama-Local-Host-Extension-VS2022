# ?? Phase 4: Advanced Features & Enhancements

## ?? Overview

**Status:** ?? Ready to Implement  
**Timeline:** Week 4-5  
**Goal:** Add advanced features that enhance user productivity and make the extension production-ready

---

## ? Completed (Phases 1-3)

- ? Core infrastructure (Mode system, services)
- ? UI/UX enhancements (Rich chat, diff preview)
- ? Context menu integration (Explain, Refactor, Find Issues)
- ? Keyboard shortcuts (Ctrl+Shift+O/E/R/I)
- ? Agent mode with code editing
- ? Settings persistence
- ? Dark mode support
- ? Segoe MDL2 Assets icons

---

## ?? Phase 4 Features

### 4.1 **Conversation History Persistence** ??
Save and restore chat conversations across VS sessions

**Benefits:**
- Resume work on long conversations
- Reference past solutions
- Build knowledge base
- Export for documentation

**Implementation:**
- Save to JSON in `%APPDATA%\OllamaVSExtension\History\`
- Auto-save after each message
- List/search past conversations
- Export to Markdown

### 4.2 **Streaming Responses** ?
Real-time token-by-token display as Ollama generates

**Benefits:**
- Faster perceived response time
- See progress during long responses
- Cancel if going wrong direction
- More interactive feel

**Implementation:**
- Use Ollama streaming API
- Update chat UI in real-time
- Add stop/cancel button
- Show typing indicator

### 4.3 **Multi-File Context** ??
Include multiple files in context for better understanding

**Benefits:**
- Understand relationships between files
- Get more accurate suggestions
- Analyze entire features
- Better refactoring suggestions

**Implementation:**
- File selector in settings panel
- Drag & drop files to add context
- Show token usage per file
- Smart file selection (related files)

### 4.4 **Code Generation Templates** ??
Pre-defined prompts for common tasks

**Benefits:**
- Faster common tasks
- Consistent code style
- Best practices built-in
- Reduced typing

**Templates:**
- Generate unit tests
- Create documentation
- Add logging
- Implement interface
- Convert synchronous to async
- Add error handling

### 4.5 **Inline Code Suggestions** ??
Autocomplete-style AI suggestions while typing

**Benefits:**
- Copilot-like experience
- Non-intrusive suggestions
- Accept with Tab key
- Context-aware completions

**Implementation:**
- Hook into VS IntelliSense
- Trigger on pause or Ctrl+Space
- Show ghost text preview
- Accept/reject easily

### 4.6 **Solution-Wide Analysis** ??
Analyze entire solution structure

**Benefits:**
- Find patterns across projects
- Suggest architectural improvements
- Detect code smells
- Generate dependency graphs

**Implementation:**
- Parse .csproj files
- Build file tree
- Extract namespaces/classes
- Generate context automatically

---

## ?? Files to Create

### 4.1 Conversation History
```
Services/
  ?? ConversationHistoryService.cs
Models/
  ?? Conversation.cs
  ?? ConversationMetadata.cs
Dialogs/
  ?? ConversationHistoryDialog.xaml
  ?? ConversationHistoryDialog.xaml.cs
```

### 4.2 Streaming
```
Services/
  ?? OllamaStreamingService.cs (extend OllamaService)
```

### 4.3 Multi-File Context
```
Services/
  ?? FileContextService.cs
Dialogs/
  ?? FileContextDialog.xaml
  ?? FileContextDialog.xaml.cs
```

### 4.4 Templates
```
Services/
  ?? TemplateService.cs
Models/
  ?? CodeTemplate.cs
Templates/
  ?? UnitTest.json
  ?? Documentation.json
  ?? Logging.json
  ?? ErrorHandling.json
```

### 4.5 Inline Suggestions
```
Services/
  ?? InlineCompletionService.cs
Commands/
  ?? InlineCompletionCommand.cs
```

### 4.6 Solution Analysis
```
Services/
  ?? SolutionAnalysisService.cs
  ?? ProjectParserService.cs
Models/
  ?? SolutionContext.cs
  ?? ProjectInfo.cs
```

---

## ?? Priority Order

### High Priority (Implement First):
1. **Conversation History** - Most requested feature
2. **Streaming Responses** - Better UX
3. **Multi-File Context** - Critical for complex tasks

### Medium Priority:
4. **Code Templates** - Nice to have, easy to implement
5. **Solution Analysis** - Useful but complex

### Low Priority (Future):
6. **Inline Suggestions** - Complex, requires IntelliSense integration

---

## ?? Detailed Implementation Plans

### 4.1 Conversation History Service

**ConversationHistoryService.cs:**
```csharp
public class ConversationHistoryService
{
    private string _historyPath;
    
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
        string filename = $"{conversation.Id}.json";
        string filePath = Path.Combine(_historyPath, filename);
        string json = JsonSerializer.Serialize(conversation, new JsonSerializerOptions 
        { 
            WriteIndented = true 
        });
        await File.WriteAllTextAsync(filePath, json);
    }
    
    public async Task<List<Conversation>> LoadAllConversationsAsync()
    {
        var conversations = new List<Conversation>();
        foreach (var file in Directory.GetFiles(_historyPath, "*.json"))
        {
            string json = await File.ReadAllTextAsync(file);
            var conversation = JsonSerializer.Deserialize<Conversation>(json);
            conversations.Add(conversation);
        }
        return conversations.OrderByDescending(c => c.LastModified).ToList();
    }
    
    public async Task<Conversation> LoadConversationAsync(Guid id)
    {
        string filename = $"{id}.json";
        string filePath = Path.Combine(_historyPath, filename);
        if (!File.Exists(filePath)) return null;
        
        string json = await File.ReadAllTextAsync(filePath);
        return JsonSerializer.Deserialize<Conversation>(json);
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
        sb.AppendLine();
        
        foreach (var message in conversation.Messages)
        {
            sb.AppendLine($"## {(message.IsUser ? "You" : "Ollama")}");
            sb.AppendLine(message.Content);
            sb.AppendLine();
        }
        
        await File.WriteAllTextAsync(outputPath, sb.ToString());
    }
}
```

**Conversation.cs:**
```csharp
public class Conversation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; }
    public DateTime Created { get; set; } = DateTime.Now;
    public DateTime LastModified { get; set; } = DateTime.Now;
    public string ModelUsed { get; set; }
    public InteractionMode Mode { get; set; }
    public List<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
    public int TokensUsed { get; set; }
    public List<string> Tags { get; set; } = new List<string>();
}
```

**UI Changes:**
Add to toolbar:
```xaml
<!-- Conversation History Button -->
<Button Content="&#xE81C;" 
        FontFamily="Segoe MDL2 Assets"
        ToolTip="Conversation History"
        Click="ConversationHistoryClick"/>
```

---

### 4.2 Streaming Responses

**Extend OllamaService:**
```csharp
public async IAsyncEnumerable<string> StreamChatResponseAsync(
    string userMessage, 
    string systemPrompt, 
    string codeContext)
{
    var request = new
    {
        model = _currentModel,
        messages = BuildMessageHistory(userMessage, systemPrompt, codeContext),
        stream = true
    };
    
    var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_serverAddress}/api/chat")
    {
        Content = new StringContent(
            JsonSerializer.Serialize(request), 
            Encoding.UTF8, 
            "application/json"
        )
    };
    
    using var response = await _httpClient.SendAsync(
        httpRequest, 
        HttpCompletionOption.ResponseHeadersRead
    );
    
    response.EnsureSuccessStatusCode();
    
    using var stream = await response.Content.ReadAsStreamAsync();
    using var reader = new StreamReader(stream);
    
    string line;
    while ((line = await reader.ReadLineAsync()) != null)
    {
        if (string.IsNullOrWhiteSpace(line)) continue;
        
        var json = JsonDocument.Parse(line);
        var root = json.RootElement;
        
        if (root.TryGetProperty("message", out var messageElement))
        {
            if (messageElement.TryGetProperty("content", out var contentElement))
            {
                yield return contentElement.GetString();
            }
        }
        
        if (root.TryGetProperty("done", out var doneElement) && doneElement.GetBoolean())
        {
            break;
        }
    }
}
```

**UI Changes:**
```csharp
private async Task SendUserMessageWithStreaming()
{
    // ... existing code ...
    
    var responseChatMessage = new ChatMessage("", false);
    _chatMessages.Add(responseChatMessage);
    
    await foreach (var token in _ollamaService.StreamChatResponseAsync(userMessage, systemPrompt, codeContext))
    {
        responseChatMessage.Content += token;
        // Force UI update
        Application.Current.Dispatcher.Invoke(() => { });
    }
    
    // Parse for code blocks after complete
    var finalMessage = _messageParser.ParseMessage(responseChatMessage.Content, false);
    _chatMessages[_chatMessages.Count - 1] = finalMessage;
}
```

---

### 4.3 Multi-File Context Service

**FileContextService.cs:**
```csharp
public class FileContextService
{
    private List<string> _contextFiles = new List<string>();
    private Dictionary<string, string> _fileContents = new Dictionary<string, string>();
    
    public async Task<string> BuildMultiFileContextAsync()
    {
        var sb = new StringBuilder();
        
        foreach (var filePath in _contextFiles)
        {
            if (_fileContents.ContainsKey(filePath))
            {
                string content = _fileContents[filePath];
                string filename = Path.GetFileName(filePath);
                string relativePath = GetRelativePath(filePath);
                
                sb.AppendLine($"// File: {relativePath}");
                sb.AppendLine(content);
                sb.AppendLine();
            }
        }
        
        return sb.ToString();
    }
    
    public void AddFile(string filePath)
    {
        if (!_contextFiles.Contains(filePath))
        {
            _contextFiles.Add(filePath);
            LoadFileContent(filePath);
        }
    }
    
    public void RemoveFile(string filePath)
    {
        _contextFiles.Remove(filePath);
        _fileContents.Remove(filePath);
    }
    
    public void ClearFiles()
    {
        _contextFiles.Clear();
        _fileContents.Clear();
    }
    
    public List<string> GetContextFiles() => _contextFiles.ToList();
    
    public int EstimateTotalTokens()
    {
        int total = 0;
        foreach (var content in _fileContents.Values)
        {
            total += content.Length / 4; // Rough estimate
        }
        return total;
    }
    
    private void LoadFileContent(string filePath)
    {
        try
        {
            string content = File.ReadAllText(filePath);
            _fileContents[filePath] = content;
        }
        catch
        {
            _fileContents[filePath] = "// Error reading file";
        }
    }
}
```

**UI - Add to Settings Panel:**
```xaml
<TextBlock Text="Context Files:" FontWeight="SemiBold"/>
<ListBox x:Name="lstContextFiles" 
         Height="100" 
         Margin="0,4,0,4">
    <ListBox.ItemTemplate>
        <DataTemplate>
            <Grid>
                <TextBlock Text="{Binding}"/>
                <Button Content="×" 
                        HorizontalAlignment="Right"
                        Click="RemoveContextFile"/>
            </Grid>
        </DataTemplate>
    </ListBox.ItemTemplate>
</ListBox>
<Button Content="Add File..." Click="AddContextFileClick"/>
```

---

### 4.4 Code Templates

**TemplateService.cs:**
```csharp
public class TemplateService
{
    private Dictionary<string, CodeTemplate> _templates;
    
    public TemplateService()
    {
        LoadBuiltInTemplates();
    }
    
    private void LoadBuiltInTemplates()
    {
        _templates = new Dictionary<string, CodeTemplate>
        {
            ["unittest"] = new CodeTemplate
            {
                Name = "Generate Unit Tests",
                Description = "Creates unit tests for selected code",
                SystemPrompt = "You are an expert at writing unit tests. Generate comprehensive unit tests for the provided code using xUnit/NUnit/MSTest as appropriate. Include edge cases and error scenarios.",
                Icon = "&#xE7C0;"
            },
            ["documentation"] = new CodeTemplate
            {
                Name = "Generate Documentation",
                Description = "Creates XML documentation comments",
                SystemPrompt = "Generate XML documentation comments for the provided code. Include <summary>, <param>, <returns>, and <example> tags where appropriate.",
                Icon = "&#xE8A5;"
            },
            ["logging"] = new CodeTemplate
            {
                Name = "Add Logging",
                Description = "Adds logging statements",
                SystemPrompt = "Add appropriate logging statements using ILogger. Include information, warning, and error logs at key points. Add try-catch blocks where needed.",
                Icon = "&#xE7C3;"
            },
            ["async"] = new CodeTemplate
            {
                Name = "Convert to Async",
                Description = "Converts synchronous code to async/await",
                SystemPrompt = "Convert the provided synchronous code to use async/await patterns. Update method signatures, add ConfigureAwait(false) where appropriate, and handle cancellation tokens.",
                Icon = "&#xE895;"
            },
            ["errorhandling"] = new CodeTemplate
            {
                Name = "Add Error Handling",
                Description = "Adds comprehensive error handling",
                SystemPrompt = "Add comprehensive error handling to the code. Include try-catch blocks, validate inputs, handle edge cases, and provide meaningful error messages.",
                Icon = "&#xE783;"
            }
        };
    }
    
    public List<CodeTemplate> GetAllTemplates() => _templates.Values.ToList();
    
    public CodeTemplate GetTemplate(string id) => 
        _templates.TryGetValue(id, out var template) ? template : null;
}
```

**UI - Template Selector:**
```xaml
<!-- Add Templates dropdown to toolbar -->
<ComboBox x:Name="comboTemplates"
          Width="120"
          ToolTip="Quick Templates">
    <ComboBoxItem Content="?? Templates" IsEnabled="False"/>
    <Separator/>
    <ComboBoxItem Content="Unit Tests" Tag="unittest"/>
    <ComboBoxItem Content="Documentation" Tag="documentation"/>
    <ComboBoxItem Content="Add Logging" Tag="logging"/>
    <ComboBoxItem Content="To Async" Tag="async"/>
    <ComboBoxItem Content="Error Handling" Tag="errorhandling"/>
</ComboBox>
```

---

## ?? UI Mockups

### Conversation History Dialog:
```
?? Conversation History ???????????????????
? ?? Search: [              ]             ?
????????????????????????????????????????????
? ?? Today                                 ?
?   • Refactor user service (3 msg)       ?
?   • Fix null reference bug (8 msg)      ?
?                                          ?
? ?? Yesterday                             ?
?   • Add logging to API (5 msg)          ?
?   • Unit tests for parser (12 msg)      ?
?                                          ?
? ?? This Week                             ?
?   • Database schema design (15 msg)     ?
????????????????????????????????????????????
? [Load] [Delete] [Export] [Close]        ?
????????????????????????????????????????????
```

### Multi-File Context Panel:
```
?? Context Files ??????????????????????????
? ?? UserService.cs              ~250 tok ?
? ?? IUserRepository.cs          ~100 tok ?
? ?? UserDTO.cs                   ~80 tok ?
? ??????????????????????????????????????  ?
? Total: ~430 tokens                      ?
? [+ Add File] [Clear All]                ?
????????????????????????????????????????????
```

---

## ?? Implementation Timeline

### Week 4:
- **Day 1-2:** Conversation History Service & UI
- **Day 3-4:** Streaming Responses
- **Day 5:** Multi-File Context Service

### Week 5:
- **Day 1-2:** Code Templates
- **Day 3:** Solution Analysis (basic)
- **Day 4-5:** Testing & Polish

---

## ? Success Criteria

### Must Have:
- ? Conversation history saves/loads correctly
- ? Streaming shows tokens in real-time
- ? Multi-file context includes all selected files
- ? Templates generate appropriate prompts

### Nice to Have:
- ? Export conversations to Markdown
- ? Search conversation history
- ? Smart file suggestions
- ? Custom template creation

---

## ?? Getting Started

**Step 1:** Create Conversation History
```bash
# Create new files
New-Item -Path "Services\ConversationHistoryService.cs" -ItemType File
New-Item -Path "Models\Conversation.cs" -ItemType File
New-Item -Path "Dialogs\ConversationHistoryDialog.xaml" -ItemType File
```

**Step 2:** Implement streaming
**Step 3:** Add multi-file context
**Step 4:** Create templates

---

## ?? Resources

- [Ollama Streaming API](https://github.com/ollama/ollama/blob/main/docs/api.md#generate-a-chat-completion-with-streaming)
- [VS SDK - File Management](https://docs.microsoft.com/en-us/visualstudio/extensibility/)
- [JSON Serialization in .NET](https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-how-to)

---

**Ready to implement Phase 4!** ??

Which feature would you like to tackle first?
1. Conversation History
2. Streaming Responses
3. Multi-File Context
4. Code Templates
