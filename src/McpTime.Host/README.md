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

The server then waits for JSON-RPC messages from an MCP client on stdin; it is not an interactive prompt. Text typed into the console (for example `today`) is not a valid message: the server logs a parse error with a stack trace to stderr and keeps running. Stop it with Ctrl+C. Logs always go to stderr, stdout carries only protocol messages.

To try the tools by hand, use the [MCP Inspector](https://github.com/modelcontextprotocol/inspector) (requires Node.js):

```bash
# command line: call one tool and print the result
npx -y @modelcontextprotocol/inspector --cli dotnet dnx Vompa.McpTime --yes --method tools/call --tool-name get_current_time --tool-arg timezone=Europe/Vienna

# browser UI: list the tools and call them interactively
npx -y @modelcontextprotocol/inspector dotnet dnx Vompa.McpTime --yes
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
