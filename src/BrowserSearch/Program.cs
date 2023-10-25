namespace BrowserSearch;

using CommandLine;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

public class Program
{
	private const int CommandCompleteWaitDefaultMs = 5000;

	public static int Main(string[] args)
	{
		var exit = 0;
		ParserResult<CommandLineOptions> result = Parser.Default.ParseArguments<CommandLineOptions>(args)
		.WithParsed(options =>  // options is an instance of Options type
		{
			exit = new Program().Run(options);
		})
		.WithNotParsed(errors =>  // errors is a sequence of type IEnumerable<Error>
		{
			exit = 0;
		});

		return exit;
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
		try
		{
			var browser = options.BrowserName;
			if (string.IsNullOrWhiteSpace(options.BrowserName))
			{
				browser = "OS default";
			}
			Console.WriteLine($"Using browser: {browser}");

			var wordHistory = new HashSet<string>();
			var random = new Random();

			ExecuteCommandBefore(options);

			for (var i = 0; i < options.SearchCount; ++i)
			{
				string words = null;
				do
				{
					words = string.Join("+", this.GetWords(random, options).Select(w => w.ToLower().Trim()).OrderBy(w => w));

				} while (wordHistory.Contains(words));

				Console.WriteLine($"Opening search {i + 1}/{options.SearchCount} using \"{words.Replace("+", " ")}\"");
				wordHistory.Add(words);

				// search bing
				var url = FormatSearchEngineUrl(words);
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

		return 1;
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
		else if (this.osPlatform == OSPlatform.OSX)
		{
			_ = Process.Start("open", url);
		}
		else
		{
			throw new ApplicationException($"Internal error; invalid OS defined in {nameof(osPlatform)}");
		}
	}

	private static void ExecuteCommandBefore(CommandLineOptions options)
	{
		if (string.IsNullOrWhiteSpace(options.CommandBefore))
		{
			return;
		}

		Console.WriteLine($"Executing command before search, \"{options.CommandBefore}\"");
		ExecuteOperatingSystemCommand(options.CommandBefore, options.CommandBeforeWait);

		if (options.CommandBeforePauseMs > 0)
		{
			Console.WriteLine($"Pause after executing command, before search, {options.CommandBeforePauseMs} ms");
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
			Console.WriteLine($"Pause before executing command, after search {options.CommandAfterPauseMs} ms");
			Thread.Sleep(options.CommandAfterPauseMs);
		}

		Console.WriteLine($"Executing the command after search, \"{options.CommandAfter}\"");
		ExecuteOperatingSystemCommand(options.CommandAfter, options.CommandAfterWait);
	}

	private static void ExecuteOperatingSystemCommand(string command, int waitToCompleteMs)
	{
		// a delimiter to separate the process command from the command arguments
		const string separator = "::";

		string args = null;
		var separatorIndex = command.IndexOf(separator);
		if (separatorIndex != -1)
		{
			// set args first, before changing command
			args = command[(separatorIndex + separator.Length)..].Trim();
			command = command[..separatorIndex].Trim();
		}

		try
		{
			Process process = null;
			if (args is null)
			{
				process = Process.Start(command);
			}
			else
			{
				process = Process.Start(command, args);
			}

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
		var count = random.Next(options.MinSearchWordCount, options.MaxSearchWordCount + 1); // max is exclusive so add 1
		for (var i = 0; i < count; ++i)
		{
			string word;
			do
			{
				word = availableSearchWords[random.Next(availableSearchWords.Count)];
			} while (words.Contains(word));

			words.Add(word);
		}

		return words.ToArray();
	}

	private static IList<string> LoadAvailableWords()
	{
		const string FileName = "words.txt";
		if (!File.Exists(FileName))
		{
			throw new FileNotFoundException($"Missing search words file, {FileName}");
		}

		// read all non-blank lines, ignore those marked as a comment
		return Array.AsReadOnly<string>(File.ReadAllLines(FileName)
			.Where(l => !l.StartsWith("#") && l.Trim().Length > 0)
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
		else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
		{
			this.osPlatform = OSPlatform.OSX;
		}
		else
		{
			throw new ApplicationException($"Unsupported platform to launch process, {RuntimeInformation.OSDescription}");
		}
	}
}
