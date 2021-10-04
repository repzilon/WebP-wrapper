//#define LossyExperiment
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using WebPWrapper;

#pragma warning disable IDE0007 // Use implicit type

namespace WebP42
{
	[StructLayout(LayoutKind.Auto)]
	internal struct CompressionTrial
	{
		public byte CompressionLevel;

		public byte[] CompressedData;

		public TimeSpan TimeTook;

#if LossyExperiment
		public byte Quality;

		public float PictureSsim;

		public float AlphaSsim;

		public float PicturePsnr;

		public float AlphaPsnr;

		public bool IsVisuallyLossless()
		{
			return (PicturePsnr >= 42) && (PictureSsim >= 20) && (AlphaPsnr >= 42) && (AlphaSsim >= 20);
		}
#endif
	}

	internal static class Program
	{
		private static long totalOriginalSize;
		private static long totalOptimizedSize;
		private static long totalFiles;

		private static void Main(string[] args)
		{
#pragma warning disable U2U1017 // Initialized locals should be used
			DateTime dtmStart = DateTime.UtcNow;
#pragma warning restore U2U1017 // Initialized locals should be used
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

#if LossyExperiment
				WebPAuxStats lossyStats;
				List<CompressionTrial> lossyTrials = new List<CompressionTrial>();
				CompressionTrial ctLossy = TryLossy(bmpGdiplus, 100, lossyTrials, out lossyStats);
				bool blnCandidateForLossy = ctLossy.IsVisuallyLossless();
#endif

				// TODO : Perform some kind of binary search. Start with level 0, 4 and 9, then refine
				CompressionTrial[] losslessTrials = new CompressionTrial[9 + 1];
				for (byte i = 0; i <= 9; i++) {
					DateTime dtmStart = DateTime.UtcNow;
					byte[] bytarCoded = WebP.EncodeLossless(bmpGdiplus, i);
					TimeSpan tsDuration = DateTime.UtcNow - dtmStart;
					losslessTrials[i] = new CompressionTrial() { CompressedData = bytarCoded, CompressionLevel = i, TimeTook = tsDuration };
				}

				CompressionTrial? nctBestLossless = FastestOfSmallest(lngOrigSize, losslessTrials);

#if LossyExperiment
				if (blnCandidateForLossy) {
					for (byte q = 99; q >= 1 && ctLossy.IsVisuallyLossless(); q--) {
						ctLossy = TryLossy(bmpGdiplus, q, lossyTrials, out lossyStats);
					}
					if (lossyTrials.Count >= 2) {
						var ctBestLossy = lossyTrials[lossyTrials.Count - 2];
						var q = ctBestLossy.Quality;
						lossyTrials.Clear();
						for (byte s = 0; s <= 9; s++) {
							ctLossy = TryLossy(bmpGdiplus, q, s, lossyTrials, out lossyStats);
						}
						CompressionTrial? nctBestLossy = FastestOfSmallest(lngOrigSize, lossyTrials);
						if (nctBestLossy != null) {
							File.WriteAllBytes(inputPath + ".lossy" + nctBestLossy.Value.Quality + "z" + nctBestLossy.Value.CompressionLevel + ".webp", nctBestLossy.Value.CompressedData);
						}
					}
				}
#endif

				if (nctBestLossless.HasValue) {
					string strWebpFile = OutputFilePath(inputPath);
					File.WriteAllBytes(strWebpFile, nctBestLossless.Value.CompressedData);
					int intWebpSize = nctBestLossless.Value.CompressedData.Length;

					totalOriginalSize += lngOrigSize;
					totalOptimizedSize += intWebpSize;
					totalFiles++;
					Console.WriteLine("{0,10} {1,10} {2,3}% {3}\t(lossless -z {4})", lngOrigSize, intWebpSize,
					 ComputePercentage(lngOrigSize, intWebpSize), RelativePath(strWebpFile, directory), nctBestLossless.Value.CompressionLevel);
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

#if LossyExperiment
		private static CompressionTrial TryLossy(Bitmap original, byte quality, ICollection<CompressionTrial> trialInfo, out WebPAuxStats emptyReusableStats)
		{
			return TryLossy(original, quality, 9, trialInfo, out emptyReusableStats);
		}

		private static CompressionTrial TryLossy(Bitmap original, byte quality, byte speed, ICollection<CompressionTrial> trialInfo, out WebPAuxStats emptyReusableStats)
		{
			DateTime dtmStart = DateTime.UtcNow;
			byte[] bytarLossy = WebP.EncodeLossy(original, quality, speed, false, out emptyReusableStats);
			TimeSpan tsDuration = DateTime.UtcNow - dtmStart;
			using (Bitmap bmpLossy = WebP.Decode(bytarLossy)) {
				float[] sngarSsim = WebP.GetPictureDistortion(bmpLossy, original, DistorsionMetric.StructuralSimilarity);
				float[] sngarPsnr = WebP.GetPictureDistortion(bmpLossy, original, DistorsionMetric.PeakSignalNoiseRatio);
				CompressionTrial trial = new CompressionTrial() { CompressedData = bytarLossy, CompressionLevel = speed, Quality = quality, TimeTook = tsDuration, PictureSsim = sngarSsim[4], AlphaSsim = sngarSsim[3], PicturePsnr = sngarPsnr[4], AlphaPsnr = sngarPsnr[3] };
				trialInfo.Add(trial);
				return trial;
			}
		}
#endif

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

#pragma warning disable U2U1012 // Parameter types should be specific
		private static bool IsAnimated(Image image, string filePath)
#pragma warning restore U2U1012 // Parameter types should be specific
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
				bytPercent = (byte)Math.Round(newSize * 100.0 / (originalSize * 1.0));
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

		private static CompressionTrial? FastestOfSmallest(long originalSize, IList<CompressionTrial> trials)
		{
			int c = trials.Count;
			bool[] blnarKeep = new bool[c];
			int i;
			for (i = 0; i < c; i++) {
				blnarKeep[i] = trials[i].CompressedData.Length < originalSize;
			}
			int intSmallestSize = Int32.MaxValue;
			for (i = 0; i < c; i++) {
				if (blnarKeep[i]) {
					int l = trials[i].CompressedData.Length;
					if (l < intSmallestSize) {
						intSmallestSize = l;
					}
				}
			}
			if (intSmallestSize < Int32.MaxValue) {
				for (i = 0; i < c; i++) {
					blnarKeep[i] &= trials[i].CompressedData.Length == intSmallestSize;
				}
				CompressionTrial? nctFastest = null;
				for (i = 0; i < c; i++) {
					if (blnarKeep[i]) {
						if (!nctFastest.HasValue || (trials[i].CompressionLevel < nctFastest.Value.CompressionLevel)) {
							nctFastest = trials[i];
						}
					}
				}
				return nctFastest;
			} else {
				return null;
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
