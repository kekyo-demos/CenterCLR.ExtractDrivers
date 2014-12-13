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
		private static async Task ExtractDriverFilesAsync(string infFilePath, string outputFolderPath, Dictionary<string, List<FileInfo>> filesIndex, TextWriter outputMessage)
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
				return;
			}

			var catalogFileInfos =
				from entry in versionKeyValues
				where entry.Key.StartsWith("CatalogFile")
				from catalogFileInfo in filesIndex.GetCollection(entry.Value)
				select Tuple.Create(catalogFileInfo, catalogFileInfo.Name);

			var sourceDisksFileInfos =
				from entry in sections.GetCollection("SourceDisksFiles")
				from sourceDisksFileInfo in filesIndex.GetCollection(entry.Key)
				select Tuple.Create(sourceDisksFileInfo, sourceDisksFileInfo.Name);

			var storeInfName = Path.GetFileNameWithoutExtension(
				catalogFileInfos.Select(catalogFileInfo => catalogFileInfo.Item2).FirstOrDefault() ?? infFilePath);

			var targetPaths = new[] { Tuple.Create(new FileInfo(infFilePath), storeInfName + ".inf") }.
				Concat(catalogFileInfos.Concat(sourceDisksFileInfos)).
				Distinct(FileEntryEqualityComparer.Instance).
				ToList();

			await outputMessage.WriteLineAsync(string.Format(
				"  Target: {0} ({1}, Files={2})",
				infFilePath,
				storeInfName,
				targetPaths.Count));

			var infFolderPath = Path.Combine(outputFolderPath, storeInfName);
			if (Directory.Exists(infFolderPath) == false)
			{
				Directory.CreateDirectory(infFolderPath);
			}

			await Task.WhenAll(
				targetPaths.Select(entry =>
					Utilities.CopyFileAsync(entry.Item1.FullName, Path.Combine(infFolderPath, entry.Item2))));
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

				var infFolder = new DirectoryInfo(Path.Combine(windowsPath, driverBaseFolderPath));

				var filesIndex =
					infFolder.Traverse().
					AsParallel().
					GroupBy(fileInfo => fileInfo.Name).
					ToDictionary(g => g.Key, g => g.ToList());

				Console.WriteLine("Step1: Indexed system files (Files={0})", filesIndex.Count);

				if (Directory.Exists(outputFolderPath) == false)
				{
					Directory.CreateDirectory(outputFolderPath);
				}

				Console.WriteLine("Step2: Begin extract driver files...");

				Task.WhenAll(
					Directory.EnumerateFiles(Path.Combine(windowsPath, infFolderPath), searchPattern, SearchOption.TopDirectoryOnly).
					Select(path => ExtractDriverFilesAsync(path, outputFolderPath, filesIndex, Console.Out))).
					Wait();

				Console.WriteLine("Step2: Done.");
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
