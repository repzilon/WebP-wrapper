﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using WebPWrapper;

namespace WebP42
{
	[StructLayout(LayoutKind.Auto)]
	internal struct CompressionTrial
	{
		public byte CompressionLevel;

		public byte[] CompressedData;

		public TimeSpan TimeTook;
	}

	internal static class Program
	{
		private static long totalOriginalSize;
		private static long totalOptimizedSize;
		private static long totalFiles;

		private static void Main(string[] args)
		{
			DateTime dtmStart = DateTime.UtcNow;
			bool blnRecursive = false;

			try {
				string strWorkDir = Directory.GetCurrentDirectory();
				if ((args == null) || (args.Length < 1) || (args[0].Length < 1)) {
					ProcessDirectory(strWorkDir, blnRecursive);
				} else {
					int c = 0;
					for (int i = 0; i < args.Length; i++) {
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
			if (totalOriginalSize > 0) {
				Console.WriteLine("{0,10} {1,10} {2,3}% TOTAL for {3} files in {4}", totalOriginalSize, totalOptimizedSize,
				 ComputePercentage(totalOriginalSize, totalOptimizedSize), totalFiles, DateTime.UtcNow - dtmStart);
			}
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

		private static void ConvertSingleImage(string inputPath, string directory)
		{
			Bitmap bmpGdiplus = null;
			try {
				long lngOrigSize = new FileInfo(inputPath).Length;
				string strExt = Path.GetExtension(inputPath);
				Bitmap bmpTemp = new Bitmap(inputPath);
				if ((bmpTemp.PixelFormat == PixelFormat.Format24bppRgb) || (bmpTemp.PixelFormat == PixelFormat.Format32bppArgb)) {
					bmpGdiplus = bmpTemp;
				} else {
					bmpGdiplus = new Bitmap(bmpTemp.Width, bmpTemp.Height, PixelFormat.Format32bppArgb);
					using (Graphics gfx = Graphics.FromImage(bmpGdiplus)) {
						gfx.DrawImageUnscaled(bmpTemp, 0, 0);
					}
					bmpTemp.Dispose();
				}
				if (IsAnimated(bmpGdiplus, inputPath)) {
					throw new NotSupportedException("Animated images are not supported yet by WebP42.");
				}

				WebP codec = new WebP();
				CompressionTrial[] losslessTrials = new CompressionTrial[9 + 1];
				for (int i = 0; i <= 9; i++) {
					DateTime dtmStart = DateTime.UtcNow;
					byte[] bytarCoded = codec.EncodeLossless(bmpGdiplus, i);
					TimeSpan tsDuration = DateTime.UtcNow - dtmStart;
					losslessTrials[i] = new CompressionTrial() { CompressedData = bytarCoded, CompressionLevel = (byte)i, TimeTook = tsDuration };
				}
				IEnumerable<CompressionTrial> colSmaller = SmallerThanOriginal(lngOrigSize, losslessTrials);
				CompressionTrial? nctSmallest = SmallestOf(colSmaller);

				if (nctSmallest.HasValue) {
					IEnumerable<CompressionTrial> colMin = SizeEquals(nctSmallest.Value.CompressedData.Length, colSmaller);
					CompressionTrial nctFastest = FastestOf(colMin).Value;

					string strWebpFile = OutputFilePath(inputPath);
					File.WriteAllBytes(strWebpFile, nctFastest.CompressedData);
					int intWebpSize = nctFastest.CompressedData.Length;

					totalOriginalSize += lngOrigSize;
					totalOptimizedSize += intWebpSize;
					totalFiles++;
					Console.WriteLine("{0,10} {1,10} {2,3}% {3}\t(lossless -z {4})", lngOrigSize, intWebpSize,
					 ComputePercentage(lngOrigSize, intWebpSize), RelativePath(strWebpFile, directory), nctFastest.CompressionLevel);
				} else {
					totalOriginalSize += lngOrigSize;
					totalOptimizedSize += lngOrigSize;
					totalFiles++;
					Console.WriteLine("{0,10} {0,10} 100% {1}", lngOrigSize, RelativePath(inputPath, directory));
				}
			} catch (AccessViolationException) {
				Console.Error.WriteLine(inputPath + " cannot be converted.");
				throw;
			} catch (NotSupportedException excNS) {
				Console.Error.WriteLine(inputPath + " cannot be converted. " + excNS.Message);
			} catch (Exception ex) {
				Console.Error.WriteLine(inputPath + " cannot be converted.");
				Console.Error.WriteLine(ex);
			} finally {
				if (bmpGdiplus != null) {
					bmpGdiplus.Dispose();
				}
			}
		}

		private static string RelativePath(string fullPath, string basePath)
		{
			return String.IsNullOrEmpty(basePath) ? fullPath : fullPath.Replace(basePath, ".");
		}

		private static string OutputFilePath(string inputPath)
		{
			string strDir = Path.GetDirectoryName(inputPath);
			string strTitle = Path.GetFileNameWithoutExtension(inputPath);
			List<string> lstImages = FindImages(strDir, strTitle + ".*g*");
			int c = lstImages.Count;
			int m = 0;
			for (int i = 0; i < c; i++) {
				string strFound = lstImages[i];
				if (Path.Combine(strDir, strTitle + Path.GetExtension(strFound)) == strFound) {
					m++;
				}
			}
			// Check if we have more than one file with the same title and different extensions, 
			// and append .webp instead of replace the extension.
			return (m > 1) ? inputPath + ".webp" : Path.ChangeExtension(inputPath, ".webp");
		}

		private static bool IsAnimated(Image image, string filePath)
		{
			if (ImageAnimator.CanAnimate(image)) {
				return true;
			} else {
				Guid uidImageFormat = image.RawFormat.Guid;
				return ((uidImageFormat == ImageFormat.Png.Guid) || (uidImageFormat == ImageFormat.MemoryBmp.Guid)) && IdentifyApng(filePath);
			}
		}

		private static byte ComputePercentage(long originalSize, long newSize)
		{
			byte bytPercent = 100;
			if (newSize < originalSize) {
				bytPercent = (byte)Math.Round((newSize * 100.0) / (originalSize * 1.0));
				if (bytPercent == 100) {
					bytPercent = 99;
				}
			}
			return bytPercent;
		}

		// Translated to C# from https://stackoverflow.com/questions/4525152/can-i-programmatically-determine-if-a-png-is-animated 
		private static bool IdentifyApng(string filepath)
		{
			const int kBufferSize = 1024;
			// Use ISO Latin-1 encoding, not ASCII, so we can get all 8 bits of every byte, even gibberish, as single byte characters.
			using (StreamReader fh = new StreamReader(filepath, System.Text.Encoding.GetEncoding("iso-8859-1"))) {
				string previousdata = "";
				char[] chrarBuffer = new char[kBufferSize];
				while (!fh.EndOfStream) {
					int readLength = fh.ReadBlock(chrarBuffer, 0, kBufferSize);
					string data = new string(chrarBuffer, 0, readLength);
					// The Contains method below does not need we strip NUL bytes beforehand.
					// However, not stripping will hinder usage of default string visualizer in a Visual Studio debug session.
					if (data.Contains("acTL") || (previousdata + data).Contains("acTL")) {
						return true;
					}
					//*
					if (data.Contains("IDAT") || (previousdata + data).Contains("IDAT")) {
						return false;
					}// */
					previousdata = data;
				}
			}
			return false;
		}

		private static CompressionTrial? SmallestOf(IEnumerable<CompressionTrial> colSmaller)
		{
			CompressionTrial? nctSmallest = null;
			foreach (CompressionTrial trial in colSmaller) {
				if (!nctSmallest.HasValue || (trial.CompressedData.Length < nctSmallest.Value.CompressedData.Length)) {
					nctSmallest = trial;
				}
			}
			return nctSmallest;
		}

		private static CompressionTrial? FastestOf(IEnumerable<CompressionTrial> colSmaller)
		{
			CompressionTrial? nctSmallest = null;
			foreach (CompressionTrial trial in colSmaller) {
				if (!nctSmallest.HasValue || (trial.CompressionLevel < nctSmallest.Value.CompressionLevel)) {
					nctSmallest = trial;
				}
			}
			return nctSmallest;
		}

		private static IEnumerable<CompressionTrial> SmallerThanOriginal(long originalSize, CompressionTrial[] trials)
		{
			for (int i = 0; i < trials.Length; i++) {
				var x = trials[i];
				if (x.CompressedData.Length < originalSize) {
					yield return x;
				}
			}
		}

		private static IEnumerable<CompressionTrial> SizeEquals(int size, IEnumerable<CompressionTrial> trials)
		{
			foreach (CompressionTrial x in trials) {
				if (x.CompressedData.Length == size) {
					yield return x;
				}
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