using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace LS2OVR
{
	// https://github.com/fragmer/fNbt/blob/bf8fbb6/fNbt/ZLibStream.cs
	internal sealed class ZLibStream: DeflateStream
	{
		Int32 adler32A = 1,
			adler32B;

		const Int32 ChecksumModulus = 65521;

		public Int32 Checksum
		{
			get { return unchecked((adler32B*65536) + adler32A); }
		}


		void UpdateChecksum(IList<Byte> data, Int32 offset, Int32 length) {
			for (Int32 counter = 0; counter < length; ++counter) {
				adler32A = (adler32A + (data[offset + counter]))%ChecksumModulus;
				adler32B = (adler32B + adler32A)%ChecksumModulus;
			}
		}


		public ZLibStream(Stream stream, CompressionMode mode, Boolean leaveOpen)
			: base(stream, mode, leaveOpen) { }


		public override void Write(Byte[] array, Int32 offset, Int32 count) {
			UpdateChecksum(array, offset, count);
			base.Write(array, offset, count);
		}
	}
}
