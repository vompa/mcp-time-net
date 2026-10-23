using System.Reflection;

namespace McpTime.ArchitectureTests;

internal static class Assemblies
{
    private const string SolutionFileName = "McpTime.slnx";

    public static Assembly Core { get; } = Assembly.Load(new AssemblyName("McpTime.Core"));

    public static Assembly Server { get; } = Assembly.Load(new AssemblyName("McpTime.Server"));

    public static Assembly Host { get; } = Assembly.Load(new AssemblyName("McpTime.Host"));

    public static string RepositoryRoot { get; } = FindRepositoryRoot();

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, SolutionFileName)))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException(
            $"Repository root not found: no {SolutionFileName} in '{AppContext.BaseDirectory}' or any parent directory.");
    }
}
