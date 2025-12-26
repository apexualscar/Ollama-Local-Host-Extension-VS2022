using System.Collections.Generic;
using System.Linq;
using OllamaLocalHostIntergration.Models;

namespace OllamaLocalHostIntergration.Services
{
    /// <summary>
    /// Service for managing code generation templates
    /// </summary>
    public class TemplateService
    {
        private readonly Dictionary<string, CodeTemplate> _templates;

        public TemplateService()
        {
            _templates = new Dictionary<string, CodeTemplate>();
            LoadBuiltInTemplates();
        }

        /// <summary>
        /// Loads built-in code templates
        /// </summary>
        private void LoadBuiltInTemplates()
        {
            // Unit Testing
            _templates["unittest"] = new CodeTemplate
            {
                Id = "unittest",
                Name = "Generate Unit Tests",
                Description = "Creates comprehensive unit tests for selected code",
                Category = "Testing",
                Icon = "&#xE7C0;",
                RequiresCodeSelection = true,
                RecommendedMode = InteractionMode.Agent,
                SystemPrompt = "You are an expert at writing unit tests. Generate comprehensive unit tests using xUnit, NUnit, or MSTest as appropriate. Include edge cases, error scenarios, and mock objects where needed. Follow testing best practices.",
                UserPromptTemplate = "Generate unit tests for this code:\n\n{code}\n\nInclude:\n- Happy path tests\n- Edge case tests\n- Error handling tests\n- Mock setup if needed"
            };

            // Documentation
            _templates["documentation"] = new CodeTemplate
            {
                Id = "documentation",
                Name = "Generate Documentation",
                Description = "Creates XML documentation comments",
                Category = "Documentation",
                Icon = "&#xE8A5;",
                RequiresCodeSelection = true,
                RecommendedMode = InteractionMode.Agent,
                SystemPrompt = "You are an expert at writing clear, comprehensive code documentation. Generate XML documentation comments following .NET conventions. Include <summary>, <param>, <returns>, <exception>, and <example> tags where appropriate.",
                UserPromptTemplate = "Generate XML documentation for this code:\n\n{code}\n\nInclude clear descriptions, parameter explanations, return value details, and usage examples."
            };

            // Logging
            _templates["logging"] = new CodeTemplate
            {
                Id = "logging",
                Name = "Add Logging",
                Description = "Adds comprehensive logging statements",
                Category = "Debugging",
                Icon = "&#xE7C3;",
                RequiresCodeSelection = true,
                RecommendedMode = InteractionMode.Agent,
                SystemPrompt = "You are an expert at adding appropriate logging to code. Use ILogger pattern. Add Information logs for key operations, Warning logs for potential issues, and Error logs with try-catch blocks for exceptions. Include relevant context in log messages.",
                UserPromptTemplate = "Add comprehensive logging to this code:\n\n{code}\n\nInclude:\n- Entry/exit logging for methods\n- Information logs for key operations\n- Warning logs for edge cases\n- Error logs with exception handling"
            };

            // Async Conversion
            _templates["async"] = new CodeTemplate
            {
                Id = "async",
                Name = "Convert to Async",
                Description = "Converts synchronous code to async/await",
                Category = "Refactoring",
                Icon = "&#xE895;",
                RequiresCodeSelection = true,
                RecommendedMode = InteractionMode.Agent,
                SystemPrompt = "You are an expert at async/await patterns in C#. Convert synchronous code to use async/await properly. Update method signatures to return Task/Task<T>, use ConfigureAwait(false) for library code, handle cancellation tokens, and follow async best practices.",
                UserPromptTemplate = "Convert this code to use async/await:\n\n{code}\n\nEnsure:\n- Proper async method signatures\n- ConfigureAwait usage\n- Cancellation token support\n- Async naming conventions"
            };

            // Error Handling
            _templates["errorhandling"] = new CodeTemplate
            {
                Id = "errorhandling",
                Name = "Add Error Handling",
                Description = "Adds comprehensive error handling",
                Category = "Quality",
                Icon = "&#xE783;",
                RequiresCodeSelection = true,
                RecommendedMode = InteractionMode.Agent,
                SystemPrompt = "You are an expert at defensive programming and error handling. Add try-catch blocks, input validation, null checks, and meaningful error messages. Handle specific exception types appropriately and include logging.",
                UserPromptTemplate = "Add comprehensive error handling to this code:\n\n{code}\n\nInclude:\n- Input validation\n- Null checks\n- Try-catch blocks\n- Specific exception handling\n- Meaningful error messages"
            };

            // Performance Optimization
            _templates["performance"] = new CodeTemplate
            {
                Id = "performance",
                Name = "Optimize Performance",
                Description = "Suggests performance improvements",
                Category = "Optimization",
                Icon = "&#xE9F3;",
                RequiresCodeSelection = true,
                RecommendedMode = InteractionMode.Ask,
                SystemPrompt = "You are an expert at code performance optimization. Analyze code for performance bottlenecks, suggest improvements like caching, lazy loading, efficient algorithms, and LINQ optimization. Explain the performance impact of each suggestion.",
                UserPromptTemplate = "Analyze this code for performance and suggest optimizations:\n\n{code}\n\nFocus on:\n- Algorithmic efficiency\n- Memory usage\n- LINQ optimization\n- Caching opportunities\n- Database queries"
            };

            // Security Review
            _templates["security"] = new CodeTemplate
            {
                Id = "security",
                Name = "Security Review",
                Description = "Identifies security vulnerabilities",
                Category = "Security",
                Icon = "&#xE72E;",
                RequiresCodeSelection = true,
                RecommendedMode = InteractionMode.Ask,
                SystemPrompt = "You are a security expert. Analyze code for security vulnerabilities including SQL injection, XSS, CSRF, insecure deserialization, authentication issues, and data exposure. Provide specific recommendations.",
                UserPromptTemplate = "Perform a security review of this code:\n\n{code}\n\nCheck for:\n- Injection vulnerabilities\n- Authentication/authorization issues\n- Data exposure\n- Insecure practices\n- OWASP Top 10 issues"
            };

            // Code Review
            _templates["codereview"] = new CodeTemplate
            {
                Id = "codereview",
                Name = "Code Review",
                Description = "Comprehensive code review",
                Category = "Quality",
                Icon = "&#xE8FD;",
                RequiresCodeSelection = true,
                RecommendedMode = InteractionMode.Ask,
                SystemPrompt = "You are an experienced code reviewer. Provide a comprehensive review covering code quality, maintainability, best practices, potential bugs, and design patterns. Be constructive and specific.",
                UserPromptTemplate = "Perform a comprehensive code review:\n\n{code}\n\nEvaluate:\n- Code quality and readability\n- Best practices adherence\n- Potential bugs\n- Design patterns\n- SOLID principles"
            };

            // Interface Implementation
            _templates["interface"] = new CodeTemplate
            {
                Id = "interface",
                Name = "Implement Interface",
                Description = "Generates interface implementation",
                Category = "Code Generation",
                Icon = "&#xE943;",
                RequiresCodeSelection = true,
                RecommendedMode = InteractionMode.Agent,
                SystemPrompt = "You are an expert at interface implementation. Generate a complete implementation with proper error handling, validation, and documentation. Follow SOLID principles.",
                UserPromptTemplate = "Generate a complete implementation for this interface:\n\n{code}\n\nInclude:\n- All interface members\n- Proper error handling\n- Input validation\n- XML documentation\n- Constructor with dependencies"
            };

            // Design Pattern
            _templates["designpattern"] = new CodeTemplate
            {
                Id = "designpattern",
                Name = "Apply Design Pattern",
                Description = "Applies appropriate design pattern",
                Category = "Architecture",
                Icon = "&#xE8B7;",
                RequiresCodeSelection = false,
                RecommendedMode = InteractionMode.Agent,
                SystemPrompt = "You are an expert in design patterns. Suggest and implement appropriate design patterns to solve the given problem. Explain why the pattern is suitable and how it improves the code.",
                UserPromptTemplate = "Suggest and implement an appropriate design pattern for: {description}\n\nConsider patterns like:\n- Singleton, Factory, Builder\n- Strategy, Observer, Command\n- Repository, Unit of Work\n- Dependency Injection"
            };
        }

        /// <summary>
        /// Gets all available templates
        /// </summary>
        public List<CodeTemplate> GetAllTemplates()
        {
            return _templates.Values.ToList();
        }

        /// <summary>
        /// Gets templates by category
        /// </summary>
        public List<CodeTemplate> GetTemplatesByCategory(string category)
        {
            return _templates.Values.Where(t => t.Category == category).ToList();
        }

        /// <summary>
        /// Gets all unique categories
        /// </summary>
        public List<string> GetCategories()
        {
            return _templates.Values.Select(t => t.Category).Distinct().OrderBy(c => c).ToList();
        }

        /// <summary>
        /// Gets a specific template by ID
        /// </summary>
        public CodeTemplate GetTemplate(string id)
        {
            return _templates.TryGetValue(id, out var template) ? template : null;
        }

        /// <summary>
        /// Builds a prompt from a template
        /// </summary>
        public string BuildPromptFromTemplate(string templateId, string code = null, string description = null)
        {
            var template = GetTemplate(templateId);
            if (template == null)
                return null;

            string prompt = template.UserPromptTemplate;

            if (!string.IsNullOrEmpty(code))
            {
                prompt = prompt.Replace("{code}", code);
            }

            if (!string.IsNullOrEmpty(description))
            {
                prompt = prompt.Replace("{description}", description);
            }

            return prompt;
        }

        /// <summary>
        /// Checks if a template can be applied to the current selection
        /// </summary>
        public bool CanApplyTemplate(string templateId, bool hasCodeSelection)
        {
            var template = GetTemplate(templateId);
            if (template == null)
                return false;

            return !template.RequiresCodeSelection || hasCodeSelection;
        }
    }
}
