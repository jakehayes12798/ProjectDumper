using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        
        if (args.Length < 1)
        {
            Console.WriteLine("Usage: ProjectFileDumper <project-directory> [output-file]");
            return;
        }

        string projectDir = args[0];

        string outputFile;

        if (args.Length > 1 && !string.IsNullOrWhiteSpace(args[1]))
        {
            outputFile = args[1];
        }
        else
        {
            // Use the directory name as the output file name
            string dirName = Path.GetFileName(projectDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            outputFile = dirName + ".txt";
        }

        var includeFolders = new List<string> { }; // Optional: folders to include
        var excludeFolders = new List<string> { }; // Optional: folders to skip

        using var writer = new StreamWriter(outputFile);

        foreach (var file in Directory.EnumerateFiles(projectDir, "*.*", SearchOption.AllDirectories)
            .Where(f => f.EndsWith(".cs") || f.EndsWith(".json") || f.EndsWith(".sql")))
        {
            string relativePath = Path.GetRelativePath(projectDir, file);
            string folder = Path.GetDirectoryName(relativePath);

            // Skip excluded folders
            if (excludeFolders.Any(ex => folder.Contains(ex, StringComparison.OrdinalIgnoreCase)))
                continue;

            // If includeFolders is set, skip anything not in the list
            if (includeFolders.Any() && !includeFolders.Any(inc => folder.Contains(inc, StringComparison.OrdinalIgnoreCase)))
                continue;

            writer.WriteLine($"// File: {relativePath}");
            writer.WriteLine(File.ReadAllText(file));
            writer.WriteLine();
        }

        Console.WriteLine($"Project files dumped from {projectDir} to {outputFile}");
    }
}