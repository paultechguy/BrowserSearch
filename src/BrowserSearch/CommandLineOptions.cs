namespace BrowserSearch;

using CommandLine;

public class CommandLineOptions
{
	[Option('c', "count", Required = true, HelpText = "Number of searches to perform.")]
	public int SearchCount { get; set; }

	[Option('b', "browser", Default = "", Required = false, HelpText = "OS command-line browser name (e.g. msedge, chrome, etc.).")]
	public string BrowserName { get; set; }

	[Option('d', "delay", Required = true, HelpText = "The millisecond delay between each search; will automatically vary by 500ms")]
	public int SearchDelayMs { get; set; }

	[Option("minwords", Default = 2, Required = false, HelpText = "The minimium number of search words.")]
	public int MinSearchWordCount { get; set; }

	[Option("maxwords", Default = 4, Required = false, HelpText = "The maximum number of search words.")]
	public int MaxSearchWordCount { get; set; }

	[Option("commandBefore", Required = false, HelpText = "OS command to execute before searching (separate cmd from args using \"::\").")]
	public string CommandBefore { get; set; }

	[Option("commandAfter", Required = false, HelpText = "OS command to execute after searching (separate cmd from args using \"::\").")]
	public string CommandAfter { get; set; }

	[Option("commandBeforePause", Required = false, Default = 0, HelpText = "Milliseconds to pause before executing commandBefore (default = 0ms).")]
	public int CommandBeforePauseMs { get; set; }

	[Option("commandAfterPause", Required = false, Default = 0, HelpText = "Milliseconds to pause after excecuting commandAfter (default = 0ms).")]
	public int CommandAfterPauseMs { get; set; }

	[Option(
		"commandBeforeWait",
		Required = false,
		Default = 5000,
		HelpText = $"Milliseconds to wait for command to complete (default = 5000ms).")]
	public int CommandBeforeWait { get; set; }

	[Option(
		"commandAfterWait",
		Required = false,
		Default = 5000,
		HelpText = $"Milliseconds to wait for command to complete (default = 5000ms).")]
	public int CommandAfterWait { get; set; }

}
