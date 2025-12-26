using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using OllamaLocalHostIntergration.Models;

namespace OllamaLocalHostIntergration.Services
{
    /// <summary>
    /// Service for managing conversation history persistence
    /// </summary>
    public class ConversationHistoryService
    {
        private readonly string _historyPath;

        public ConversationHistoryService()
        {
            _historyPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "OllamaVSExtension",
                "History"
            );
            
            // Ensure directory exists
            Directory.CreateDirectory(_historyPath);
        }

        /// <summary>
        /// Saves a conversation to disk
        /// </summary>
        public async Task SaveConversationAsync(Conversation conversation)
        {
            if (conversation == null)
                return;

            conversation.LastModified = DateTime.Now;
            string filename = $"{conversation.Id}.json";
            string filePath = Path.Combine(_historyPath, filename);

            try
            {
                await Task.Run(() =>
                {
                    var settings = new JsonSerializerSettings
                    {
                        Formatting = Formatting.Indented,
                        NullValueHandling = NullValueHandling.Ignore
                    };
                    
                    string json = JsonConvert.SerializeObject(conversation, settings);
                    File.WriteAllText(filePath, json);
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save conversation: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads all conversations from disk
        /// </summary>
        public async Task<List<Conversation>> LoadAllConversationsAsync()
        {
            var conversations = new List<Conversation>();

            if (!Directory.Exists(_historyPath))
                return conversations;

            try
            {
                await Task.Run(() =>
                {
                    foreach (var file in Directory.GetFiles(_historyPath, "*.json"))
                    {
                        try
                        {
                            string json = File.ReadAllText(file);
                            var conversation = JsonConvert.DeserializeObject<Conversation>(json);
                            if (conversation != null)
                            {
                                conversations.Add(conversation);
                            }
                        }
                        catch
                        {
                            // Skip corrupted files
                            System.Diagnostics.Debug.WriteLine($"Skipped corrupted conversation file: {file}");
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading conversations: {ex.Message}");
            }

            return conversations.OrderByDescending(c => c.LastModified).ToList();
        }

        /// <summary>
        /// Loads a specific conversation by ID
        /// </summary>
        public async Task<Conversation> LoadConversationAsync(Guid id)
        {
            string filename = $"{id}.json";
            string filePath = Path.Combine(_historyPath, filename);

            if (!File.Exists(filePath))
                return null;

            try
            {
                return await Task.Run(() =>
                {
                    string json = File.ReadAllText(filePath);
                    return JsonConvert.DeserializeObject<Conversation>(json);
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load conversation {id}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Deletes a conversation from disk
        /// </summary>
        public async Task DeleteConversationAsync(Guid id)
        {
            string filename = $"{id}.json";
            string filePath = Path.Combine(_historyPath, filename);

            try
            {
                await Task.Run(() =>
                {
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to delete conversation {id}: {ex.Message}");
            }
        }

        /// <summary>
        /// Exports a conversation to Markdown format
        /// </summary>
        public async Task ExportToMarkdownAsync(Conversation conversation, string outputPath)
        {
            if (conversation == null)
                return;

            try
            {
                await Task.Run(() =>
                {
                    var sb = new StringBuilder();
                    sb.AppendLine($"# {conversation.Title}");
                    sb.AppendLine();
                    sb.AppendLine($"**Created:** {conversation.Created:yyyy-MM-dd HH:mm}");
                    sb.AppendLine($"**Last Modified:** {conversation.LastModified:yyyy-MM-dd HH:mm}");
                    sb.AppendLine($"**Model:** {conversation.ModelUsed}");
                    sb.AppendLine($"**Mode:** {conversation.Mode}");
                    sb.AppendLine($"**Messages:** {conversation.Messages.Count}");
                    
                    if (conversation.Tags.Any())
                    {
                        sb.AppendLine($"**Tags:** {string.Join(", ", conversation.Tags)}");
                    }
                    
                    sb.AppendLine();
                    sb.AppendLine("---");
                    sb.AppendLine();

                    foreach (var message in conversation.Messages)
                    {
                        string role = message.IsUser ? "?? You" : "?? Ollama";
                        sb.AppendLine($"## {role}");
                        sb.AppendLine();
                        sb.AppendLine(message.Content);
                        sb.AppendLine();
                        
                        if (message.HasCodeBlocks)
                        {
                            sb.AppendLine($"*({message.CodeBlocks.Count} code block(s))*");
                            sb.AppendLine();
                        }
                    }

                    File.WriteAllText(outputPath, sb.ToString());
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to export conversation: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gets the path where conversation history is stored
        /// </summary>
        public string GetHistoryPath() => _historyPath;

        /// <summary>
        /// Gets count of saved conversations
        /// </summary>
        public int GetConversationCount()
        {
            if (!Directory.Exists(_historyPath))
                return 0;

            try
            {
                return Directory.GetFiles(_historyPath, "*.json").Length;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Clears all conversation history
        /// </summary>
        public async Task ClearAllHistoryAsync()
        {
            if (!Directory.Exists(_historyPath))
                return;

            try
            {
                await Task.Run(() =>
                {
                    foreach (var file in Directory.GetFiles(_historyPath, "*.json"))
                    {
                        File.Delete(file);
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to clear history: {ex.Message}");
            }
        }
    }
}
