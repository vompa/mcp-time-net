<!-- mcp-name: io.github.vompa/mcp-time-net -->

# Vompa.McpTime

A small local [MCP](https://modelcontextprotocol.io) server (stdio) for time and timezones, built on .NET 10. It offers two read-only tools:

- `get_current_time` – the current time in an IANA timezone.
- `convert_time` – converts a wall-clock time from one IANA timezone to another.

## Run

Requires the .NET 10 SDK.

```bash
dotnet dnx Vompa.McpTime --yes
```

## Options

| Option | Description |
| --- | --- |
| `--local-timezone <name>` | IANA timezone name used as the local timezone, for example `Europe/Vienna`. Default: the system timezone, else `UTC`. An unknown name stops the server with an error. |
| `--help` | Shows the usage. |

## MCP client configuration

```json
{
  "mcpServers": {
    "time": {
      "type": "stdio",
      "command": "dotnet",
      "args": ["dnx", "Vompa.McpTime", "--yes", "--", "--local-timezone", "Europe/Vienna"]
    }
  }
}
```
