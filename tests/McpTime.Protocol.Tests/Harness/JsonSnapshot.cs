using System.Text.Encodings.Web;
using System.Text.Json;
using ModelContextProtocol;

namespace McpTime.Protocol.Tests.Harness;

internal static class JsonSnapshot
{
    private const string ProjectFileName = "McpTime.Protocol.Tests.csproj";

    private static readonly JsonSerializerOptions s_options = new(McpJsonUtilities.DefaultOptions)
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static void Match(string name, object value)
    {
        var directory = Path.Combine(FindProjectDirectory(), "Snapshots");
        var verifiedPath = Path.Combine(directory, $"{name}.verified.json");
        var receivedPath = Path.Combine(directory, $"{name}.received.json");
        var actual = Normalize(JsonSerializer.Serialize(value, s_options));

        if (File.Exists(verifiedPath) && Normalize(File.ReadAllText(verifiedPath)) == actual)
        {
            File.Delete(receivedPath);
            return;
        }

        Directory.CreateDirectory(directory);
        File.WriteAllText(receivedPath, actual + "\n");
        Assert.Fail($"Snapshot '{name}' differs from or lacks the verified file. Received: {receivedPath}{Environment.NewLine}Verified: {verifiedPath}");
    }

    private static string Normalize(string text) => text.Replace("\r\n", "\n").TrimEnd('\n');

    private static string FindProjectDirectory()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, ProjectFileName)))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException($"{ProjectFileName} not found above {AppContext.BaseDirectory}.");
    }
}
