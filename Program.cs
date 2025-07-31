class Program
{
    static void Main(string[] args)
    {
        string projectDir;

        if (args.Length < 1)
        {
            Console.WriteLine("Usage: ProjectFileDumper <project-directory> [output-file]");
            Console.WriteLine("Enter the full path to the project directory and optionally an output file name.");
            projectDir = Console.ReadLine()?.Trim();
            projectDir = projectDir.Replace("\"", ""); // Remove quotes if any
            if (string.IsNullOrWhiteSpace(projectDir) || !Directory.Exists(projectDir))
            {
                Console.WriteLine("Invalid project directory. Please provide a valid path.");
                return;
            }
        }
        else
        {
            projectDir = args[0];
        }



        string outputFile = args.Length > 1 && !string.IsNullOrWhiteSpace(args[1])
            ? args[1]
            : Path.GetFileName(projectDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)) + ".txt";

        // Prompt for folders to exclude
        string defaultExcludes = ".git,.github,.vs,bin,docs,obj,releases,publish,resources";
        Console.WriteLine("Enter folders to exclude (comma-separated, e.g., bin,obj,.vs)");
        Console.WriteLine($"Default: {defaultExcludes}");
        string excludeInput = Console.ReadLine();
        var excludeFolders = (string.IsNullOrWhiteSpace(excludeInput) ? defaultExcludes : excludeInput)
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(f => f.Trim())
            .ToList();

        // Prompt for file extensions to skip
        string defaultSkipExts = ".dll,.exe,.png,.jpg,.jpeg,.gif,.zip,.pdb,.user,.pfx,.ico,.ttf,.otf,.woff,.woff2,.svg,.mp4,.mp3,.wav,.bmp,.resx";
        Console.WriteLine("Enter file extensions to skip (comma-separated, e.g., .dll,.exe)");
        Console.WriteLine($"Default: {defaultSkipExts}");
        string skipExtInput = Console.ReadLine();
        var skipExtensions = (string.IsNullOrWhiteSpace(skipExtInput) ? defaultSkipExts : skipExtInput)
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(ext => ext.Trim().StartsWith('.') ? ext.Trim() : "." + ext.Trim())
            .ToList();

        var includeFolders = new List<string>(); // Optional: folders to include

        // Summary counters
        int totalFiles = 0;
        int skippedByFolder = 0;
        int skippedByExtension = 0;
        int includedFiles = 0;

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

            Console.WriteLine($"Processing file: {relativePath}");
            writer.WriteLine($"// File: {relativePath}");
            writer.WriteLine(File.ReadAllText(file));
            writer.WriteLine();
            includedFiles++;
        }

        // Summary
        Console.WriteLine("\n--- Summary ---");
        Console.WriteLine($"Total files scanned: {totalFiles}");
        Console.WriteLine($"Files skipped by folder: {skippedByFolder}");
        Console.WriteLine($"Files skipped by extension: {skippedByExtension}");
        Console.WriteLine($"Files included in output: {includedFiles}");
        Console.WriteLine($"Project files dumped from {projectDir} to {outputFile}");

        Console.ReadKey(); // Wait for user input before closing
    }
}
