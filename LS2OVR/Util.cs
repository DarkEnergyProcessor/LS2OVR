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
using System.Security.Cryptography;
using fNbt;

namespace LS2OVR
{

internal class Util
{
	internal static MD5 Hash = MD5.Create();

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

	public static Boolean MD5HashEqual(Byte[] data, Byte[] hash)
	{
		return ByteArrayEquals(hash, Hash.ComputeHash(data));
	}

	public static Byte[] MD5Hash(Byte[] data)
	{
		return Hash.ComputeHash(data);
	}

	public static Byte GetRequiredByteField(NbtCompound compound, String key)
	{
		if (compound.TryGet(key, out NbtTag result))
		{
			if (result is NbtByte)
				return result.ByteValue;
			else
				throw new FieldInvalidValueException(key, "invalid type");
		}
		else
			throw new MissingRequiredFieldException(key);
	}

	public static Double GetRequiredDoubleField(NbtCompound compound, String key)
	{
		if (compound.TryGet(key, out NbtTag result))
		{
			if (result is NbtDouble)
				return result.DoubleValue;
			else
				throw new FieldInvalidValueException(key, "invalid type");
		}
		else
			throw new MissingRequiredFieldException(key);
	}

	public static Int32 GetRequiredIntField(NbtCompound compound, String key)
	{
		if (compound.TryGet(key, out NbtTag result))
		{
			if (result is NbtInt)
				return result.IntValue;
			else
				throw new FieldInvalidValueException(key, "invalid type");
		}
		else
			throw new MissingRequiredFieldException(key);
	}

	public static String GetRequiredStringField(NbtCompound compound, String key)
	{
		if (compound.TryGet(key, out NbtTag result))
		{
			if (result is NbtString)
			{
				if (result.StringValue == null || result.StringValue.Equals(String.Empty))
					throw new FieldInvalidValueException(key, "empty");
				else
					return result.StringValue;
			}
			else
				throw new FieldInvalidValueException(key, "invalid type");
		}
		else
			throw new MissingRequiredFieldException(key);
	}

	public static Byte[] CreateNbtList(NbtList list)
	{
		NbtCompound workaround = new NbtCompound("a") {list};
		MemoryStream buffer = new MemoryStream();
		
		(new NbtFile(workaround)).SaveToStream(buffer, NbtCompression.None);
		Byte[] output = new Byte[buffer.Position - 1 - 4];
		Array.Copy(buffer.ToArray(), 1, output, 0, output.Length);
		return output;
	}

	public static Int32 AlignNextMultiple(Int32 value, Int32 multiple = 16)
	{
		Int32 remainder = value % multiple;
		if (remainder == 0)
			return value;
		
		return value + multiple - remainder;
	}

	private Util() {}
};

}
