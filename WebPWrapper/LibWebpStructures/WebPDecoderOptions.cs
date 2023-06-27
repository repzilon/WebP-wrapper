using System;
using System.Runtime.InteropServices;

#pragma warning disable IDE0007 // Use implicit type

namespace WebPWrapper
{
    /// <summary>Decoding options</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct WebPDecoderOptions : IEquatable<WebPDecoderOptions>
    {
        /// <summary>if true, skip the in-loop filtering.</summary>
        public int bypass_filtering;

        /// <summary>if true, use faster point-wise up-sampler.</summary>
        public int no_fancy_upsampling;

        /// <summary>if true, cropping is applied _first_</summary>
        public int use_cropping;

        /// <summary>left position for cropping. Will be snapped to even values.</summary>
        public int crop_left;

        /// <summary>top position for cropping. Will be snapped to even values.</summary>
        public int crop_top;

        /// <summary>width of the cropping area</summary>
        public int crop_width;

        /// <summary>height of the cropping area</summary>
        public int crop_height;

        /// <summary>if true, scaling is applied _afterward_</summary>
        public int use_scaling;

        /// <summary>final width</summary>
        public int scaled_width;

        /// <summary>final height</summary>
        public int scaled_height;

        /// <summary>if true, use multi-threaded decoding</summary>
        public int use_threads;

        /// <summary>dithering strength (0=Off, 100=full)</summary>
        public int dithering_strength;

        /// <summary>flip output vertically</summary>
        public int flip;

        /// <summary>alpha dithering strength in [0..100]</summary>
        public int alpha_dithering_strength;

        /// <summary>padding for later use.</summary>
        private readonly uint pad1;

        /// <summary>padding for later use.</summary>
        private readonly uint pad2;

        /// <summary>padding for later use.</summary>
        private readonly uint pad3;

        /// <summary>padding for later use.</summary>
        private readonly uint pad4;

        /// <summary>padding for later use.</summary>
        private readonly uint pad5;

        public override bool Equals(object obj)
        {
            return obj is WebPDecoderOptions && this.Equals((WebPDecoderOptions)obj);
        }

        public bool Equals(WebPDecoderOptions other)
        {
            return bypass_filtering == other.bypass_filtering &&
             no_fancy_upsampling == other.no_fancy_upsampling &&
             use_cropping == other.use_cropping &&
             crop_left == other.crop_left &&
             crop_top == other.crop_top &&
             crop_width == other.crop_width &&
             crop_height == other.crop_height &&
             use_scaling == other.use_scaling &&
             scaled_width == other.scaled_width &&
             scaled_height == other.scaled_height &&
             use_threads == other.use_threads &&
             dithering_strength == other.dithering_strength &&
             flip == other.flip &&
             alpha_dithering_strength == other.alpha_dithering_strength;
        }

        public override int GetHashCode()
        {
#pragma warning disable U2U1000 // Local variable can be inlined or declared constant
#pragma warning disable RCS1118 // Mark local variable as constant.
            /* const */
            int hashKey = -1521134295; // being a variable makes IL smaller
#pragma warning restore RCS1118 // Mark local variable as constant.
#pragma warning restore U2U1000 // Local variable can be inlined or declared constant
            int hashCode = 1943392893;
            // This is a non-issue because this is a structure, not a class.
#pragma warning disable RECS0025 // Non read-only field referenced in 'GetHashCode()'
            hashCode = (hashCode * hashKey) + bypass_filtering;
            hashCode = (hashCode * hashKey) + no_fancy_upsampling;
            hashCode = (hashCode * hashKey) + use_cropping;
            hashCode = (hashCode * hashKey) + crop_left;
            hashCode = (hashCode * hashKey) + crop_top;
            hashCode = (hashCode * hashKey) + crop_width;
            hashCode = (hashCode * hashKey) + crop_height;
            hashCode = (hashCode * hashKey) + use_scaling;
            hashCode = (hashCode * hashKey) + scaled_width;
            hashCode = (hashCode * hashKey) + scaled_height;
            hashCode = (hashCode * hashKey) + use_threads;
            hashCode = (hashCode * hashKey) + dithering_strength;
            hashCode = (hashCode * hashKey) + flip;
            hashCode = (hashCode * hashKey) + alpha_dithering_strength;
#pragma warning restore RECS0025 // Non read-only field referenced in 'GetHashCode()'
            return hashCode;
        }

        public static bool operator ==(WebPDecoderOptions options1, WebPDecoderOptions options2)
        {
            return options1.Equals(options2);
        }

        public static bool operator !=(WebPDecoderOptions options1, WebPDecoderOptions options2)
        {
            return !(options1 == options2);
        }
    }
}
