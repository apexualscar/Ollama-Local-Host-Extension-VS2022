# ?? Phase 5.3 Quick Test Guide

## ? Agent Mode - Test Now!

### Test 1: Basic Agent Mode (2 min)
1. Open extension (Ctrl+Shift+O)
2. Switch to **Agent** mode
3. Type: "Create a complete C# hello world program"
4. **Expected Result:**
   - Complete program with using statements
   - Full namespace and class
   - Complete Main method
   - **[Apply]** button visible

---

### Test 2: Refactor Command (2 min)
1. Open a C# file
2. Select any method
3. Right-click ? **Refactor with Ollama**
4. **Expected Result:**
   - Switches to Agent mode
   - Returns COMPLETE refactored code
   - No "..." or "rest of code" comments
   - **[Apply]** button appears

---

### Test 3: Apply Button (1 min)
1. Get AI response with code (from Test 1 or 2)
2. Click **[Apply]** button
3. **Expected Result:**
   - Diff preview dialog opens
   - Shows side-by-side comparison
   - Can switch to unified view
   - "Accept" and "Reject" buttons work

---

### Test 4: Code Completeness (3 min)
1. Switch to Agent mode
2. Ask: "Convert this to async: `public string GetData() { return \"data\"; }`"
3. **Check Response Has:**
   - ? Using statements (System.Threading.Tasks)
   - ? Complete class structure
   - ? Full method body
   - ? Return statement
   - ? No ellipsis (...)
   - ? No "// rest here" comments

---

## ? Signs Something's Wrong

### Bad Response Example:
```csharp
public async Task<string> GetDataAsync()
{
    // ... implementation here
    return result;
}
```
? This means AI didn't follow the new prompt

### Good Response Example:
```csharp
using System.Threading.Tasks;

namespace MyApp
{
    public class DataService
    {
        public async Task<string> GetDataAsync()
        {
            await Task.Delay(100);
            return "data";
        }
    }
}
```
? This is complete and usable!

---

## ?? Quick Verification

**All should be ?:**
- [ ] Agent mode switch works
- [ ] AI returns complete code (no ...)
- [ ] Apply button appears on code blocks
- [ ] Clicking Apply opens diff preview
- [ ] Accept applies changes to editor
- [ ] Context menu Refactor command works

---

## ?? Test Phrases

Try these in Agent mode:

1. **"Add dependency injection to this class"**
   - Should return complete class with DI

2. **"Make this method async"**
   - Should return complete async version

3. **"Add error handling"**
   - Should return complete code with try/catch

4. **"Convert to LINQ"**
   - Should return complete LINQ version

---

## ?? Total Test Time: ~8 minutes

All tests should pass! If any fail, check:
1. Is Ollama running?
2. Is model selected (Qwen)?
3. Is Agent mode active?
4. Does `Services/ModeManager.cs` have updated prompt?

---

**Ready to test?** Start with Test 1! ??
