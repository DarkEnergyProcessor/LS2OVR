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

namespace LS2OVR
{

/// <summary>
/// Structure which holds song composer information role and name.
/// </summary>
public struct ComposerData
{
	/// <summary>
	/// Composer role.
	/// </summary>
	public String Role {get; set;}
	/// <summary>
	/// Composer name.
	/// </summary>
	public String Name {get; set;}

	/// <summary>
	/// Create new ComposerData object with specified role and name.
	/// </summary>
	/// <param name="r">Composer role.</param>
	/// <param name="n">Composer name.</param>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="n"/> or <paramref name="r"/> is null</exception>
	public ComposerData(String r, String n)
	{
		Role = r ?? throw new ArgumentNullException("r");
		Name = n ?? throw new ArgumentNullException("n");
	}
};

}
