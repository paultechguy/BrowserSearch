namespace BrowserSearch
{
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
		public static int Main(string[] args)
		{
			int exit = 0;
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
				string browser = options.BrowserName;
				if (string.IsNullOrWhiteSpace(options.BrowserName))
				{
					browser = "OS default";
				}
				Console.WriteLine($"Using browser: {browser}");

				var wordHistory = new HashSet<string>();
				var rnd = new Random();

				ExecuteCommandBefore(options);

				for (int i = 0; i < options.SearchCount; ++i)
				{
					string words = null;
					do
					{
						words = string.Join("+", this.GetWords(rnd, options).Select(w => w.ToLower().Trim()).OrderBy(w => w));

					} while (wordHistory.Contains(words));

					Console.WriteLine($"Opening search #{i + 1} using \"{words.Replace("+", " ")}\"");
					wordHistory.Add(words);

					// search bing
					string url = FormatSearchEngineUrl(words);
					this.OpenBrowser(url, options);

					// sleep if not the last time
					if (i + 1 < options.SearchCount)
					{
						Thread.Sleep(options.SearchDelay + rnd.Next(1000)); // add a random delta to help look non-automated
					}
				}

				this.ExecuteCommandAfter(options);
			}
			catch (Exception)
			{
				throw;
			}

			return 1;
		}

		private static string FormatSearchEngineUrl(string words)
		{
			return $"https://www.bing.com/search?q={words}";
		}

		private void OpenBrowser(string url, CommandLineOptions options)
		{
			// hack because of this: https://github.com/dotnet/corefx/issues/10361
			if (this.osPlatform == OSPlatform.Windows)
			{
				url = url.Replace("&", "^&");
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

			if (options.CommandBeforePause > 0)
			{
				Console.WriteLine($"Pause after executing command, before search, {options.CommandBeforePause} ms");
				Thread.Sleep(options.CommandBeforePause);
			}
		}

		private void ExecuteCommandAfter(CommandLineOptions options)
		{
			if (string.IsNullOrWhiteSpace(options.CommandAfter))
			{
				return;
			}

			if (options.CommandAfterPause > 0)
			{
				Console.WriteLine($"Pause before executing command, after search {options.CommandAfterPause} ms");
				Thread.Sleep(options.CommandAfterPause);
			}

			Console.WriteLine($"Executing the command after search, \"{options.CommandAfter}\"");
			ExecuteOperatingSystemCommand(options.CommandAfter, options.CommandAfterWait);
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
				command = command.Substring(0, separatorIndex).Trim();
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

				process.WaitForExit(waitToCompleteMs > 0 ? waitToCompleteMs : 5000); // default of 5 seconds
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"Error excuting command \"{command}\". Reason: {ex.Message}");
			}
		}

		private string[] GetWords(Random rnd, CommandLineOptions options)
		{
			var words = new HashSet<string>();
			int count = rnd.Next(options.MinSearchWordCount, options.MaxSearchWordCount + 1); // max is exclusive so add 1
			for (int i = 0; i < count; ++i)
			{
				string word;
				do
				{
					word = availableSearchWords[rnd.Next(availableSearchWords.Count)];
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
}
