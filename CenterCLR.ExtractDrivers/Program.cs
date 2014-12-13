////////////////////////////////////////////////////////////////////////////////////////////////////
//
// CenterCLR.ExtractDrivers - Extract driver files from installed windows.
// Copyright (c) Kouji Matsui, All rights reserved.
//
// Redistribution and use in source and binary forms, with or without modification,
// are permitted provided that the following conditions are met:
//
// * Redistributions of source code must retain the above copyright notice,
//   this list of conditions and the following disclaimer.
// * Redistributions in binary form must reproduce the above copyright notice,
//   this list of conditions and the following disclaimer in the documentation
//   and/or other materials provided with the distribution.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO,
// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.
// IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT,
// INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION)
// HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
// OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE,
// EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
//
////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CenterCLR.ExtractDrivers
{
	internal sealed class Program
	{
		private static async Task<string> ExtractDriverFilesAsync(
			string infFilePath,
			string outputFolderPath,
			Dictionary<string, List<FileInfo>> filesIndex,
			TextWriter outputMessage)
		{
			var sections = new Dictionary<string, List<KeyValuePair<string, string>>>(StringComparer.InvariantCultureIgnoreCase);

			using (var stream = new FileStream(infFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 65536, FileOptions.SequentialScan))
			{
				var tr = new StreamReader(stream, Encoding.Default, true);
				await Utilities.ParseInfFormatAsync(tr, sections);
			}

			var versionKeyValues = sections.GetCollection("Version");
			if (versionKeyValues.Any() == false)
			{
				return null;
			}

			var catalogFileInfos =
				from entry in versionKeyValues
				where entry.Key.StartsWith("CatalogFile", StringComparison.InvariantCultureIgnoreCase)
				from catalogFileInfo in filesIndex.GetCollection(entry.Value)
				select Tuple.Create(catalogFileInfo, catalogFileInfo.Name);

			var copyFileInfos =
				from section in sections.Values
				from entry in section
				where entry.Key.Equals("CopyFiles", StringComparison.InvariantCultureIgnoreCase)
				from copySectionName in entry.Value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(cs => cs.Trim())
				from entry2 in sections.GetCollection(copySectionName)
				from copyFile in filesIndex.GetCollection(entry2.Key)
				select Tuple.Create(copyFile, copyFile.Name);

			var sourceDisksFileInfos =
				from entry in sections.GetCollection("SourceDisksFiles")
				from sourceDisksFileInfo in filesIndex.GetCollection(entry.Key)
				select Tuple.Create(sourceDisksFileInfo, sourceDisksFileInfo.Name);

			var storeInfName = Path.GetFileNameWithoutExtension(
				catalogFileInfos.Select(catalogFileInfo => catalogFileInfo.Item2).FirstOrDefault() ?? infFilePath);

			var storeInfFileName = storeInfName + ".inf";

			var targetEntries = new[] { Tuple.Create(new FileInfo(infFilePath), storeInfFileName) }.
				Concat(catalogFileInfos).
				Concat(copyFileInfos).
				Concat(sourceDisksFileInfos).
				Distinct(FileEntryEqualityComparer.Instance).
				ToList();

			await outputMessage.WriteLineAsync(string.Format(
				"  Target: {0} [{1}] (Files={2})",
				infFilePath,
				storeInfName,
				targetEntries.Count));
//				string.Join(",", targetEntries.Select(entry => entry.Item2))));

			var infFolderPath = Path.Combine(outputFolderPath, storeInfName);
			if (Directory.Exists(infFolderPath) == false)
			{
				Directory.CreateDirectory(infFolderPath);
			}

			await Task.WhenAll(
				targetEntries.Select(entry =>
					Utilities.CopyFileAsync(entry.Item1.FullName, Path.Combine(infFolderPath, entry.Item2))));

			return Path.Combine(infFolderPath, storeInfFileName);
		}

		private static async Task WriteDismScriptAsync(
			string outputFolderPath,
			IEnumerable<string> infPaths)
		{
			using (var stream = new FileStream(
				Path.Combine(outputFolderPath, "template.bat"),
				FileMode.Create,
				FileAccess.ReadWrite,
				FileShare.ReadWrite,
				65536,
				FileOptions.SequentialScan))
			{
				var tw = new StreamWriter(stream, Encoding.Default);
				await tw.WriteLineAsync(@"set MountFolderPath=C:\mount\windows");

				foreach (var infPath in infPaths)
				{
					await tw.WriteLineAsync(string.Format("dism.exe /Add-Driver /Image:\"%%MountFolderPath%%\" /Driver:\"{0}\"", infPath));
				}

				await tw.FlushAsync();
				await stream.FlushAsync();
			}
		}

		static int Main(string[] args)
		{
			Console.WriteLine("CenterCLR.ExtractDrivers - Extract driver files from installed windows.");
			Console.WriteLine("Copyright (c) Kouji Matsui, All rights reserved.");
			Console.WriteLine();

			if (args.Length == 0)
			{
				var executableFileName = Path.GetFileName(Assembly.GetEntryAssembly().Location);

				Console.WriteLine(string.Format(
					@"usage: {0} <pickup windows folder path> [<inf search pattern> [<output folder path>]]",
					executableFileName));
				Console.WriteLine(string.Format(
					@"   ex: {0} C:\Windows oem*.inf DriverStore",
					executableFileName));

				return 0;
			}

			try
			{
				var windowsPath = (args.Length >= 1) ? args[0] : @"C:\Windows";
				var searchPattern = (args.Length >= 2) ? args[1] : "oem*.inf";
				var outputFolderPath = (args.Length >= 3) ? args[2] : "DriverStore";

				var driverBaseFolderPath = "System32";
				var infFolderPath = "Inf";

				Console.WriteLine("Pickup Windows folder path: {0}", windowsPath);
				Console.WriteLine("Inf file search pattern: {0}", searchPattern);
				Console.WriteLine("Output folder path: {0}", outputFolderPath);
				Console.WriteLine();

				Console.WriteLine("Step1: Indexing system files ...");

				var infFolder = new DirectoryInfo(Path.Combine(windowsPath, driverBaseFolderPath));

				var filesIndex =
					infFolder.Traverse().
					AsParallel().
					GroupBy(fileInfo => fileInfo.Name).
					ToDictionary(g => g.Key, g => g.ToList());

				Console.WriteLine("Step1: Done. (Files={0})", filesIndex.Count);

				if (Directory.Exists(outputFolderPath) == false)
				{
					Directory.CreateDirectory(outputFolderPath);
				}

				Console.WriteLine("Step2: Begin extract driver files ...");

				var storeInfFilePaths = Task.WhenAll(
					Directory.EnumerateFiles(Path.Combine(windowsPath, infFolderPath), searchPattern, SearchOption.TopDirectoryOnly).
					Select(path => ExtractDriverFilesAsync(path, outputFolderPath, filesIndex, Console.Out))).
					Result.
					Where(storeInfFilePath => storeInfFilePath != null).
					ToList();

				Console.WriteLine("Step2: Done. (Stored={0})", storeInfFilePaths.Count);

				Console.WriteLine("Step3: Output optional dism script ...");

				WriteDismScriptAsync(outputFolderPath, storeInfFilePaths).
					Wait();

				Console.WriteLine("Step3: Done.");
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine(ex.ToString());

				return Marshal.GetHRForException(ex);
			}

			return 0;
		}
	}
}
