using System;
using System.Runtime.InteropServices;

namespace WebPWrapper
{
	internal sealed class Win64NativeWrapper : INativeWrapper
	{
		public int ConfigLosslessPreset(ref WebPConfig config, byte level)
		{
			return WebPConfigLosslessPreset_x64(ref config, level);
		}

		public VP8StatusCode Decode(IntPtr data, int data_size, ref WebPDecoderConfig webPDecoderConfig)
		{
			return WebPDecode_x64(data, (UIntPtr)data_size, ref webPDecoderConfig);
		}

		public IntPtr DecodeBGRAInto(IntPtr data, int data_size, IntPtr output_buffer, int output_buffer_size, int output_stride)
		{
			return WebPDecodeBGRAInto_x64(data, (UIntPtr)data_size, output_buffer, output_buffer_size, output_stride);
		}

		public IntPtr DecodeBGRInto(IntPtr data, int data_size, IntPtr output_buffer, int output_buffer_size, int output_stride)
		{
			return WebPDecodeBGRInto_x64(data, (UIntPtr)data_size, output_buffer, output_buffer_size, output_stride);
		}

		public int Encode(ref WebPConfig config, ref WebPPicture picture)
		{
			return WebPEncode_x64(ref config, ref picture);
		}

		public int EncodeBGR(IntPtr bgr, short width, short height, int stride, float quality_factor, out IntPtr output)
		{
			return WebPEncodeBGR_x64(bgr, width, height, stride, quality_factor, out output);
		}

		public int EncodeBGRA(IntPtr bgra, short width, short height, int stride, float quality_factor, out IntPtr output)
		{
			return WebPEncodeBGRA_x64(bgra, width, height, stride, quality_factor, out output);
		}

		public int EncodeLosslessBGR(IntPtr bgr, short width, short height, int stride, out IntPtr output)
		{
			return WebPEncodeLosslessBGR_x64(bgr, width, height, stride, out output);
		}

		public int EncodeLosslessBGRA(IntPtr bgra, short width, short height, int stride, out IntPtr output)
		{
			return WebPEncodeLosslessBGRA_x64(bgra, width, height, stride, out output);
		}

		public void Free(ref WebPPicture picture)
		{
			WebPPictureFree_x64(ref picture);
		}

		public void Free(ref WebPDecBuffer buffer)
		{
			WebPFreeDecBuffer_x64(ref buffer);
		}

		public void Free(IntPtr p)
		{
			WebPFree_x64(p);
		}

		public int GetDecoderVersion()
		{
			return WebPGetDecoderVersion_x64();
		}

		public VP8StatusCode GetFeatures(IntPtr rawWebP, int data_size, ref WebPBitstreamFeatures features)
		{
			return WebPGetFeaturesInternal_x64(rawWebP, (UIntPtr)data_size, ref features, Win32NativeWrapper.WEBP_DECODER_ABI_VERSION);
		}

		public int GetInfo(IntPtr data, int data_size, out int width, out int height)
		{
			return WebPGetInfo_x64(data, (UIntPtr)data_size, out width, out height);
		}

		public int ImportBGR(ref WebPPicture wpic, IntPtr bgr, int stride)
		{
			return WebPPictureImportBGR_x64(ref wpic, bgr, stride);
		}

		public int ImportBGRA(ref WebPPicture wpic, IntPtr bgra, int stride)
		{
			return WebPPictureImportBGRA_x64(ref wpic, bgra, stride);
		}

		public int ImportBGRX(ref WebPPicture wpic, IntPtr bgr, int stride)
		{
			return WebPPictureImportBGRX_x64(ref wpic, bgr, stride);
		}

		public int InitConfig(ref WebPConfig config, WebPPreset preset, float quality)
		{
			return WebPConfigInitInternal_x64(ref config, preset, quality, Win32NativeWrapper.WEBP_DECODER_ABI_VERSION);
		}

		public int InitConfig(ref WebPDecoderConfig webPDecoderConfig)
		{
			return WebPInitDecoderConfigInternal_x64(ref webPDecoderConfig, Win32NativeWrapper.WEBP_DECODER_ABI_VERSION);
		}

		public int InitPicture(ref WebPPicture wpic)
		{
			return WebPPictureInitInternal_x64(ref wpic, Win32NativeWrapper.WEBP_DECODER_ABI_VERSION);
		}

		public int PictureDistortion(ref WebPPicture srcPicture, ref WebPPicture refPicture, byte metric_type, IntPtr pResult)
		{
			return WebPPictureDistortion_x64(ref srcPicture, ref refPicture, metric_type, pResult);
		}

		public int ValidateConfig(ref WebPConfig config)
		{
			return WebPValidateConfig_x64(ref config);
		}

		[DllImport("libwebp_x64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPConfigLosslessPreset")]
		private static extern int WebPConfigLosslessPreset_x64(ref WebPConfig config, int level);

		[DllImport("libwebp_x64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPConfigInitInternal")]
		private static extern int WebPConfigInitInternal_x64(ref WebPConfig config, WebPPreset preset, float quality, int version);

		[DllImport("libwebp_x64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPGetFeaturesInternal")]
		private static extern VP8StatusCode WebPGetFeaturesInternal_x64([In] IntPtr rawWebP, UIntPtr data_size, ref WebPBitstreamFeatures features, int version);

		[DllImport("libwebp_x64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPValidateConfig")]
		private static extern int WebPValidateConfig_x64(ref WebPConfig config);

		[DllImport("libwebp_x64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPPictureInitInternal")]
		private static extern int WebPPictureInitInternal_x64(ref WebPPicture wpic, int version);

		[DllImport("libwebp_x64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPPictureImportBGR")]
		private static extern int WebPPictureImportBGR_x64(ref WebPPicture wpic, IntPtr bgr, int stride);

		[DllImport("libwebp_x64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPPictureImportBGRA")]
		private static extern int WebPPictureImportBGRA_x64(ref WebPPicture wpic, IntPtr bgra, int stride);

		[DllImport("libwebp_x64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPPictureImportBGRX")]
		private static extern int WebPPictureImportBGRX_x64(ref WebPPicture wpic, IntPtr bgr, int stride);

		[DllImport("libwebp_x64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPEncode")]
		private static extern int WebPEncode_x64(ref WebPConfig config, ref WebPPicture picture);

		[DllImport("libwebp_x64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPPictureFree")]
		private static extern void WebPPictureFree_x64(ref WebPPicture wpic);

		[DllImport("libwebp_x64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPGetInfo")]
		private static extern int WebPGetInfo_x64([In] IntPtr data, UIntPtr data_size, out int width, out int height);

		[DllImport("libwebp_x64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPDecodeBGRInto")]
		private static extern IntPtr WebPDecodeBGRInto_x64([In] IntPtr data, UIntPtr data_size, IntPtr output_buffer, int output_buffer_size, int output_stride);

		[DllImport("libwebp_x64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPDecodeBGRAInto")]
		private static extern IntPtr WebPDecodeBGRAInto_x64([In] IntPtr data, UIntPtr data_size, IntPtr output_buffer, int output_buffer_size, int output_stride);

		[DllImport("libwebp_x64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPInitDecoderConfigInternal")]
		private static extern int WebPInitDecoderConfigInternal_x64(ref WebPDecoderConfig webPDecoderConfig, int version);

		[DllImport("libwebp_x64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPDecode")]
		private static extern VP8StatusCode WebPDecode_x64(IntPtr data, UIntPtr data_size, ref WebPDecoderConfig config);

		[DllImport("libwebp_x64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPFreeDecBuffer")]
		private static extern void WebPFreeDecBuffer_x64(ref WebPDecBuffer buffer);

		[DllImport("libwebp_x64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPEncodeBGR")]
		private static extern int WebPEncodeBGR_x64([In] IntPtr bgr, int width, int height, int stride, float quality_factor, out IntPtr output);

		[DllImport("libwebp_x64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPEncodeBGRA")]
		private static extern int WebPEncodeBGRA_x64([In] IntPtr bgra, int width, int height, int stride, float quality_factor, out IntPtr output);

		[DllImport("libwebp_x64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPEncodeLosslessBGR")]
		private static extern int WebPEncodeLosslessBGR_x64([In] IntPtr bgr, int width, int height, int stride, out IntPtr output);

		[DllImport("libwebp_x64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPEncodeLosslessBGRA")]
		private static extern int WebPEncodeLosslessBGRA_x64([In] IntPtr bgra, int width, int height, int stride, out IntPtr output);

		[DllImport("libwebp_x64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPFree")]
		private static extern void WebPFree_x64(IntPtr p);

		[DllImport("libwebp_x64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPGetDecoderVersion")]
		private static extern int WebPGetDecoderVersion_x64();

		[DllImport("libwebp_x64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPPictureDistortion")]
		private static extern int WebPPictureDistortion_x64(ref WebPPicture srcPicture, ref WebPPicture refPicture, int metric_type, IntPtr pResult);

		public void CopyMemory(IntPtr dest, IntPtr src, uint count)
		{
			Win32NativeWrapper.RtlMoveMemory(dest, src, count);
		}
	}
}
