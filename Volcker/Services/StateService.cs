using System.Collections.ObjectModel;

namespace Volcker.Services;

public class BlockedApp
{
    public string DirectoryName { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty; // Might not be recoverable from rules alone if we only store DirName in rule name.
    // Requirement says: "Volcker - <BaseFolderName> - <ExeFileName>"
    // If we only store BaseFolderName, we might lose the full path if multiple folders have same name.
    // Ideally we should store a hash or the full path in the rule description?
    // Or just persist a JSON file as suggested.
    
    public int BlockedExeCount { get; set; }
    public DateTime LastActionDate { get; set; }
}

public class StateService
{
    private readonly FirewallService _firewallService;
    private readonly string _configPath;

    public StateService(FirewallService firewallService)
    {
        _firewallService = firewallService;
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData); // Changed to LocalAppData
        string volckerData = System.IO.Path.Combine(appData, "Volcker");
        System.IO.Directory.CreateDirectory(volckerData);
        _configPath = System.IO.Path.Combine(volckerData, "blocked_apps.json");
    }

    public async Task<AppState> LoadStateAsync()
    {
        var state = new AppState();
        
        // 1. Load from JSON to get History and Metadata
        if (System.IO.File.Exists(_configPath))
        {
            try
            {
                string json = await System.IO.File.ReadAllTextAsync(_configPath);
                var loadedState = System.Text.Json.JsonSerializer.Deserialize<AppState>(json);
                if (loadedState != null)
                {
                    state = loadedState;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading state: {ex.Message}");
            }
        }

        // 2. Reconcile "Blocked" list with actual Firewall rules
        // We trust the Firewall for what is CURRENTLY blocked.
        // We trust JSON for History and Metadata (Full Path).
        
        var firewallDirs = await _firewallService.GetBlockedDirectoriesAsync();
        var reconciledBlockedApps = new List<BlockedApp>();

        foreach (var dir in firewallDirs)
        {
            int count = await _firewallService.CountBlockedExecutablesAsync(dir);
            
            // Try to find metadata in loaded state (either in Blocked or History)
            var knownApp = state.BlockedApps.FirstOrDefault(a => a.DirectoryName == dir) 
                           ?? state.HistoryApps.FirstOrDefault(a => a.DirectoryName == dir);

            reconciledBlockedApps.Add(new BlockedApp 
            { 
                DirectoryName = dir, 
                BlockedExeCount = count,
                FullPath = knownApp?.FullPath ?? "Unknown Path", // Preserve path if known
                LastActionDate = DateTime.Now
            });
        }

        state.BlockedApps = reconciledBlockedApps;
        
        // Save back to ensure consistency
        await SaveStateAsync(state);
        
        return state;
    }

    public async Task SaveStateAsync(AppState state)
    {
        try
        {
            string json = System.Text.Json.JsonSerializer.Serialize(state, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            await System.IO.File.WriteAllTextAsync(_configPath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving state: {ex.Message}");
        }
    }
}

public class AppState
{
    public List<BlockedApp> BlockedApps { get; set; } = new();
    public List<BlockedApp> HistoryApps { get; set; } = new();
}
