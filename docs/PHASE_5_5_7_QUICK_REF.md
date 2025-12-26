# ?? Phases 5.5-5.7 Quick Reference

## Overview
Transform extension UI to match GitHub Copilot before implementing agentic features.

---

## ? Phase 5.5: UI Cleanup (2h 15m)

### 5.5.1: Conversation Header (30m)
**Add:** `[Conversations ?] [+ New] [?? Delete]` at top  
**Move:** New/Delete buttons from bottom to top  
**Benefit:** Copilot-style conversation management  

### 5.5.2: Context References UI (45m)
**Add:** Context chip display above input  
**Add:** `[+ Add Context]` button with picker dialog  
**Types:** Files, Selection, Methods, Classes, Solution, Project  
**Remove:** Context from settings panel  
**Benefit:** Visual context management like Copilot  
**Note:** UI only - functionality in Phase 5.6  

### 5.5.3: Total Changes UI (45m)
**Add:** `?? 3 changes pending [Keep All] [Undo All]`  
**Show:** List of pending changes with individual actions  
**Integration:** VS diff viewer until kept  
**Benefit:** Safety net for mistakes like Copilot  
**Note:** UI only - functionality in Phase 5.7  

### 5.5.4: Input Improvements (15m)
**Change:** ENTER = send, SHIFT+ENTER = new line  
**Fix:** Refresh icon (not calculator)  
**Add:** Placeholder text  
**Benefit:** Standard chat UX  

---

## ?? Phase 5.6: Context Feature (2-3h)

Implement functionality for 5.5.2 context references:
- File browsing and selection
- Selection capture
- Method/class search
- Solution/project context
- Token counting
- Context prompt building

**Result:** Fully functional context reference system

---

## ?? Phase 5.7: Change Tracking (2-3h)

Implement functionality for 5.5.3 total changes:
- Track all pending changes
- Show VS diffs
- Keep/Undo operations
- Batch operations (Keep All / Undo All)
- Diff window management

**Result:** GitHub Copilot-style change workflow

---

## ?? Timeline

| Phase | Time | UI/Func | Priority |
|-------|------|---------|----------|
| 5.5.1 | 30m | UI | HIGH |
| 5.5.2 | 45m | UI | HIGH |
| 5.5.3 | 45m | UI | HIGH |
| 5.5.4 | 15m | UI | MEDIUM |
| 5.6 | 2-3h | Func | MED-HIGH |
| 5.7 | 2-3h | Func | MED-HIGH |
| **Total** | **6-8h** | **Both** | **HIGH** |

---

## ?? Before vs. After

### Before (Current):
```
??????????????????????????????
? [Ask?][Model?][??][?][??]   ?
?                            ?
? Chat Area                  ?
?                            ?
? [Input box]     [Send]     ?
??????????????????????????????
```

### After (Phase 5.5-5.7):
```
??????????????????????????????????
? [Conversations?][+New][??Delete] ? ? NEW
??????????????????????????????????
?                                ?
? Chat Area                      ?
?                                ?
??????????????????????????????????
? [Ask?][Model?][?]              ?
?                                ?
? Context: [??File][??Selection]  ? ? NEW
?          [+ Add Context]       ?
?                                ?
? ?? 3 changes [Keep All][Undo]  ? ? NEW
?                                ?
? [Input box]         [Send]     ?
??????????????????????????????????
```

---

## ?? Key Benefits

1. **Professional Look** - Matches GitHub Copilot
2. **Better Organization** - Logical grouping
3. **Safety Features** - Keep/Undo workflow
4. **Context Control** - Visual, intuitive
5. **User Confidence** - See what's in context, what's changing

---

**Next:** Start with Phase 5.5.1 (Conversation Header)  
**Goal:** GitHub Copilot-level UX before agentic features  
**Timeline:** 6-8 hours total  

See `docs/DEPLOYMENT_PLAN_UPDATED.md` for full details!
