using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CenterCLR.ExtractDrivers
{
	class Program
	{
		private static async Task ParseInfFormatAsync(StreamReader tr, Dictionary<string, List<KeyValuePair<string, string>>> sections)
		{
			List<KeyValuePair<string, string>> currentKeyValues = null;
			while (tr.EndOfStream == false)
			{
				var line = await tr.ReadLineAsync();

				var sanitized = line.Trim();
				var index = sanitized.IndexOf(';');
				if (index >= 0)
				{
					sanitized = sanitized.Substring(0, index).Trim();
				}

				if (string.IsNullOrWhiteSpace(sanitized) == true)
				{
					continue;
				}

				if ((sanitized[0] == '[') && (sanitized[sanitized.Length - 1] == ']'))
				{
					var sectionName = sanitized.Substring(1, sanitized.Length - 2).Trim();
					if (string.IsNullOrWhiteSpace(sectionName) == false)
					{
						currentKeyValues = new List<KeyValuePair<string, string>>();
						sections.Add(sectionName, currentKeyValues);
					}

					continue;
				}

				if (currentKeyValues == null)
				{
					continue;
				}

				var equalIndex = sanitized.IndexOf('=');
				if (equalIndex == -1)
				{
					continue;
				}

				var keyName = sanitized.Substring(0, equalIndex).Trim();
				if (string.IsNullOrWhiteSpace(keyName) == true)
				{
					continue;
				}

				var value = sanitized.Substring(equalIndex + 1).Trim();
				currentKeyValues.Add(new KeyValuePair<string, string>(keyName, value));
			}
		}

		private static readonly string[] empty_ = new string[0];
		private static string[] GetFilePaths(Dictionary<string, string[]> filesIndex, string fileName)
		{
			string[] filePaths;
			if (filesIndex.TryGetValue(fileName, out filePaths) == true)
			{
				return filePaths;
			}

			return empty_;
		}

		private static async Task ExtractDriverFilesAsync(string infPath, string outputFolder, Dictionary<string, string[]> filesIndex, TextWriter outputMessage)
		{
			await outputMessage.WriteLineAsync(string.Format("InfFile = {0}", infPath)).ConfigureAwait(false);

			var sections = new Dictionary<string, List<KeyValuePair<string, string>>>();

			using (var stream = new FileStream(infPath, FileMode.Open, FileAccess.Read, FileShare.Read, 65536, FileOptions.SequentialScan))
			{
				var tr = new StreamReader(stream, Encoding.Default, true);
				await ParseInfFormatAsync(tr, sections);
			}

			var catalogFilePaths =
				from entry in sections["Version"]
				where entry.Key.StartsWith("CatalogFile")
				from categoryFilePath in GetFilePaths(filesIndex, entry.Value)
				select categoryFilePath;

			var sourceDisksFilePaths =
				from entry in sections["SourceDisksFiles"]
				from driverFilePath in GetFilePaths(filesIndex, entry.Key)
				select driverFilePath;

			var targetPaths = new[] { infPath }.Concat(catalogFilePaths.Concat(sourceDisksFilePaths).Distinct().ToList());

			var infFileName = catalogFilePaths.FirstOrDefault() ?? infPath;
			infFileName = Path.GetFileNameWithoutExtension(infFileName);

			await Task.WhenAll(targetPaths.Select(targetPath =>
				Task.Run(() => File.Copy(targetPath, Path.Combine(outputFolder, Path.GetFileName(targetPath)), true))));
		}

		static void Main(string[] args)
		{
			var windowsPath = @"C:\Windows";
			var searchPattern = @"oem*.inf";
			var driverBaseFolder = @"System32";
			var outputFolder = ".";

			var filesIndex =
				Directory.EnumerateFiles(Path.Combine(windowsPath, driverBaseFolder), "*.*", SearchOption.AllDirectories).
				AsParallel().
				GroupBy(path => Path.GetFileName(path)).
				ToDictionary(g => g.Key, g => g.ToArray());

			Task.WhenAll(
				Directory.EnumerateFiles(Path.Combine(windowsPath, "Inf"), searchPattern, SearchOption.TopDirectoryOnly).
				Select(path => ExtractDriverFilesAsync(path, outputFolder, filesIndex, Console.Out))).
				Wait();
		}
	}
}
