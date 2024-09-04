namespace BrowserSearch;

using CommandLine;

public class CommandLineOptions
{
    [Option('c', "count", Required = true, HelpText = "Number of searches to perform.")]
    public int SearchCount { get; set; }

    [Option('b', "browser", Default = "", Required = false, HelpText = "OS command-line browser name (e.g. msedge, chrome, etc.).")]
    public string BrowserName { get; set; }

    [Option('p', "pause", Required = true, HelpText = "Milliseconds to pause between each search; automatically vary by 500ms")]
    public int SearchPauseMs { get; set; }

    [Option("minWords", Default = 2, Required = false, HelpText = "The minimium number of search words.")]
    public int MinSearchWordCount { get; set; }

    [Option("maxWords", Default = 4, Required = false, HelpText = "The maximum number of search words.")]
    public int MaxSearchWordCount { get; set; }

    [Option("cmdBeforeCycle", Required = false, HelpText = "OS command to execute before a search cycle (separate cmd from args using \"::\").")]
    public string CommandBeforeCycle { get; set; }

    [Option("cmdAfterCycle", Required = false, HelpText = "OS command to execute after a search cycle (separate cmd from args using \"::\").")]
    public string CommandAfterCycle { get; set; }

    [Option("cmdBeforeCyclePause", Required = false, Default = 0, HelpText = "Milliseconds to pause before executing cmdBeforeCycle (default = 0ms).")]
    public int CommandBeforeCyclePauseMs { get; set; }

    [Option("cmdAfterCyclePause", Required = false, Default = 0, HelpText = $"Milliseconds to pause after excecuting cmdAfterCycle (default = 0ms).")]
    public int CommandAfterCyclePauseMs { get; set; }

    [Option("cycles", Required = false, Default = 1, HelpText = "The number of cycles, repeating the \"count\" searches.")]
    public int CycleCount { get; set; }

    [Option("cyclePause", Required = false, Default = 1200000, HelpText = "Milliseconds to pause after each cycle (default = 1200000ms).")]
    public int CyclePauseMs{ get; set; }

    [Option(
        "cmdBeforeCycleWaitToComplete",
        Required = false,
        Default = 5000,
        HelpText = $"Milliseconds to wait for \"before\" command to complete (default = 5000ms).")]
    public int CommandBeforeCycleWaitToCompleteMs { get; set; }

    [Option(
        "cmdAfterCycleWaitToComplete",
        Required = false,
        Default = 5000,
        HelpText = $"Milliseconds to wait for \"after\" command to complete (default = 5000ms).")]
    public int CommandAfterCycleWaitToCompleteMs { get; set; }

    [Option("startPause", Required = false, Default = 0, HelpText = "Milliseconds to pause before starting (default = 0ms).")]
    public int StartAllCyclesPauseMs { get; set; }
}
