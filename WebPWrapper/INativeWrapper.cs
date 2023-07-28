using System;

namespace WebPWrapper
{
	internal interface INativeWrapper
	{
		/// <summary>This function will initialize the configuration according to a predefined set of parameters (referred to by 'preset') and a given quality factor.</summary>
		/// <param name="config">The WebPConfig structure</param>
		/// <param name="preset">Type of image</param>
		/// <param name="quality">Quality of compression</param>
		/// <returns>0 if error</returns>
		int InitConfig(ref WebPConfig config, WebPPreset preset, float quality);

		/// <summary>Get info of WepP image</summary>
		/// <param name="rawWebP">Bytes[] of WebP image</param>
		/// <param name="data_size">Size of rawWebP</param>
		/// <param name="features">Features of WebP image</param>
		/// <returns>VP8StatusCode</returns>
		VP8StatusCode GetFeatures(IntPtr rawWebP, int data_size, ref WebPBitstreamFeatures features);

		/// <summary>Activate the lossless compression mode with the desired efficiency.</summary>
		/// <param name="config">The WebPConfig structure</param>
		/// <param name="level">between 0 (fastest, lowest compression) and 9 (slower, best compression)</param>
		/// <returns>0 in case of parameter error</returns>
		int ConfigLosslessPreset(ref WebPConfig config, byte level);

		/// <summary>Check that configuration is non-NULL and all configuration parameters are within their valid ranges.</summary>
		/// <param name="config">The WebPConfig structure</param>
		/// <returns>1 if configuration is OK</returns>
		int ValidateConfig(ref WebPConfig config);

		/// <summary>Initialize the WebPPicture structure checking the DLL version</summary>
		/// <param name="wpic">The WebPPicture structure</param>
		/// <returns>1 if not error</returns>
		int InitPicture(ref WebPPicture wpic);

		/// <summary>Color-space conversion function to import RGB samples.</summary>
		/// <param name="wpic">The WebPPicture structure</param>
		/// <param name="bgr">Point to BGR data</param>
		/// <param name="stride">stride of BGR data</param>
		/// <returns>Returns 0 in case of memory error.</returns>
		int ImportBGR(ref WebPPicture wpic, IntPtr bgr, int stride);

		/// <summary>Color-space conversion function to import RGB samples.</summary>
		/// <param name="wpic">The WebPPicture structure</param>
		/// <param name="bgra">Point to BGRA data</param>
		/// <param name="stride">stride of BGRA data</param>
		/// <returns>Returns 0 in case of memory error.</returns>
		int ImportBGRA(ref WebPPicture wpic, IntPtr bgra, int stride);

		/// <summary>Color-space conversion function to import RGB samples.</summary>
		/// <param name="wpic">The WebPPicture structure</param>
		/// <param name="bgr">Point to BGR data</param>
		/// <param name="stride">stride of BGR data</param>
		/// <returns>Returns 0 in case of memory error.</returns>
		int ImportBGRX(ref WebPPicture wpic, IntPtr bgr, int stride);

		/// <summary>Compress to WebP format</summary>
		/// <param name="config">The configuration structure for compression parameters</param>
		/// <param name="picture">'picture' hold the source samples in both YUV(A) or ARGB input</param>
		/// <returns>Returns 0 in case of error, 1 otherwise. In case of error, picture->error_code is updated accordingly.</returns>
		int Encode(ref WebPConfig config, ref WebPPicture picture);

		/// <summary>Release the memory allocated by WebPPictureAlloc() or WebPPictureImport*()
		/// Note that this function does _not_ free the memory used by the 'picture' object itself.
		/// Besides memory (which is reclaimed) all other fields of 'picture' are preserved.</summary>
		/// <param name="picture">Picture structure</param>
		void Free(ref WebPPicture picture);

		/// <summary>Validate the WebP image header and retrieve the image height and width. Pointers *width and *height can be passed NULL if deemed irrelevant</summary>
		/// <param name="data">Pointer to WebP image data</param>
		/// <param name="data_size">This is the size of the memory block pointed to by data containing the image data</param>
		/// <param name="width">The range is limited currently from 1 to 16383</param>
		/// <param name="height">The range is limited currently from 1 to 16383</param>
		/// <returns>1 if success, otherwise error code returned in the case of (a) formatting error(s).</returns>
		int GetInfo(IntPtr data, int data_size, out int width, out int height);

		/// <summary>Decode WEBP image pointed to by *data and returns BGR samples into a preallocated buffer</summary>
		/// <param name="data">Pointer to WebP image data</param>
		/// <param name="data_size">This is the size of the memory block pointed to by data containing the image data</param>
		/// <param name="output_buffer">Pointer to decoded WebP image</param>
		/// <param name="output_buffer_size">Size of allocated buffer</param>
		/// <param name="output_stride">Specifies the distance between scan lines</param>
		/// <returns>output_buffer if function succeeds; NULL otherwise</returns>
		IntPtr DecodeBGRInto(IntPtr data, int data_size, IntPtr output_buffer, int output_buffer_size, int output_stride);

		/// <summary>Decode WEBP image pointed to by *data and returns BGR samples into a preallocated buffer</summary>
		/// <param name="data">Pointer to WebP image data</param>
		/// <param name="data_size">This is the size of the memory block pointed to by data containing the image data</param>
		/// <param name="output_buffer">Pointer to decoded WebP image</param>
		/// <param name="output_buffer_size">Size of allocated buffer</param>
		/// <param name="output_stride">Specifies the distance between scan lines</param>
		/// <returns>output_buffer if function succeeds; NULL otherwise</returns>
		IntPtr DecodeBGRAInto(IntPtr data, int data_size, IntPtr output_buffer, int output_buffer_size, int output_stride);

		/// <summary>Initialize the configuration as empty. This function must always be called first, unless WebPGetFeatures() is to be called.</summary>
		/// <param name="webPDecoderConfig">Configuration structure</param>
		/// <returns>False in case of mismatched version.</returns>
		int InitConfig(ref WebPDecoderConfig webPDecoderConfig);

		/// <summary>Decodes the full data at once, taking configuration into account.</summary>
		/// <param name="data">WebP raw data to decode</param>
		/// <param name="data_size">Size of WebP data </param>
		/// <param name="webPDecoderConfig">Configuration structure</param>
		/// <returns>VP8_STATUS_OK if the decoding was successful</returns>
		VP8StatusCode Decode(IntPtr data, int data_size, ref WebPDecoderConfig webPDecoderConfig);

		/// <summary>Free any memory associated with the buffer. Must always be called last. Doesn't free the 'buffer' structure itself.</summary>
		/// <param name="buffer">WebPDecBuffer</param>
		void Free(ref WebPDecBuffer buffer);

		/// <summary>Lossy encoding images</summary>
		/// <param name="bgr">Pointer to BGR image data</param>
		/// <param name="width">The range is limited currently from 1 to 16383</param>
		/// <param name="height">The range is limited currently from 1 to 16383</param>
		/// <param name="stride">Specifies the distance between scan lines</param>
		/// <param name="quality_factor">Ranges from 0 (lower quality) to 100 (highest quality). Controls the loss and quality during compression</param>
		/// <param name="output">output_buffer with WebP image</param>
		/// <returns>Size of WebP Image or 0 if an error occurred</returns>
		int EncodeBGR(IntPtr bgr, short width, short height, int stride, float quality_factor, out IntPtr output);

		/// <summary>Lossy encoding images</summary>
		/// <param name="bgra">Pointer to BGRA image data</param>
		/// <param name="width">The range is limited currently from 1 to 16383</param>
		/// <param name="height">The range is limited currently from 1 to 16383</param>
		/// <param name="stride">Specifies the distance between scan lines</param>
		/// <param name="quality_factor">Ranges from 0 (lower quality) to 100 (highest quality). Controls the loss and quality during compression</param>
		/// <param name="output">output_buffer with WebP image</param>
		/// <returns>Size of WebP Image or 0 if an error occurred</returns>
		int EncodeBGRA(IntPtr bgra, short width, short height, int stride, float quality_factor, out IntPtr output);

		/// <summary>Lossless encoding images pointed to by *data in WebP format</summary>
		/// <param name="bgr">Pointer to BGR image data</param>
		/// <param name="width">The range is limited currently from 1 to 16383</param>
		/// <param name="height">The range is limited currently from 1 to 16383</param>
		/// <param name="stride">Specifies the distance between scan lines</param>
		/// <param name="output">output_buffer with WebP image</param>
		/// <returns>Size of WebP Image or 0 if an error occurred</returns>
		int EncodeLosslessBGR(IntPtr bgr, short width, short height, int stride, out IntPtr output);

		/// <summary>Lossless encoding images pointed to by *data in WebP format</summary>
		/// <param name="bgra">Pointer to BGR image data</param>
		/// <param name="width">The range is limited currently from 1 to 16383</param>
		/// <param name="height">The range is limited currently from 1 to 16383</param>
		/// <param name="stride">Specifies the distance between scan lines</param>
		/// <param name="output">output_buffer with WebP image</param>
		/// <returns>Size of WebP Image or 0 if an error occurred</returns>
		int EncodeLosslessBGRA(IntPtr bgra, short width, short height, int stride, out IntPtr output);

		/// <summary>Releases memory returned by the WebPEncode</summary>
		/// <param name="p">Pointer to memory</param>
		void Free(IntPtr p);

		/// <summary>Get the WebP version library</summary>
		/// <returns>8bits for each of major/minor/revision packet in integer. E.g: v2.5.7 is 0x020507</returns>
		int GetDecoderVersion();

		/// <summary>Compute PSNR, SSIM or LSIM distortion metric between two pictures.</summary>
		/// <param name="srcPicture">Picture to measure</param>
		/// <param name="refPicture">Reference picture</param>
		/// <param name="metric_type">0 = PSNR, 1 = SSIM, 2 = LSIM</param>
		/// <param name="pResult">dB in the Y/U/V/Alpha/All order</param>
		/// <returns>False in case of error (the two pictures don't have same dimension, ...)</returns>
		int PictureDistortion(ref WebPPicture srcPicture, ref WebPPicture refPicture, byte metric_type, IntPtr pResult);

		void CopyMemory(IntPtr dest, IntPtr src, uint count);
	}
}
