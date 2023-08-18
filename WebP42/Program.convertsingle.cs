//
//  Program.convertsingle.cs
//
//  Author:
//       René Rhéaume <repzilon@users.noreply.github.com>
//
//  Copyright (c) 2023 René Rhéaume
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
#define LossyExperiment
#define NearLosslessExperiment
using System;
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

#if LossyExperiment || NearLosslessExperiment
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

	partial class Program
	{
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
#if NearLosslessExperiment
				WebPAuxStats nearLosslessStats;
				List<CompressionTrial> nearLosslessTrials = new List<CompressionTrial>();
				CompressionTrial ctNear = TryNearLossless(bmpGdiplus, 80, nearLosslessTrials, out nearLosslessStats);
				bool blnCandidateForNearLossless = ctNear.IsVisuallyLossless();
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
				// TODO : Use lossy compression when a command line option is specified
				// It's very rare to see lossy compression win over lossless when converting PNG files
				// TODO : For heavily compressed JPEG input files, try to guess the JPEG quality index
				// to use it with the advanced WebP setting made to mimic JPEG quality index. Of course,
				// measure the distortion afterwards.
				CompressionTrial? nctBestLossy = null;
				if (blnCandidateForLossy) {
					// TODO : Go by steps of 4, then refine
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
						nctBestLossy = FastestOfSmallest(lngOrigSize, lossyTrials);
						if (nctBestLossy != null) {
							File.WriteAllBytes(inputPath + ".lossy" + nctBestLossy.Value.Quality + "z" + nctBestLossy.Value.CompressionLevel + ".webp", nctBestLossy.Value.CompressedData);
						}
					}
				}
#endif

#if NearLosslessExperiment
				CompressionTrial? nctBestNear = null;
				if (blnCandidateForNearLossless) {
					for (short q = 60; q >= 0 && ctNear.IsVisuallyLossless(); q -= 20) {
						ctNear = TryNearLossless(bmpGdiplus, (byte)q, nearLosslessTrials, out nearLosslessStats);
					}
					if (nearLosslessTrials.Count >= 2) {
						var ctBestNear = nearLosslessTrials[nearLosslessTrials.Count - 1];
						if (!ctBestNear.IsVisuallyLossless()) {
							ctBestNear = nearLosslessTrials[nearLosslessTrials.Count - 2];
						}
						var q = ctBestNear.Quality;
						nearLosslessTrials.Clear();
						for (byte s = 0; s <= 9; s++) {
							ctNear = TryNearLossless(bmpGdiplus, q, s, nearLosslessTrials, out nearLosslessStats);
						}
						nctBestNear = FastestOfSmallest(lngOrigSize, nearLosslessTrials);
						if (nctBestNear != null) {
							File.WriteAllBytes(inputPath + ".nearlossless" + nctBestNear.Value.Quality + "z" + nctBestNear.Value.CompressionLevel + ".webp", nctBestNear.Value.CompressedData);
						}
					}
				}
#endif

				if (nctBestLossless.HasValue) {
					string strWebpFile = OutputFilePath(inputPath);
					File.WriteAllBytes(strWebpFile, nctBestLossless.Value.CompressedData);
					int intWebpSize = nctBestLossless.Value.CompressedData.Length;

					RunSummary.Accumulate(lngOrigSize, intWebpSize);
					Console.Write("{0,10} {1,10} {2,3}% {3}\t(lossless -z {4})", lngOrigSize, intWebpSize,
					 RunSummary.ComputePercentage(lngOrigSize, intWebpSize), RelativePath(strWebpFile, directory), nctBestLossless.Value.CompressionLevel);
				} else {
					RunSummary.Accumulate(lngOrigSize, lngOrigSize);
					Console.Write("{0,10} {0,10} 100% {1}", lngOrigSize, RelativePath(inputPath, directory));
				}
#if NearLosslessExperiment
				if (!blnCandidateForNearLossless) {
					Console.Write(" [n--]");
				} else if (nctBestNear == null) {
					Console.Write(" [n+-]");
				} else {
					Console.Write(" [n{0} {1:n2}dB SSIM {2:n2}dB PSNR]", nctBestNear.Value.Quality, nctBestNear.Value.PictureSsim, nctBestNear.Value.PicturePsnr);
				}
#endif
#if LossyExperiment
				if (!blnCandidateForNearLossless) {
					Console.Write(" [L--]");
				} else if (nctBestLossy == null) {
					Console.Write(" [L+-]");
				} else {
					Console.Write(" [L{0} {1:n2}dB SSIM {2:n2}dB PSNR]", nctBestLossy.Value.Quality, nctBestLossy.Value.PictureSsim, nctBestLossy.Value.PicturePsnr);
				}
#endif
				Console.Write(Environment.NewLine);
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

		private static string RelativePath(string fullPath, string basePath)
		{
			return String.IsNullOrEmpty(basePath) ? fullPath : fullPath.Replace(basePath, ".");
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
			return RateNonLosslessTrial(original, quality, speed, trialInfo, bytarLossy, tsDuration);
		}
#endif

#if NearLosslessExperiment
		private static CompressionTrial TryNearLossless(Bitmap original, byte quality, ICollection<CompressionTrial> trialInfo, out WebPAuxStats emptyReusableStats)
		{
			return TryNearLossless(original, quality, 9, trialInfo, out emptyReusableStats);
		}

		private static CompressionTrial TryNearLossless(Bitmap original, byte quality, byte speed, ICollection<CompressionTrial> trialInfo, out WebPAuxStats emptyReusableStats)
		{
			DateTime dtmStart = DateTime.UtcNow;
			byte[] bytarLossy = WebP.EncodeNearLossless(original, quality, speed, false, out emptyReusableStats);
			TimeSpan tsDuration = DateTime.UtcNow - dtmStart;
			return RateNonLosslessTrial(original, quality, speed, trialInfo, bytarLossy, tsDuration);
		}
#endif

#if LossyExperiment || NearLosslessExperiment
		private static CompressionTrial RateNonLosslessTrial(Bitmap original, byte quality, byte speed, ICollection<CompressionTrial> trialInfo, byte[] bytarLossy, TimeSpan tsDuration)
		{
			using (Bitmap bmpLossy = WebP.Decode(bytarLossy)) {
				// TODO : use PSNR-HVM-S to compute distortion
				float[] sngarSsim = WebP.GetPictureDistortion(bmpLossy, original, DistorsionMetric.StructuralSimilarity);
				float[] sngarPsnr = WebP.GetPictureDistortion(bmpLossy, original, DistorsionMetric.PeakSignalNoiseRatio);
				CompressionTrial trial = new CompressionTrial() { CompressedData = bytarLossy, CompressionLevel = speed, Quality = quality, TimeTook = tsDuration, PictureSsim = sngarSsim[4], AlphaSsim = sngarSsim[3], PicturePsnr = sngarPsnr[4], AlphaPsnr = sngarPsnr[3] };
				trialInfo.Add(trial);
				return trial;
			}
		}
#endif

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
	}
}

