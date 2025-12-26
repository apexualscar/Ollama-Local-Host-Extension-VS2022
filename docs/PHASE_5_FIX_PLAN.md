# ?? CRITICAL ISSUES - Updated Deployment Plan

## ?? Issues Identified

### ?? CRITICAL Issues:
1. **Wrong AI Model** - Calling ChatGPT instead of local Qwen
2. **Rich Text Not Displaying** - No boxes, code blocks broken
3. **Agent Mode Broken** - Just acts like Ask mode
4. **Template UI Messy** - Should only be context menu

---

## ?? Fix Priority Order

### Phase 5.1: Fix AI Model Connection (CRITICAL)
**Priority:** ?? URGENT  
**Time:** 15 minutes  
**Impact:** BLOCKER

**Problem:**
- Extension calling ChatGPT instead of local Ollama
- Should be using locally installed Qwen model

**Solution:**
1. Verify Ollama server address (should be localhost:11434)
2. Check model selection - ensure Qwen is selected
3. Verify no hardcoded external API calls
4. Test with `ollama list` to confirm Qwen is installed

**Files to Check:**
- `Services/OllamaService.cs` - Verify server address
- `ToolWindows/MyToolWindowControl.xaml.cs` - Check model selection
- No external API keys or endpoints

---

### Phase 5.2: Fix Rich Chat Display (CRITICAL)
**Priority:** ?? URGENT  
**Time:** 30 minutes  
**Impact:** CRITICAL

**Problem:**
- No visual separation between messages
- Code blocks not displaying in bordered boxes
- Missing Copy/Apply buttons
- Text just flowing without structure

**Root Cause:**
- `RichChatMessageControl` exists but not being used
- Current UI uses simple `TextBlock` in `ItemsControl`
- Need to switch to `RichChatMessageControl`

**Solution:**
1. Update `MyToolWindowControl.xaml` to use `RichChatMessageControl`
2. Ensure `MessageParserService` is extracting code blocks
3. Bind `CodeBlocks` property to UI
4. Test Copy/Apply buttons

**Files to Modify:**
- `ToolWindows/MyToolWindowControl.xaml` - Change ItemTemplate
- Verify `Controls/RichChatMessageControl.xaml` styling
- Ensure `ChatMessage.CodeBlocks` is populated

---

### Phase 5.3: Fix Agent Mode (HIGH PRIORITY)
**Priority:** ?? HIGH  
**Time:** 45 minutes  
**Impact:** HIGH

**Problem:**
- Agent mode behaves exactly like Ask mode
- No code editing capabilities
- Apply button never appears
- Diff preview not working

**Root Cause:**
- Mode switching works, but behavior is identical
- `CodeModificationService` not being called
- `AssociatedCodeEdit` not being created
- Agent system prompt not distinctive enough

**Solution:**
1. Strengthen Agent mode system prompt to generate code blocks
2. Ensure `CreateCodeEditFromResponseAsync` is called
3. Fix `IsApplicable` flag setting
4. Test diff preview dialog

**Files to Modify:**
- `Services/ModeManager.cs` - Improve Agent prompt
- `ToolWindows/MyToolWindowControl.xaml.cs` - Fix Agent flow
- `Services/CodeModificationService.cs` - Verify code edit creation

---

### Phase 5.4: Remove Template Dropdown (MEDIUM PRIORITY)
**Priority:** ?? MEDIUM  
**Time:** 20 minutes  
**Impact:** MEDIUM

**Problem:**
- Template dropdown clutters toolbar
- Should be context menu only
- Takes up valuable space

**Solution:**
1. Remove `comboTemplates` from toolbar
2. Add templates to editor context menu
3. Keep template service for context menu use
4. Add to existing Explain/Refactor/Find Issues menu

**Files to Modify:**
- `ToolWindows/MyToolWindowControl.xaml` - Remove dropdown
- `ToolWindows/MyToolWindowControl.xaml.cs` - Remove dropdown code
- `VSCommandTable.vsct` - Add template commands
- Create template command classes

---

## ?? Detailed Fix Plans

### Fix 5.1: AI Model Connection

#### Step 1: Diagnose Current Connection
```powershell
# Check Ollama status
ollama list

# Verify qwen is installed
ollama show qwen

# Test connection
curl http://localhost:11434/api/tags
```

#### Step 2: Verify Code
**Check OllamaService.cs:**
- Constructor defaults to `http://localhost:11434` ?
- Model set via `SetModel()` ?
- No external API calls ?

**Check MyToolWindowControl:**
- Server address from settings ?
- Model from dropdown ?

#### Step 3: Debug Model Selection
- Verify Qwen appears in model dropdown
- Ensure it's being set correctly
- Check conversation history is local

**Expected Fix:**
Model connection should work - likely user just needs to:
1. Ensure Ollama is running
2. Select Qwen from model dropdown
3. Verify server address is localhost:11434

---

### Fix 5.2: Rich Chat Display

#### Current Problem:
```xaml
<!-- Current (WRONG) - Simple TextBlock -->
<TextBlock Text="{Binding Content}" TextWrapping="Wrap"/>
```

#### Required Fix:
```xaml
<!-- Replace ItemTemplate with RichChatMessageControl -->
<ItemsControl.ItemTemplate>
    <DataTemplate>
        <local:RichChatMessageControl DataContext="{Binding}"/>
    </DataTemplate>
</ItemsControl.ItemTemplate>
```

#### Step-by-Step:
1. **Update MyToolWindowControl.xaml**
   - Remove simple `TextBlock` template
   - Add `RichChatMessageControl` reference
   - Use control in `ItemTemplate`

2. **Verify RichChatMessageControl**
   - Code blocks display in boxes ?
   - Copy button works ?
   - Apply button shows for Agent mode ?

3. **Test MessageParser**
   - Extracts code blocks correctly ?
   - Sets `HasCodeBlocks` flag ?
   - Populates `CodeBlocks` collection ?

---

### Fix 5.3: Agent Mode Behavior

#### Current Issue:
**Agent Mode System Prompt (WEAK):**
```csharp
"You are an expert programming assistant with code editing capabilities..."
```

This is too general - needs to be more directive.

#### Required Fix:
**Stronger Agent Prompt:**
```csharp
@"You are a code editing assistant. When the user asks you to modify code:

1. ALWAYS provide the COMPLETE modified code in a markdown code block
2. Format: ```language\n[complete code]\n```
3. Do NOT provide partial snippets
4. Do NOT use ellipsis (...) or omit code
5. Include ALL necessary imports, methods, and classes

Example response format:
Here's the refactored code with improvements:

```csharp
// Complete, working code here
public class Example {
    // Full implementation
}
```

Explanation of changes:
- Change 1: ...
- Change 2: ...
"
```

#### Step-by-Step:
1. **Update ModeManager.cs**
   - Strengthen Agent system prompt
   - Be explicit about code block format
   - Demand complete code, not snippets

2. **Update SendUserMessage in MyToolWindowControl**
   - After parsing response, check for code blocks
   - If Agent mode + has code blocks:
     - Create `CodeEdit` via `CodeModificationService`
     - Set `IsApplicable = true`
     - Add to pending edits

3. **Test Flow:**
   - Select code
   - Right-click ? Refactor
   - Verify: Switches to Agent mode
   - Verify: Response has code block
   - Verify: Apply button appears
   - Verify: Apply opens diff preview

---

### Fix 5.4: Template UI Cleanup

#### Remove from Toolbar:
```xaml
<!-- DELETE THIS -->
<ComboBox x:Name="comboTemplates" Grid.Column="2"...>
```

#### Add to Context Menu:
```xml
<!-- VSCommandTable.vsct -->
<Group guid="guidOllamaLocalHostIntergrationPackageCmdSet" 
       id="TemplatesMenuGroup" 
       priority="0x0300">
  <Parent guid="guidOllamaLocalHostIntergrationPackageCmdSet" 
          id="EditorContextMenu"/>
</Group>

<Menu guid="guidOllamaLocalHostIntergrationPackageCmdSet" 
      id="TemplatesSubMenu" 
      priority="0x0100" 
      type="Menu">
  <Parent guid="guidOllamaLocalHostIntergrationPackageCmdSet" 
          id="TemplatesMenuGroup"/>
  <Strings>
    <ButtonText>Code Templates</ButtonText>
  </Strings>
</Menu>

<!-- Template commands -->
<Button guid="guidOllamaLocalHostIntergrationPackageCmdSet" 
        id="GenerateUnitTestsCommandId" 
        priority="0x0100">
  <Parent guid="guidOllamaLocalHostIntergrationPackageCmdSet" 
          id="TemplatesSubMenu"/>
  <Strings>
    <ButtonText>Generate Unit Tests</ButtonText>
  </Strings>
</Button>
<!-- ... more template commands -->
```

---

## ?? Implementation Order

### Sprint 1: CRITICAL FIXES (Day 1)
**Goal:** Get basic functionality working

1. **Morning (2 hours):**
   - ? Fix 5.1: AI Model Connection (15 min)
   - ? Fix 5.2: Rich Chat Display (30 min)
   - ? Test basic chat with code blocks (15 min)
   - ? Verify Copy button works (15 min)
   - ? Document findings (15 min)

2. **Afternoon (2 hours):**
   - ? Fix 5.3: Agent Mode Behavior (45 min)
   - ? Test Apply button appears (15 min)
   - ? Test diff preview (15 min)
   - ? End-to-end Agent flow test (30 min)

### Sprint 2: UI CLEANUP (Day 2)
**Goal:** Polish UI and remove clutter

3. **Morning (2 hours):**
   - ? Fix 5.4: Remove template dropdown (20 min)
   - ? Add templates to context menu (60 min)
   - ? Test all template commands (30 min)

4. **Afternoon (1 hour):**
   - ? Final testing all features (30 min)
   - ? Update documentation (30 min)

---

## ?? Testing Checklist

### After Fix 5.1: AI Model
- [ ] Ollama server responds at localhost:11434
- [ ] Qwen model appears in dropdown
- [ ] Selecting Qwen works
- [ ] Messages go to local Ollama, not ChatGPT
- [ ] Responses are from Qwen

### After Fix 5.2: Rich Chat
- [ ] Messages have visual boxes/borders
- [ ] User messages have different styling than AI
- [ ] Code blocks appear in bordered boxes
- [ ] Code blocks have language header
- [ ] Copy button visible on all code blocks
- [ ] Copy button works
- [ ] Code has monospace font (Consolas)

### After Fix 5.3: Agent Mode
- [ ] Switching to Agent mode changes system prompt
- [ ] Agent responses contain complete code blocks
- [ ] Apply button appears on code blocks in Agent mode
- [ ] Apply button opens diff preview
- [ ] Diff preview shows old vs new code
- [ ] Accept applies code to editor
- [ ] Reject closes dialog without changes

### After Fix 5.4: Template UI
- [ ] Template dropdown removed from toolbar
- [ ] Toolbar is cleaner
- [ ] Context menu has "Code Templates" submenu
- [ ] All 10 templates in submenu
- [ ] Template commands work from context menu
- [ ] Templates only show with code selected

---

## ?? Quick Fixes Reference

### Fix Model Connection:
```csharp
// Verify in OllamaService constructor
public OllamaService(string serverAddress = "http://localhost:11434")

// Check model is being set
_ollamaService.SetModel("qwen");  // or qwen2, qwen:latest, etc.
```

### Fix Rich Chat Display:
```xaml
<!-- In MyToolWindowControl.xaml -->
<ItemsControl.ItemTemplate>
    <DataTemplate>
        <local:RichChatMessageControl/>
    </DataTemplate>
</ItemsControl.ItemTemplate>
```

### Fix Agent Mode Prompt:
```csharp
// In ModeManager.GetSystemPrompt()
if (CurrentMode == InteractionMode.Agent)
{
    return @"You are a code editing AI. 
    
CRITICAL: Always provide COMPLETE modified code in code blocks.
Format: ```language\n[COMPLETE code]\n```
NO partial snippets. NO ellipsis. NO omissions.";
}
```

---

## ?? Success Criteria

### All Fixes Complete When:
? Model connects to local Qwen  
? Chat displays with rich formatting  
? Code blocks in bordered boxes  
? Copy button works on all code  
? Agent mode generates complete code  
? Apply button appears in Agent mode  
? Diff preview works  
? Templates only in context menu  
? Toolbar is clean  
? All tests pass  

---

## ?? Next Steps

1. **Start with Fix 5.1** - Verify model connection
2. **Then Fix 5.2** - Get rich chat working
3. **Then Fix 5.3** - Fix Agent mode
4. **Finally Fix 5.4** - Clean up templates UI

**Total Time:** ~2 hours for critical fixes  
**Total Time Including Polish:** ~4 hours total

---

## ?? Notes

### Why This Order?
1. **Model first** - Nothing works without correct AI
2. **Display second** - Need to see responses properly
3. **Agent third** - Core feature, needs working display
4. **Templates last** - Nice to have, not critical

### After Fixes:
- Extension will work with local Qwen
- Chat will look like GitHub Copilot
- Agent mode will actually edit code
- UI will be cleaner and more focused

---

**Ready to start?** Begin with Phase 5.1: Fix AI Model Connection!
