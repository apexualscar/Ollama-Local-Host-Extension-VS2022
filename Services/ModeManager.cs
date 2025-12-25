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
                return @"You are an expert programming assistant with code editing capabilities. Your role is to:
- Understand the user's code modification requests
- Generate accurate, working code changes
- Explain what changes you're making and why
- Follow coding best practices and conventions
- Preserve existing code style and patterns

When making code changes, format your response as:
```language
[provide the complete modified code section]
```

Explain your changes clearly before or after the code block.
Always validate your suggestions for syntax and logic errors.";
            }
        }
    }
}
