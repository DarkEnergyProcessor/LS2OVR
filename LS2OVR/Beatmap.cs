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
using System.IO;
using System.IO.Compression;
using System.Net;
using fNbt;

namespace LS2OVR
{
	public enum BeatmapCompressionType
	{
		None,
		GZip,
		ZLib,
	};

	public class Beatmap
	{
		/// <summary>
		/// Format version that this library able to read.
		/// </summary>
		public const Int32 TargetVersion = 0;
		/// <summary>
		/// Specification versionthat this library target, in string.
		/// </summary>
		public const String TargetVersionString = "0.5";
		/// <summary>
		/// Library version.
		/// </summary>
		public const String LibraryVersion = "0.1";
		/// <summary>
		/// The file format signature.
		/// </summary>
		public static readonly Byte[] Signature = {108, 105, 118, 101, 115, 105, 109, 51};

		/// <summary>
		/// Create new Beatmap object by reading LS2OVR beatmap.
		/// </summary>
		/// <param name="stream">Input beatmap stream</param>
		/// <exception cref="System.ArgumentException">Thrown when the stream is unreadable.</exception>
		/// <exception cref="System.ArgumentNullException">Thrown when stream is null.</exception>
		/// <exception cref="System.IO.EndOfStreamException">Thrown when unexected end of stream occured.</exception>
		/// <exception cref="System.IO.IOException">Thrown when I/O problems occured.</exception>
		/// <exception cref="LS2OVR.InvalidBeatmapFileException">Thrown when file is invalid LS2OVR beatmap.</exception>
		public Beatmap(Stream stream)
		{
			if (stream == null)
				throw new ArgumentNullException("stream");

			// The stream must be readable
			if (!stream.CanRead)
				throw new ArgumentException("stream is not readable");

			BinaryReader reader = new BinaryReader(stream);

			// Read signature
			Byte[] firstHeader = reader.ReadBytes(8);
			if (Util.ByteArrayEquals(firstHeader, Signature) == false)
				throw new InvalidBeatmapFileException("invalid LS2OVR header");

			// Read format
			Int32 format = IPAddress.NetworkToHostOrder(reader.ReadInt32());

			if ((format & 0x80000000U) == 0)
				throw new InvalidBeatmapFileException("non-8-bit transmission detected");
			if ((format & 0x7FFFFFFFU) > TargetVersion)
				throw new InvalidBeatmapFileException("file format is too new");

			// Detect EOL translation
			Byte[] eolTest = reader.ReadBytes(4);
			if (eolTest[0] != 0x1A || eolTest[1] != 0x0A || eolTest[2] != 0x0D || eolTest[3] != 0x0A)
				throw new InvalidBeatmapFileException("unexpected EOL translation detected");

			// Read metadata
			Int32 metadataSize = IPAddress.NetworkToHostOrder(reader.ReadInt32());
			if (metadataSize <= 0)
				throw new InvalidBeatmapFileException("invalid metadata length");

			Byte[] metadataNBT = reader.ReadBytes(metadataSize);
			Byte[] metadataMD5 = reader.ReadBytes(16);

			if (Util.MD5HashEqual(metadataNBT, metadataMD5) == false)
				throw new InvalidBeatmapFileException("MD5 metadata mismatch");

			// Parse metadata
			NbtFile metadataNBTFile = new NbtFile();
			metadataNBTFile.LoadFromBuffer(metadataNBT, 0, metadataSize, NbtCompression.None);
			NbtCompound metadata = metadataNBTFile.RootTag;

			// Read beatmap data header
			Byte beatmapDataCompressionType = reader.ReadByte();
			Int32 beatmapDataCompressedSize = IPAddress.NetworkToHostOrder(reader.ReadInt32());
			Int32 beatmapDataUncompressedSize = IPAddress.NetworkToHostOrder(reader.ReadInt32());
			MemoryStream beatmapData = null;

			switch (beatmapDataCompressionType)
			{
				case (Byte) BeatmapCompressionType.None:
				{
					if (beatmapDataCompressedSize != beatmapDataUncompressedSize)
						throw new InvalidBeatmapFileException("uncompressed size mismatch");

					beatmapData = new MemoryStream(reader.ReadBytes(beatmapDataCompressedSize));
					break;
				}
				case (Byte) BeatmapCompressionType.GZip:
				{
					using (MemoryStream memoryStream = new MemoryStream(reader.ReadBytes(beatmapDataCompressedSize)))
					{
						MemoryStream decompressedData = new MemoryStream(beatmapDataUncompressedSize);
						try
						{
							GZipStream compressedStream = new GZipStream(memoryStream, CompressionMode.Decompress);
							compressedStream.CopyTo(decompressedData);
							decompressedData.Seek(0, SeekOrigin.Begin);
							beatmapData = decompressedData;
						}
						catch (InvalidDataException)
						{
							throw new InvalidBeatmapFileException("invalid gzip beatmap data");
						}
					}
					break;
				}
				case (Byte) BeatmapCompressionType.ZLib:
				{
					// https://github.com/fragmer/fNbt/blob/bf8fbb6/fNbt/NbtFile.cs#L268-L280
					using (MemoryStream memoryStream = new MemoryStream(reader.ReadBytes(beatmapDataCompressedSize)))
					{
						if (memoryStream.ReadByte() != 78)
							throw new InvalidBeatmapFileException("invalid zlib beatmap data");
						memoryStream.ReadByte();

						MemoryStream decompressedData = new MemoryStream(beatmapDataUncompressedSize);
						try
						{
							DeflateStream compressedStream = new DeflateStream(memoryStream, CompressionMode.Decompress);
							compressedStream.CopyTo(decompressedData);
							decompressedData.Seek(0, SeekOrigin.Begin);
							beatmapData = decompressedData;
						}
						catch (InvalidDataException)
						{
							throw new InvalidBeatmapFileException("invalid zlib beatmap data");
						}
					}

					break;
				}
				default:
				{
					throw new InvalidBeatmapFileException("unknown or unsupported compression mode");
				}
			}

			// Read beatmap data
		}
	};
}
