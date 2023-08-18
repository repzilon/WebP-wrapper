//
//  Program.cs
//
//  Author:
//       René Rhéaume <repzilon@users.noreply.github.com>
//
//  Copyright (c) 2021-2023 René Rhéaume
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
//
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
//
using System;
using System.Collections.Generic;
using System.IO;

#pragma warning disable IDE0007 // Use implicit type

namespace WebP42
{
	internal static partial class Program
	{
		private static void Main(string[] args)
		{
			RunSummary.Start();
			bool blnRecursive = false;

			try {
				string strWorkDir = Directory.GetCurrentDirectory();
				// TODO : Help screen
				if ((args == null) || (args.Length < 1) || (args[0].Length < 1)) {
					ProcessDirectory(strWorkDir, blnRecursive);
				} else {
					int c = 0;
					for (int i = 0; i < args.Length; i++) {
						// TODO : change references in files, activated by a command line parameter
						string argi = args[i];
						if ((argi == "-R") || (argi == "/R") || (argi == "--recursive")) {
							blnRecursive = true;
						} else if (File.Exists(argi)) {
							ConvertSingleImage(argi, "");
							c++;
						} else if (Directory.Exists(argi)) {
							ProcessDirectory(argi, blnRecursive);
							c++;
						} else {
							Console.Error.WriteLine(argi + " does not exist.");
						}
						if (c < 1) {
							ProcessDirectory(strWorkDir, blnRecursive);
						}
					}
				}
			} catch (AccessViolationException) {
				Console.Error.WriteLine("FATAL: Detected memory corruption. Stopping.");
			}
			RunSummary.Output();
			// TODO : Use this only when parent process is not a command line prompt (like cmd, PowerShell, a *nix shell port, etc.)
			Console.WriteLine("Press any key to exit...");
			Console.ReadKey();
		}

		private static void ProcessDirectory(string folder, bool recursive)
		{
			var lstGfx = FindImages(folder, recursive);
			var c = lstGfx.Count;
			for (int j = 0; j < c; j++) {
				ConvertSingleImage(lstGfx[j], folder);
			}
		}

		private static bool RemoveUnsupportedFiles(string path)
		{
			string strExt = Path.GetExtension(path);
			return (strExt == ".config") || (strExt == ".svg") || (strExt == ".webp");
		}

		private static List<string> FindImages(string directory, string globPattern)
		{
			return FindImages(directory, globPattern, false);
		}

		private static List<string> FindImages(string directory, bool recursive)
		{
			return FindImages(directory, "*.*g*", recursive);
		}

		private static List<string> FindImages(string directory, string globPattern, bool recursive)
		{
			List<string> lstImages = new List<string>(Directory.GetFiles(directory, globPattern, recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly));
			lstImages.RemoveAll(RemoveUnsupportedFiles);
			lstImages.Sort();
			return lstImages;
		}
	}
}
