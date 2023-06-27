using System;
using System.Runtime.InteropServices;

#pragma warning disable IDE0007 // Use implicit type

namespace WebPWrapper
{
    /// <summary>Generic structure for describing the output sample buffer.</summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct WebPRGBABuffer
    {
        /// <summary>pointer to RGBA samples.</summary>
        public IntPtr rgba;

        /// <summary>stride in bytes from one scan line to the next.</summary>
        public int stride;

        /// <summary>total size of the RGBA buffer.</summary>
        public UIntPtr size;
    }
}