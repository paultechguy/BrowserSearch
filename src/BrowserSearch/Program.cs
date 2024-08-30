namespace BrowserSearch;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using BrowserSearch.Helpers;
using CommandLine;

public class Program
{
    private const int EXIT_FAIL = 1;
    private const int EXIT_SUCCESS = 0;
    private const int CommandCompleteWaitDefaultMs = 5000;
    private readonly string[] startingPhrases =
    [
            "what is the",
            "how to",
            "what color",
            "describe the",
            "where is it",
            "why is it",
            "show tall",
            "the length of",
            "who is",
            "who did",
            "where is",
            "are the",
            "the best of",
            "which of",
            "the location of",
        ];

    public static int Main(string[] args)
    {
        int status = EXIT_FAIL;
        ParserResult<CommandLineOptions> result = Parser.Default.ParseArguments<CommandLineOptions>(args)
        .WithParsed(options =>  // options is an instance of Options type
        {
            status = new Program().Run(options);
        })
        .WithNotParsed(errors =>  // errors is a sequence of type IEnumerable<Error>
        {
            status = EXIT_FAIL;
        });

        return status;
    }

    private readonly IList<string> availableSearchWords;
    private OSPlatform? osPlatform = null;

    public Program()
    {
        this.availableSearchWords = LoadAvailableWords();
        this.DetermineOSPlatform();
    }

    private int Run(CommandLineOptions options)
    {
        int status;
        DateTime startTime = DateTime.Now;
        try
        {
            if (options.StartPauseMs > 0)
            {
                LogHelper.LogInformation($"Performing initial startup pause of {options.StartPauseMs}ms...");
                Thread.Sleep(options.StartPauseMs);
            }

            for (int i = 0; i < options.CycleCount; i++)
            {
                LogHelper.LogInformation($"--------------------- Executing cycle #{i + 1}/{options.CycleCount} ---------------------");

                // delay if we're repeating
                if (i > 0)
                {
                    PauseUntilNextCycle(options);
                }

                // execute an entire cycle
                this.ExecuteCycle(options);
            }

            // all good if we got here
            status = EXIT_SUCCESS;
        }
        catch (Exception ex)
        {
            status = EXIT_FAIL;
            LogHelper.LogError(ex.ToString());
        }

        TimeSpan duration = DateTime.Now - startTime;
        LogHelper.LogInformation($"Completed: {DateTime.Now:g}, Duration: {duration}");

        return status;
    }

    private static void PauseUntilNextCycle(CommandLineOptions options)
    {
        const double MsInOneMinute = 60000.0;

        double pauseMs = options.CyclePauseMs;
        int pauseCount = (int)Math.Ceiling(pauseMs / MsInOneMinute);
        LogHelper.LogWarning($"Pausing for {Math.Round(pauseMs / MsInOneMinute, 2)} minute(s)");

        double msRemaining = pauseMs;
        for (int i = pauseCount; i > 0; i--)
        {
            string timeString;
            int sleepMs = (int)(msRemaining - MsInOneMinute > 0 ? MsInOneMinute : msRemaining);
            timeString = sleepMs < MsInOneMinute
                ? $"Time remaining: {Math.Truncate(sleepMs / 1000.0)} second(s)"
                : $"Time remaining: {Math.Round(msRemaining / MsInOneMinute)} minute(s)";

            LogHelper.Log($"\r{timeString,-80}\r", color: ConsoleColor.Green, linefeed: false);

            msRemaining -= MsInOneMinute;

            Thread.Sleep(sleepMs);
        }
    }

    private void ExecuteCycle(CommandLineOptions options)
    {
        try
        {
            string browser = options.BrowserName;
            if (string.IsNullOrWhiteSpace(options.BrowserName))
            {
                browser = "OS default";
            }
            LogHelper.LogInformation($"\nUsing browser: {browser}");

            var wordHistory = new HashSet<string>();
            var random = new Random();

            ExecuteCommandBefore(options);

            for (int i = 0; i < options.SearchCount; ++i)
            {
                string searchPhrase = null;
                do
                {
                    searchPhrase = string.Join("+", this.GetWords(random, options).Select(w => w.ToLower().Trim()));

                } while (wordHistory.Contains(searchPhrase));

                LogHelper.LogInformation($"Opening search {i + 1}/{options.SearchCount} using \"{searchPhrase.Replace("+", " ")}\"");
                wordHistory.Add(searchPhrase);

                // search bing
                string url = FormatSearchEngineUrl(searchPhrase);
                this.OpenBrowser(url, options);

                // sleep if not the last time
                if (options.SearchDelayMs > 0 && i + 1 < options.SearchCount)
                {
                    Thread.Sleep(options.SearchDelayMs + random.Next(500, 1001)); // add a random delta to help look non-automated
                }
            }

            ExecuteCommandAfter(options);
        }
        catch (Exception)
        {
            throw;
        }
    }

    private static string FormatSearchEngineUrl(string words)
    {
        return $"https://www.bing.com/search?q={words}&form=QBLH";
    }

    private void OpenBrowser(string url, CommandLineOptions options)
    {
        // hack because of this: https://github.com/dotnet/corefx/issues/10361
        if (this.osPlatform == OSPlatform.Windows)
        {
            // commented this out 10/24/2023; not sure why this replace was being done, but seems
            // to work without it (for now)
            //url = url.Replace("&", "^&");

            _ = Process.Start(new ProcessStartInfo("cmd", $"/c start {options.BrowserName} \"{url}\"") { CreateNoWindow = true });
        }
        else if (this.osPlatform == OSPlatform.Linux)
        {
            _ = Process.Start("xdg-open", url);
        }
        else
        {
            _ = this.osPlatform == OSPlatform.OSX
                ? Process.Start("open", url)
                : throw new ApplicationException($"Internal error; invalid OS defined in {nameof(this.osPlatform)}");
        }
    }

    private static void ExecuteCommandBefore(CommandLineOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.CommandBefore))
        {
            return;
        }

        LogHelper.LogInformation($"Executing command before search, \"{options.CommandBefore}\"");
        ExecuteOperatingSystemCommand(options.CommandBefore, options.CommandBeforeWaitMs);

        if (options.CommandBeforePauseMs > 0)
        {
            LogHelper.LogInformation($"Pause after executing command, before search, {options.CommandBeforePauseMs} ms");
            Thread.Sleep(options.CommandBeforePauseMs);
        }
    }

    private static void ExecuteCommandAfter(CommandLineOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.CommandAfter))
        {
            return;
        }

        if (options.CommandAfterPauseMs > 0)
        {
            LogHelper.LogInformation($"Pause before executing command, after search {options.CommandAfterPauseMs} ms");
            Thread.Sleep(options.CommandAfterPauseMs);
        }

        LogHelper.LogInformation($"Executing the command after search, \"{options.CommandAfter}\"");
        ExecuteOperatingSystemCommand(options.CommandAfter, options.CommandAfterWaitMs);
    }

    private static void ExecuteOperatingSystemCommand(string command, int waitToCompleteMs)
    {
        // a delimiter to separate the process command from the command arguments
        const string separator = "::";

        string args = null;
        int separatorIndex = command.IndexOf(separator);
        if (separatorIndex != -1)
        {
            // set args first, before changing command
            args = command[(separatorIndex + separator.Length)..].Trim();
            command = command[..separatorIndex].Trim();
        }

        try
        {
            Process process = args is null ? Process.Start(command) : Process.Start(command, args);
            process.WaitForExit(waitToCompleteMs > 0 ? waitToCompleteMs : CommandCompleteWaitDefaultMs);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error excuting command \"{command}\". Reason: {ex.Message}");
        }
    }

    private string[] GetWords(Random random, CommandLineOptions options)
    {
        var words = new HashSet<string>();
        int count = random.Next(options.MinSearchWordCount, options.MaxSearchWordCount + 1); // max is exclusive so add 1
        for (int i = 0; i < count; ++i)
        {
            string word;
            do
            {
                word = this.availableSearchWords[random.Next(this.availableSearchWords.Count)];
            } while (words.Contains(word));

            words.Add(word);
        }

        // now insert a starting phrase
        var listWords = words.ToList();
        string startingPhrase = this.startingPhrases[random.Next(this.startingPhrases.Length)];
        listWords.Insert(0, startingPhrase);

        return [.. listWords];
    }

    private static ReadOnlyCollection<string> LoadAvailableWords()
    {
        const string FileName = "words.txt";
        if (!File.Exists(FileName))
        {
            throw new FileNotFoundException($"Missing search words file, {FileName}");
        }

        // read all non-blank lines, ignore those marked as a comment
        return Array.AsReadOnly<string>(File.ReadAllLines(FileName)
            .Where(l => !l.StartsWith('#') && l.Trim().Length > 0)
            .ToArray());
    }

    private void DetermineOSPlatform()
    {

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            this.osPlatform = OSPlatform.Windows;
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            this.osPlatform = OSPlatform.Linux;
        }
        else
        {
            this.osPlatform = RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                ? (OSPlatform?)OSPlatform.OSX
                : throw new ApplicationException($"Unsupported platform to launch process, {RuntimeInformation.OSDescription}");
        }
    }
}
