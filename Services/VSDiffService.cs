using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using OllamaLocalHostIntergration.Models;

namespace OllamaLocalHostIntergration.Services
{
    /// <summary>
    /// Service for showing Visual Studio diff windows (Phase 5.7)
    /// </summary>
    public class VSDiffService
    {
        private readonly IServiceProvider _serviceProvider;

        public VSDiffService(IServiceProvider serviceProvider = null)
        {
            _serviceProvider = serviceProvider ?? ServiceProvider.GlobalProvider;
        }

        /// <summary>
        /// Show diff in Visual Studio's diff viewer
        /// </summary>
        public async Task ShowDiffAsync(CodeEdit codeEdit)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            try
            {
                // Create temp files for diff
                var tempOriginal = Path.GetTempFileName();
                var tempModified = Path.GetTempFileName();

                // Get proper extension
                var extension = Path.GetExtension(codeEdit.FilePath) ?? ".cs";
                var tempOriginalWithExt = Path.ChangeExtension(tempOriginal, extension);
                var tempModifiedWithExt = Path.ChangeExtension(tempModified, extension);

                // Rename temp files to have proper extension
                if (File.Exists(tempOriginalWithExt))
                    File.Delete(tempOriginalWithExt);
                if (File.Exists(tempModifiedWithExt))
                    File.Delete(tempModifiedWithExt);

                File.Move(tempOriginal, tempOriginalWithExt);
                File.Move(tempModified, tempModifiedWithExt);

                // Write content
                File.WriteAllText(tempOriginalWithExt, codeEdit.OriginalCode ?? "");
                File.WriteAllText(tempModifiedWithExt, codeEdit.ModifiedCode ?? "");

                // Get VS diff service
                var shellOpenDocument = _serviceProvider.GetService(typeof(SVsUIShellOpenDocument)) as IVsUIShellOpenDocument;
                
                if (shellOpenDocument != null)
                {
                    // Open diff window
                    Microsoft.VisualStudio.Shell.Interop.IVsWindowFrame frame;
                    
                    var options = (uint)(__VSDIFFSERVICEOPTIONS.VSDIFFOPT_DetectBinaryFiles |
                                        __VSDIFFSERVICEOPTIONS.VSDIFFOPT_LeftFileIsTemporary |
                                        __VSDIFFSERVICEOPTIONS.VSDIFFOPT_RightFileIsTemporary);

                    var diffService = _serviceProvider.GetService(typeof(SVsDifferenceService)) as IVsDifferenceService;
                    
                    if (diffService != null)
                    {
                        var fileName = Path.GetFileName(codeEdit.FilePath) ?? "Code";
                        
                        diffService.OpenComparisonWindow2(
                            tempOriginalWithExt,
                            tempModifiedWithExt,
                            $"Original: {fileName}",
                            $"Proposed: {fileName}",
                            $"AI Suggested Changes - {fileName}",
                            null, // tooltip for left file
                            null, // tooltip for right file
                            null, // inline diff tooltip
                            options
                        );
                    }
                }

                // Store temp file paths for cleanup
                codeEdit.TempOriginalPath = tempOriginalWithExt;
                codeEdit.TempModifiedPath = tempModifiedWithExt;
            }
            catch (Exception ex)
            {
                // Fallback to our custom diff dialog
                await ShowCustomDiffAsync(codeEdit);
                System.Diagnostics.Debug.WriteLine($"Failed to show VS diff: {ex.Message}");
            }
        }

        /// <summary>
        /// Show custom diff dialog (fallback)
        /// </summary>
        private async Task ShowCustomDiffAsync(CodeEdit codeEdit)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            
            var dialog = new Dialogs.DiffPreviewDialog(codeEdit);
            dialog.ShowDialog();
        }

        /// <summary>
        /// Cleanup temp files for a code edit
        /// </summary>
        public void CleanupTempFiles(CodeEdit codeEdit)
        {
            try
            {
                if (!string.IsNullOrEmpty(codeEdit.TempOriginalPath) && File.Exists(codeEdit.TempOriginalPath))
                {
                    File.Delete(codeEdit.TempOriginalPath);
                }

                if (!string.IsNullOrEmpty(codeEdit.TempModifiedPath) && File.Exists(codeEdit.TempModifiedPath))
                {
                    File.Delete(codeEdit.TempModifiedPath);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to cleanup temp files: {ex.Message}");
            }
        }

        /// <summary>
        /// Show side-by-side diff for multiple changes
        /// </summary>
        public async Task ShowMultipleDiffsAsync(System.Collections.Generic.List<CodeEdit> codeEdits)
        {
            foreach (var edit in codeEdits)
            {
                await ShowDiffAsync(edit);
                
                // Small delay between opening multiple diffs
                await Task.Delay(500);
            }
        }
    }
}
