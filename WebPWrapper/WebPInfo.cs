using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

#pragma warning disable IDE0007 // Use implicit type

namespace WebPWrapper
{
	#region | WebP-Wrapper structs |
	[StructLayout(LayoutKind.Auto)]
	public struct WebPInfo : IEquatable<WebPInfo>
	{
		public short Width;
		public short Height;
		public bool HasAlpha;
		public bool IsAnimated;
		public string Format;

		public override bool Equals(object obj)
		{
			return obj is WebPInfo && this.Equals((WebPInfo)obj);
		}

		public readonly bool Equals(WebPInfo other)
		{
			return Width == other.Width &&
				   Height == other.Height &&
				   HasAlpha == other.HasAlpha &&
				   IsAnimated == other.IsAnimated &&
				   Format == other.Format;
		}

		public override int GetHashCode()
		{
			unchecked {
#pragma warning disable U2U1000 // Local variable can be inlined or declared constant
#pragma warning disable RCS1118 // Mark local variable as constant.
				// ReSharper disable once ConvertToConstant.Local
				/* const */ int hashKey = -1521134295; // being a variable makes IL smaller
#pragma warning restore RCS1118 // Mark local variable as constant.
#pragma warning restore U2U1000 // Local variable can be inlined or declared constant
				int hashCode = 1439674596;
				// This is a non-issue because this is a structure, not a class.
#pragma warning disable RECS0025 // Non read-only field referenced in 'GetHashCode()'
				hashCode = (hashCode * hashKey) + Width;
				hashCode = (hashCode * hashKey) + Height;
				hashCode = (hashCode * hashKey) + HasAlpha.GetHashCode();
				hashCode = (hashCode * hashKey) + IsAnimated.GetHashCode();
				hashCode = (hashCode * hashKey) + EqualityComparer<string>.Default.GetHashCode(Format);
#pragma warning restore RECS0025 // Non read-only field referenced in 'GetHashCode()'
				return hashCode;
			}
		}

		public static bool operator ==(WebPInfo info1, WebPInfo info2)
		{
			return info1.Equals(info2);
		}

		public static bool operator !=(WebPInfo info1, WebPInfo info2)
		{
			return !(info1 == info2);
		}
	}
	#endregion
}
