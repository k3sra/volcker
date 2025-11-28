using System.IO;

namespace Volcker.Services;

public class FileService
{
    public async Task<List<string>> ScanForExecutablesAsync(string directoryPath)
    {
        return await Task.Run(() =>
        {
            var executables = new List<string>();
            try
            {
                if (!Directory.Exists(directoryPath))
                    return executables;

                var options = new EnumerationOptions
                {
                    IgnoreInaccessible = true,
                    RecurseSubdirectories = true,
                    AttributesToSkip = FileAttributes.ReparsePoint | FileAttributes.System // Skip symlinks and system files
                };

                foreach (var file in Directory.EnumerateFiles(directoryPath, "*.exe", options))
                {
                    executables.Add(file);
                }
            }
            catch (Exception ex)
            {
                // Log error? For now just return what we found.
                System.Diagnostics.Debug.WriteLine($"Error scanning directory: {ex.Message}");
            }
            return executables;
        });
    }
}
