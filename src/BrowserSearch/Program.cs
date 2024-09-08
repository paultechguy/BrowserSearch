namespace BrowserSearch;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using CommandLine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

public class Program
{
    private const int EXIT_FAIL = 1;
    private const int EXIT_SUCCESS = 0;
    private const int CommandCompleteWaitDefaultMs = 5000;
    private const string SearchWordsFileName = "searchwords.txt";
    private const string SearchPrefixesFileName = "searchprefixes.txt";

    public static int Main(string[] args)
    {
        int status = EXIT_FAIL;
        ParserResult<CommandLineOptions> result = Parser.Default.ParseArguments<CommandLineOptions>(args)
        .WithParsed(options =>  // options is an instance of Options type
        {
            IHost host = ConfigureApplication(args);
            status = new Program(host).Run(options);
        })
        .WithNotParsed(errors =>  // errors is a sequence of type IEnumerable<Error>
        {
            status = EXIT_FAIL;
        });

        return status;
    }

    private static IHost ConfigureApplication(string[] args)
    {
        IHost host = Host.CreateDefaultBuilder(args)
            .ConfigureHostConfiguration(configHost =>
            {
                configHost.SetBasePath(Directory.GetCurrentDirectory());
                configHost.AddJsonFile("appsettings.json", optional: true);
            })
            .ConfigureServices(services =>
            {
            })
            .UseSerilog((context, configuration) =>
            {
                configuration.ReadFrom.Configuration(context.Configuration);
            })
            .Build();

        return host;

        // Note: host won't be disposed until after the app finishes
    }

    private readonly ReadOnlyCollection<string> availableSearchWords;
    private readonly ReadOnlyCollection<string> availableSearchPrefixes;
    private readonly Serilog.ILogger logger;
    private OSPlatform? osPlatform = null;

    public Program(IHost host)
    {
        this.availableSearchWords = LoadSearchContentLines(SearchWordsFileName);
        this.availableSearchPrefixes = LoadSearchContentLines(SearchPrefixesFileName);
        this.logger = host.Services.GetRequiredService<ILogger>();
        this.DetermineOSPlatform();
    }

    private int Run(CommandLineOptions options)
    {
        int status;
        DateTime startTime = DateTime.Now;
        try
        {
            if (options.StartAllCyclesPauseMs > 0)
            {
                this.logger.Information("Performing initial startup pause of {milliseconds} milliseconds...", options.StartAllCyclesPauseMs);
                Thread.Sleep(options.StartAllCyclesPauseMs);
            }

            for (int i = 0; i < options.CycleCount; i++)
            {
                this.logger.Information("--------------------- Executing cycle {cycle} of {count} ---------------------", i + 1, options.CycleCount);

                // delay if we're repeating
                if (i > 0)
                {
                    this.PauseUntilNextCycle(options);
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
            this.logger.Error(ex.ToString());
        }

        TimeSpan duration = DateTime.Now - startTime;
        this.logger.Information("Completed: {date}, Duration: {duration}", DateTime.Now.ToString("g"), duration);

        return status;
    }

    private void PauseUntilNextCycle(CommandLineOptions options)
    {
        const double MsInOneMinute = 60000.0;

        double pauseMs = options.CyclePauseMs;
        int pauseCount = (int)Math.Ceiling(pauseMs / MsInOneMinute);
        this.logger.Information("Pausing for {minutes} minute(s)", Math.Round(pauseMs / MsInOneMinute, 2));

        double msRemaining = pauseMs;
        for (int i = pauseCount; i > 0; i--)
        {
            int sleepMs = (int)(msRemaining - MsInOneMinute > 0 ? MsInOneMinute : msRemaining);
            if (sleepMs < MsInOneMinute)
            {
                this.logger.Information("Time remaining: {seconds} second(s)", Math.Truncate(sleepMs / 1000.0));
            }
            else
            {
                this.logger.Information("Time remaining: {minutes} minute(s)", Math.Round(msRemaining / MsInOneMinute));
            }

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
            this.logger.Information("Using browser: {browser}", browser);

            var wordHistory = new HashSet<string>();
            var random = new Random();

            this.ExecuteCommandBefore(options);

            for (int i = 0; i < options.SearchCount; ++i)
            {
                string searchPhrase = null;
                do
                {
                    searchPhrase = string.Join("+", this.GetWords(random, options).Select(w => w.ToLower().Trim()));

                } while (wordHistory.Contains(searchPhrase));

                this.logger.Information("Opening search {count} of {maxCount} using \"{phrase}\"", i + 1, options.SearchCount, searchPhrase.Replace("+", " "));
                wordHistory.Add(searchPhrase);

                // search bing
                string url = FormatSearchEngineUrl(searchPhrase);
                this.OpenBrowser(url, options);

                // sleep if not the last time
                if (options.SearchPauseMs > 0 && i + 1 < options.SearchCount)
                {
                    Thread.Sleep(options.SearchPauseMs + random.Next(500, 1001)); // add a random delta to help look non-automated
                }
            }

            this.ExecuteCommandAfter(options);
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
        else
        {
            _ = this.osPlatform == OSPlatform.Linux
                ? Process.Start("xdg-open", url)
                : this.osPlatform == OSPlatform.OSX
                            ? Process.Start("open", url)
                            : throw new ApplicationException($"Internal error; invalid OS defined in {nameof(this.osPlatform)}");
        }
    }

    private void ExecuteCommandBefore(CommandLineOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.CommandBeforeCycle))
        {
            return;
        }

        this.logger.Information("Executing command before search, \"{command}\"", options.CommandBeforeCycle);
        this.ExecuteOperatingSystemCommand(options.CommandBeforeCycle, options.CommandBeforeCycleWaitToCompleteMs);

        if (options.CommandBeforeCyclePauseMs > 0)
        {
            this.logger.Information("Pause after executing command, before search, {milliseconds} milliseconds", options.CommandBeforeCyclePauseMs);
            Thread.Sleep(options.CommandBeforeCyclePauseMs);
        }
    }

    private void ExecuteCommandAfter(CommandLineOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.CommandAfterCycle))
        {
            return;
        }

        if (options.CommandAfterCyclePauseMs > 0)
        {
            this.logger.Information("Pause before executing command, after search, {milliseconds} milliseconds", options.CommandAfterCyclePauseMs);
            Thread.Sleep(options.CommandAfterCyclePauseMs);
        }

        this.logger.Information("Executing the command after search, \"{command}\"", options.CommandAfterCycle);
        this.ExecuteOperatingSystemCommand(options.CommandAfterCycle, options.CommandAfterCycleWaitToCompleteMs);
    }

    private void ExecuteOperatingSystemCommand(string command, int waitToCompleteMs)
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
            this.logger.Error("Error excuting command \"{command}\". Reason: {reason}", command, ex.Message);
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
        string startingPhrase = this.availableSearchPrefixes[random.Next(this.availableSearchPrefixes.Count)];
        listWords.Insert(0, startingPhrase);

        return [.. listWords];
    }

    private static ReadOnlyCollection<string> LoadSearchContentLines(string fileName)
    {
        if (!File.Exists(fileName))
        {
            throw new FileNotFoundException($"Missing search words file, {fileName}");
        }

        // read all non-blank lines, ignore those marked as a comment
        return Array.AsReadOnly<string>(File.ReadAllLines(fileName)
            .Where(l => !l.StartsWith('#') && l.Trim().Length > 0)
            .ToArray());
    }

    private void DetermineOSPlatform()
    {

        this.osPlatform = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? OSPlatform.Windows
            : RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
                ? OSPlatform.Linux
                : RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                            ? (OSPlatform?)OSPlatform.OSX
                            : throw new ApplicationException($"Unsupported platform to launch process, {RuntimeInformation.OSDescription}");
    }
}
