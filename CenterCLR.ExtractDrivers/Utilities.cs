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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CenterCLR.ExtractDrivers
{
	internal static class Utilities
	{
		private static class Empty<T>
			where T : new()
		{
			public static readonly T Instance = new T();
		}

		public static U GetCollection<T, U>(this Dictionary<T, U> dictionary, T key)
			where U : IList, new()
		{
			U collection;
			if (dictionary.TryGetValue(key, out collection) == true)
			{
				return collection;
			}

			return Empty<U>.Instance;
		}

		public static async Task ParseInfFormatAsync(StreamReader tr, Dictionary<string, List<KeyValuePair<string, string>>> sections)
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

				if (String.IsNullOrWhiteSpace(sanitized) == true)
				{
					continue;
				}

				if ((sanitized[0] == '[') && (sanitized[sanitized.Length - 1] == ']'))
				{
					var sectionName = sanitized.Substring(1, sanitized.Length - 2).Trim();
					if (String.IsNullOrWhiteSpace(sectionName) == false)
					{
						if (sections.TryGetValue(sectionName, out currentKeyValues) == false)
						{
							currentKeyValues = new List<KeyValuePair<string, string>>();
							sections.Add(sectionName, currentKeyValues);
						}
					}

					continue;
				}

				if (currentKeyValues == null)
				{
					continue;
				}

				var equalIndex = sanitized.IndexOf('=');
				var keyName = (equalIndex >= 0) ? sanitized.Substring(0, equalIndex).Trim() : sanitized;
				if (String.IsNullOrWhiteSpace(keyName) == true)
				{
					continue;
				}

				var value = (equalIndex >= 0) ? sanitized.Substring(equalIndex + 1).Trim() : null;
				currentKeyValues.Add(new KeyValuePair<string, string>(keyName, value));
			}
		}

		public static IEnumerable<FileInfo> Traverse(this DirectoryInfo folder)
		{
			IEnumerable<DirectoryInfo> subFolders = Enumerable.Empty<DirectoryInfo>();
			try
			{
				subFolders = folder.GetDirectories("*.*", SearchOption.TopDirectoryOnly);
			}
			catch (UnauthorizedAccessException)
			{
				// ignore.
			}

			foreach (var folderInfo in subFolders)
			{
				foreach (var fileInfo in folderInfo.Traverse())
				{
					yield return fileInfo;
				}
			}

			IEnumerable<FileInfo> files = Enumerable.Empty<FileInfo>();
			try
			{
				files = folder.GetFiles("*.*", SearchOption.TopDirectoryOnly);
			}
			catch (UnauthorizedAccessException)
			{
				// ignore.
			}

			foreach (var fileInfo in files)
			{
				yield return fileInfo;
			}
		}

		public static async Task CopyFileAsync(string sourcePath, string destinationPath)
		{
			using (var sourceStream = new FileStream(
				sourcePath,
				FileMode.Open,
				FileAccess.Read,
				FileShare.ReadWrite,
				65536,
				FileOptions.SequentialScan | FileOptions.Asynchronous))
			{
				using (var destinationStream = new FileStream(
					destinationPath,
					FileMode.Create,
					FileAccess.ReadWrite,
					FileShare.ReadWrite,
					65536,
					FileOptions.SequentialScan | FileOptions.Asynchronous))
				{
					await sourceStream.CopyToAsync(destinationStream);
					await destinationStream.FlushAsync();
				}
			}
		}
	}
}
