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

		public CustomUnitInfo(Byte position, String filename)
		{
			if (position <= 0 || position > 9)
				throw new ArgumentOutOfRangeException("position", "must be in 1-9");

			Position = position;
			Filename = filename ?? throw new ArgumentNullException("filename");
		}

		public CustomUnitInfo(NbtCompound data)
		{
			if (data.TryGet<NbtByte>("position", out NbtByte position))
			{
				Position = position.ByteValue;
				if (Position <= 0 || Position > 9)
					// Cannot use ArgumentOutOfRange exception
					throw new InvalidOperationException("\"position\" is out of range");
			}
			else
				throw new InvalidOperationException("\"position\" field is invalid or missing");

			if (data.TryGet<NbtString>("filename", out NbtString filename))
				Filename = filename.StringValue;
			else
				throw new InvalidOperationException("\"filename\" field is invalid or missing");
		}
	}
}
