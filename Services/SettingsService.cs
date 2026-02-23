using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Settings;
using System;

namespace OllamaLocalHostIntergration.Services
{
    /// <summary>
    /// Service for persisting extension settings
    /// </summary>
    public class SettingsService
    {
        private const string CollectionPath = "OllamaLocalHostIntergration";
        private const string ServerAddressKey = "ServerAddress";
        private const string SelectedModelKey = "SelectedModel";
        private const string DefaultServerAddress = "http://localhost:11434";

        private readonly WritableSettingsStore _settingsStore;

        public SettingsService()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            
            var shellSettingsManager = new ShellSettingsManager(ServiceProvider.GlobalProvider);
            _settingsStore = shellSettingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);

            // Ensure collection exists
            if (!_settingsStore.CollectionExists(CollectionPath))
            {
                _settingsStore.CreateCollection(CollectionPath);
            }
        }

        /// <summary>
        /// Gets the saved Ollama server address, or default if not set
        /// </summary>
        public string GetServerAddress()
        {
            try
            {
                if (_settingsStore.PropertyExists(CollectionPath, "ServerAddress"))
                {
                    return _settingsStore.GetString(CollectionPath, "ServerAddress");
                }
            }
            catch { }

            return "http://localhost:11434";
        }

        /// <summary>
        /// Saves the Ollama server address
        /// </summary>
        public void SaveServerAddress(string serverAddress)
        {
            try
            {
                if (!_settingsStore.CollectionExists(CollectionPath))
                {
                    _settingsStore.CreateCollection(CollectionPath);
                }

                _settingsStore.SetString(CollectionPath, "ServerAddress", serverAddress);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save server address: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the last selected model name, or null if not set
        /// </summary>
        public string GetSelectedModel()
        {
            try
            {
                if (_settingsStore.PropertyExists(CollectionPath, "SelectedModel"))
                {
                    return _settingsStore.GetString(CollectionPath, "SelectedModel");
                }
            }
            catch { }

            return null;
        }

        /// <summary>
        /// Saves the selected model name
        /// </summary>
        public void SaveSelectedModel(string modelName)
        {
            try
            {
                if (!_settingsStore.CollectionExists(CollectionPath))
                {
                    _settingsStore.CreateCollection(CollectionPath);
                }

                _settingsStore.SetString(CollectionPath, "SelectedModel", modelName);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save selected model: {ex.Message}");
            }
        }

        /// <summary>
        /// Phase 6.5: Gets whether to auto-add active document to context
        /// </summary>
        public bool GetAutoAddActiveDocument()
        {
            try
            {
                if (_settingsStore.PropertyExists(CollectionPath, "AutoAddActiveDocument"))
                {
                    return _settingsStore.GetBoolean(CollectionPath, "AutoAddActiveDocument");
                }
            }
            catch { }

            return true; // Default to true
        }

        /// <summary>
        /// Phase 6.5: Saves whether to auto-add active document to context
        /// </summary>
        public void SaveAutoAddActiveDocument(bool autoAdd)
        {
            try
            {
                if (!_settingsStore.CollectionExists(CollectionPath))
                {
                    _settingsStore.CreateCollection(CollectionPath);
                }

                _settingsStore.SetBoolean(CollectionPath, "AutoAddActiveDocument", autoAdd);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save auto-add setting: {ex.Message}");
            }
        }

        /// <summary>
        /// Clears all saved settings
        /// </summary>
        public void ClearSettings()
        {
            try
            {
                if (_settingsStore.CollectionExists(CollectionPath))
                {
                    _settingsStore.DeleteCollection(CollectionPath);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to clear settings: {ex.Message}");
            }
        }
    }
}
