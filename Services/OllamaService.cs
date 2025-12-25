using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace OllamaLocalHostIntergration.Services
{
    public class OllamaService
    {
        private readonly HttpClient _httpClient;
        private string _serverAddress;
        private string _model = "codellama";
        private List<OllamaChatMessage> _conversationHistory;

        public OllamaService(string serverAddress = "http://localhost:11434")
        {
            _serverAddress = serverAddress;
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromMinutes(5); // Longer timeout for complex requests
            _conversationHistory = new List<OllamaChatMessage>();
        }

        public void UpdateServerAddress(string address)
        {
            _serverAddress = address;
        }

        public void SetModel(string model)
        {
            _model = model;
        }

        public void ClearConversationHistory()
        {
            _conversationHistory.Clear();
        }

        public async Task<List<string>> GetAvailableModelsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_serverAddress}/api/tags");
                response.EnsureSuccessStatusCode();
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var tagsResponse = JsonConvert.DeserializeObject<OllamaTagsResponse>(jsonResponse);
                var models = new List<string>();
                if (tagsResponse?.models != null)
                {
                    foreach (var model in tagsResponse.models)
                    {
                        if (!string.IsNullOrEmpty(model.name))
                            models.Add(model.name);
                    }
                }
                return models;
            }
            catch
            {
                return new List<string>();
            }
        }

        /// <summary>
        /// Generate a response using the chat API with conversation history
        /// </summary>
        public async Task<string> GenerateChatResponseAsync(string userMessage, string systemPrompt = null, string context = "")
        {
            try
            {
                // Add system prompt if provided and not already in history
                if (!string.IsNullOrEmpty(systemPrompt) && (_conversationHistory.Count == 0 || _conversationHistory[0].role != "system"))
                {
                    _conversationHistory.Insert(0, new OllamaChatMessage { role = "system", content = systemPrompt });
                }

                // Build the user message with context if provided
                string fullMessage = string.IsNullOrEmpty(context) 
                    ? userMessage 
                    : $"Context:\n```\n{context}\n```\n\nQuestion: {userMessage}";

                // Add user message to history
                _conversationHistory.Add(new OllamaChatMessage { role = "user", content = fullMessage });

                var requestBody = new
                {
                    model = _model,
                    messages = _conversationHistory,
                    stream = false
                };

                var content = new StringContent(
                    JsonConvert.SerializeObject(requestBody),
                    Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.PostAsync($"{_serverAddress}/api/chat", content);
                response.EnsureSuccessStatusCode();

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var responseObj = JsonConvert.DeserializeObject<OllamaChatResponse>(jsonResponse);

                string assistantMessage = responseObj?.message?.content ?? "No response generated";

                // Add assistant response to history
                _conversationHistory.Add(new OllamaChatMessage { role = "assistant", content = assistantMessage });

                return assistantMessage;
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        /// <summary>
        /// Legacy generate method (kept for backwards compatibility)
        /// </summary>
        public async Task<string> GenerateResponse(string prompt, string context = "")
        {
            try
            {
                var requestBody = new
                {
                    model = _model,
                    prompt = $"Context:\n{context}\n\nQuestion:\n{prompt}",
                    stream = false
                };

                var content = new StringContent(
                    JsonConvert.SerializeObject(requestBody),
                    Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.PostAsync($"{_serverAddress}/api/generate", content);
                response.EnsureSuccessStatusCode();

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var responseObj = JsonConvert.DeserializeObject<OllamaResponse>(jsonResponse);

                return responseObj?.Response ?? "No response generated";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        /// <summary>
        /// Specialized method for code explanation
        /// </summary>
        public async Task<string> ExplainCodeAsync(string code, string language = "")
        {
            string languageInfo = string.IsNullOrEmpty(language) ? "" : $" (Language: {language})";
            string prompt = $"Please explain the following code{languageInfo}:\n\n```\n{code}\n```";
            return await GenerateChatResponseAsync(prompt);
        }

        /// <summary>
        /// Specialized method for code refactoring suggestions
        /// </summary>
        public async Task<string> SuggestRefactoringAsync(string code, string language = "")
        {
            string languageInfo = string.IsNullOrEmpty(language) ? "" : $" (Language: {language})";
            string prompt = $"Please suggest refactoring improvements for the following code{languageInfo}:\n\n```\n{code}\n```\n\nProvide specific improvements with explanations.";
            return await GenerateChatResponseAsync(prompt);
        }

        /// <summary>
        /// Specialized method for generating code based on requirements
        /// </summary>
        public async Task<string> GenerateCodeAsync(string requirements, string language = "csharp", string existingContext = "")
        {
            string contextInfo = string.IsNullOrEmpty(existingContext) ? "" : $"\n\nExisting code context:\n```\n{existingContext}\n```";
            string prompt = $"Generate {language} code for the following requirements:\n{requirements}{contextInfo}\n\nProvide complete, working code with comments.";
            return await GenerateChatResponseAsync(prompt);
        }

        /// <summary>
        /// Specialized method for finding issues in code
        /// </summary>
        public async Task<string> FindCodeIssuesAsync(string code, string language = "")
        {
            string languageInfo = string.IsNullOrEmpty(language) ? "" : $" (Language: {language})";
            string prompt = $"Please analyze the following code{languageInfo} and identify any potential issues, bugs, or improvements:\n\n```\n{code}\n```";
            return await GenerateChatResponseAsync(prompt);
        }

        /// <summary>
        /// Specialized method for Agent mode code editing
        /// Requests the AI to provide a complete modified version of code
        /// </summary>
        public async Task<string> GenerateCodeEditAsync(string originalCode, string modificationRequest, string language = "", string additionalContext = "")
        {
            string languageInfo = string.IsNullOrEmpty(language) ? "" : $" (Language: {language})";
            string contextInfo = string.IsNullOrEmpty(additionalContext) ? "" : $"\n\nAdditional context:\n{additionalContext}";
            
            string prompt = $@"Please modify the following code{languageInfo} according to this request:

Request: {modificationRequest}

Original code:
```
{originalCode}
```{contextInfo}

Provide the complete modified code in a code block. Explain the changes you made and why.";
            
            return await GenerateChatResponseAsync(prompt);
        }

        /// <summary>
        /// Generate documentation for code
        /// </summary>
        public async Task<string> GenerateDocumentationAsync(string code, string language = "")
        {
            string languageInfo = string.IsNullOrEmpty(language) ? "" : $" (Language: {language})";
            string prompt = $"Please generate comprehensive documentation for the following code{languageInfo}:\n\n```\n{code}\n```\n\nInclude: purpose, parameters, return values, and usage examples.";
            return await GenerateChatResponseAsync(prompt);
        }

        /// <summary>
        /// Get conversation history count
        /// </summary>
        public int GetConversationMessageCount()
        {
            return _conversationHistory.Count;
        }

        /// <summary>
        /// Get conversation history
        /// </summary>
        public IReadOnlyList<OllamaChatMessage> GetConversationHistory()
        {
            return _conversationHistory.AsReadOnly();
        }
    }

    public class OllamaResponse
    {
        public string Response { get; set; }
    }

    public class OllamaChatMessage
    {
        public string role { get; set; }
        public string content { get; set; }
    }

    public class OllamaChatResponse
    {
        public OllamaChatMessage message { get; set; }
        public bool done { get; set; }
    }

    public class OllamaTagsResponse
    {
        public List<OllamaModelTag> models { get; set; }
    }

    public class OllamaModelTag
    {
        public string name { get; set; }
    }
}