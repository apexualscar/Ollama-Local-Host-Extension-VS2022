using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using OllamaLocalHostIntergration.Models;

namespace OllamaLocalHostIntergration.Services
{
    /// <summary>
    /// Service for persisting pending changes between sessions (Phase 5.7)
    /// </summary>
    public class ChangePersistenceService
    {
        private readonly string _storageDirectory;
        private readonly string _pendingChangesFile;

        public ChangePersistenceService()
        {
            _storageDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "OllamaVSExtension",
                "PendingChanges"
            );

            _pendingChangesFile = Path.Combine(_storageDirectory, "pending_changes.json");

            // Ensure directory exists
            if (!Directory.Exists(_storageDirectory))
            {
                Directory.CreateDirectory(_storageDirectory);
            }
        }

        /// <summary>
        /// Save pending changes to disk
        /// </summary>
        public async Task SavePendingChangesAsync(IEnumerable<CodeEdit> codeEdits)
        {
            try
            {
                var editsList = codeEdits.ToList();
                
                // Create a serializable version (exclude temp file paths)
                var serializableEdits = editsList.Select(e => new
                {
                    e.FilePath,
                    e.OriginalCode,
                    e.ModifiedCode,
                    e.StartLine,
                    e.EndLine,
                    e.Description,
                    e.CreatedAt,
                    e.Applied
                }).ToList();

                var json = JsonConvert.SerializeObject(serializableEdits, Formatting.Indented);

                await Task.Run(() => File.WriteAllText(_pendingChangesFile, json));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save pending changes: {ex.Message}");
            }
        }

        /// <summary>
        /// Load pending changes from disk
        /// </summary>
        public async Task<List<CodeEdit>> LoadPendingChangesAsync()
        {
            try
            {
                if (!File.Exists(_pendingChangesFile))
                {
                    return new List<CodeEdit>();
                }

                var json = await Task.Run(() => File.ReadAllText(_pendingChangesFile));
                
                var codeEdits = JsonConvert.DeserializeObject<List<CodeEdit>>(json);
                
                return codeEdits ?? new List<CodeEdit>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load pending changes: {ex.Message}");
                return new List<CodeEdit>();
            }
        }

        /// <summary>
        /// Clear all saved pending changes
        /// </summary>
        public async Task ClearPendingChangesAsync()
        {
            try
            {
                if (File.Exists(_pendingChangesFile))
                {
                    await Task.Run(() => File.Delete(_pendingChangesFile));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to clear pending changes: {ex.Message}");
            }
        }

        /// <summary>
        /// Save a single change (for auto-save scenarios)
        /// </summary>
        public async Task SaveSingleChangeAsync(CodeEdit codeEdit)
        {
            try
            {
                // Load existing changes
                var existingChanges = await LoadPendingChangesAsync();
                
                // Add or update this change
                var existing = existingChanges.FirstOrDefault(e => 
                    e.FilePath == codeEdit.FilePath && 
                    e.CreatedAt == codeEdit.CreatedAt);
                
                if (existing != null)
                {
                    existingChanges.Remove(existing);
                }
                
                existingChanges.Add(codeEdit);
                
                // Save all changes
                await SavePendingChangesAsync(existingChanges);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save single change: {ex.Message}");
            }
        }

        /// <summary>
        /// Remove a single change from persistence
        /// </summary>
        public async Task RemoveSingleChangeAsync(CodeEdit codeEdit)
        {
            try
            {
                // Load existing changes
                var existingChanges = await LoadPendingChangesAsync();
                
                // Remove this change
                var existing = existingChanges.FirstOrDefault(e => 
                    e.FilePath == codeEdit.FilePath && 
                    e.CreatedAt == codeEdit.CreatedAt);
                
                if (existing != null)
                {
                    existingChanges.Remove(existing);
                    await SavePendingChangesAsync(existingChanges);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to remove single change: {ex.Message}");
            }
        }

        /// <summary>
        /// Get the number of saved pending changes
        /// </summary>
        public async Task<int> GetPendingChangesCountAsync()
        {
            try
            {
                var changes = await LoadPendingChangesAsync();
                return changes.Count;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Check if there are any saved pending changes
        /// </summary>
        public bool HasPendingChanges()
        {
            return File.Exists(_pendingChangesFile) && new FileInfo(_pendingChangesFile).Length > 0;
        }
    }
}
