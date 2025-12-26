using System;

namespace OllamaLocalHostIntergration.Models
{
    /// <summary>
    /// Represents a context reference type
    /// </summary>
    public enum ContextReferenceType
    {
        /// <summary>
        /// File from solution
        /// </summary>
        File,
        
        /// <summary>
        /// Current editor selection
        /// </summary>
        Selection,
        
        /// <summary>
        /// Specific method
        /// </summary>
        Method,
        
        /// <summary>
        /// Specific class
        /// </summary>
        Class,
        
        /// <summary>
        /// Entire solution
        /// </summary>
        Solution,
        
        /// <summary>
        /// Specific project
        /// </summary>
        Project
    }

    /// <summary>
    /// Represents a context reference that can be added to the chat
    /// </summary>
    public class ContextReference
    {
        public Guid Id { get; set; }
        public ContextReferenceType Type { get; set; }
        public string DisplayText { get; set; }
        public string FilePath { get; set; }
        public string ClassName { get; set; }
        public string MethodName { get; set; }
        public string ProjectName { get; set; }
        public string Content { get; set; }
        public int TokenCount { get; set; }
        public DateTime CreatedAt { get; set; }

        public ContextReference()
        {
            Id = Guid.NewGuid();
            CreatedAt = DateTime.Now;
        }

        /// <summary>
        /// Gets an icon for the context reference type
        /// </summary>
        public string Icon
        {
            get
            {
                return Type switch
                {
                    ContextReferenceType.File => "\uE8A5",        // Document icon
                    ContextReferenceType.Selection => "\uE8C8",    // Edit icon
                    ContextReferenceType.Method => "\uE8E3",       // Method icon
                    ContextReferenceType.Class => "\uE8D3",        // Class icon
                    ContextReferenceType.Solution => "\uE8F0",     // Solution icon
                    ContextReferenceType.Project => "\uE8F1",      // Project icon
                    _ => "\uE8A5"
                };
            }
        }

        /// <summary>
        /// Gets a short display text with token count
        /// </summary>
        public string ShortDisplay => $"{DisplayText} (~{TokenCount} tokens)";
    }
}
