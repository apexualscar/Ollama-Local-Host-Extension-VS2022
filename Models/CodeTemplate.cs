namespace OllamaLocalHostIntergration.Models
{
    /// <summary>
    /// Represents a code generation template
    /// </summary>
    public class CodeTemplate
    {
        /// <summary>
        /// Unique identifier for the template
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Display name for the template
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Brief description of what the template does
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// System prompt to use with this template
        /// </summary>
        public string SystemPrompt { get; set; }

        /// <summary>
        /// User prompt template (can include {code} placeholder)
        /// </summary>
        public string UserPromptTemplate { get; set; }

        /// <summary>
        /// Icon code for UI display (Segoe MDL2 Assets)
        /// </summary>
        public string Icon { get; set; }

        /// <summary>
        /// Category for grouping templates
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// Whether this template requires code selection
        /// </summary>
        public bool RequiresCodeSelection { get; set; }

        /// <summary>
        /// Recommended mode for this template (Ask or Agent)
        /// </summary>
        public InteractionMode RecommendedMode { get; set; }
    }
}
