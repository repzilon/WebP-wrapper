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
		private const int WEBP_MAX_DIMENSION = 16383;
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
			var pinnedWebP = GCHandle.Alloc(rawWebP, GCHandleType.Pinned);

			try
			{
				//Get image width and height
				WebPInfo info = GetInfo(rawWebP);

				//Create a BitmapData and Lock all pixels to be written
				pixelMap = new Bitmap(info.Width, info.Height, info.HasAlpha ? PixelFormat.Format32bppArgb : PixelFormat.Format24bppRgb);

				bmpData = pixelMap.LockBits(new Rectangle(0, 0, info.Width, info.Height), ImageLockMode.WriteOnly, pixelMap.PixelFormat);

				//Uncompress the image
				int outputSize = bmpData.Stride * info.Height;
				IntPtr ptrData = pinnedWebP.AddrOfPinnedObject();
				var nwc = NativeWrapper.Current;
				IntPtr size = pixelMap.PixelFormat == PixelFormat.Format24bppRgb
				 ? nwc.DecodeBGRInto(ptrData, rawWebP.Length, bmpData.Scan0, outputSize, bmpData.Stride)
				 : nwc.DecodeBGRAInto(ptrData, rawWebP.Length, bmpData.Scan0, outputSize, bmpData.Stride);

				if (size == IntPtr.Zero)
				{
					throw new Exception("Can´t encode WebP");
				}

				return pixelMap;
			}
			finally
			{
				//Unlock the pixels
				if (bmpData != null)
				{
					pixelMap.UnlockBits(bmpData);
				}

				//Free memory
				if (pinnedWebP.IsAllocated)
				{
					pinnedWebP.Free();
				}
			}
		}

		/// <summary>Decode a WebP image</summary>
		/// <param name="rawWebP">the data to uncompress</param>
		/// <param name="options">Options for advanced decode</param>
		/// <returns>Bitmap with the WebP image</returns>
		public static Bitmap Decode(byte[] rawWebP, WebPDecoderOptions options)
		{
			var pinnedWebP = GCHandle.Alloc(rawWebP, GCHandleType.Pinned);
			Bitmap pixelMap = null;
			BitmapData bmpData = null;
			VP8StatusCode result;
			try
			{
				var config = new WebPDecoderConfig();
				var nwc = NativeWrapper.Current;
				if (nwc.InitConfig(ref config) == 0)
				{
					throw new Exception("WebPInitDecoderConfig failed. Wrong version?");
				}
				// Read the .webp input file information
				IntPtr ptrRawWebP = pinnedWebP.AddrOfPinnedObject();
				int height;
				int width;
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

						width = options.crop_width;
						height = options.crop_height;
					}
				}
				else
				{
					width = options.scaled_width;
					height = options.scaled_height;
				}

				config.options.bypass_filtering = options.bypass_filtering;
				config.options.no_fancy_upsampling = options.no_fancy_upsampling;
				config.options.use_cropping = options.use_cropping;
				config.options.crop_left = options.crop_left;
				config.options.crop_top = options.crop_top;
				config.options.crop_width = options.crop_width;
				config.options.crop_height = options.crop_height;
				config.options.use_scaling = options.use_scaling;
				config.options.scaled_width = options.scaled_width;
				config.options.scaled_height = options.scaled_height;
				config.options.use_threads = options.use_threads;
				config.options.dithering_strength = options.dithering_strength;
				config.options.flip = options.flip;
				config.options.alpha_dithering_strength = options.alpha_dithering_strength;

				//Create a BitmapData and Lock all pixels to be written
				if (config.input.Has_alpha == 1)
				{
					config.output.colorspace = WEBP_CSP_MODE.MODE_bgrA;
					pixelMap = new Bitmap(config.input.Width, config.input.Height, PixelFormat.Format32bppArgb);
				}
				else
				{
					config.output.colorspace = WEBP_CSP_MODE.MODE_BGR;
					pixelMap = new Bitmap(config.input.Width, config.input.Height, PixelFormat.Format24bppRgb);
				}
				bmpData = pixelMap.LockBits(new Rectangle(0, 0, pixelMap.Width, pixelMap.Height), ImageLockMode.WriteOnly, pixelMap.PixelFormat);

				// Specify the output format
				config.output.u.RGBA.rgba = bmpData.Scan0;
				config.output.u.RGBA.stride = bmpData.Stride;
				config.output.u.RGBA.size = (UIntPtr)(pixelMap.Height * bmpData.Stride);
				config.output.height = pixelMap.Height;
				config.output.width = pixelMap.Width;
				config.output.is_external_memory = 1;

				// Decode
				result = nwc.Decode(ptrRawWebP, rawWebP.Length, ref config);
				if (result != VP8StatusCode.VP8_STATUS_OK)
				{
					throw new Exception("Failed WebPDecode with error " + result);
				}
				nwc.Free(ref config.output);

				return pixelMap;
			}
			finally
			{
				//Unlock the pixels
				if (bmpData != null)
				{
					pixelMap.UnlockBits(bmpData);
				}

				//Free memory
				if (pinnedWebP.IsAllocated)
				{
					pinnedWebP.Free();
				}
			}
		}

		/// <summary>Get Thumbnail from webP in mode faster/low quality</summary>
		/// <param name="rawWebP">The data to uncompress</param>
		/// <param name="width">Wanted width of thumbnail</param>
		/// <param name="height">Wanted height of thumbnail</param>
		/// <returns>Bitmap with the WebP thumbnail in 24bpp</returns>
		public static Bitmap GetThumbnailFast(byte[] rawWebP, short width, short height)
		{
			var pinnedWebP = GCHandle.Alloc(rawWebP, GCHandleType.Pinned);
			Bitmap pixelMap = null;
			BitmapData bmpData = null;

			try
			{
				var config = new WebPDecoderConfig();
				var nwc = NativeWrapper.Current;
				if (nwc.InitConfig(ref config) == 0)
				{
					throw new Exception("WebPInitDecoderConfig failed. Wrong version?");
				}

				// Set up decode options
				config.options.bypass_filtering = 1;
				config.options.no_fancy_upsampling = 1;
				config.options.use_threads = 1;
				config.options.use_scaling = 1;
				config.options.scaled_width = width;
				config.options.scaled_height = height;

				// Create a BitmapData and Lock all pixels to be written
				pixelMap = new Bitmap(width, height, PixelFormat.Format24bppRgb);
				bmpData = pixelMap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, pixelMap.PixelFormat);

				// Specify the output format
				config.output.colorspace = WEBP_CSP_MODE.MODE_BGR;
				config.output.u.RGBA.rgba = bmpData.Scan0;
				config.output.u.RGBA.stride = bmpData.Stride;
				config.output.u.RGBA.size = (UIntPtr)(height * bmpData.Stride);
				config.output.height = height;
				config.output.width = width;
				config.output.is_external_memory = 1;

				// Decode
				IntPtr ptrRawWebP = pinnedWebP.AddrOfPinnedObject();
				VP8StatusCode result = nwc.Decode(ptrRawWebP, rawWebP.Length, ref config);
				if (result != VP8StatusCode.VP8_STATUS_OK)
				{
					throw new ExternalException("Failed WebPDecode with error " + result, (int)result);
				}

				nwc.Free(ref config.output);

				return pixelMap;
			}
			finally
			{
				//Unlock the pixels
				if (bmpData != null)
				{
					pixelMap.UnlockBits(bmpData);
				}

				//Free memory
				if (pinnedWebP.IsAllocated)
				{
					pinnedWebP.Free();
				}
			}
		}

		/// <summary>Thumbnail from webP in mode slow/high quality</summary>
		/// <param name="rawWebP">The data to uncompress</param>
		/// <param name="width">Wanted width of thumbnail</param>
		/// <param name="height">Wanted height of thumbnail</param>
		/// <returns>Bitmap with the WebP thumbnail</returns>
		public static Bitmap GetThumbnailQuality(byte[] rawWebP, short width, short height)
		{
			var pinnedWebP = GCHandle.Alloc(rawWebP, GCHandleType.Pinned);
			Bitmap pixelMap = null;
			BitmapData bmpData = null;

			try
			{
				var config = new WebPDecoderConfig();
				var nwc = NativeWrapper.Current;
				if (nwc.InitConfig(ref config) == 0)
				{
					throw new Exception("WebPInitDecoderConfig failed. Wrong version?");
				}

				IntPtr ptrRawWebP = pinnedWebP.AddrOfPinnedObject();
				VP8StatusCode result = nwc.GetFeatures(ptrRawWebP, rawWebP.Length, ref config.input);
				if (result != VP8StatusCode.VP8_STATUS_OK)
				{
					throw new ExternalException("Failed WebPGetFeatures with error " + result, (int)result);
				}

				// Set up decode options
				config.options.bypass_filtering = 0;
				config.options.no_fancy_upsampling = 0;
				config.options.use_threads = 1;
				config.options.use_scaling = 1;
				config.options.scaled_width = width;
				config.options.scaled_height = height;

				//Create a BitmapData and Lock all pixels to be written
				if (config.input.Has_alpha == 1)
				{
					config.output.colorspace = WEBP_CSP_MODE.MODE_bgrA;
					pixelMap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
				}
				else
				{
					config.output.colorspace = WEBP_CSP_MODE.MODE_BGR;
					pixelMap = new Bitmap(width, height, PixelFormat.Format24bppRgb);
				}
				bmpData = pixelMap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, pixelMap.PixelFormat);

				// Specify the output format
				config.output.u.RGBA.rgba = bmpData.Scan0;
				config.output.u.RGBA.stride = bmpData.Stride;
				config.output.u.RGBA.size = (UIntPtr)(height * bmpData.Stride);
				config.output.height = height;
				config.output.width = width;
				config.output.is_external_memory = 1;

				// Decode
				result = nwc.Decode(ptrRawWebP, rawWebP.Length, ref config);
				if (result != VP8StatusCode.VP8_STATUS_OK)
				{
					throw new ExternalException("Failed WebPDecode with error " + result, (int)result);
				}

				nwc.Free(ref config.output);

				return pixelMap;
			}
			finally
			{
				//Unlock the pixels
				if (bmpData != null)
				{
					pixelMap.UnlockBits(bmpData);
				}

				//Free memory
				if (pinnedWebP.IsAllocated)
				{
					pinnedWebP.Free();
				}
			}
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
			// test pixelMap
			if (pixelMap.Width == 0 || pixelMap.Height == 0)
			{
				throw new ArgumentException("Bitmap contains no data.", "pixelMap");
			}

			if (pixelMap.Width > WEBP_MAX_DIMENSION || pixelMap.Height > WEBP_MAX_DIMENSION)
			{
				throw new NotSupportedException("Bitmap's dimension is too large. Max is " + WEBP_MAX_DIMENSION + "x" + WEBP_MAX_DIMENSION + " pixels.");
			}

			if (pixelMap.PixelFormat != PixelFormat.Format24bppRgb && pixelMap.PixelFormat != PixelFormat.Format32bppArgb)
			{
				throw new NotSupportedException("Only support Format24bppRgb and Format32bppArgb pixelFormat.");
			}

			BitmapData bmpData = null;
			IntPtr unmanagedData = IntPtr.Zero;
			var nwc = NativeWrapper.Current;

			try
			{
				short w = checked((short)pixelMap.Width);
				short h = checked((short)pixelMap.Height);
				//Get pixelMap data
				bmpData = pixelMap.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, pixelMap.PixelFormat);

				//Compress the pixelMap data
				int size = pixelMap.PixelFormat == PixelFormat.Format24bppRgb
				 ? nwc.EncodeBGR(bmpData.Scan0, w, h, bmpData.Stride, quality, out unmanagedData)
				 : nwc.EncodeBGRA(bmpData.Scan0, w, h, bmpData.Stride, quality, out unmanagedData);

				if (size == 0)
				{
					throw new Exception("Can´t encode WebP");
				}

				//Copy image compress data to output array
				byte[] rawWebP = new byte[size];
				Marshal.Copy(unmanagedData, rawWebP, 0, size);

				return rawWebP;
			}
			finally
			{
				//Unlock the pixels
				if (bmpData != null)
				{
					pixelMap.UnlockBits(bmpData);
				}

				//Free memory
				if (unmanagedData != IntPtr.Zero)
				{
					nwc.Free(unmanagedData);
				}
			}
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
			//Initialize configuration structure
			var config = new WebPConfig();

			//Set compression parameters
			var nwc = NativeWrapper.Current;
			if (nwc.InitConfig(ref config, WebPPreset.WEBP_PRESET_DEFAULT, quality) == 0)
			{
				throw new Exception("Can´t configure preset");
			}

			// Add additional tuning:
			config.method = speed;
			if (config.method > 6)
			{
				config.method = 6;
			}

			config.quality = quality;
			config.autofilter = 1;
			config.pass = speed + 1;
			config.segments = 4;
			config.partitions = 3;
			config.thread_level = 1;
			config.alpha_quality = quality;
			config.alpha_filtering = 2;
			config.use_sharp_yuv = 1;

			if (nwc.GetDecoderVersion() > 1082)     //Old version does not support preprocessing 4
			{
				config.preprocessing = 4;
				config.use_sharp_yuv = 1;
			}
			else
			{
				config.preprocessing = 3;
			}

			return AdvancedEncode(pixelMap, config, info, out stats);
		}

		/// <summary>Lossless encoding bitmap to WebP (Simple encoding API)</summary>
		/// <param name="pixelMap">Bitmap with the image</param>
		/// <returns>Compressed data</returns>
		public static byte[] EncodeLossless(Bitmap pixelMap)
		{
			//test pixelMap
			if (pixelMap.Width == 0 || pixelMap.Height == 0)
			{
				throw new ArgumentException("Bitmap contains no data.", "pixelMap");
			}

			if (pixelMap.Width > WEBP_MAX_DIMENSION || pixelMap.Height > WEBP_MAX_DIMENSION)
			{
				throw new NotSupportedException("Bitmap's dimension is too large. Max is " + WEBP_MAX_DIMENSION + "x" + WEBP_MAX_DIMENSION + " pixels.");
			}

			if (pixelMap.PixelFormat != PixelFormat.Format24bppRgb && pixelMap.PixelFormat != PixelFormat.Format32bppArgb)
			{
				throw new NotSupportedException("Only support Format24bppRgb and Format32bppArgb pixelFormat.");
			}

			BitmapData bmpData = null;
			IntPtr unmanagedData = IntPtr.Zero;
			var nwc = NativeWrapper.Current;
			try
			{
				short w = checked((short)pixelMap.Width);
				short h = checked((short)pixelMap.Height);
				//Get pixelMap data
				bmpData = pixelMap.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, pixelMap.PixelFormat);

				//Compress the pixelMap data
				int size = pixelMap.PixelFormat == PixelFormat.Format24bppRgb
				 ? nwc.EncodeLosslessBGR(bmpData.Scan0, w, h, bmpData.Stride, out unmanagedData)
				 : nwc.EncodeLosslessBGRA(bmpData.Scan0, w, h, bmpData.Stride, out unmanagedData);

				//Copy image compress data to output array
				byte[] rawWebP = new byte[size];
				Marshal.Copy(unmanagedData, rawWebP, 0, size);

				return rawWebP;
			}
			finally
			{
				//Unlock the pixels
				if (bmpData != null)
				{
					pixelMap.UnlockBits(bmpData);
				}

				//Free memory
				if (unmanagedData != IntPtr.Zero)
				{
					nwc.Free(unmanagedData);
				}
			}
		}

		/// <summary>Lossless encoding image in bitmap (Advanced encoding API)</summary>
		/// <param name="pixelMap">Bitmap with the image</param>
		/// <param name="speed">Between 0 (fastest, lowest compression) and 9 (slower, best compression)</param>
		/// <returns>Compressed data</returns>
		public static byte[] EncodeLossless(Bitmap pixelMap, byte speed)
		{
			//Initialize configuration structure
			var config = new WebPConfig();

			//Set compression parameters
			var nwc = NativeWrapper.Current;
			if (nwc.InitConfig(ref config, WebPPreset.WEBP_PRESET_DEFAULT, (speed + 1) * 10) == 0)
			{
				throw new Exception("Can´t configure preset");
			}

			//Old version of DLL does not support info and WebPConfigLosslessPreset
			if (nwc.GetDecoderVersion() > 1082)
			{
				if (nwc.ConfigLosslessPreset(ref config, speed) == 0)
				{
					throw new Exception("Can´t configure lossless preset");
				}
			}
			else
			{
				config.lossless = 1;
				config.method = speed;
				if (config.method > 6)
				{
					config.method = 6;
				}

				config.quality = (speed + 1) * 10;
			}
			config.pass = speed + 1;
			config.thread_level = 1;
			config.alpha_filtering = 2;
			config.use_sharp_yuv = 1;
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
				throw new Exception("This DLL version not support EncodeNearLossless");
			}

			//Initialize configuration structure
			var config = new WebPConfig();
			//Set compression parameters
			if (nwc.InitConfig(ref config, WebPPreset.WEBP_PRESET_DEFAULT, (speed + 1) * 10) == 0)
			{
				throw new Exception("Can´t configure preset");
			}

			if (nwc.ConfigLosslessPreset(ref config, speed) == 0)
			{
				throw new Exception("Can´t configure lossless preset");
			}

			config.pass = speed + 1;
			config.near_lossless = quality;
			config.thread_level = 1;
			config.alpha_filtering = 2;
			config.use_sharp_yuv = 1;
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
			var revision = v % 256;
			var minor = (v >> 8) % 256;
			var major = (v >> 16) % 256;
			return new Version(major, minor, revision);
		}

		/// <summary>Get info of WEBP data</summary>
		/// <param name="rawWebP">The data of WebP</param>
		public static WebPInfo GetInfo(byte[] rawWebP)
		{
			VP8StatusCode result;
			var pinnedWebP = GCHandle.Alloc(rawWebP, GCHandleType.Pinned);

			try
			{
				IntPtr ptrRawWebP = pinnedWebP.AddrOfPinnedObject();

				var features = new WebPBitstreamFeatures();
				result = NativeWrapper.Current.GetFeatures(ptrRawWebP, rawWebP.Length, ref features);
				if (result != 0)
				{
					throw new ExternalException("Unable to get features of WebP image. Status is " + result.ToString(), (int)result);
				}
				var info = new WebPInfo() { Width = (short)features.Width, Height = (short)features.Height, HasAlpha = features.Has_alpha == 1, IsAnimated = features.Has_animation == 1 };

				switch (features.Format)
				{
					case 1:
						info.Format = "lossy";
						break;
					case 2:
						info.Format = "lossless";
						break;
					default:
						info.Format = "undefined";
						break;
				}
				return info;
			}
			finally
			{
				//Free memory
				if (pinnedWebP.IsAllocated)
				{
					pinnedWebP.Free();
				}
			}
		}

		/// <summary>Compute PSNR, SSIM or LSIM distortion metric between two pictures. Warning: this function is rather CPU-intensive.</summary>
		/// <param name="source">Picture to measure</param>
		/// <param name="reference">Reference picture</param>
		/// <param name="metricType">distortion metric type: PSNR, SSIM or LSIM</param>
		/// <returns>dB in the Y/U/V/Alpha/All order</returns>
		public static float[] GetPictureDistortion(Bitmap source, Bitmap reference, DistorsionMetric metricType)
		{
			var wpicSource = new WebPPicture();
			var wpicReference = new WebPPicture();
			BitmapData sourceBmpData = null;
			BitmapData referenceBmpData = null;
			float[] result = new float[5];
			var pinnedResult = GCHandle.Alloc(result, GCHandleType.Pinned);
			var nwc = NativeWrapper.Current;
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
					throw new InvalidEnumArgumentException("Bad metric_type. Use 0 = PSNR, 1 = SSIM, 2 = LSIM");
				}

				if (source.Width != reference.Width || source.Height != reference.Height)
				{
					throw new ArgumentException("Source and Reference pictures have different dimensions");
				}

				// Setup the source picture data, allocating the bitmap, width and height
				sourceBmpData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);
				wpicSource = new WebPPicture();
				
				if (nwc.InitPicture(ref wpicSource) != 1)
				{
					throw new Exception("Can´t initialize WebPPictureInit");
				}

				wpicSource.width = source.Width;
				wpicSource.height = source.Height;

				//Put the source bitmap contents in WebPPicture instance
				if (sourceBmpData.PixelFormat == PixelFormat.Format32bppArgb)
				{
					wpicSource.use_argb = 1;
					if (nwc.ImportBGRA(ref wpicSource, sourceBmpData.Scan0, sourceBmpData.Stride) != 1)
					{
						throw new Exception("Can´t allocate memory in WebPPictureImportBGR");
					}
				}
				else
				{
					wpicSource.use_argb = 0;
					if (nwc.ImportBGR(ref wpicSource, sourceBmpData.Scan0, sourceBmpData.Stride) != 1)
					{
						throw new Exception("Can´t allocate memory in WebPPictureImportBGR");
					}
				}

				// Setup the reference picture data, allocating the bitmap, width and height
				referenceBmpData = reference.LockBits(new Rectangle(0, 0, reference.Width, reference.Height), ImageLockMode.ReadOnly, reference.PixelFormat);
				wpicReference = new WebPPicture();
				if (nwc.InitPicture(ref wpicReference) != 1)
				{
					throw new Exception("Can´t initialize WebPPictureInit");
				}

				wpicReference.width = reference.Width;
				wpicReference.height = reference.Height;
				wpicReference.use_argb = 1;

				//Put the source bitmap contents in WebPPicture instance
				if (sourceBmpData.PixelFormat == PixelFormat.Format32bppArgb)
				{
					wpicSource.use_argb = 1;
					if (nwc.ImportBGRA(ref wpicReference, referenceBmpData.Scan0, referenceBmpData.Stride) != 1)
					{
						throw new Exception("Can´t allocate memory in WebPPictureImportBGR");
					}
				}
				else
				{
					wpicSource.use_argb = 0;
					if (nwc.ImportBGR(ref wpicReference, referenceBmpData.Scan0, referenceBmpData.Stride) != 1)
					{
						throw new Exception("Can´t allocate memory in WebPPictureImportBGR");
					}
				}

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
				//Unlock the pixels
				if (sourceBmpData != null)
				{
					source.UnlockBits(sourceBmpData);
				}

				if (referenceBmpData != null)
				{
					reference.UnlockBits(referenceBmpData);
				}

				//Free memory
				if (wpicSource.argb != IntPtr.Zero)
				{
					nwc.Free(ref wpicSource);
				}

				if (wpicReference.argb != IntPtr.Zero)
				{
					nwc.Free(ref wpicReference);
				}
				//Free memory
				if (pinnedResult.IsAllocated)
				{
					pinnedResult.Free();
				}
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
			byte[] rawWebP = null;
			byte[] dataWebp = null;
			var wpic = new WebPPicture();
			BitmapData bmpData = null;
			IntPtr ptrStats = IntPtr.Zero;
			var pinnedArrayHandle = new GCHandle();
			var nwc = NativeWrapper.Current;
			try
			{
				//Validate the configuration
				if (nwc.ValidateConfig(ref config) != 1)
				{
					throw new Exception("Bad configuration parameters");
				}

				//test pixelMap
				if (pixelMap.Width == 0 || pixelMap.Height == 0)
				{
					throw new ArgumentException("Bitmap contains no data.", "pixelMap");
				}

				if (pixelMap.Width > WEBP_MAX_DIMENSION || pixelMap.Height > WEBP_MAX_DIMENSION)
				{
					throw new NotSupportedException("Bitmap's dimension is too large. Max is " + WEBP_MAX_DIMENSION + "x" + WEBP_MAX_DIMENSION + " pixels.");
				}

				if (pixelMap.PixelFormat != PixelFormat.Format24bppRgb && pixelMap.PixelFormat != PixelFormat.Format32bppArgb)
				{
					throw new NotSupportedException("Only support Format24bppRgb and Format32bppArgb pixelFormat.");
				}

				// Setup the input data, allocating a the bitmap, width and height
				bmpData = pixelMap.LockBits(new Rectangle(0, 0, pixelMap.Width, pixelMap.Height), ImageLockMode.ReadOnly, pixelMap.PixelFormat);
				if (nwc.InitPicture(ref wpic) != 1)
				{
					throw new Exception("Can´t initialize WebPPictureInit");
				}

				wpic.width = pixelMap.Width;
				wpic.height = pixelMap.Height;
				wpic.use_argb = 1;

				if (pixelMap.PixelFormat == PixelFormat.Format32bppArgb)
				{
					//Put the bitmap contents in WebPPicture instance
					int result = nwc.ImportBGRA(ref wpic, bmpData.Scan0, bmpData.Stride);
					if (result != 1)
					{
						throw new Exception("Can´t allocate memory in WebPPictureImportBGRA");
					}
					wpic.colorspace = (uint)WEBP_CSP_MODE.MODE_bgrA;
				}
				else
				{
					//Put the bitmap contents in WebPPicture instance
					int result = nwc.ImportBGR(ref wpic, bmpData.Scan0, bmpData.Stride);
					if (result != 1)
					{
						throw new Exception("Can´t allocate memory in WebPPictureImportBGR");
					}
				}

				//Set up statistics of compression
				if (info)
				{
					stats = new WebPAuxStats();
					ptrStats = Marshal.AllocHGlobal(Marshal.SizeOf(stats));
					Marshal.StructureToPtr(stats, ptrStats, false);
					wpic.stats = ptrStats;
				}

				dataWebp = new byte[Math.Max(1024, checked(pixelMap.Width * pixelMap.Height * 2))];
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
				int size = (int)((long)wpic.custom_ptr - (long)initPtr);
				rawWebP = new byte[size];
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
				//Free temporal compress memory
				if (pinnedArrayHandle.IsAllocated)
				{
					pinnedArrayHandle.Free();
				}

				//Free statistics memory
				if (ptrStats != IntPtr.Zero)
				{
					Marshal.FreeHGlobal(ptrStats);
				}

				//Unlock the pixels
				if (bmpData != null)
				{
					pixelMap.UnlockBits(bmpData);
				}

				//Free memory
				if (wpic.argb != IntPtr.Zero)
				{
					nwc.Free(ref wpic);
				}
			}
		}

		private static int MyWriter([In] IntPtr data, UIntPtr data_size, ref WebPPicture picture)
		{
			NativeWrapper.Current.CopyMemory(picture.custom_ptr, data, (uint)data_size);
			//picture.custom_ptr = IntPtr.Add(picture.custom_ptr, (int)data_size);   //Only in .NET > 4.0
			picture.custom_ptr = new IntPtr(picture.custom_ptr.ToInt64() + (int)data_size);
			return 1;
		}
		#endregion
	}
}
