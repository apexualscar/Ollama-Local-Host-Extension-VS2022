# ?? Phase 4.1 Testing Guide

## Quick Test: Verify Conversation History Works

### Test 1: Basic Auto-Save ?
**Time:** 2 minutes

1. **Start the extension** (Press `Ctrl+Shift+O`)
2. **Send a message:** "Explain dependency injection"
3. **Wait for response**
4. **Check the file was created:**
   ```powershell
   # Open PowerShell
   explorer $env:APPDATA\OllamaVSExtension\History
   ```
5. **? Pass:** You should see a `.json` file

---

### Test 2: Title Generation ?
**Time:** 1 minute

1. **Send a long message:**
   ```
   Can you explain how async/await works in C# with detailed examples and best practices?
   ```
2. **Open the saved file:**
   ```powershell
   notepad "$env:APPDATA\OllamaVSExtension\History\*.json"
   ```
3. **? Pass:** Title should be truncated to ~50 chars:
   ```json
   "Title": "Can you explain how async/await works in C..."
   ```

---

### Test 3: Multiple Messages ?
**Time:** 3 minutes

1. **Have a conversation:**
   - Message 1: "What is SOLID?"
   - Wait for response
   - Message 2: "Give me an example"
   - Wait for response
2. **Check the JSON file**
3. **? Pass:** Should have 4 messages (2 yours, 2 Ollama's)

---

### Test 4: New Conversation Button ?
**Time:** 2 minutes

1. **Have a conversation** (send 1-2 messages)
2. **Click the "?? New Conversation" button**
3. **Check:**
   - Chat UI cleared? ?
   - Status bar says "New conversation started"? ?
4. **Check history folder:**
   ```powershell
   explorer $env:APPDATA\OllamaVSExtension\History
   ```
5. **? Pass:** Old conversation file still exists

---

### Test 5: Clear Chat (Saves First) ?
**Time:** 2 minutes

1. **Have a conversation**
2. **Click "?? Clear Chat" button**
3. **Check:**
   - Status says "Chat cleared - conversation saved"? ?
   - Chat UI cleared? ?
4. **Check history folder:**
   ```powershell
   Get-ChildItem $env:APPDATA\OllamaVSExtension\History
   ```
5. **? Pass:** Conversation was saved before clearing

---

### Test 6: Context Menu Integration ?
**Time:** 3 minutes

1. **Select some code in the editor**
2. **Right-click ? "Explain Code"**
3. **Wait for explanation**
4. **Check the JSON file**
5. **? Pass:** Messages include the explanation

---

### Test 7: VS Restart Persistence ?
**Time:** 3 minutes

1. **Have a conversation**
2. **Note how many files are in history:**
   ```powershell
   (Get-ChildItem $env:APPDATA\OllamaVSExtension\History).Count
   ```
3. **Close Visual Studio**
4. **Reopen Visual Studio**
5. **Check history folder again**
6. **? Pass:** Files still there!

---

### Test 8: Error Handling ?
**Time:** 2 minutes

1. **Create a corrupted JSON file:**
   ```powershell
   echo "invalid json" > "$env:APPDATA\OllamaVSExtension\History\test.json"
   ```
2. **Restart extension or VS**
3. **? Pass:** Extension doesn't crash, skips corrupted file

---

### Test 9: Export to Markdown ?
**Time:** 2 minutes

**Note:** This tests the service method directly (UI coming in Phase 4.2)

```csharp
// In Immediate Window or test:
var history = new ConversationHistoryService();
var conversations = await history.LoadAllConversationsAsync();
var first = conversations.FirstOrDefault();
if (first != null)
{
    await history.ExportToMarkdownAsync(first, "C:\\temp\\conversation.md");
}
```

**? Pass:** Markdown file created with formatted conversation

---

### Test 10: Model & Mode Tracking ?
**Time:** 2 minutes

1. **Select "Ask" mode**
2. **Select "codellama" model**
3. **Send a message**
4. **Open the JSON file**
5. **? Pass:** Should show:
   ```json
   "ModelUsed": "codellama:latest",
   "Mode": "Ask"
   ```

---

## ?? Test Results

### Expected Results:
```
? Test 1: Basic Auto-Save
? Test 2: Title Generation
? Test 3: Multiple Messages
? Test 4: New Conversation Button
? Test 5: Clear Chat
? Test 6: Context Menu Integration
? Test 7: VS Restart Persistence
? Test 8: Error Handling
? Test 9: Export to Markdown
? Test 10: Model & Mode Tracking
```

**Total Tests:** 10  
**Pass Rate:** Should be 10/10 ?

---

## ?? What to Look For

### In the JSON File:
```json
{
  "Id": "guid-here",
  "Title": "First message truncated...",
  "Created": "2024-01-15T10:30:00",
  "LastModified": "2024-01-15T10:35:00",
  "ModelUsed": "codellama:latest",
  "Mode": "Ask",
  "Messages": [
    {
      "Content": "User message...",
      "IsUser": true,
      "Timestamp": "...",
      "HasCodeBlocks": false
    },
    {
      "Content": "Ollama response...",
      "IsUser": false,
      "Timestamp": "...",
      "HasCodeBlocks": true,
      "CodeBlocks": [...]
    }
  ],
  "TokensUsed": 0,
  "Tags": []
}
```

### Good Signs:
- ? Files are created automatically
- ? JSON is valid and formatted
- ? Timestamps are correct
- ? Messages are preserved
- ? Code blocks are saved
- ? No errors in Output window

### Bad Signs:
- ? No files created
- ? Corrupted JSON
- ? Missing messages
- ? Extension crashes
- ? Errors in Output window

---

## ?? Troubleshooting

### Problem: No files created

**Check:**
1. **Permissions:** Can you write to `%APPDATA%`?
2. **Output window:** Any errors?
3. **Debugger:** Set breakpoint in `SaveConversationAsync`

**Fix:**
```powershell
# Check if directory exists
Test-Path $env:APPDATA\OllamaVSExtension\History

# Create if missing
New-Item -Path "$env:APPDATA\OllamaVSExtension\History" -ItemType Directory
```

---

### Problem: Corrupted JSON

**Check:**
```powershell
# Validate JSON
Get-Content "$env:APPDATA\OllamaVSExtension\History\*.json" | ConvertFrom-Json
```

**Fix:**
- Delete corrupted file
- Extension will skip it automatically

---

### Problem: Extension Crashes

**Check:**
1. **Output window ? Debug**
2. **Exception details**
3. **Stack trace**

**Common causes:**
- Newtonsoft.Json not referenced
- File system permissions
- Null reference

---

## ?? Performance Check

### Expected Performance:
- **Save time:** <100ms
- **Load time:** <50ms
- **Memory usage:** Minimal
- **UI blocking:** None (async)

### How to Check:
```csharp
// Add to code temporarily:
var sw = System.Diagnostics.Stopwatch.StartNew();
await _conversationHistory.SaveConversationAsync(_currentConversation);
sw.Stop();
System.Diagnostics.Debug.WriteLine($"Save took {sw.ElapsedMilliseconds}ms");
```

**? Pass:** Should be <100ms

---

## ? Sign-Off Checklist

Before moving to Phase 4.2:

- [ ] All 10 tests passed
- [ ] No errors in Output window
- [ ] Files created correctly
- [ ] JSON is valid
- [ ] Performance is good
- [ ] No UI freezing
- [ ] Works after VS restart
- [ ] Context menu commands work
- [ ] New conversation button works
- [ ] Clear chat works

**All checked?** ? Phase 4.1 is production-ready!

---

## ?? Next Steps

Once all tests pass:

1. **Use it normally for a day** - Dog food the feature
2. **Monitor for issues** - Check Output window
3. **Note any UX improvements** needed
4. **Move to Phase 4.2:** Conversation History UI

---

**Test completed?** Congratulations! Phase 4.1 is working! ??

**Ready for more?** Move on to Phase 4.2 (Streaming) or Phase 4.3 (Multi-File Context)!
