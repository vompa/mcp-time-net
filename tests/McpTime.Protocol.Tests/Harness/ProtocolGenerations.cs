namespace McpTime.Protocol.Tests.Harness;

internal static class ProtocolGenerations
{
    public const string Handshake = "2025-11-25";
    public const string Sessionless = "2026-07-28";

    public static TheoryData<string> All => [Handshake, Sessionless];
}
