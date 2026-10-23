namespace McpTime.Protocol.Tests;

public sealed class HarnessTests
{
    [Fact]
    public void HostRuntimeConfigIsCopiedNextToTheTests()
        => Assert.True(File.Exists(Path.Combine(AppContext.BaseDirectory, "McpTime.Host.runtimeconfig.json")));
}
