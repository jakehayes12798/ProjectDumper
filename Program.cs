using System.Diagnostics;

internal class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        string projectDir;

        if (args.Length == 0)
        {
            // Open a folder browser dialog to select the project directory
            Console.WriteLine("Please wait, opening folder selection dialog...");

            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select a folder";
                dialog.UseDescriptionForTitle = true;

                DialogResult result = dialog.ShowDialog();

                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
                {
                    projectDir = dialog.SelectedPath;
                    Console.WriteLine("Selected folder: " + projectDir);
                }
                else
                {
                    Console.WriteLine("No folder selected.");
                    return;
                }
            }
        }
        else
        {
            projectDir = args[0].Trim().Replace("\"", "");
        }

        string outputFile = args.Length > 1 && !string.IsNullOrWhiteSpace(args[1])
            ? args[1]
            : Path.GetFileName(projectDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)) + ".txt";

        // Prompt for folders to exclude
        string defaultExcludes = ".git,.github,.vs,bin,docs,obj,releases,publish,resources";
        Console.WriteLine("Enter folders to exclude (comma-separated, e.g., bin,obj,.vs)");
        Console.WriteLine($"Default: {defaultExcludes}");
        Console.WriteLine("Note: Folders should be separated with a comma. Begin the line with '+' to include all the default folder exclusions:");
        string excludeInput = Console.ReadLine();

        // Process the exclude input
        if (excludeInput?.Trim().StartsWith("+") == true)
        {
            excludeInput = defaultExcludes + "," + excludeInput.TrimStart('+').Trim();
        }

        var excludeFolders = (string.IsNullOrWhiteSpace(excludeInput) ? defaultExcludes : excludeInput)
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(f => f.Trim())
            .ToList();

        Console.WriteLine($"Excluding folders:");
        foreach (var folder in excludeFolders)
        {
            Console.WriteLine($"- {folder}");
        }

        // Prompt for file extensions to skip
        string defaultSkipExts = ".dll,.exe,.png,.jpg,.jpeg,.gif,.zip,.pdb,.user,.pfx,.ico,.ttf,.otf,.woff,.woff2,.svg,.mp4,.mp3,.wav,.bmp,.resx";
        Console.WriteLine("Enter file extensions to skip (comma-separated, e.g., .dll,.exe)");
        Console.WriteLine($"Default: {defaultSkipExts}");
        Console.WriteLine("Note: Extensions should start with a dot and be separated by a comma. Begin the line with '+' to include all the default skip extensions:");
        string skipExtInput = Console.ReadLine();

        // Process the skip extensions input
        if (skipExtInput?.Trim().StartsWith("+") == true)
        {
            skipExtInput = defaultSkipExts + "," + skipExtInput.TrimStart('+').Trim();
        }

        var skipExtensions = (string.IsNullOrWhiteSpace(skipExtInput) ? defaultSkipExts : skipExtInput)
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(ext => ext.Trim().StartsWith('.') ? ext.Trim() : "." + ext.Trim())
            .ToList();

        Console.WriteLine($"Excluding extensions:");
        foreach (var ext in skipExtensions)
        {
            Console.WriteLine($"- {ext}");
        }

        var includeFolders = new List<string>(); // Optional: folders to include

        // Summary counters
        int totalFiles = 0;
        int skippedByFolder = 0;
        int skippedByExtension = 0;
        int includedFiles = 0;
        int erroredFiles = 0;

        using var writer = new StreamWriter(outputFile);

        foreach (var file in Directory.EnumerateFiles(projectDir, "*.*", SearchOption.AllDirectories))
        {
            totalFiles++;
            string relativePath = Path.GetRelativePath(projectDir, file);
            string folder = Path.GetDirectoryName(relativePath);

            // Skip excluded folders
            var normalizedFolder = folder?.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            var folderParts = normalizedFolder?.Split(Path.DirectorySeparatorChar) ?? Array.Empty<string>();
            if (excludeFolders.Any(ex => folderParts.Contains(ex, StringComparer.OrdinalIgnoreCase)))
            {
                skippedByFolder++;
                continue;
            }

            // Skip files not in included folders (if any are specified)
            if (includeFolders.Any() && !includeFolders.Any(inc => folder.Contains(inc, StringComparison.OrdinalIgnoreCase)))
                continue;

            // Skip files with excluded extensions
            if (skipExtensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
            {
                //Console.WriteLine($"Skipping file due to extension: {relativePath}");
                skippedByExtension++;
                continue;
            }

            try
            {
                Console.WriteLine($"Processing file: {relativePath}");
                writer.WriteLine($"// File: {relativePath}");
                writer.WriteLine(File.ReadAllText(file));
                writer.WriteLine();
                includedFiles++;
            }
            catch (IOException ex)
            {
                Console.WriteLine($"WARNING: Could not read file '{relativePath}' due to IO error: {ex.Message}");
                erroredFiles++;
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine($"WARNING: Access denied to file '{relativePath}': {ex.Message}");
                erroredFiles++;
            }
        }

        // Summary
        Console.WriteLine("\n       ---=== Summary ===---");
        Console.WriteLine($"Total files scanned: {totalFiles}");
        Console.WriteLine($"Files skipped by folder: {skippedByFolder}");
        Console.WriteLine($"Files skipped by extension: {skippedByExtension}");
        Console.WriteLine($"Files included in output: {includedFiles}");
        Console.WriteLine($"Files skipped due to errors: {erroredFiles}");
        Console.WriteLine($"Project files dumped from {projectDir} to {outputFile}");

        Console.WriteLine("Opening output now, please wait...");

        // Open File Explorer with the file selected
        System.Diagnostics.Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = $"/select,\"{outputFile}\"",
            UseShellExecute = true
        });

        // Pause before exit
        Console.WriteLine("Press any key to exit ... ");
    }
}