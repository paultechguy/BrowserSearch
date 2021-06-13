namespace BrowserSearch
{
	using CommandLine;
	public class CommandLineOptions
	{
		[Option('c', "count", Required = true, HelpText = "Number of searches to perform.")]
		public int SearchCount { get; set; }

		[Option('b', "browser", Default = "", Required = false, HelpText = "OS command-line browser name (e.g. msedge, chrome, etc.).")]
		public string BrowserName { get; set; }

		[Option('d', "delay", Required = true, HelpText = "The millisecond delay between each search.")]
		public int SearchDelay { get; set; }

		[Option("minwords", Default = 2, Required = false, HelpText = "The minimium number of search words.")]
		public int MinSearchWordCount { get; set; }

		[Option("maxwords", Default = 4, Required = false, HelpText = "The maximum number of search words.")]
		public int MaxSearchWordCount { get; set; }

		[Option("commandBefore", Required = false, HelpText = "OS command to execute before searching (separate cmd from args using \"::\").")]
		public string CommandBefore { get; set; }

		[Option("commandAfter", Required = false, HelpText = "OS command to execute after searching (separate cmd from args using \"::\").")]
		public string CommandAfter { get; set; }

		[Option("commandBeforePause", Required = false, Default = 0, HelpText = "Milliseconds to pause after executing commandBefore (before searching).")]
		public int CommandBeforePause { get; set; }

		[Option("commandAfterPause", Required = false, Default = 0, HelpText = "Milliseconds to pause before excecuting commandAfter (after seraching).")]
		public int CommandAfterPause { get; set; }

		[Option("commandBeforeWait", Required = false, Default = 0, HelpText = "Milliseconds to wait for command to complete (before seraching).")]
		public int CommandBeforeWait { get; set; }

		[Option("commandAfterWait", Required = false, Default = 0, HelpText = "Milliseconds to wait for command to complete (after seraching).")]
		public int CommandAfterWait { get; set; }

	}
}
