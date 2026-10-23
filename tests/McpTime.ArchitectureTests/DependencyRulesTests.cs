using NetArchTest.Rules;

namespace McpTime.ArchitectureTests;

public class DependencyRulesTests
{
    [Fact]
    public void CoreDependsOnNothingButNodaTime()
    {
        var result = Types.InAssembly(Assemblies.Core)
            .ShouldNot()
            .HaveDependencyOnAny(
                "McpTime.Server",
                "McpTime.Host",
                "ModelContextProtocol",
                "System.CommandLine",
                "Microsoft.Extensions",
                "System.TimeZoneInfo",
                "System.Console")
            .GetResult();

        Assert.True(result.IsSuccessful, string.Join(", ", (result.FailingTypes ?? []).Select(type => type.FullName)));
    }

    [Fact]
    public void ServerDoesNotDependOnHostOrCommandLineOrConsole()
    {
        var result = Types.InAssembly(Assemblies.Server)
            .ShouldNot()
            .HaveDependencyOnAny("McpTime.Host", "System.CommandLine", "System.Console")
            .GetResult();

        Assert.True(result.IsSuccessful, string.Join(", ", (result.FailingTypes ?? []).Select(type => type.FullName)));
    }

    [Fact]
    public void HostIsTheOnlyPlaceForTheProcessEntryPoint()
    {
        Assert.NotNull(Assemblies.Host.EntryPoint);
        Assert.Null(Assemblies.Core.EntryPoint);
        Assert.Null(Assemblies.Server.EntryPoint);
    }

    [Fact]
    public void AllAssembliesAreLoaded()
    {
        Assert.Equal("McpTime.Core", Assemblies.Core.GetName().Name);
        Assert.Equal("McpTime.Server", Assemblies.Server.GetName().Name);
        Assert.Equal("McpTime.Host", Assemblies.Host.GetName().Name);
    }
}
