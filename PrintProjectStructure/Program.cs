
using Microsoft.Extensions.Configuration;

namespace PrintProjectStructure;

class Program
{
    static void Main(string[] args)
    {
        // Opret konfiguration
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
        var configuration = builder.Build();

        // Få computerens navn og rootPath fra konfiguration
        var machineName = Environment.MachineName;
        Console.WriteLine($"Machine name: {machineName}");
        var rootPath = @"C:\Users\Tommi\RiderProjects\Hive5";

        // Udelukkede mapper og filer
        var excludeDirs = new HashSet<string> {".github", ".vs", ".git", ".idea", "bin", "obj", "lib", "Migrations", "Identity", "Properties" };
        var excludeFiles = new HashSet<string>();
        var includeExtensions = new HashSet<string>
            { ".cs", ".html", ".js", ".cshtml", ".razor" }; // Filtyper, der skal inkluderes

        // Hent strukturen og antal linjer
        var (structure, totalLines) =
            ListStructureAndCountLines(rootPath, "", excludeDirs, excludeFiles, includeExtensions);

        // Udskriv resultatet
        Console.WriteLine($"{new DirectoryInfo(rootPath).Name}  ");
        foreach (var line in structure)
        {
            Console.WriteLine($"{line}  ");
        }

        Console.WriteLine($"Total: {totalLines} lines  ");
    }


    static (List<string>, int) ListStructureAndCountLines(string path, string prefix = "",
        HashSet<string> excludeDirs = null, HashSet<string> excludeFiles = null,
        HashSet<string> includeExtensions = null)
    {
        excludeDirs ??= new HashSet<string>();
        excludeFiles ??= new HashSet<string>();
        includeExtensions ??= [];

        var output = new List<string>();
        int totalLines = 0;

        var allEntries = Directory.EnumerateFileSystemEntries(path).OrderBy(n => n);

        // List all files first
        foreach (var entry in allEntries)
        {
            var fileInfo = new FileInfo(entry);
            if (fileInfo.Exists &&
                (includeExtensions.Count == 0 || includeExtensions.Contains(fileInfo.Extension)) &&
                !excludeFiles.Contains(fileInfo.Name))
            {
                var lines = File.ReadLines(fileInfo.FullName).Count();
                totalLines += lines;
                output.Add($"{prefix}├── {fileInfo.Name} ({lines} lines)");
            }
        }

        // Then list directories
        foreach (var entry in allEntries)
        {
            var dirInfo = new DirectoryInfo(entry);
            if (dirInfo.Exists && !excludeDirs.Contains(dirInfo.Name))
            {
                output.Add($"{prefix}├── {dirInfo.Name}/");
                var (subdirStructure, subdirLines) = ListStructureAndCountLines(dirInfo.FullName, prefix + "│   ",
                    excludeDirs, excludeFiles, includeExtensions);
                output.AddRange(subdirStructure);
                totalLines += subdirLines;
            }
        }

        return (output, totalLines);
    }
}