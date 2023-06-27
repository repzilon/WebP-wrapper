using System;
using System.Runtime.InteropServices;

#pragma warning disable IDE0007 // Use implicit type

namespace WebPWrapper
{
    /// <summary>Structure for storing auxiliary statistics (mostly for lossy encoding).</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct WebPAuxStats : IEquatable<WebPAuxStats>
    {
        /// <summary>Final size</summary>
        public int coded_size;

        /// <summary>Peak-signal-to-noise ratio for Y</summary>
        public float PSNRY;

        /// <summary>Peak-signal-to-noise ratio for U</summary>
        public float PSNRU;

        /// <summary>Peak-signal-to-noise ratio for V</summary>
        public float PSNRV;

        /// <summary>Peak-signal-to-noise ratio for All</summary>
        public float PSNRALL;

        /// <summary>Peak-signal-to-noise ratio for Alpha</summary>
        public float PSNRAlpha;

        /// <summary>Number of intra4</summary>
        public int block_count_intra4;

        /// <summary>Number of intra16</summary>
        public int block_count_intra16;

        /// <summary>Number of skipped macro-blocks</summary>
        public int block_count_skipped;

        /// <summary>Approximate number of bytes spent for header</summary>
        public int header_bytes;

        /// <summary>Approximate number of bytes spent for  mode-partition #0</summary>
        public int mode_partition_0;

        /// <summary>Approximate number of bytes spent for DC coefficients for segment 0.</summary>
        public int residual_bytes_DC_segments0;

        /// <summary>Approximate number of bytes spent for AC coefficients for segment 0.</summary>
        public int residual_bytes_AC_segments0;

        /// <summary>Approximate number of bytes spent for UV coefficients for segment 0.</summary>
        public int residual_bytes_uv_segments0;

        /// <summary>Approximate number of bytes spent for DC coefficients for segment 1.</summary>
        public int residual_bytes_DC_segments1;

        /// <summary>Approximate number of bytes spent for AC coefficients for segment 1.</summary>
        public int residual_bytes_AC_segments1;

        /// <summary>Approximate number of bytes spent for UV coefficients for segment 1.</summary>
        public int residual_bytes_uv_segments1;

        /// <summary>Approximate number of bytes spent for DC coefficients for segment 2.</summary>
        public int residual_bytes_DC_segments2;

        /// <summary>Approximate number of bytes spent for AC coefficients for segment 2.</summary>
        public int residual_bytes_AC_segments2;

        /// <summary>Approximate number of bytes spent for UV coefficients for segment 2.</summary>
        public int residual_bytes_uv_segments2;

        /// <summary>Approximate number of bytes spent for DC coefficients for segment 3.</summary>
        public int residual_bytes_DC_segments3;

        /// <summary>Approximate number of bytes spent for AC coefficients for segment 3.</summary>
        public int residual_bytes_AC_segments3;

        /// <summary>Approximate number of bytes spent for UV coefficients for segment 3.</summary>
        public int residual_bytes_uv_segments3;

        /// <summary>Number of macro-blocks in segments 0</summary>
        public int segment_size_segments0;

        /// <summary>Number of macro-blocks in segments 1</summary>
        public int segment_size_segments1;

        /// <summary>Number of macro-blocks in segments 2</summary>
        public int segment_size_segments2;

        /// <summary>Number of macro-blocks in segments 3</summary>
        public int segment_size_segments3;

        /// <summary>Quantizer values for segment 0</summary>
        public int segment_quant_segments0;

        /// <summary>Quantizer values for segment 1</summary>
        public int segment_quant_segments1;

        /// <summary>Quantizer values for segment 2</summary>
        public int segment_quant_segments2;

        /// <summary>Quantizer values for segment 3</summary>
        public int segment_quant_segments3;

        /// <summary>Filtering strength for segment 0 [0..63]</summary>
        public int segment_level_segments0;

        /// <summary>Filtering strength for segment 1 [0..63]</summary>
        public int segment_level_segments1;

        /// <summary>Filtering strength for segment 2 [0..63]</summary>
        public int segment_level_segments2;

        /// <summary>Filtering strength for segment 3 [0..63]</summary>
        public int segment_level_segments3;

        /// <summary>Size of the transparency data</summary>
        public int alpha_data_size;

        /// <summary>Size of the enhancement layer data</summary>
        public int layer_data_size;

        // lossless encoder statistics
        /// <summary>bit0:predictor bit1:cross-color transform bit2:subtract-green bit3:color indexing</summary>
        public int lossless_features;

        /// <summary>Number of precision bits of histogram</summary>
        public int histogram_bits;

        /// <summary>Precision bits for transform</summary>
        public int transform_bits;

        /// <summary>Number of bits for color cache lookup</summary>
        public int cache_bits;

        /// <summary>Number of color in palette, if used</summary>
        public int palette_size;

        /// <summary>Final lossless size</summary>
        public int lossless_size;

        /// <summary>Lossless header (transform, Huffman, etc.) size</summary>
        public int lossless_hdr_size;

        /// <summary>Lossless image data size</summary>
        public int lossless_data_size;

        /// <summary>Padding for later use.</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2, ArraySubType = UnmanagedType.U4)]
        private readonly uint[] pad;

        public override bool Equals(object obj)
        {
            return obj is WebPAuxStats && this.Equals((WebPAuxStats)obj);
        }

        public bool Equals(WebPAuxStats other)
        {
#pragma warning disable U2U1000 // Local variable can be inlined or declared constant
#pragma warning disable RCS1118 // Mark local variable as constant.
            /* const */
            float EPSILON = 0.0001F; // being a variable makes IL smaller
#pragma warning restore RCS1118 // Mark local variable as constant.
#pragma warning restore U2U1000 // Local variable can be inlined or declared constant
            return coded_size == other.coded_size &&
             Math.Abs(PSNRY - other.PSNRY) < EPSILON &&
             Math.Abs(PSNRU - other.PSNRU) < EPSILON &&
             Math.Abs(PSNRV - other.PSNRV) < EPSILON &&
             Math.Abs(PSNRALL - other.PSNRALL) < EPSILON &&
             Math.Abs(PSNRAlpha - other.PSNRAlpha) < EPSILON &&
             block_count_intra4 == other.block_count_intra4 &&
             block_count_intra16 == other.block_count_intra16 &&
             block_count_skipped == other.block_count_skipped &&
             header_bytes == other.header_bytes &&
             mode_partition_0 == other.mode_partition_0 &&
             residual_bytes_DC_segments0 == other.residual_bytes_DC_segments0 &&
             residual_bytes_AC_segments0 == other.residual_bytes_AC_segments0 &&
             residual_bytes_uv_segments0 == other.residual_bytes_uv_segments0 &&
             residual_bytes_DC_segments1 == other.residual_bytes_DC_segments1 &&
             residual_bytes_AC_segments1 == other.residual_bytes_AC_segments1 &&
             residual_bytes_uv_segments1 == other.residual_bytes_uv_segments1 &&
             residual_bytes_DC_segments2 == other.residual_bytes_DC_segments2 &&
             residual_bytes_AC_segments2 == other.residual_bytes_AC_segments2 &&
             residual_bytes_uv_segments2 == other.residual_bytes_uv_segments2 &&
             residual_bytes_DC_segments3 == other.residual_bytes_DC_segments3 &&
             residual_bytes_AC_segments3 == other.residual_bytes_AC_segments3 &&
             residual_bytes_uv_segments3 == other.residual_bytes_uv_segments3 &&
             segment_size_segments0 == other.segment_size_segments0 &&
             segment_size_segments1 == other.segment_size_segments1 &&
             segment_size_segments2 == other.segment_size_segments2 &&
             segment_size_segments3 == other.segment_size_segments3 &&
             segment_quant_segments0 == other.segment_quant_segments0 &&
             segment_quant_segments1 == other.segment_quant_segments1 &&
             segment_quant_segments2 == other.segment_quant_segments2 &&
             segment_quant_segments3 == other.segment_quant_segments3 &&
             segment_level_segments0 == other.segment_level_segments0 &&
             segment_level_segments1 == other.segment_level_segments1 &&
             segment_level_segments2 == other.segment_level_segments2 &&
             segment_level_segments3 == other.segment_level_segments3 &&
             alpha_data_size == other.alpha_data_size &&
             layer_data_size == other.layer_data_size &&
             lossless_features == other.lossless_features &&
             histogram_bits == other.histogram_bits &&
             transform_bits == other.transform_bits &&
             cache_bits == other.cache_bits &&
             palette_size == other.palette_size &&
             lossless_size == other.lossless_size &&
             lossless_hdr_size == other.lossless_hdr_size &&
             lossless_data_size == other.lossless_data_size;
        }

        public override int GetHashCode()
        {
#pragma warning disable U2U1000 // Local variable can be inlined or declared constant
#pragma warning disable RCS1118 // Mark local variable as constant.
            /* const */
            int hashKey = -1521134295; // being a variable makes IL smaller
#pragma warning restore RCS1118 // Mark local variable as constant.
#pragma warning restore U2U1000 // Local variable can be inlined or declared constant
            int hashCode = -2133640376;
            // This is a non-issue because this is a structure, not a class.
#pragma warning disable RECS0025 // Non read-only field referenced in 'GetHashCode()'
            hashCode = (hashCode * hashKey) + coded_size;
            hashCode = (hashCode * hashKey) + PSNRY.GetHashCode();
            hashCode = (hashCode * hashKey) + PSNRU.GetHashCode();
            hashCode = (hashCode * hashKey) + PSNRV.GetHashCode();
            hashCode = (hashCode * hashKey) + PSNRALL.GetHashCode();
            hashCode = (hashCode * hashKey) + PSNRAlpha.GetHashCode();
            hashCode = (hashCode * hashKey) + block_count_intra4;
            hashCode = (hashCode * hashKey) + block_count_intra16;
            hashCode = (hashCode * hashKey) + block_count_skipped;
            hashCode = (hashCode * hashKey) + header_bytes;
            hashCode = (hashCode * hashKey) + mode_partition_0;
            hashCode = (hashCode * hashKey) + residual_bytes_DC_segments0;
            hashCode = (hashCode * hashKey) + residual_bytes_AC_segments0;
            hashCode = (hashCode * hashKey) + residual_bytes_uv_segments0;
            hashCode = (hashCode * hashKey) + residual_bytes_DC_segments1;
            hashCode = (hashCode * hashKey) + residual_bytes_AC_segments1;
            hashCode = (hashCode * hashKey) + residual_bytes_uv_segments1;
            hashCode = (hashCode * hashKey) + residual_bytes_DC_segments2;
            hashCode = (hashCode * hashKey) + residual_bytes_AC_segments2;
            hashCode = (hashCode * hashKey) + residual_bytes_uv_segments2;
            hashCode = (hashCode * hashKey) + residual_bytes_DC_segments3;
            hashCode = (hashCode * hashKey) + residual_bytes_AC_segments3;
            hashCode = (hashCode * hashKey) + residual_bytes_uv_segments3;
            hashCode = (hashCode * hashKey) + segment_size_segments0;
            hashCode = (hashCode * hashKey) + segment_size_segments1;
            hashCode = (hashCode * hashKey) + segment_size_segments2;
            hashCode = (hashCode * hashKey) + segment_size_segments3;
            hashCode = (hashCode * hashKey) + segment_quant_segments0;
            hashCode = (hashCode * hashKey) + segment_quant_segments1;
            hashCode = (hashCode * hashKey) + segment_quant_segments2;
            hashCode = (hashCode * hashKey) + segment_quant_segments3;
            hashCode = (hashCode * hashKey) + segment_level_segments0;
            hashCode = (hashCode * hashKey) + segment_level_segments1;
            hashCode = (hashCode * hashKey) + segment_level_segments2;
            hashCode = (hashCode * hashKey) + segment_level_segments3;
            hashCode = (hashCode * hashKey) + alpha_data_size;
            hashCode = (hashCode * hashKey) + layer_data_size;
            hashCode = (hashCode * hashKey) + lossless_features;
            hashCode = (hashCode * hashKey) + histogram_bits;
            hashCode = (hashCode * hashKey) + transform_bits;
            hashCode = (hashCode * hashKey) + cache_bits;
            hashCode = (hashCode * hashKey) + palette_size;
            hashCode = (hashCode * hashKey) + lossless_size;
            hashCode = (hashCode * hashKey) + lossless_hdr_size;
            hashCode = (hashCode * hashKey) + lossless_data_size;
#pragma warning restore RECS0025 // Non read-only field referenced in 'GetHashCode()'
            return hashCode;
        }

        public static bool operator ==(WebPAuxStats stats1, WebPAuxStats stats2)
        {
            return stats1.Equals(stats2);
        }

        public static bool operator !=(WebPAuxStats stats1, WebPAuxStats stats2)
        {
            return !(stats1 == stats2);
        }
    }
}
