using System.Runtime.InteropServices;

#pragma warning disable IDE0007 // Use implicit type

namespace WebPWrapper
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct WebPDecoderConfig
    {
        /// <summary>Immutable bit stream features (optional)</summary>
        public WebPBitstreamFeatures input;

        /// <summary>Output buffer (can point to external memory)</summary>
        public WebPDecBuffer output;

        /// <summary>Decoding options</summary>
        public WebPDecoderOptions options;
    }
}
