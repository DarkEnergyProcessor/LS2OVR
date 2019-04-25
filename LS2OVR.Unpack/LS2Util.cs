using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace LS2OVR.Unpack.LS2
{

internal class Util
{
	public static Boolean ByteArrayEquals(Byte[] a, Byte[] b)
	{
		if (a.Length == 0 || b.Length == 0)
			return false;
		if (a.Length != b.Length)
			return false;

		for (Int32 i = 0; i < a.Length; i++)
		{
			if (a[i].Equals(b[i]) == false)
				return false;
		}

		return true;
	}

	public static UInt16 ReadUInt16LE(BinaryReader reader)
	{
		Byte[] b = reader.ReadBytes(2);
		return (UInt16) (b[0] | ((UInt16) (b[1] << 8)));
	}

	public static UInt32 ReadUInt32LE(BinaryReader reader)
	{
		Byte[] b = reader.ReadBytes(4);
		return b[0] | (((UInt32) b[1]) << 8) | (((UInt32) b[2]) << 16) | (((UInt32) b[3]) << 24);
	}

	public static Byte[] ReadLS2ByteArray(BinaryReader reader)
	{
		UInt32 len = ReadUInt32LE(reader);
		if (len == 0) return null;
		return reader.ReadBytes(checked((Int32) len));
	}

	public static String ReadLS2String(BinaryReader reader)
	{
		UInt32 len = ReadUInt32LE(reader);
		if (len == 0) return String.Empty;
		return Encoding.UTF8.GetString(reader.ReadBytes(checked((Int32) len)));
	}
};

internal class ByteArrayCompare: IEqualityComparer<Byte[]>
{
	public Boolean Equals(Byte[] a, Byte[] b)
	{
		if (a == null || b == null)
			return a == b;

		return Util.ByteArrayEquals(a, b);
	}

	public Int32 GetHashCode(Byte[] k)
	{
		if (k == null)
			throw new ArgumentNullException("k");
		
		Int32 sum = 0;
		for (Int32 i = 0; i < k.Length; i++)
			sum = 33 * sum + k[i];

		return sum;
	}
};

}
