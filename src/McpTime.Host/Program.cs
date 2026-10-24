using System.CommandLine;
using McpTime.Host;

// Response files (@file) are off: a client-supplied token must never be replaced by file contents.
var configuration = new ParserConfiguration { ResponseFileTokenReplacer = null };
return await ServerCommand.Create().Parse(args, configuration).InvokeAsync();
