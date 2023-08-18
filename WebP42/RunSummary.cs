//
//  RunSummary.cs
//
//  Author:
//       René Rhéaume <repzilon@users.noreply.github.com>
//
//  Copyright (c) 2023 René Rhéaume
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
//
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
//
using System;

namespace WebP42
{
	internal static class RunSummary
	{
		private static long totalOriginalSize;
		private static long totalOptimizedSize;
		private static long totalFiles;
		private static DateTime dtmStart = DateTime.UtcNow;

		public static void Start()
		{
			dtmStart = DateTime.UtcNow;
		}

		public static void Accumulate(long originalSize, long optimizedSize)
		{
			totalOriginalSize += originalSize;
			totalOptimizedSize += optimizedSize;
			totalFiles++;
		}

		public static void Output()
		{
			if (totalOriginalSize > 0) {
				Console.WriteLine("{0,10} {1,10} {2,3}% TOTAL for {3} files in {4}", totalOriginalSize, totalOptimizedSize,
				 ComputePercentage(totalOriginalSize, totalOptimizedSize), totalFiles, DateTime.UtcNow - dtmStart);
			}
		}

		public static byte ComputePercentage(long originalSize, long newSize)
		{
			byte bytPercent = 100;
			if (newSize < originalSize) {
				bytPercent = (byte)Math.Round(newSize * 100.0 / (originalSize * 1.0));
				if (bytPercent == 100) {
					bytPercent = 99;
				}
			}
			return bytPercent;
		}
	}
}

