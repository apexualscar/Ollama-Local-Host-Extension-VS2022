# ?? Phase 5.4: Template UI Cleanup - Implementation Plan

## Goal
Remove template dropdown from toolbar to prepare for agentic controls.

---

## Why This Change?

### Current State:
- Template dropdown clutters toolbar
- Takes up valuable horizontal space
- Mixes different UX patterns (dropdown vs. context menu)
- Will conflict with future agent control buttons

### Target State:
- Clean, focused toolbar
- Templates accessible via context menu
- Consistent UX (all code actions in context menu)
- Space reserved for agent action buttons

---

## Changes Required

### 1. Remove Template Dropdown from XAML ?
**File:** `ToolWindows/MyToolWindowControl.xaml`

**Remove:**
- `comboTemplates` ComboBox
- Associated Grid column
- Template icon/label

### 2. Remove Template Code from Code-Behind ?
**File:** `ToolWindows/MyToolWindowControl.xaml.cs`

**Remove:**
- `LoadTemplates()` method
- `ComboTemplates_SelectionChanged` event handler
- Template initialization in constructor

### 3. Keep Template Service ?
**File:** `Services/TemplateService.cs`

**Keep intact** - Will be used by context menu commands

### 4. Add Template Commands to Context Menu ?
**File:** `VSCommandTable.vsct`

**Add:**
- Templates submenu
- Individual template commands
- Proper command grouping

---

## Implementation Steps

### Step 1: Update XAML (5 minutes)

Remove template dropdown from toolbar:

```xaml
<!-- REMOVE THIS ENTIRE SECTION -->
<Grid Grid.Column="X">
    <TextBlock Text="Templates"/>
    <ComboBox x:Name="comboTemplates" ...>
    </ComboBox>
</Grid>
```

Update Grid column definitions to remove template column.

### Step 2: Update Code-Behind (5 minutes)

Remove template-related methods:

```csharp
// REMOVE:
private void LoadTemplates() { ... }
private async void ComboTemplates_SelectionChanged(...) { ... }

// REMOVE from constructor:
LoadTemplates();
```

### Step 3: Create Template Commands (10 minutes)

**Create:** `Commands/TemplateCommands/` directory

Commands to create:
1. `GenerateUnitTestsCommand.cs`
2. `AddLoggingCommand.cs`
3. `AddErrorHandlingCommand.cs`
4. `CreateInterfaceCommand.cs`
5. `GenerateDocumentationCommand.cs`
6. `OptimizePerformanceCommand.cs`
7. `AddValidationCommand.cs`
8. `CreateDTOCommand.cs`
9. `AddCachingCommand.cs`
10. `RefactorToAsyncCommand.cs`

**Base Template:**
```csharp
[Command(PackageIds.GenerateUnitTestsCommandId)]
internal sealed class GenerateUnitTestsCommand : BaseCommand<GenerateUnitTestsCommand>
{
    protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        
        try
        {
            var codeEditorService = new CodeEditorService();
            string selectedText = await codeEditorService.GetSelectedTextAsync();
            
            if (string.IsNullOrEmpty(selectedText))
            {
                await VS.MessageBox.ShowAsync(
                    "Ollama Copilot",
                    "Please select code to generate tests for.",
                    OLEMSGICON.OLEMSGICON_INFO,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK
                );
                return;
            }
            
            var templateService = new TemplateService();
            string prompt = templateService.BuildPromptFromTemplate("generate-unit-tests", selectedText);
            
            var window = await MyToolWindow.ShowAsync();
            if (window?.Content is MyToolWindowControl control)
            {
                // Send prompt to chat with template's recommended mode
                // This will be handled by the control
            }
        }
        catch (Exception ex)
        {
            await VS.MessageBox.ShowErrorAsync(
                "Ollama Copilot Error",
                $"Failed to apply template: {ex.Message}"
            );
        }
    }
}
```

### Step 4: Update VSCommandTable.vsct

Add templates to context menu:

```xml
<!-- Template Menu Group -->
<Group guid="guidOllamaLocalHostIntergrationPackageCmdSet" 
       id="TemplatesMenuGroup" 
       priority="0x0400">
  <Parent guid="guidOllamaLocalHostIntergrationPackageCmdSet" 
          id="EditorContextMenu"/>
</Group>

<!-- Templates Submenu -->
<Menu guid="guidOllamaLocalHostIntergrationPackageCmdSet" 
      id="TemplatesSubMenu" 
      priority="0x0100" 
      type="Menu">
  <Parent guid="guidOllamaLocalHostIntergrationPackageCmdSet" 
          id="TemplatesMenuGroup"/>
  <Strings>
    <ButtonText>Code Templates</ButtonText>
    <CommandName>Code Templates</CommandName>
  </Strings>
</Menu>

<!-- Template Commands -->
<Button guid="guidOllamaLocalHostIntergrationPackageCmdSet" 
        id="GenerateUnitTestsCommandId" 
        priority="0x0100">
  <Parent guid="guidOllamaLocalHostIntergrationPackageCmdSet" 
          id="TemplatesSubMenu"/>
  <Strings>
    <ButtonText>Generate Unit Tests</ButtonText>
    <CommandName>Generate Unit Tests</CommandName>
  </Strings>
</Button>

<!-- Additional template commands... -->
```

---

## Testing Checklist

### After Implementation:

- [ ] Toolbar is cleaner (no template dropdown)
- [ ] Extension still builds successfully
- [ ] Right-click context menu shows "Code Templates"
- [ ] Templates submenu contains all 10 templates
- [ ] Selecting template opens tool window
- [ ] Template applies correctly in chat
- [ ] Mode switches appropriately (Ask/Agent)
- [ ] All existing functionality still works

---

## Benefits

### Immediate:
? Cleaner toolbar  
? More screen real estate  
? Consistent UX pattern  
? Prepares for agent controls  

### Future:
? Space for "Execute Plan" button  
? Space for "Review Actions" button  
? Space for agent status indicator  
? Professional appearance  

---

## Time Estimate

| Step | Time |
|------|------|
| Remove XAML | 5 min |
| Update code-behind | 5 min |
| Create template commands | 10 min |
| Update VSCommandTable | 5 min |
| **Total** | **25 min** |

---

## Next Steps

1. **Implement Phase 5.4** (this phase)
2. **Test thoroughly**
3. **Begin Phase 6.1** (File Creation Service)
4. **Add agent action buttons to toolbar**

---

**Status:** Ready to implement  
**Priority:** MEDIUM  
**Blocking:** Phase 6 (need toolbar space)  

**Shall I proceed with implementation?**
