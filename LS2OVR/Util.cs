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
using System.Security.Cryptography;

namespace LS2OVR
{
	class Util
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

		public static Boolean MD5HashEqual(Byte[] data, Byte[] hash)
		{
			MD5 hasher = MD5.Create();
			return ByteArrayEquals(hash, hasher.ComputeHash(data));
		}

		private Util() {}
	};
}
