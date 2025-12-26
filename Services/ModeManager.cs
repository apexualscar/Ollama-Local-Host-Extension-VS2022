using System;
using System.Collections.Generic;
using System.Linq;
using OllamaLocalHostIntergration.Models;

namespace OllamaLocalHostIntergration.Services
{
    /// <summary>
    /// Manages interaction modes and their behaviors
    /// </summary>
    public class ModeManager
    {
        private InteractionMode _currentMode;
        private readonly List<CodeEdit> _pendingEdits;

        public InteractionMode CurrentMode
        {
            get => _currentMode;
            set
            {
                if (_currentMode != value)
                {
                    _currentMode = value;
                    OnModeChanged?.Invoke(value);
                }
            }
        }

        public IReadOnlyList<CodeEdit> PendingEdits => _pendingEdits.AsReadOnly();

        public event Action<InteractionMode> OnModeChanged;

        public ModeManager()
        {
            _currentMode = InteractionMode.Ask;
            _pendingEdits = new List<CodeEdit>();
        }

        public bool IsAskMode => CurrentMode == InteractionMode.Ask;
        public bool IsAgentMode => CurrentMode == InteractionMode.Agent;

        public void SwitchToAskMode()
        {
            CurrentMode = InteractionMode.Ask;
        }

        public void SwitchToAgentMode()
        {
            CurrentMode = InteractionMode.Agent;
        }

        public void AddPendingEdit(CodeEdit edit)
        {
            if (edit != null)
            {
                _pendingEdits.Add(edit);
            }
        }

        public void RemovePendingEdit(CodeEdit edit)
        {
            _pendingEdits.Remove(edit);
        }

        public void ClearPendingEdits()
        {
            _pendingEdits.Clear();
        }

        public void MarkEditApplied(CodeEdit edit)
        {
            if (_pendingEdits.Contains(edit))
            {
                edit.Applied = true;
            }
        }

        public string GetSystemPrompt()
        {
            if (IsAskMode)
            {
                return @"You are an expert programming assistant. Your role is to:
- Answer questions about code clearly and concisely
- Explain programming concepts and best practices
- Provide guidance on debugging and optimization
- Suggest improvements WITHOUT modifying code directly
- Use code examples for illustration but mark them as examples

Format code examples in markdown code blocks with language tags.
Be helpful, accurate, and professional.";
            }
            else // Agent mode
            {
                return @"You are a code editing AI assistant. Your CRITICAL RULES:

1. ALWAYS provide COMPLETE, WORKING code in markdown code blocks
2. NEVER use ellipsis (...) or comments like '// rest of code here'
3. NEVER omit ANY part of the code
4. ALWAYS include ALL imports, methods, classes, and properties
5. Format as: ```language\n[COMPLETE CODE]\n```

RESPONSE FORMAT:
[Brief explanation of what you're changing]

```language
// COMPLETE, WORKING CODE HERE
// Include EVERYTHING - no shortcuts!
```

[Explanation of specific changes]

EXAMPLE (Good Response):
I'll refactor this method to use async/await:

```csharp
using System;
using System.Threading.Tasks;

public class Example
{
    public async Task<string> GetDataAsync()
    {
        await Task.Delay(1000);
        return ""Data loaded"";
    }
}
```

Changes made:
- Added async/await pattern
- Changed return type to Task<string>
- Added necessary using statements

NEVER DO THIS (Bad Response):
```csharp
public async Task<string> GetDataAsync()
{
    // ... rest of implementation
}
```

Remember: The user needs COMPLETE, compilable code to apply the changes!";
            }
        }
    }
}
