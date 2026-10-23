namespace McpTime.ArchitectureTests;

public class SourceFileRulesTests
{
    private const int MaxLines = 300;

    private static readonly string[] s_sourceFolders = ["src", "tests"];

    [Fact]
    public void NoSourceFileExceeds300Lines()
    {
        var root = Assemblies.RepositoryRoot;

        var filesOverLimit = s_sourceFolders
            .SelectMany(folder => Directory.EnumerateFiles(Path.Combine(root, folder), "*.cs", SearchOption.AllDirectories))
            .Select(path => Path.GetRelativePath(root, path))
            .Where(relativePath => !IsBuildOutput(relativePath))
            .Select(relativePath => (Path: relativePath, Lines: File.ReadLines(Path.Combine(root, relativePath)).Count()))
            .Where(file => file.Lines > MaxLines)
            .Select(file => $"{file.Path} ({file.Lines} lines)")
            .ToList();

        Assert.True(filesOverLimit.Count == 0, $"Files over {MaxLines} lines: {string.Join(", ", filesOverLimit)}");
    }

    private static bool IsBuildOutput(string relativePath) =>
        relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Any(segment => segment is "bin" or "obj");
}
