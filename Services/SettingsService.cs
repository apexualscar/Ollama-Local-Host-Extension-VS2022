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
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                if (_settingsStore.PropertyExists(CollectionPath, ServerAddressKey))
                {
                    return _settingsStore.GetString(CollectionPath, ServerAddressKey);
                }
            }
            catch (Exception)
            {
                // If any error occurs, return default
            }

            return DefaultServerAddress;
        }

        /// <summary>
        /// Saves the Ollama server address
        /// </summary>
        public void SaveServerAddress(string serverAddress)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                if (!string.IsNullOrWhiteSpace(serverAddress))
                {
                    _settingsStore.SetString(CollectionPath, ServerAddressKey, serverAddress);
                }
            }
            catch (Exception ex)
            {
                // Log error but don't throw - settings save failures shouldn't crash the extension
                System.Diagnostics.Debug.WriteLine($"Failed to save server address: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the last selected model name, or null if not set
        /// </summary>
        public string GetSelectedModel()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                if (_settingsStore.PropertyExists(CollectionPath, SelectedModelKey))
                {
                    return _settingsStore.GetString(CollectionPath, SelectedModelKey);
                }
            }
            catch (Exception)
            {
                // If any error occurs, return null
            }

            return null;
        }

        /// <summary>
        /// Saves the selected model name
        /// </summary>
        public void SaveSelectedModel(string modelName)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                if (!string.IsNullOrWhiteSpace(modelName))
                {
                    _settingsStore.SetString(CollectionPath, SelectedModelKey, modelName);
                }
            }
            catch (Exception ex)
            {
                // Log error but don't throw
                System.Diagnostics.Debug.WriteLine($"Failed to save selected model: {ex.Message}");
            }
        }

        /// <summary>
        /// Clears all saved settings
        /// </summary>
        public void ClearSettings()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                if (_settingsStore.CollectionExists(CollectionPath))
                {
                    _settingsStore.DeleteCollection(CollectionPath);
                    _settingsStore.CreateCollection(CollectionPath);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to clear settings: {ex.Message}");
            }
        }
    }
}
