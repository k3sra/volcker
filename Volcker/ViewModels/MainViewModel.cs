using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows;
using Volcker.Services;
using Microsoft.Win32; // For OpenFolderDialog (if available in .NET 8 WPF, otherwise System.Windows.Forms or Ookii)
// Actually .NET 8 WPF has OpenFolderDialog!

namespace Volcker.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly FileService _fileService;
    private readonly FirewallService _firewallService;
    private readonly StateService _stateService;

    [ObservableProperty]
    private ObservableCollection<BlockedApp> _blockedApps = new();

    [ObservableProperty]
    private BlockedApp? _selectedApp;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private ObservableCollection<BlockedApp> _historyApps = new();

    public MainViewModel(FileService fileService, FirewallService firewallService, StateService stateService)
    {
        _fileService = fileService;
        _firewallService = firewallService;
        _stateService = stateService;
        
        LoadStateCommand.Execute(null);
    }

    [RelayCommand]
    private async Task LoadStateAsync()
    {
        IsBusy = true;
        StatusMessage = "Loading apps...";
        try
        {
            var state = await _stateService.LoadStateAsync();
            BlockedApps = new ObservableCollection<BlockedApp>(state.BlockedApps);
            HistoryApps = new ObservableCollection<BlockedApp>(state.HistoryApps);
            StatusMessage = "Ready";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading state: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task BlockNewAppAsync()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Select Application Directory to Block",
            Multiselect = false
        };

        if (dialog.ShowDialog() == true)
        {
            await BlockDirectoryAsync(dialog.FolderName);
        }
    }

    [RelayCommand]
    private async Task BlockFromHistoryAsync(BlockedApp? app)
    {
        if (app == null) return;
        await BlockDirectoryAsync(app.FullPath);
    }

    private async Task BlockDirectoryAsync(string folderPath)
    {
        string dirName = System.IO.Path.GetFileName(folderPath);
        
        // Check if already blocked
        if (BlockedApps.Any(a => a.DirectoryName.Equals(dirName, StringComparison.OrdinalIgnoreCase)))
        {
            MessageBox.Show("This directory is already in the blocked list.", "Already Blocked", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        IsBusy = true;
        StatusMessage = $"Scanning {dirName} for executables...";

        try
        {
            var exes = await _fileService.ScanForExecutablesAsync(folderPath);
            if (exes.Count == 0)
            {
                MessageBox.Show("No executable files (.exe) found in this directory.", "No Executables", MessageBoxButton.OK, MessageBoxImage.Warning);
                StatusMessage = "No executables found.";
                IsBusy = false;
                return;
            }

            StatusMessage = $"Blocking {exes.Count} executables...";
            
            int successCount = 0;
            foreach (var exe in exes)
            {
                bool success = await _firewallService.BlockExecutableAsync(exe, dirName);
                if (success) successCount++;
            }

            // Create App Object
            var newApp = new BlockedApp
            {
                DirectoryName = dirName,
                FullPath = folderPath,
                BlockedExeCount = successCount,
                LastActionDate = DateTime.Now
            };

            // Add to Blocked
            BlockedApps.Add(newApp);
            SelectedApp = newApp;

            // Remove from History if exists (to avoid duplicates)
            var existingHistory = HistoryApps.FirstOrDefault(h => h.DirectoryName == dirName);
            if (existingHistory != null)
            {
                HistoryApps.Remove(existingHistory);
            }

            await SaveStateAsync();
            StatusMessage = $"Blocked {successCount}/{exes.Count} executables in {dirName}.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error blocking app: {ex.Message}";
            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task UnblockAppAsync()
    {
        if (SelectedApp == null) return;

        var result = MessageBox.Show($"Are you sure you want to unblock '{SelectedApp.DirectoryName}'?\nThis will remove all Volcker firewall rules for this directory.", 
            "Confirm Unblock", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            IsBusy = true;
            StatusMessage = $"Unblocking {SelectedApp.DirectoryName}...";
            try
            {
                await _firewallService.UnblockDirectoryAsync(SelectedApp.DirectoryName);
                
                // Move to History
                var appToMove = SelectedApp;
                appToMove.LastActionDate = DateTime.Now;
                
                BlockedApps.Remove(appToMove);
                
                // Add to History if not already there (shouldn't be, but check)
                if (!HistoryApps.Any(h => h.DirectoryName == appToMove.DirectoryName))
                {
                    HistoryApps.Insert(0, appToMove);
                }

                SelectedApp = null;
                await SaveStateAsync();
                StatusMessage = "Unblock complete.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error unblocking: {ex.Message}";
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }
    }

    private async Task SaveStateAsync()
    {
        var state = new AppState
        {
            BlockedApps = BlockedApps.ToList(),
            HistoryApps = HistoryApps.ToList()
        };
        await _stateService.SaveStateAsync(state);
    }
}
