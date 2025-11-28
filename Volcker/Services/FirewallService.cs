using System.Diagnostics;
using System.IO;
using System.Text;

namespace Volcker.Services;

public class FirewallService
{
    private const string RulePrefix = "Volcker";

    public async Task<bool> BlockExecutableAsync(string exePath, string directoryName)
    {
        string ruleNameIn = GetRuleName(directoryName, exePath, "Inbound");
        string ruleNameOut = GetRuleName(directoryName, exePath, "Outbound");

        bool inSuccess = await RunNetshCommandAsync($"advfirewall firewall add rule name=\"{ruleNameIn}\" dir=in action=block program=\"{exePath}\" enable=yes");
        bool outSuccess = await RunNetshCommandAsync($"advfirewall firewall add rule name=\"{ruleNameOut}\" dir=out action=block program=\"{exePath}\" enable=yes");

        return inSuccess && outSuccess;
    }

    public async Task UnblockDirectoryAsync(string directoryName)
    {
        // We delete by rule name pattern. 
        // netsh doesn't support wildcards well for delete, so we might need to find them first or just try to delete known patterns if we track them.
        // However, the requirement says "Finds and deletes all firewall rules previously created by Volcker for that directory".
        // Since we can't easily query netsh with wildcards, we might need to use PowerShell or just iterate.
        // Actually, netsh is bad at "delete all starting with X".
        // PowerShell is better: Remove-NetFirewallRule -DisplayName "Volcker - DirectoryName - *"
        // Let's use PowerShell for Unblock to be robust.
        
        string pattern = $"{RulePrefix} - {directoryName} - *";
        await RunPowerShellCommandAsync($"Remove-NetFirewallRule -DisplayName '{pattern}' -ErrorAction SilentlyContinue");
    }

    public async Task<int> CountBlockedExecutablesAsync(string directoryName)
    {
        // This is tricky with netsh. PowerShell is easier.
        // Get-NetFirewallRule -DisplayName "Volcker - DirectoryName - *" | Measure-Object
        // We divide by 2 because we have In/Out rules per exe.
        
        string pattern = $"{RulePrefix} - {directoryName} - *";
        var result = await RunPowerShellCommandWithOutputAsync($"(Get-NetFirewallRule -DisplayName '{pattern}').Count");
        
        if (int.TryParse(result, out int count))
        {
            return count / 2;
        }
        return 0;
    }

    public async Task<List<string>> GetBlockedDirectoriesAsync()
    {
        // Scan all rules starting with Volcker
        // Format: Volcker - DirectoryName - ExeName - Direction
        // We need to extract DirectoryName.
        
        var output = await RunPowerShellCommandWithOutputAsync($"Get-NetFirewallRule -DisplayName '{RulePrefix} - *' | Select-Object -ExpandProperty DisplayName");
        
        var directories = new HashSet<string>();
        if (string.IsNullOrWhiteSpace(output)) return directories.ToList();

        var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            var parts = line.Split(new[] { " - " }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                directories.Add(parts[1]); // Index 0 is Volcker, 1 is DirectoryName
            }
        }
        
        return directories.ToList();
    }

    private string GetRuleName(string directoryName, string exePath, string direction)
    {
        string exeName = Path.GetFileName(exePath);
        return $"{RulePrefix} - {directoryName} - {exeName} - {direction}";
    }

    private async Task<bool> RunNetshCommandAsync(string arguments)
    {
        var psi = new ProcessStartInfo("netsh", arguments)
        {
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true
        };
        
        using var process = Process.Start(psi);
        if (process == null) return false;
        
        await process.WaitForExitAsync();
        return process.ExitCode == 0;
    }

    private async Task<bool> RunPowerShellCommandAsync(string command)
    {
        var psi = new ProcessStartInfo("powershell", $"-NoProfile -ExecutionPolicy Bypass -Command \"{command}\"")
        {
            CreateNoWindow = true,
            UseShellExecute = false
        };
        
        using var process = Process.Start(psi);
        if (process == null) return false;
        
        await process.WaitForExitAsync();
        return process.ExitCode == 0;
    }

    private async Task<string> RunPowerShellCommandWithOutputAsync(string command)
    {
        var psi = new ProcessStartInfo("powershell", $"-NoProfile -ExecutionPolicy Bypass -Command \"{command}\"")
        {
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            StandardOutputEncoding = Encoding.UTF8
        };
        
        using var process = Process.Start(psi);
        if (process == null) return string.Empty;
        
        string output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();
        return output.Trim();
    }
}
