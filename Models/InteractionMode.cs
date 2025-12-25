namespace OllamaLocalHostIntergration.Models
{
    /// <summary>
    /// Defines the interaction modes for the Ollama assistant
    /// </summary>
    public enum InteractionMode
    {
        /// <summary>
        /// Ask mode: Read-only Q&A about code, explanations, and guidance
        /// </summary>
        Ask,

        /// <summary>
        /// Agent mode: Active code editing and modification capabilities
        /// </summary>
        Agent
    }
}
