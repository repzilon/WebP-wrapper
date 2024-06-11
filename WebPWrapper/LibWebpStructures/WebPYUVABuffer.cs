using System;
using System.Runtime.InteropServices;

#pragma warning disable IDE0007 // Use implicit type

namespace WebPWrapper
{
	[StructLayout(LayoutKind.Sequential)]
	internal struct WebPYUVABuffer
	{
		/// <summary>Pointer to luma samples</summary>
		public IntPtr y;

		/// <summary>Pointer to chroma U samples</summary>
		public IntPtr u;

		/// <summary>Pointer to chroma V samples</summary>
		public IntPtr v;

		/// <summary>Pointer to alpha samples</summary>
		public IntPtr a;

		/// <summary>luma stride</summary>
		public int y_stride;

		/// <summary>chroma U stride</summary>
		public int u_stride;

		/// <summary>chroma V stride</summary>
		public int v_stride;

		/// <summary>alpha stride</summary>
		public int a_stride;

		/// <summary>luma plane size</summary>
		public UIntPtr y_size;

		/// <summary>chroma plane U size</summary>
		public UIntPtr u_size;

		/// <summary>chroma plane V size</summary>
		public UIntPtr v_size;

		/// <summary>alpha plane size</summary>
		public UIntPtr a_size;
	}
}
