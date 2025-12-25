using System;
using System.Text;
using OllamaLocalHostIntergration.Models;

namespace OllamaLocalHostIntergration.Services
{
    /// <summary>
    /// Service for building context-aware prompts for different scenarios
    /// </summary>
    public class PromptBuilder
    {
        private const int MaxContextLength = 8000; // Approximate token limit consideration

        /// <summary>
        /// Builds a prompt for asking questions about code
        /// </summary>
        public string BuildAskPrompt(string question, string codeContext = "", string language = "")
        {
            var prompt = new StringBuilder();
            
            if (!string.IsNullOrEmpty(codeContext))
            {
                string langInfo = !string.IsNullOrEmpty(language) ? $" ({language})" : "";
                prompt.AppendLine($"Code context{langInfo}:");
                prompt.AppendLine("```");
                prompt.AppendLine(TruncateContext(codeContext));
                prompt.AppendLine("```");
                prompt.AppendLine();
            }

            prompt.AppendLine($"Question: {question}");
            
            return prompt.ToString();
        }

        /// <summary>
        /// Builds a prompt for agent mode code modifications
        /// </summary>
        public string BuildAgentPrompt(string request, string codeContext, string language = "", string additionalContext = "")
        {
            var prompt = new StringBuilder();
            
            prompt.AppendLine($"I need you to modify the following code according to this request:");
            prompt.AppendLine();
            prompt.AppendLine($"**Request:** {request}");
            prompt.AppendLine();
            
            if (!string.IsNullOrEmpty(language))
            {
                prompt.AppendLine($"**Language:** {language}");
                prompt.AppendLine();
            }

            if (!string.IsNullOrEmpty(additionalContext))
            {
                prompt.AppendLine($"**Additional Context:**");
                prompt.AppendLine(additionalContext);
                prompt.AppendLine();
            }

            prompt.AppendLine("**Current Code:**");
            prompt.AppendLine("```");
            prompt.AppendLine(TruncateContext(codeContext));
            prompt.AppendLine("```");
            prompt.AppendLine();
            prompt.AppendLine("Please provide the complete modified code in a code block, and explain your changes.");
            
            return prompt.ToString();
        }

        /// <summary>
        /// Builds a prompt for code explanation
        /// </summary>
        public string BuildExplainPrompt(string code, string language = "", string specificQuestion = "")
        {
            var prompt = new StringBuilder();
            
            string langInfo = !string.IsNullOrEmpty(language) ? $" ({language})" : "";
            prompt.AppendLine($"Please explain the following code{langInfo}:");
            prompt.AppendLine("```");
            prompt.AppendLine(TruncateContext(code));
            prompt.AppendLine("```");
            
            if (!string.IsNullOrEmpty(specificQuestion))
            {
                prompt.AppendLine();
                prompt.AppendLine($"Specifically: {specificQuestion}");
            }
            
            return prompt.ToString();
        }

        /// <summary>
        /// Builds a prompt for refactoring suggestions
        /// </summary>
        public string BuildRefactoringPrompt(string code, string language = "", string focusArea = "")
        {
            var prompt = new StringBuilder();
            
            string langInfo = !string.IsNullOrEmpty(language) ? $" ({language})" : "";
            prompt.AppendLine($"Please suggest refactoring improvements for the following code{langInfo}:");
            prompt.AppendLine("```");
            prompt.AppendLine(TruncateContext(code));
            prompt.AppendLine("```");
            prompt.AppendLine();
            
            if (!string.IsNullOrEmpty(focusArea))
            {
                prompt.AppendLine($"Focus on: {focusArea}");
            }
            else
            {
                prompt.AppendLine("Focus on:");
                prompt.AppendLine("- Code readability");
                prompt.AppendLine("- Performance optimization");
                prompt.AppendLine("- Best practices");
                prompt.AppendLine("- Maintainability");
            }
            
            return prompt.ToString();
        }

        /// <summary>
        /// Builds a prompt for finding code issues
        /// </summary>
        public string BuildFindIssuesPrompt(string code, string language = "")
        {
            var prompt = new StringBuilder();
            
            string langInfo = !string.IsNullOrEmpty(language) ? $" ({language})" : "";
            prompt.AppendLine($"Please analyze the following code{langInfo} for potential issues:");
            prompt.AppendLine("```");
            prompt.AppendLine(TruncateContext(code));
            prompt.AppendLine("```");
            prompt.AppendLine();
            prompt.AppendLine("Look for:");
            prompt.AppendLine("- Syntax errors");
            prompt.AppendLine("- Logic bugs");
            prompt.AppendLine("- Security vulnerabilities");
            prompt.AppendLine("- Performance issues");
            prompt.AppendLine("- Code smells");
            
            return prompt.ToString();
        }

        /// <summary>
        /// Builds a prompt for generating documentation
        /// </summary>
        public string BuildDocumentationPrompt(string code, string language = "", string documentationType = "")
        {
            var prompt = new StringBuilder();
            
            string langInfo = !string.IsNullOrEmpty(language) ? $" ({language})" : "";
            string docType = !string.IsNullOrEmpty(documentationType) ? documentationType : "comprehensive";
            
            prompt.AppendLine($"Please generate {docType} documentation for the following code{langInfo}:");
            prompt.AppendLine("```");
            prompt.AppendLine(TruncateContext(code));
            prompt.AppendLine("```");
            prompt.AppendLine();
            prompt.AppendLine("Include:");
            prompt.AppendLine("- Purpose and functionality");
            prompt.AppendLine("- Parameters and return values");
            prompt.AppendLine("- Usage examples");
            prompt.AppendLine("- Any important notes or warnings");
            
            return prompt.ToString();
        }

        /// <summary>
        /// Builds a prompt for generating code from requirements
        /// </summary>
        public string BuildGenerateCodePrompt(string requirements, string language = "csharp", string existingContext = "")
        {
            var prompt = new StringBuilder();
            
            prompt.AppendLine($"Generate {language} code based on these requirements:");
            prompt.AppendLine();
            prompt.AppendLine(requirements);
            prompt.AppendLine();
            
            if (!string.IsNullOrEmpty(existingContext))
            {
                prompt.AppendLine("Existing code context:");
                prompt.AppendLine("```");
                prompt.AppendLine(TruncateContext(existingContext));
                prompt.AppendLine("```");
                prompt.AppendLine();
            }
            
            prompt.AppendLine("Provide:");
            prompt.AppendLine("- Complete, working code");
            prompt.AppendLine("- Inline comments for complex logic");
            prompt.AppendLine("- Following best practices and conventions");
            
            return prompt.ToString();
        }

        /// <summary>
        /// Truncates context if it exceeds maximum length
        /// </summary>
        private string TruncateContext(string context)
        {
            if (string.IsNullOrEmpty(context))
                return context;

            if (context.Length <= MaxContextLength)
                return context;

            // Truncate with indicator
            return context.Substring(0, MaxContextLength) + 
                   "\n\n... [Context truncated due to length] ...";
        }

        /// <summary>
        /// Estimates token count (rough approximation)
        /// </summary>
        public int EstimateTokenCount(string text)
        {
            if (string.IsNullOrEmpty(text))
                return 0;

            // Rough estimation: 1 token ? 4 characters
            return text.Length / 4;
        }

        /// <summary>
        /// Builds a system prompt based on interaction mode
        /// </summary>
        public string BuildSystemPrompt(InteractionMode mode)
        {
            if (mode == InteractionMode.Ask)
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
