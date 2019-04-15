// Copyright(c) 2040 Dark Energy Processor
// 
// This software is provided 'as-is', without any express or implied
// warranty.In no event will the authors be held liable for any damages
// arising from the use of this software.
// 
// Permission is granted to anyone to use this software for any purpose,
// including commercial applications, and to alter it and redistribute it
// freely, subject to the following restrictions:
// 
// 1. The origin of this software must not be misrepresented; you must not
//    claim that you wrote the original software.If you use this software
//    in a product, an acknowledgment in the product documentation would be
//    appreciated but is not required.
// 2. Altered source versions must be plainly marked as such, and must not be
//    misrepresented as being the original software.
// 3. This notice may not be removed or altered from any source distribution.

using System;
using fNbt;

namespace LS2OVR
{
	public struct CustomUnitInfo
	{
		/// <summary>
		/// Unit position where 1 is rightmost and 9 is leftmost.
		/// </summary>
		public Byte Position {get; set;}
		/// <summary>
		/// Custom unit image filename.
		/// </summary>
		public String Filename {get; set;}

		/// <summary>
		/// Create new CustomUnitInfo with specified unit position and filename.
		/// </summary>
		/// <param name="position">Unit position in range 1-9 where 1 is rightmost and 9 is leftmost.</param>
		/// <param name="filename">Custom unit filename.</param>
		public CustomUnitInfo(Byte position, String filename)
		{
			if (position <= 0 || position > 9)
				throw new ArgumentOutOfRangeException("position", "must be in 1-9");

			Position = position;
			Filename = filename ?? throw new ArgumentNullException("filename");
		}

		/// <summary>
		/// Create new CustomUnitInfo from NBT data.
		/// </summary>
		/// <param name="data">NBT compound data.</param>
		public CustomUnitInfo(NbtCompound data)
		{
			if (data.TryGet("position", out NbtByte position))
			{
				Position = position.ByteValue;
				if (Position <= 0 || Position > 9)
					// Cannot use ArgumentOutOfRange exception
					throw new InvalidOperationException("\"position\" is out of range");
			}
			else
				throw new InvalidOperationException("\"position\" field is invalid or missing");

			if (data.TryGet("filename", out NbtString filename))
				Filename = filename.StringValue;
			else
				throw new InvalidOperationException("\"filename\" field is invalid or missing");
		}
	}
}
