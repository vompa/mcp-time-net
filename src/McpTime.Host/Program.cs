using McpTime.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);
// Slice 04 replaces the fixed value with the detected or overridden local time zone (PRD F4).
builder.Services.AddMcpTimeServer(localTimeZone: "UTC").WithStdioServerTransport();
await builder.Build().RunAsync();
