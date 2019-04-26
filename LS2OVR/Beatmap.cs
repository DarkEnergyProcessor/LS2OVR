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
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using fNbt;

namespace LS2OVR
{

internal struct FileInfo
{
	public String filename;
	public Int32 offset;
	public Int32 size;

	public FileInfo(NbtCompound data)
	{
		filename = data.Get<NbtString>("filename").StringValue;
		offset = data.Get<NbtInt>("offset").IntValue;
		size = data.Get<NbtInt>("size").IntValue;
	}

	public static explicit operator NbtCompound(FileInfo self)
	{
		return new NbtCompound() {
			new NbtString("filename", self.filename),
			new NbtInt("offset", self.offset),
			new NbtInt("size", self.size)
		};
	}
};

/// <summary>
/// Supported beatmap compression types.
/// </summary>
public enum BeatmapCompressionType
{
	None,
	GZip,
	ZLib,
	LZ4,
	Zstandard,
	Brotli
};

/// <summary>
/// LS2OVR beatmap object.
/// </summary>
public class Beatmap
{
	/// <summary>
	/// Format version that this library able to read.
	/// </summary>
	public const Int32 TargetVersion = 0;
	/// <summary>
	/// Specification versionthat this library target, in string.
	/// </summary>
	public const String TargetVersionString = "0.7";
	/// <summary>
	/// Library version.
	/// </summary>
	public const String LibraryVersion = "0.1";
	/// <summary>
	/// The file format signature.
	/// </summary>
	public static readonly Byte[] Signature = {108, 105, 118, 101, 115, 105, 109, 51};
	/// <summary>
	/// The end-of-file marker signature.
	/// </summary>
	public static readonly Byte[] EndOfFile = {111, 118, 101, 114, 114, 110, 98, 119};

	/// <summary>
	/// Beatmap format version.
	/// </summary>
	public Int32 BeatmapFormatVersion {get; set;}
	/// <summary>
	/// Beatmap metadata.
	/// </summary>
	public Metadata BeatmapMetadata {get; set;}
	/// <summary>
	/// List of beatmaps.
	/// </summary>
	public List<BeatmapData> BeatmapList {get; set;}
	/// <summary>
	/// List of additional files embedded inside the beatmap file.
	/// </summary>
	public Dictionary<String, Byte[]> FileDatabase {get; set;}

	private static readonly Byte[] TagListWorkaround = {10, 0, 1, 95};
	private static readonly Byte[] TransmissionCheck = {26, 10, 13, 10};
	private static readonly Byte[] ZeroBuffer = {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0};

	/// <summary>
	/// Create new Beatmap object by reading LS2OVR beatmap.
	/// </summary>
	/// <param name="stream">Input beatmap stream</param>
	/// <exception cref="ArgumentNullException">Thrown when stream is null.</exception>
	/// <exception cref="ArgumentException">Thrown when the stream is unreadable.</exception>
	/// <exception cref="EndOfStreamException">Thrown when unexected end of stream occured.</exception>
	/// <exception cref="IOException">Thrown when I/O problems occured in stream.</exception>
	/// <exception cref="InvalidBeatmapFileException">Thrown when file is invalid LS2OVR beatmap.</exception>
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
		if ((BeatmapFormatVersion = (Int32) (format & 0x7FFFFFFFU)) > TargetVersion)
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
		BeatmapMetadata = new Metadata(metadataNBTFile.RootTag);

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
			case (Byte) BeatmapCompressionType.LZ4:
			case (Byte) BeatmapCompressionType.Zstandard:
			case (Byte) BeatmapCompressionType.Brotli:
				throw new InvalidBeatmapFileException(String.Format("unsupported compression mode {0}", (Int32) beatmapDataCompressionType));
			default:
				throw new InvalidBeatmapFileException("unknown compression mode");
		}

		// Read beatmap data
		BeatmapList = new List<BeatmapData>();
		using (BinaryReader beatmapDataReader = new BinaryReader(beatmapData))
		{
			Byte beatmapDataCount = beatmapDataReader.ReadByte();

			for (Byte i = 0; i < beatmapDataCount; i++)
			{
				Int32 eachBeatmapDataSize = IPAddress.NetworkToHostOrder(beatmapDataReader.ReadInt32());
				if (eachBeatmapDataSize <= 0)
					throw new InvalidBeatmapFileException("beatmap data has invalid size");

				Byte[] beatmapDataBuffer = beatmapDataReader.ReadBytes(eachBeatmapDataSize);
				Byte[] beatmapDataMD5Hash = beatmapDataReader.ReadBytes(16);

				if (Util.MD5HashEqual(beatmapDataBuffer, beatmapDataMD5Hash))
				{
					try
					{
						NbtFile beatmapRoot = new NbtFile();
						beatmapRoot.LoadFromBuffer(beatmapDataBuffer, 0, eachBeatmapDataSize, NbtCompression.None);
						BeatmapData beatmapDataObject = new BeatmapData(beatmapRoot.RootTag);
						BeatmapList.Add(beatmapDataObject);
					}
					catch (EndOfStreamException) {}
					catch (InvalidDataException) {}
					catch (NbtFormatException) {}
					catch (InvalidCastException) {}
				}
			}
		}

		// Read additional file
		// fNbt doesn't allow TAG_List as root tag, so workaround must be done.
		Int32 fileDatabaseSize = IPAddress.NetworkToHostOrder(reader.ReadInt32());
		Byte[] fileDatabaseWorkaround = new Byte[TagListWorkaround.Length + fileDatabaseSize + 1];
		TagListWorkaround.CopyTo(fileDatabaseWorkaround, 0);
		reader.ReadBytes(fileDatabaseSize).CopyTo(fileDatabaseWorkaround, 4);
		fileDatabaseWorkaround[4 + fileDatabaseSize] = 0;
		
		// Start parse
		NbtFile additionalFiles = new NbtFile();
		additionalFiles.LoadFromBuffer(fileDatabaseWorkaround, 0, fileDatabaseWorkaround.Length, NbtCompression.None);
		List<FileInfo> fileList = new List<FileInfo>();

		// Enumerate file list
		try
		{
			foreach (NbtCompound files in additionalFiles.RootTag.Get<NbtList>("additionalData").ToArray<NbtCompound>())
			{
				try
				{
					FileInfo f = new FileInfo(files);

					if ((f.offset & 0x0F) == 0)
						// Only add ones that are 16-byte aligned.
						fileList.Add(f);
				}
				catch (InvalidCastException) {}
			}
		}
		catch (InvalidCastException) {}

		if (Util.ByteArrayEquals(reader.ReadBytes(8), EndOfFile) == false)
			throw new InvalidBeatmapFileException("EOF marker not found");

		if (fileList.Count > 0)
		{
			Int32 currentPosition = 0;
			Func<Int32, Int32> seekFunc = null;

			if (stream.CanSeek)
			{
				currentPosition = (Int32) stream.Seek(0, SeekOrigin.Current);
				seekFunc = (Int32 curoff) => (Int32) stream.Seek(curoff, SeekOrigin.Current);
			}
			else
			{
				currentPosition =
					// header section
					Signature.Length + 8 +
					// metadata section
					4 + metadataSize + 16 +
					// beatmap data
					9 + beatmapDataCompressedSize +
					// additional data
					4 + fileDatabaseSize +
					// end of file marker
					EndOfFile.Length;
				seekFunc = (Int32 curoff) => {
					reader.ReadBytes(curoff);
					return curoff + currentPosition;
				};
			}

			fileList.Sort((FileInfo a, FileInfo b) => a.offset.CompareTo(b.offset));

			Dictionary<String, Byte[]> fileDB = new Dictionary<String, Byte[]>();

			// enumerate all files
			foreach (FileInfo files in fileList)
			{
				Int32 offset = files.offset - currentPosition;
				if (offset > 0)
					currentPosition = seekFunc(offset);
				else if (offset < 0)
					// Should've detect intersection but uh oh.
					break;

				fileDB.Add(files.filename, reader.ReadBytes(files.size));
				currentPosition += files.size;
			}

			if (fileDB.Count > 0)
				FileDatabase = fileDB;
		}
	}

	/// <summary>
	/// Create new Beatmap object with specified Metadata.
	/// </summary>
	/// <param name="metadata">Beatmap metadata.</param>
	public Beatmap(Metadata metadata)
	{
		BeatmapMetadata = metadata;
	}

	/// <summary>
	/// Writes the beatmap to specified stream.
	/// </summary>
	/// <param name="output">Stream output.</param>
	/// <param name="compression">Beatmap data compression algorithm.</param>
	public void WriteTo(Stream output, BeatmapCompressionType compression = BeatmapCompressionType.GZip)
	{
		//MemoryStream output = new MemoryStream();
		if (output == null)
			throw new ArgumentNullException("output");
		else if (output.CanWrite == false)
			throw new NotSupportedException("stream is not open for writing");

		BinaryWriter writer = new BinaryWriter(output);

		// Header
		// Workaround format writing.
		UInt32 format = TargetVersion | 0x80000000;
		writer.Write(Signature);
		writer.Write(IPAddress.HostToNetworkOrder((Int32) format));
		writer.Write(TransmissionCheck);

		// Metadata
		Byte[] metadataNbt = (new NbtFile((NbtCompound) BeatmapMetadata)).SaveToBuffer(NbtCompression.None);
		Byte[] metadataMD5 = Util.MD5Hash(metadataNbt);
		writer.Write(IPAddress.HostToNetworkOrder(metadataNbt.Length));
		writer.Write(metadataNbt);
		writer.Write(metadataMD5);

		// Beatmap data
		Byte[] beatmapDataBuffer = null;
		writer.Write((Byte) compression);

		using (MemoryStream beatmapDataStream = new MemoryStream())
		{
			BinaryWriter beatmapDataWriter = new BinaryWriter(beatmapDataStream);
			beatmapDataStream.WriteByte((Byte) BeatmapList.Count);
			foreach (BeatmapData beatmap in BeatmapList)
			{
				Byte[] beatmapDataNbt = (new NbtFile((NbtCompound) beatmap)).SaveToBuffer(NbtCompression.None);
				Byte[] beatmapDataMD5 = Util.MD5Hash(beatmapDataNbt);
				beatmapDataWriter.Write(IPAddress.HostToNetworkOrder(beatmapDataNbt.Length));
				beatmapDataWriter.Write(beatmapDataNbt);
				beatmapDataWriter.Write(beatmapDataMD5);
			}

			beatmapDataBuffer = beatmapDataStream.ToArray();
		}

		switch (compression)
		{
			case BeatmapCompressionType.None:
			{
				writer.Write(IPAddress.HostToNetworkOrder(beatmapDataBuffer.Length));
				writer.Write(IPAddress.HostToNetworkOrder(beatmapDataBuffer.Length));
				writer.Write(beatmapDataBuffer);
				break;
			}
			case BeatmapCompressionType.GZip:
			{
				MemoryStream compressorStream = new MemoryStream();
				GZipStream compressor = new GZipStream(compressorStream, CompressionMode.Compress, true);
				compressor.Write(beatmapDataBuffer, 0, beatmapDataBuffer.Length);
				compressor.Close();
				Byte[] compressedData = compressorStream.ToArray();
				writer.Write(IPAddress.HostToNetworkOrder(compressedData.Length));
				writer.Write(IPAddress.HostToNetworkOrder(beatmapDataBuffer.Length));
				writer.Write(compressedData);
				break;
			}
			case BeatmapCompressionType.ZLib:
			{
				MemoryStream compressorStream = new MemoryStream();
				ZLibStream compressor = new ZLibStream(compressorStream, CompressionMode.Compress, true);
				compressor.Write(beatmapDataBuffer, 0, beatmapDataBuffer.Length);
				compressor.Close();
				Byte[] compressedData = compressorStream.ToArray();
				writer.Write(IPAddress.HostToNetworkOrder(compressedData.Length));
				writer.Write(IPAddress.HostToNetworkOrder(beatmapDataBuffer.Length));
				writer.Write(compressedData);
				break;
			}
			case BeatmapCompressionType.LZ4:
			case BeatmapCompressionType.Zstandard:
			case BeatmapCompressionType.Brotli:
				throw new NotImplementedException(String.Format("unsupported compression {0}", compression));
			default:
				throw new NotImplementedException("unknown compression");
		}

		// Additional file. 2-pass
		if (FileDatabase != null && FileDatabase.Count > 0)
		{
			NbtList fileListNbt = new NbtList("additionalData", NbtTagType.Compound);

			// 1st pass
			foreach (KeyValuePair<String, Byte[]> file in FileDatabase)
			{
				fileListNbt.Add(new NbtCompound() {
					new NbtString("filename", file.Key),
					new NbtInt("offset", 0),
					new NbtInt("size", 0)
				});
			}

			// file offset start = current position + filelist nbt size + filelist + eof mark
			Int32 fileStart = Util.AlignNextMultiple((Int32) output.Position + 4 + Util.CreateNbtList(fileListNbt).Length + EndOfFile.Length);
			Int32 offsetStart = fileStart;

			// 2nd pass
			fileListNbt = new NbtList("additionalData", NbtTagType.Compound);
			List<FileInfo> fileList = new List<FileInfo>();

			foreach (KeyValuePair<String, Byte[]> file in FileDatabase)
			{
				FileInfo fileInfo = new FileInfo() {
					filename = file.Key,
					offset = offsetStart,
					size = file.Value.Length
				};
				fileList.Add(fileInfo);
				fileListNbt.Add((NbtCompound) fileInfo);

				offsetStart = Util.AlignNextMultiple(offsetStart + fileInfo.size);
			}

			// Write file list header
			Byte[] additionalFile = Util.CreateNbtList(fileListNbt);
			writer.Write(IPAddress.HostToNetworkOrder(additionalFile.Length));
			writer.Write(additionalFile);
			writer.Write(EndOfFile);

			// Write file data
			foreach (FileInfo file in fileList)
			{
				Int32 padding = file.offset - (Int32) output.Position;

				if (padding > 0)
					writer.Write(ZeroBuffer, 0, padding);
				else if (padding < 0)
					break;
				
				writer.Write(FileDatabase[file.filename]);
			}
		}
		else
			writer.Write(EndOfFile);
	}

	/// <summary>
	/// Write the beatmap data to byte array.
	/// </summary>
	/// <param name="compression">Beatmap data compression algorithm.</param>
	/// <returns>Byte array containing the encoded beatmap data.</returns>
	public Byte[] ToBuffer(BeatmapCompressionType compression = BeatmapCompressionType.GZip)
	{
		MemoryStream stream = new MemoryStream();
		WriteTo(stream, compression);
		return stream.ToArray();
	}

	/// <summary>
	/// Checks whetever specified compression algorithm is supported.
	/// None (uncompressed/no compression), GZip, and Zlib support is guaranteed.
	/// </summary>
	/// <param name="compression">Compression algorithm to check.</param>
	public static Boolean IsCompressionSupported(BeatmapCompressionType compression)
	{
		switch (compression)
		{
			case BeatmapCompressionType.None:
			case BeatmapCompressionType.GZip:
			case BeatmapCompressionType.ZLib:
				return true;
			default:
				return false;
		}
	}
};

}
