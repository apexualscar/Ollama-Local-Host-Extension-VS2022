# ?? UPDATED DEPLOYMENT PLAN - UI/UX Enhancement + Agentic Behavior

## ?? Current Status (After Phase 5.4)

### ? What Works Now:
- Ask Mode: Q&A and explanations
- Agent Mode: Complete code with Apply button
- Rich chat display with code blocks
- Diff preview for single-file changes
- Context menu commands (Explain, Refactor, Find Issues)
- Conversation history (auto-saved)
- Multi-file context awareness
- Streaming responses
- Model selection
- Clean toolbar (template dropdown removed)

### ?? What's Next:
**Phases 5.5-5.7:** GitHub Copilot-style UI/UX improvements  
**Phases 6-9:** True agentic behavior

---

## ?? Phase 5.5: UI Cleanup & Copilot-Style Layout

**Priority:** ?? HIGH  
**Time:** 2-3 hours  
**Impact:** Major UX improvement - matches GitHub Copilot

### Phase 5.5.1: Conversation Management Header (30 minutes)

**Goal:** Move conversation controls to top like GitHub Copilot

**Changes:**
1. **Create new header section** at top of window (above chat area)
2. **Add conversation dropdown** - Select from saved conversations
3. **Move "New Conversation" button** from bottom toolbar to header
4. **Move "Delete Conversation" button** to header (rename from "Clear Chat")

**UI Layout:**
```
????????????????????????????????????????????????????
? [Conversations ?] [+ New] [?? Delete]             ? ? NEW HEADER
????????????????????????????????????????????????????
?                                                   ?
?  Chat Messages Area                               ?
?                                                   ?
????????????????????????????????????????????????????
? [Ask?] [Model?] [?]                              ? ? CLEANED TOOLBAR
?                                                   ?
? Context References (Phase 5.5.2)                  ?
? Total Changes (Phase 5.5.3)                       ?
? [Type your message...]                            ?
?                              [Send]               ?
????????????????????????????????????????????????????
```

**Files to Modify:**
- `ToolWindows/MyToolWindowControl.xaml` - Add header Grid row
- `ToolWindows/MyToolWindowControl.xaml.cs` - Add conversation dropdown logic
- `Services/ConversationHistoryService.cs` - Add GetAllConversations() method

**Implementation:**
```xaml
<Grid>
    <Grid.RowDefinitions>
        <RowDefinition Height="Auto"/> <!-- NEW: Conversation Header -->
        <RowDefinition Height="*"/>    <!-- Chat Area -->
        <RowDefinition Height="Auto"/> <!-- Input Area -->
        <RowDefinition Height="Auto"/> <!-- Status Bar -->
    </Grid.RowDefinitions>
    
    <!-- NEW: Conversation Management Header -->
    <Border Grid.Row="0" ...>
        <Grid>
            <ComboBox x:Name="comboConversations" .../>
            <Button Content="+ New" Click="NewConversationClick"/>
            <Button Content="??" ToolTip="Delete Conversation" Click="DeleteConversationClick"/>
        </Grid>
    </Border>
    
    <!-- Chat Messages Area -->
    <ScrollViewer Grid.Row="1" ...>
    </ScrollViewer>
    
    <!-- Input Area -->
    <Border Grid.Row="2" ...>
    </Border>
</Grid>
```

**Benefits:**
- ? Matches GitHub Copilot layout
- ? Easy conversation switching
- ? Cleaner bottom toolbar
- ? Better conversation management

---

### Phase 5.5.2: Copilot-Style Context References (45 minutes)

**Goal:** Move context management out of settings, add Copilot-style "+" button

**Changes:**
1. **Remove** "Code Context" and "Context Files" from settings panel
2. **Remove** token count from settings panel
3. **Create new "Context References" section** above text input
4. **Add "+" button** that opens context type selector
5. **Context types available:**
   - ?? Files (select from solution)
   - ?? Selection (current selected text)
   - ?? Methods (search for method)
   - ?? Classes (search for class)
   - ??? Solution (entire solution context)
   - ?? Project (specific project)

**UI Layout:**
```
????????????????????????????????????????????????????
? Context: [?? MyFile.cs] [?? Selection] [?? Method] ?
?          [+ Add Context]                          ? ? NEW SECTION
????????????????????????????????????????????????????
? [Type your message...]                            ?
?                              [Send]               ?
????????????????????????????????????????????????????
```

**Context Picker Dialog:**
```
???????????????????????????????
? Add Context                  ?
???????????????????????????????
? ?? Files                     ?
? ?? Selection                 ?
? ?? Methods                   ?
? ?? Classes                   ?
? ??? Solution                  ?
? ?? Project                   ?
???????????????????????????????
```

**Files to Create:**
- `Controls/ContextReferenceControl.xaml` - Context chip display
- `Dialogs/ContextPickerDialog.xaml` - Context type selector
- `Models/ContextReference.cs` - Context reference model
- `Services/ContextReferenceService.cs` - Manage context refs

**Files to Modify:**
- `ToolWindows/MyToolWindowControl.xaml` - Add context section
- `ToolWindows/MyToolWindowControl.xaml.cs` - Wire up context logic

**Implementation Preview:**
```csharp
public class ContextReference
{
    public ContextReferenceType Type { get; set; }
    public string DisplayText { get; set; }
    public string FilePath { get; set; }
    public string ClassName { get; set; }
    public string MethodName { get; set; }
    public int TokenCount { get; set; }
}

public enum ContextReferenceType
{
    File,
    Selection,
    Method,
    Class,
    Solution,
    Project
}
```

**Benefits:**
- ? Matches GitHub Copilot UX
- ? Cleaner settings panel
- ? More intuitive context management
- ? Visual feedback of what's in context

**NOTE:** This phase implements **UI only** - functionality in Phase 5.6

---

### Phase 5.5.3: Total Changes / Keep / Undo UI (45 minutes)

**Goal:** Add Copilot-style change tracking above text input

**Changes:**
1. **Create "Total Changes" display** above text input
2. **Show number of pending changes**
3. **Add "Keep" button** to accept all changes
4. **Add "Undo" button** to revert all changes
5. **Display changes as diffs** in Visual Studio until kept
6. **Integrate with Visual Studio diff viewer**

**UI Layout:**
```
????????????????????????????????????????????????????
? Context: [?? MyFile.cs]                          ?
????????????????????????????????????????????????????
? ?? 3 changes pending [Keep All] [Undo All]       ? ? NEW SECTION
?   • MyFile.cs (modified)                         ?
?   • NewClass.cs (created)                        ?
?   • OldFile.cs (deleted)                         ?
????????????????????????????????????????????????????
? [Type your message...]                            ?
?                              [Send]               ?
????????????????????????????????????????????????????
```

**Expanded View:**
```
????????????????????????????????????????????????????
? ?? Total Changes: 3                              ?
????????????????????????????????????????????????????
? ?? MyFile.cs (Modified)                          ?
?    Lines 45-52 changed                           ?
?    [View Diff] [Keep] [Undo]                     ?
????????????????????????????????????????????????????
? ? NewClass.cs (Created)                         ?
?    235 lines added                               ?
?    [View] [Keep] [Undo]                          ?
????????????????????????????????????????????????????
? ??? OldFile.cs (Deleted)                          ?
?    150 lines removed                             ?
?    [Restore] [Keep]                              ?
????????????????????????????????????????????????????
? [Keep All] [Undo All]                            ?
????????????????????????????????????????????????????
```

**Files to Create:**
- `Controls/TotalChangesControl.xaml` - Changes display UI
- `Models/PendingChange.cs` - Change tracking model
- `Services/ChangeTrackingService.cs` - Track pending changes

**Files to Modify:**
- `ToolWindows/MyToolWindowControl.xaml` - Add changes section
- `ToolWindows/MyToolWindowControl.xaml.cs` - Wire up change tracking
- `Services/CodeModificationService.cs` - Integrate with change tracking

**Implementation Preview:**
```csharp
public class PendingChange
{
    public string FilePath { get; set; }
    public PendingChangeType Type { get; set; } // Modified, Created, Deleted
    public string OriginalContent { get; set; }
    public string NewContent { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsKept { get; set; }
    public CodeEdit AssociatedEdit { get; set; }
}

public enum PendingChangeType
{
    Modified,
    Created,
    Deleted
}
```

**Visual Studio Integration:**
```csharp
// Show diff in VS until kept
public async Task ShowPendingDiffAsync(PendingChange change)
{
    // Use VS diff viewer to show change
    // Keep diff open until user clicks "Keep" or "Undo"
    var diffService = await VS.GetServiceAsync<SVsDifferenceService, IVsDifferenceService>();
    // ... show diff
}
```

**Benefits:**
- ? Matches GitHub Copilot workflow
- ? Safety net for mistakes
- ? Clear visibility of all changes
- ? Easy batch operations

**NOTE:** This phase implements **UI only** - functionality in Phase 5.7

---

### Phase 5.5.4: Input Box Improvements (15 minutes)

**Goal:** Fix input behavior and icon

**Changes:**
1. **Change ENTER behavior:**
   - `ENTER` ? Send message
   - `SHIFT + ENTER` ? New line
2. **Fix refresh icon:**
   - Change calculator icon (&#xE8EF;) to refresh icon (&#xE72C;)
3. **Add placeholder text:**
   - "Ask a question or describe what you want to build..."

**Files to Modify:**
- `ToolWindows/MyToolWindowControl.xaml` - Update button icon
- `ToolWindows/MyToolWindowControl.xaml.cs` - Update KeyDown handler

**Implementation:**
```csharp
// Update TxtUserInputKeyDown method
private async void TxtUserInputKeyDown(object sender, KeyEventArgs e)
{
    // SHIFT + ENTER = new line (default behavior, do nothing)
    // ENTER alone = send message
    if (e.Key == Key.Enter && !Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
    {
        e.Handled = true;
        await SendUserMessage();
    }
    // If SHIFT + ENTER, let default behavior add new line
}
```

```xaml
<!-- Update refresh button icon -->
<Button Grid.Column="2"
        Content="&#xE72C;"  <!-- Changed from &#xE8EF; -->
        FontFamily="Segoe MDL2 Assets"
        ToolTip="Refresh models"
        .../>

<!-- Add placeholder to input -->
<TextBox x:Name="txtUserInput"
         TextWrapping="Wrap"
         AcceptsReturn="True"
         VerticalScrollBarVisibility="Auto"
         Tag="Ask a question or describe what you want to build..."
         .../>
```

**Benefits:**
- ? Matches standard chat UX (ENTER to send)
- ? Correct icon usage
- ? Better user guidance with placeholder

---

## ?? Phase 5.5 Summary

| Sub-Phase | Task | Time | Priority |
|-----------|------|------|----------|
| 5.5.1 | Conversation Header | 30 min | HIGH |
| 5.5.2 | Context References UI | 45 min | HIGH |
| 5.5.3 | Total Changes UI | 45 min | HIGH |
| 5.5.4 | Input Improvements | 15 min | MEDIUM |
| **Total** | **UI Cleanup** | **2h 15m** | **HIGH** |

**Outcome:** Extension UI matches GitHub Copilot style ?

---

## ?? Phase 5.6: Implement Context Reference Feature

**Priority:** ?? MEDIUM-HIGH  
**Time:** 2-3 hours  
**Impact:** Makes context management powerful and intuitive

### Goal:
Implement the functionality for Phase 5.5.2's context reference UI

### Features to Implement:

#### 5.6.1: File Context Selection (45 minutes)
- Browse solution for files
- Multi-select files
- Add to context references
- Show file tokens in chip

#### 5.6.2: Selection Context (30 minutes)
- Capture current editor selection
- Add as context reference
- Update when selection changes (optional)

#### 5.6.3: Method/Class Context (1 hour)
- Search solution for methods
- Search solution for classes
- Parse code to extract specific members
- Add to context with token count

#### 5.6.4: Solution/Project Context (45 minutes)
- Add entire solution structure
- Add specific project
- Smart truncation if too large
- Warning if exceeds token limit

**Files to Implement:**
- `Services/ContextReferenceService.cs` - Main service
- `Services/CodeSearchService.cs` - Search for methods/classes
- `Dialogs/FilePickerDialog.xaml` - File selection dialog
- `Dialogs/MethodSearchDialog.xaml` - Method search dialog
- `Dialogs/ClassSearchDialog.xaml` - Class search dialog

**Implementation Details:**
```csharp
public class ContextReferenceService
{
    private List<ContextReference> _references = new List<ContextReference>();
    
    public async Task<ContextReference> AddFileReferenceAsync(string filePath)
    {
        var content = await File.ReadAllTextAsync(filePath);
        var tokenCount = EstimateTokenCount(content);
        
        var reference = new ContextReference
        {
            Type = ContextReferenceType.File,
            DisplayText = Path.GetFileName(filePath),
            FilePath = filePath,
            TokenCount = tokenCount
        };
        
        _references.Add(reference);
        return reference;
    }
    
    public async Task<ContextReference> AddMethodReferenceAsync(string className, string methodName)
    {
        var methodCode = await _codeSearchService.FindMethodAsync(className, methodName);
        var reference = new ContextReference
        {
            Type = ContextReferenceType.Method,
            DisplayText = $"{className}.{methodName}",
            MethodName = methodName,
            ClassName = className,
            TokenCount = EstimateTokenCount(methodCode)
        };
        
        _references.Add(reference);
        return reference;
    }
    
    public string BuildContextPrompt()
    {
        var prompt = new StringBuilder();
        
        foreach (var reference in _references)
        {
            prompt.AppendLine($"### {reference.Type}: {reference.DisplayText}");
            prompt.AppendLine("```");
            prompt.AppendLine(await GetReferenceContentAsync(reference));
            prompt.AppendLine("```");
            prompt.AppendLine();
        }
        
        return prompt.ToString();
    }
}
```

**Integration:**
```csharp
// In SendUserMessage()
private async Task SendUserMessage()
{
    // ... existing code ...
    
    // Build context from references instead of old method
    string codeContext = await _contextReferenceService.BuildContextPromptAsync();
    
    // Send to AI with context
    string response = await _ollamaService.GenerateStreamingChatResponseAsync(
        userMessage,
        token => { /* ... */ },
        systemPrompt,
        codeContext  // Now includes all context references
    );
    
    // ... rest of code ...
}
```

---

## ?? Phase 5.7: Implement Total Changes / Keep / Undo Feature

**Priority:** ?? MEDIUM-HIGH  
**Time:** 2-3 hours  
**Impact:** Critical safety feature, matches Copilot UX

### Goal:
Implement the functionality for Phase 5.5.3's change tracking UI

### Features to Implement:

#### 5.7.1: Change Tracking System (1 hour)
- Track all pending changes (modified, created, deleted)
- Integrate with CodeModificationService
- Store original state for rollback
- Generate change summaries

#### 5.7.2: Visual Studio Diff Integration (1 hour)
- Show pending changes as diffs in VS
- Keep diff windows open until "Keep" clicked
- Sync with change tracking service
- Handle multiple diffs simultaneously

#### 5.7.3: Keep/Undo Operations (45 minutes)
- Implement "Keep" - finalize changes
- Implement "Undo" - revert changes
- Implement "Keep All" - finalize all pending
- Implement "Undo All" - revert all pending
- Clean up diff windows on keep/undo

**Files to Implement:**
- `Services/ChangeTrackingService.cs` - Track pending changes
- `Services/VSDiffService.cs` - VS diff integration
- `Controls/TotalChangesControl.xaml.cs` - Wire up UI events

**Implementation Details:**
```csharp
public class ChangeTrackingService
{
    private List<PendingChange> _pendingChanges = new List<PendingChange>();
    
    public async Task<PendingChange> TrackModificationAsync(string filePath, string originalContent, string newContent)
    {
        var change = new PendingChange
        {
            FilePath = filePath,
            Type = PendingChangeType.Modified,
            OriginalContent = originalContent,
            NewContent = newContent,
            CreatedAt = DateTime.Now,
            IsKept = false
        };
        
        _pendingChanges.Add(change);
        
        // Show diff in VS
        await _vsDiffService.ShowDiffAsync(change);
        
        return change;
    }
    
    public async Task KeepChangeAsync(PendingChange change)
    {
        // Apply the change permanently
        await File.WriteAllTextAsync(change.FilePath, change.NewContent);
        
        // Close diff window
        await _vsDiffService.CloseDiffAsync(change);
        
        // Mark as kept
        change.IsKept = true;
        
        // Remove from pending
        _pendingChanges.Remove(change);
    }
    
    public async Task UndoChangeAsync(PendingChange change)
    {
        // Revert to original
        if (change.Type == PendingChangeType.Modified)
        {
            await File.WriteAllTextAsync(change.FilePath, change.OriginalContent);
        }
        else if (change.Type == PendingChangeType.Created)
        {
            File.Delete(change.FilePath);
        }
        else if (change.Type == PendingChangeType.Deleted)
        {
            await File.WriteAllTextAsync(change.FilePath, change.OriginalContent);
        }
        
        // Close diff window
        await _vsDiffService.CloseDiffAsync(change);
        
        // Remove from pending
        _pendingChanges.Remove(change);
    }
    
    public async Task KeepAllChangesAsync()
    {
        foreach (var change in _pendingChanges.ToList())
        {
            await KeepChangeAsync(change);
        }
    }
    
    public async Task UndoAllChangesAsync()
    {
        foreach (var change in _pendingChanges.ToList())
        {
            await UndoChangeAsync(change);
        }
    }
}
```

**VS Diff Integration:**
```csharp
public class VSDiffService
{
    public async Task ShowDiffAsync(PendingChange change)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        
        var diffService = await VS.GetServiceAsync<SVsDifferenceService, IVsDifferenceService>();
        
        // Create temp file for original
        var tempOriginal = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempOriginal, change.OriginalContent);
        
        // Show diff
        diffService.OpenComparisonWindow2(
            tempOriginal,
            change.FilePath,
            "Original",
            "Proposed Change",
            "Pending AI Change",
            null,
            0
        );
        
        // Track diff window for cleanup
        _openDiffs[change] = tempOriginal;
    }
    
    public async Task CloseDiffAsync(PendingChange change)
    {
        // Close diff window
        // Clean up temp file
        if (_openDiffs.TryGetValue(change, out var tempFile))
        {
            File.Delete(tempFile);
            _openDiffs.Remove(change);
        }
    }
}
```

**Integration with Agent Mode:**
```csharp
// In SendUserMessage() after AI responds
if (_modeManager.IsAgentMode && responseChatMessage.HasCodeBlocks)
{
    var codeEdit = await _codeModService.CreateCodeEditFromResponseAsync(fullResponse, codeContext);
    if (codeEdit != null)
    {
        // Instead of applying immediately, track as pending change
        var pendingChange = await _changeTrackingService.TrackModificationAsync(
            codeEdit.FilePath,
            codeEdit.OriginalCode,
            codeEdit.ModifiedCode
        );
        
        responseChatMessage.AssociatedChange = pendingChange;
        responseChatMessage.IsApplicable = true;
    }
}
```

---

## ?? Phases 5.5-5.7 Summary

| Phase | Focus | Time | Priority | Implements |
|-------|-------|------|----------|------------|
| 5.5 | UI Layout | 2h 15m | HIGH | Copilot-style UI |
| 5.6 | Context Feature | 2-3h | MED-HIGH | Context refs functionality |
| 5.7 | Change Tracking | 2-3h | MED-HIGH | Keep/Undo functionality |
| **Total** | **UI/UX Polish** | **6-8h** | **HIGH** | **GitHub Copilot parity** |

---

## ?? After Phases 5.5-5.7

### UI/UX Will Match GitHub Copilot:
- ? Conversation management header
- ? Context reference chips
- ? Add context "+" button
- ? Total changes display
- ? Keep/Undo workflow
- ? Proper ENTER behavior
- ? Clean, focused layout

### Ready for Phase 6: True Agentic Behavior
With professional UI in place, implement:
- File creation/deletion
- Multi-file operations
- Project management
- Action parsing
- Task planning

---

**Status:** Phases 5.5-5.7 planned and documented  
**Next:** Implement Phase 5.5.1 (Conversation Header)  
**Timeline:** 6-8 hours for full UI/UX overhaul  

**Ready to start Phase 5.5?**
