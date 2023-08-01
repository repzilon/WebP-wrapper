/////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Wrapper for WebP format in C#. (MIT) Jose M. Piñeiro
///////////////////////////////////////////////////////////////////////////////////////////////////////////// 
// Decode Functions:
// Bitmap Load(string pathFileName) - Load a WebP file in bitmap.
// Bitmap Decode(byte[] rawWebP) - Decode WebP data (rawWebP) to bitmap.
// Bitmap Decode(byte[] rawWebP, WebPDecoderOptions options) - Decode WebP data (rawWebP) to bitmap using 'options'.
// Bitmap GetThumbnailFast(byte[] rawWebP, short width, short height) - Get a thumbnail from WebP data (rawWebP) with dimensions 'width x height'. Fast mode.
// Bitmap GetThumbnailQuality(byte[] rawWebP, short width, short height) - Fast get a thumbnail from WebP data (rawWebP) with dimensions 'width x height'. Quality mode.
// 
// Encode Functions:
// Save(Bitmap pixelMap, string pathFileName, byte quality) - Save bitmap with quality lost to WebP file. Optionally select 'quality'.
// byte[] EncodeLossy(Bitmap pixelMap, byte quality) - Encode bitmap with quality lost to WebP byte array. Optionally select 'quality'.
// byte[] EncodeLossy(Bitmap pixelMap, byte quality, byte speed, bool info) - Encode bitmap with quality lost to WebP byte array. Select 'quality', 'speed' and optionally select 'info'.
// byte[] EncodeLossless(Bitmap pixelMap) - Encode bitmap without quality lost to WebP byte array. 
// byte[] EncodeLossless(Bitmap pixelMap, byte speed, bool info = false) - Encode bitmap without quality lost to WebP byte array. Select 'speed'. 
// byte[] EncodeNearLossless(Bitmap pixelMap, byte quality, byte speed = 9, bool info = false) - Encode bitmap with a near lossless method to WebP byte array. Select 'quality', 'speed' and optionally select 'info'.
// 
// Another functions:
// Version GetVersion() - Get the library version
// WebPInfo GetInfo(byte[] rawWebP) - Get information of WEBP data
// float[] PictureDistortion(Bitmap source, Bitmap reference, DistorsionMetric metricType) - Get PSNR, SSIM or LSIM distortion metric between two pictures
////////////////////////////////////////////////////////////////////////////////////////////////////////////
// TODO : throw more specific exceptions according to context (throw new Exception("Too short message"); is not informative)
// TODO : make multi-targeting SDK projects
// TODO : abstract Bitmap-like objects, so on macOS, Linux, iOS and Android we can use alternatives to GDI+
// TODO : make NuGet package
using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

#pragma warning disable IDE0007 // Use implicit type

namespace WebPWrapper
{
	public static class WebP
	{
		#region | Public Decode Functions |
		/// <summary>Read a WebP file</summary>
		/// <param name="pathFileName">WebP file to load</param>
		/// <returns>Bitmap with the WebP image</returns>
		public static Bitmap Load(string pathFileName)
		{
			return Decode(File.ReadAllBytes(pathFileName));
		}

		/// <summary>Decode a WebP image</summary>
		/// <param name="rawWebP">The data to uncompress</param>
		/// <returns>Bitmap with the WebP image</returns>
		public static Bitmap Decode(byte[] rawWebP)
		{
			Bitmap pixelMap = null;
			BitmapData bmpData = null;
			int st;
			WebPInfo info;
			var pinnedWebP = GCHandle.Alloc(rawWebP, GCHandleType.Pinned);

			try
			{
				//Get image width and height
				info = GetInfo(rawWebP);

				//Create a BitmapData and Lock all pixels to be written
				pixelMap = new Bitmap(info.Width, info.Height, info.HasAlpha ? PixelFormat.Format32bppArgb : PixelFormat.Format24bppRgb);

				bmpData = LockAllBits(pixelMap, ImageLockMode.WriteOnly);

				//Uncompress the image
				st = bmpData.Stride;
				int outputSize = st * info.Height;
				IntPtr ptrData = pinnedWebP.AddrOfPinnedObject();
				var nwc = NativeWrapper.Current;
				IntPtr size = info.HasAlpha
				 ? nwc.DecodeBGRAInto(ptrData, rawWebP.Length, bmpData.Scan0, outputSize, st)
				 : nwc.DecodeBGRInto(ptrData, rawWebP.Length, bmpData.Scan0, outputSize, st);

				if (size == IntPtr.Zero)
				{
					throw new Exception("Can't decode WebP");
				}

				return pixelMap;
			}
			finally
			{
				UnlockPin(pixelMap, bmpData, pinnedWebP);
			}
		}

		/// <summary>Decode a WebP image</summary>
		/// <param name="rawWebP">the data to uncompress</param>
		/// <param name="options">Options for advanced decode</param>
		/// <returns>Bitmap with the WebP image</returns>
		public static Bitmap Decode(byte[] rawWebP, WebPDecoderOptions options)
		{
			var config = new WebPDecoderConfig();
			var nwc = NativeWrapper.Current;
			Bitmap pixelMap = null;
			VP8StatusCode result;
			var pinnedWebP = GCHandle.Alloc(rawWebP, GCHandleType.Pinned);
			BitmapData bmpData = null;

			try
			{
				if (nwc.InitConfig(ref config) == 0)
				{
					throw new Exception("WebPInitDecoderConfig failed. Wrong version?");
				}
				// Read the .webp input file information
				IntPtr ptrRawWebP = pinnedWebP.AddrOfPinnedObject();
#if DEBUG
				int height;
				int width;
#endif
				if (options.use_scaling == 0)
				{
					result = nwc.GetFeatures(ptrRawWebP, rawWebP.Length, ref config.input);
					if (result != VP8StatusCode.VP8_STATUS_OK)
					{
						throw new ExternalException("Failed WebPGetFeatures with error " + result, (int)result);
					}

					//Test cropping values
					if (options.use_cropping == 1)
					{
						if (options.crop_left + options.crop_width > config.input.Width || options.crop_top + options.crop_height > config.input.Height)
						{
							throw new Exception("Crop options exceeded WebP image dimensions");
						}
#if DEBUG
						width = options.crop_width;
						height = options.crop_height;
#endif
					}
				}
#if DEBUG
				else
				{
					width = options.scaled_width;
					height = options.scaled_height;
				}
#endif

				var cop = config.options;
				cop.bypass_filtering = options.bypass_filtering;
				cop.no_fancy_upsampling = options.no_fancy_upsampling;
				cop.use_cropping = options.use_cropping;
				cop.crop_left = options.crop_left;
				cop.crop_top = options.crop_top;
				cop.crop_width = options.crop_width;
				cop.crop_height = options.crop_height;
				cop.use_scaling = options.use_scaling;
				cop.scaled_width = options.scaled_width;
				cop.scaled_height = options.scaled_height;
				cop.use_threads = options.use_threads;
				cop.dithering_strength = options.dithering_strength;
				cop.flip = options.flip;
				cop.alpha_dithering_strength = options.alpha_dithering_strength;

				return CoreDecode(ref config, out bmpData, nwc, (short)pixelMap.Height, (short)pixelMap.Width, true, rawWebP, ptrRawWebP);
			}
			finally
			{
				UnlockPin(pixelMap, bmpData, pinnedWebP);
			}
		}

		/// <summary>Get Thumbnail from webP in mode faster/low quality</summary>
		/// <param name="rawWebP">The data to uncompress</param>
		/// <param name="width">Wanted width of thumbnail</param>
		/// <param name="height">Wanted height of thumbnail</param>
		/// <returns>Bitmap with the WebP thumbnail in 24bpp</returns>
		public static Bitmap GetThumbnailFast(byte[] rawWebP, short width, short height)
		{
			return GetThumbnail(rawWebP, width, height, false);
		}

		/// <summary>Thumbnail from webP in mode slow/high quality</summary>
		/// <param name="rawWebP">The data to uncompress</param>
		/// <param name="width">Wanted width of thumbnail</param>
		/// <param name="height">Wanted height of thumbnail</param>
		/// <returns>Bitmap with the WebP thumbnail</returns>
		public static Bitmap GetThumbnailQuality(byte[] rawWebP, short width, short height)
		{
			return GetThumbnail(rawWebP, width, height, true);
		}
		#endregion

		#region | Public Encode Functions |
		/// <summary>Save bitmap to file in WebP format</summary>
		/// <param name="pixelMap">Bitmap with the WebP image</param>
		/// <param name="pathFileName">The file to write</param>
		/// <param name="quality">Between 0 (lower quality, lowest file size) and 100 (highest quality, higher file size)</param>
		public static void Save(Bitmap pixelMap, string pathFileName, byte quality = 75)
		{
			//Encode in webP format and Write webP file
			File.WriteAllBytes(pathFileName, EncodeLossy(pixelMap, quality));
		}

		/// <summary>Lossy encoding bitmap to WebP (Simple encoding API)</summary>
		/// <param name="pixelMap">Bitmap with the image</param>
		/// <param name="quality">Between 0 (lower quality, lowest file size) and 100 (highest quality, higher file size)</param>
		/// <returns>Compressed data</returns>
		public static byte[] EncodeLossy(Bitmap pixelMap, byte quality = 75)
		{
			return CoreEncode(pixelMap, quality);
		}

		private static WebPConfig ConfigureEncoding(INativeWrapper nwc, WebPPreset preset, float quality, byte speed)
		{
			//Initialize configuration structure
			var config = new WebPConfig();

			//Set compression parameters
			bool blnLossless = Single.IsPositiveInfinity(quality);
			if (blnLossless)
			{
				quality = (speed + 1) * 10;
			}
			if (nwc.InitConfig(ref config, preset, quality) == 0)
			{
				throw new Exception("Can't configure preset");
			}
			config.pass = speed + 1;
			config.thread_level = 1;
			config.alpha_filtering = 2;
			config.use_sharp_yuv = 1;

			return config;
		}

		/// <summary>Lossy encoding bitmap to WebP (Advanced encoding API)</summary>
		/// <param name="pixelMap">Bitmap with the image</param>
		/// <param name="quality">Between 0 (lower quality, lowest file size) and 100 (highest quality, higher file size)</param>
		/// <param name="speed">Between 0 (fastest, lowest compression) and 9 (slower, best compression)</param>
		/// <param name="info">Ask for compression statistics</param>
		/// <param name="stats">Compression statistics, filled when info is true</param>
		/// <returns>Compressed data</returns>
		public static byte[] EncodeLossy(Bitmap pixelMap, byte quality, byte speed, bool info, out WebPAuxStats stats)
		{
			//Set compression parameters
			var nwc = NativeWrapper.Current;
			var config = ConfigureEncoding(nwc, WebPPreset.WEBP_PRESET_DEFAULT, quality, speed);

			// Add additional tuning:
			config.method = speed > 6 ? 6 : speed;
			config.quality = quality;
			config.autofilter = 1;
			config.segments = 4;
			config.partitions = 3;
			config.alpha_quality = quality;

			// Old version does not support preprocessing 4
			config.preprocessing = nwc.GetDecoderVersion() > 1082 ? 4 : 3;

			return AdvancedEncode(pixelMap, config, info, out stats);
		}

		/// <summary>Lossless encoding bitmap to WebP (Simple encoding API)</summary>
		/// <param name="pixelMap">Bitmap with the image</param>
		/// <returns>Compressed data</returns>
		public static byte[] EncodeLossless(Bitmap pixelMap)
		{
			return CoreEncode(pixelMap, null);
		}

		/// <summary>Lossless encoding image in bitmap (Advanced encoding API)</summary>
		/// <param name="pixelMap">Bitmap with the image</param>
		/// <param name="speed">Between 0 (fastest, lowest compression) and 9 (slower, best compression)</param>
		/// <returns>Compressed data</returns>
		public static byte[] EncodeLossless(Bitmap pixelMap, byte speed)
		{
			//Set compression parameters
			var nwc = NativeWrapper.Current;
			var config = ConfigureEncoding(nwc, WebPPreset.WEBP_PRESET_DEFAULT, Single.PositiveInfinity, speed);

			//Old version of DLL does not support info and WebPConfigLosslessPreset
			if (nwc.GetDecoderVersion() <= 1082)
			{
				config.lossless = 1;
				config.method = speed > 6 ? 6 : speed;
				config.quality = (speed + 1) * 10;
			}
			else if (nwc.ConfigLosslessPreset(ref config, speed) == 0)
			{
				throw new Exception("Can´t configure lossless preset");
			}
			config.exact = 0;

			WebPAuxStats stats;
			return AdvancedEncode(pixelMap, config, false, out stats);
		}

		public static byte[] EncodeNearLossless(Bitmap pixelMap, byte quality, byte speed = 9)
		{
			WebPAuxStats dummy;
			return EncodeNearLossless(pixelMap, quality, speed, false, out dummy);
		}

		public static byte[] EncodeNearLossless(Bitmap pixelMap, byte quality, byte speed, out WebPAuxStats stats)
		{
			return EncodeNearLossless(pixelMap, quality, speed, true, out stats);
		}

		/// <summary>Near lossless encoding image in bitmap</summary>
		/// <param name="pixelMap">Bitmap with the image</param>
		/// <param name="quality">Between 0 (lower quality, lowest file size) and 100 (highest quality, higher file size)</param>
		/// <param name="speed">Between 0 (fastest, lowest compression) and 9 (slower, best compression)</param>
		/// <returns>Compress data</returns>
		public static byte[] EncodeNearLossless(Bitmap pixelMap, byte quality, byte speed, bool info, out WebPAuxStats stats)
		{
			var nwc = NativeWrapper.Current;
			//test DLL version
			if (nwc.GetDecoderVersion() <= 1082)
			{
				throw new NotSupportedException("This DLL version does not support EncodeNearLossless");
			}

			//Set compression parameters
			var config = ConfigureEncoding(nwc, WebPPreset.WEBP_PRESET_DEFAULT, Single.PositiveInfinity, speed);

			if (nwc.ConfigLosslessPreset(ref config, speed) == 0)
			{
				throw new Exception("Can´t configure lossless preset");
			}

			config.near_lossless = quality;
			config.exact = 0;

			return AdvancedEncode(pixelMap, config, info, out stats);
		}
		#endregion

		#region | Another Public Functions |
		/// <summary>Get the libwebp version</summary>
		/// <returns>Version of library</returns>
		public static Version GetVersion()
		{
			int v = NativeWrapper.Current.GetDecoderVersion();
			return new Version((v >> 16) % 256, (v >> 8) % 256, v % 256);
		}

		/// <summary>Get info of WEBP data</summary>
		/// <param name="rawWebP">The data of WebP</param>
		public static WebPInfo GetInfo(byte[] rawWebP)
		{
			var features = new WebPBitstreamFeatures();
			int fmt;
			var pinnedWebP = GCHandle.Alloc(rawWebP, GCHandleType.Pinned);

			try
			{
				var result = NativeWrapper.Current.GetFeatures(pinnedWebP.AddrOfPinnedObject(), rawWebP.Length, ref features);
				if (result != 0)
				{
					throw new ExternalException("Unable to get features of WebP image. Status is " + result, (int)result);
				}
				var info = new WebPInfo();
				info.Width = (short)features.Width;
				info.Height = (short)features.Height;
				info.HasAlpha = features.Has_alpha == 1;
				info.IsAnimated = features.Has_animation == 1;
				fmt = features.Format;
				info.Format = (fmt == 1) ? "lossy" : (fmt == 2) ? "lossless" : "undefined";
				return info;
			}
			finally
			{
				Unpin(pinnedWebP);
			}
		}

		/// <summary>Compute PSNR, SSIM or LSIM distortion metric between two pictures. Warning: this function is rather CPU-intensive.</summary>
		/// <param name="source">Picture to measure</param>
		/// <param name="reference">Reference picture</param>
		/// <param name="metricType">distortion metric type: PSNR, SSIM or LSIM</param>
		/// <returns>dB in the Y/U/V/Alpha/All order</returns>
		public static float[] GetPictureDistortion(Bitmap source, Bitmap reference, DistorsionMetric metricType)
		{
			var nwc = NativeWrapper.Current;
			float[] result = new float[5];
			BitmapData sourceBmpData = null;
			BitmapData referenceBmpData = null;
			var wpicSource = new WebPPicture();
			var wpicReference = new WebPPicture();
			var pinnedResult = GCHandle.Alloc(result, GCHandleType.Pinned);

			try
			{
				if (source == null)
				{
					throw new ArgumentNullException("source");
				}
				if (reference == null)
				{
					throw new ArgumentNullException("reference");
				}
				if (metricType > DistorsionMetric.LightweightSimilarity)
				{
					throw new InvalidEnumArgumentException("Bad metric type. Use 0 = PSNR, 1 = SSIM, 2 = LSIM",
					 (int)metricType, typeof(DistorsionMetric));
				}
				if (source.Width != reference.Width || source.Height != reference.Height)
				{
					throw new ArgumentException("Source and Reference pictures have different dimensions");
				}

				SetupPictureForComparison(source, out wpicSource, out sourceBmpData, nwc);

				SetupPictureForComparison(reference, out wpicReference, out referenceBmpData, nwc);

				//Measure
				IntPtr ptrResult = pinnedResult.AddrOfPinnedObject();
				if (nwc.PictureDistortion(ref wpicSource, ref wpicReference, (byte)metricType, ptrResult) != 1)
				{
					throw new Exception("Can´t measure.");
				}

				return result;
			}
			finally
			{
				UnlockFree(source, sourceBmpData, wpicSource, nwc);
				UnlockFree(reference, referenceBmpData, wpicReference, nwc);
				Unpin(pinnedResult);
			}
		}
		#endregion

		#region | Private Methods |
		/// <summary>Encoding image  using Advanced encoding API</summary>
		/// <param name="pixelMap">Bitmap with the image</param>
		/// <param name="config">Configuration for encode</param>
		/// <param name="info">True if need encode info.</param>
		/// <returns>Compressed data</returns>
		private static byte[] AdvancedEncode(Bitmap pixelMap, WebPConfig config, bool info, out WebPAuxStats stats)
		{
			var wpic = new WebPPicture();
			BitmapData bmpData = null;
			IntPtr ptrStats = IntPtr.Zero;
			var nwc = NativeWrapper.Current;
			var pinnedArrayHandle = new GCHandle();
			try
			{
				//Validate the configuration
				if (nwc.ValidateConfig(ref config) != 1)
				{
					throw new Exception("Bad configuration parameters");
				}

				//test pixelMap
				short w, h;
				TestPixelMapBeforeEncode(pixelMap, out w, out h);

				// Setup the input data, allocating a the bitmap, width and height
				bmpData = LockAllBits(pixelMap, ImageLockMode.ReadOnly);
				if (nwc.InitPicture(ref wpic) != 1)
				{
					throw new Exception("Can´t initialize WebPPictureInit");
				}

				wpic = ImportColorData(wpic, bmpData, nwc, true);

				//Set up statistics of compression
				if (info)
				{
					stats = new WebPAuxStats();
					ptrStats = Marshal.AllocHGlobal(Marshal.SizeOf(stats));
					Marshal.StructureToPtr(stats, ptrStats, false);
					wpic.stats = ptrStats;
				}

				var dataWebp = new byte[Math.Max(1024, checked(pixelMap.Width * pixelMap.Height * 2))];
				pinnedArrayHandle = GCHandle.Alloc(dataWebp, GCHandleType.Pinned);
				IntPtr initPtr = pinnedArrayHandle.AddrOfPinnedObject();
				wpic.custom_ptr = initPtr;

				// Set up a byte-writing method (write-to-memory, in this case)
				wpic.writer = Marshal.GetFunctionPointerForDelegate(new WebPMemoryWrite(MyWriter));

				//compress the input samples
				if (nwc.Encode(ref config, ref wpic) != 1)
				{
					throw new Exception("Encoding error: " + ((WebPEncodingError)wpic.error_code).ToString());
				}

				//Unlock the pixels
				pixelMap.UnlockBits(bmpData);
				bmpData = null;

				//Copy webpData to rawWebP
				int size = checked((int)((long)wpic.custom_ptr - (long)initPtr));
				var rawWebP = new byte[size];
#if DEBUG
				int le = dataWebp.Length;
				if ((le > 4096) && (le > (size * 5))) {
					Console.Error.WriteLine("Buffer overallocation for dataWebp: needed {0:n0} allocated {1:n0}", size, le);
				} else if (le < size) {
					Console.Error.WriteLine("Buffer under allocation for dataWebp: needed {0:n0} allocated {1:n0} for {2}x{3}", size, le, pixelMap.Width, pixelMap.Height);
				}
#endif
				Array.Copy(dataWebp, rawWebP, size);

				//Remove compression data
				pinnedArrayHandle.Free();
				dataWebp = null;

				//Show statistics
				stats = info ? (WebPAuxStats)Marshal.PtrToStructure(ptrStats, typeof(WebPAuxStats)) : new WebPAuxStats();

				return rawWebP;
			}
			finally
			{
				Unpin(pinnedArrayHandle);

				//Free statistics memory
				if (ptrStats != IntPtr.Zero)
				{
					Marshal.FreeHGlobal(ptrStats);
				}

				UnlockFree(pixelMap, bmpData, wpic, nwc);
			}
		}

		private static WebPPicture ImportColorData(WebPPicture wpic, BitmapData bmpData, INativeWrapper nwc, bool forceArgb)
		{
			wpic.width = bmpData.Width;
			wpic.height = bmpData.Height;
			bool blnAlpha = (bmpData.PixelFormat == PixelFormat.Format32bppArgb);
			wpic.use_argb = (forceArgb || blnAlpha) ? 1 : 0;

			var s0 = bmpData.Scan0;
			var st = bmpData.Stride;

			if (blnAlpha)
			{
				//Put the bitmap contents in WebPPicture instance
				if (nwc.ImportBGRA(ref wpic, s0, st) != 1)
				{
					throw new OutOfMemoryException("Can´t allocate memory in WebPPictureImportBGRA");
				}
				if (forceArgb)
				{
					wpic.colorspace = (uint)WEBP_CSP_MODE.MODE_bgrA;
				}
			}
			else
			{
				//Put the bitmap contents in WebPPicture instance
				if (nwc.ImportBGR(ref wpic, s0, st) != 1)
				{
					throw new OutOfMemoryException("Can´t allocate memory in WebPPictureImportBGR");
				}
			}

			return wpic;
		}

		private static int MyWriter([In] IntPtr data, UIntPtr data_size, ref WebPPicture picture)
		{
			var ptrCustom = picture.custom_ptr;
			NativeWrapper.Current.CopyMemory(ptrCustom, data, (uint)data_size);
			//picture.custom_ptr = IntPtr.Add(picture.custom_ptr, (int)data_size);   //Only in .NET > 4.0
			picture.custom_ptr = new IntPtr(ptrCustom.ToInt64() + (int)data_size);
			return 1;
		}

		private static BitmapData LockAllBits(Bitmap toLock, ImageLockMode mode)
		{
			return toLock.LockBits(new Rectangle(0, 0, toLock.Width, toLock.Height), mode, toLock.PixelFormat);
		}

		private static Bitmap GetThumbnail(byte[] rawWebP, short width, short height, bool fancy)
		{
			var nwc = NativeWrapper.Current;
			IntPtr ptrRawWebP;
			VP8StatusCode result;
			var pinnedWebP = GCHandle.Alloc(rawWebP, GCHandleType.Pinned);
			Bitmap pixelMap = null;
			BitmapData bmpData = null;

			try
			{
				var config = new WebPDecoderConfig();
				if (nwc.InitConfig(ref config) == 0)
				{
					throw new Exception("WebPInitDecoderConfig failed. Wrong version?");
				}

				ptrRawWebP = pinnedWebP.AddrOfPinnedObject();
				if (fancy)
				{
					result = nwc.GetFeatures(ptrRawWebP, rawWebP.Length, ref config.input);
					if (result != VP8StatusCode.VP8_STATUS_OK)
					{
						throw new ExternalException("Failed WebPGetFeatures with error " + result, (int)result);
					}
				}

				// Set up decode options
				var cop = config.options;
				cop.bypass_filtering = fancy ? 0 : 1;
				cop.no_fancy_upsampling = fancy ? 0 : 1;
				cop.use_threads = 1;
				cop.use_scaling = 1;
				cop.scaled_width = width;
				cop.scaled_height = height;

				return CoreDecode(ref config, out bmpData, nwc, height, width, fancy, rawWebP, ptrRawWebP);
			}
			finally
			{
				UnlockPin(pixelMap, bmpData, pinnedWebP);
			}
		}

		private static Bitmap CoreDecode(ref WebPDecoderConfig config, out BitmapData bmpData, INativeWrapper nwc,
		short height, short width, bool fancy, byte[] rawWebP, IntPtr ptrRawWebP)
		{
			//Create a BitmapData and Lock all pixels to be written
			bool blnAlpha = fancy && (config.input.Has_alpha == 1);
			var cou = config.output;
			cou.colorspace = blnAlpha ? WEBP_CSP_MODE.MODE_bgrA : WEBP_CSP_MODE.MODE_BGR;
			var pixelMap = new Bitmap(config.input.Width, config.input.Height,
			 blnAlpha ? PixelFormat.Format32bppArgb : PixelFormat.Format24bppRgb);
			bmpData = LockAllBits(pixelMap, ImageLockMode.WriteOnly);

			// Specify the output format
			cou.u.RGBA.rgba = bmpData.Scan0;
			cou.u.RGBA.stride = bmpData.Stride;
			cou.u.RGBA.size = (UIntPtr)(height * bmpData.Stride);
			cou.height = height;
			cou.width = width;
			cou.is_external_memory = 1;

			// Decode
			var result = nwc.Decode(ptrRawWebP, rawWebP.Length, ref config);
			if (result != VP8StatusCode.VP8_STATUS_OK)
			{
				throw new ExternalException("Failed WebPDecode with error " + result, (int)result);
			}

			nwc.Free(ref config.output);

			return pixelMap;
		}

		private static void SetupPictureForComparison(Bitmap source, out WebPPicture wpicSource, out BitmapData sourceBmpData, INativeWrapper nwc)
		{
			// Setup the source picture data, allocating the bitmap, width and height
			sourceBmpData = LockAllBits(source, ImageLockMode.ReadOnly);
			wpicSource = new WebPPicture();

			if (nwc.InitPicture(ref wpicSource) != 1)
			{
				throw new Exception("Can´t initialize WebPPictureInit");
			}

			ImportColorData(wpicSource, sourceBmpData, nwc, false);
		}

		private static byte[] CoreEncode(Bitmap pixelMap, byte? quality)
		{
			var nwc = NativeWrapper.Current;
			BitmapData bmpData = null;
			IntPtr bs0;
			int bst;
			short w, h;
			TestPixelMapBeforeEncode(pixelMap, out w, out h);
			IntPtr unmanagedData = IntPtr.Zero;

			try
			{
				//Get pixelMap data
				bmpData = LockAllBits(pixelMap, ImageLockMode.ReadOnly);

				//Compress the pixelMap data
				int size;
				bool blnWithoutAlpha = (pixelMap.PixelFormat == PixelFormat.Format24bppRgb);
				bs0 = bmpData.Scan0;
				bst = bmpData.Stride;
				if (quality.HasValue)
				{
					float qf = quality.Value;
					size = blnWithoutAlpha
					 ? nwc.EncodeBGR(bs0, w, h, bst, qf, out unmanagedData)
					 : nwc.EncodeBGRA(bs0, w, h, bst, qf, out unmanagedData);
				}
				else
				{
					size = blnWithoutAlpha
					 ? nwc.EncodeLosslessBGR(bs0, w, h, bst, out unmanagedData)
					 : nwc.EncodeLosslessBGRA(bs0, w, h, bst, out unmanagedData);
				}

				//Copy image compress data to output array
				byte[] rawWebP = new byte[size];
				Marshal.Copy(unmanagedData, rawWebP, 0, size);

				return rawWebP;
			}
			finally
			{
				UnlockFree(pixelMap, bmpData, unmanagedData, nwc);
			}
		}

		private static void TestPixelMapBeforeEncode(Bitmap pixelMap, out short w, out short h)
		{
			const int WEBP_MAX_DIMENSION = 16383;

			w = checked((short)pixelMap.Width);
			h = checked((short)pixelMap.Height);

			// test pixelMap
			if (w == 0 || h == 0)
			{
				throw new ArgumentException("Bitmap contains no data.", "pixelMap");
			}
			if (w > WEBP_MAX_DIMENSION || h > WEBP_MAX_DIMENSION)
			{
				throw new NotSupportedException("Bitmap dimensions are too large. Maximum is 16383x16383 pixels.");
			}
			var pf = pixelMap.PixelFormat;
			if (pf != PixelFormat.Format24bppRgb && pf != PixelFormat.Format32bppArgb)
			{
				throw new NotSupportedException("Only Format24bppRgb and Format32bppArgb are supported pixel formats.");
			}
		}

		private static void UnlockPin(Bitmap pixelMap, BitmapData data, GCHandle pinnedWebP)
		{
			//Unlock the pixels
			if (data != null)
			{
				pixelMap.UnlockBits(data);
			}
			//Free memory
			if (pinnedWebP.IsAllocated)
			{
				pinnedWebP.Free();
			}
		}

		private static void UnlockFree(Bitmap pixelMap, BitmapData data, IntPtr unmanagedData, INativeWrapper nwc)
		{
			//Unlock the pixels
			if (data != null)
			{
				pixelMap.UnlockBits(data);
			}
			//Free memory
			if (unmanagedData != IntPtr.Zero)
			{
				nwc.Free(unmanagedData);
			}
		}

		private static void UnlockFree(Bitmap pixelMap, BitmapData data, WebPPicture wpic, INativeWrapper nwc)
		{
			//Unlock the pixels
			if (data != null)
			{
				pixelMap.UnlockBits(data);
			}
			//Free memory
			if (wpic.argb != IntPtr.Zero)
			{
				nwc.Free(ref wpic);
			}
		}

		private static void Unpin(GCHandle pinnedWebP)
		{
			//Free memory
			if (pinnedWebP.IsAllocated)
			{
				pinnedWebP.Free();
			}
		}
		#endregion
	}
}
