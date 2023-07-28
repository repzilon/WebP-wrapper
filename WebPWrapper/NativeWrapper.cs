using System;
using System.Runtime.InteropServices;

namespace WebPWrapper
{
	internal static class NativeWrapper
	{
		public static readonly INativeWrapper Current = SelectWrapper();

		private static INativeWrapper SelectWrapper()
		{
			if (Environment.OSVersion.Platform == PlatformID.Win32NT) {
				var intPtrSize = IntPtr.Size;
				if (intPtrSize == 4) {
					return new Win32NativeWrapper();
				} else if (intPtrSize == 8) {
					return new Win64NativeWrapper();
				} else {
					throw new PlatformNotSupportedException("An unknown processor is running this version of Windows.");
				}
			} else {
				throw new PlatformNotSupportedException("The current operating system is not supported by this wrapper, only Windows for now.");
  			}
		}
	}

	/// <summary>The writer type for output compress data</summary>
	/// <param name="data">Data returned</param>
	/// <param name="data_size">Size of data returned</param>
	/// <param name="wpic">Picture structure</param>
	/// <returns></returns>
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	internal delegate int WebPMemoryWrite([In] IntPtr data, UIntPtr data_size, ref WebPPicture wpic);
}
