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
/// Exception class that is thrown when the file is invalid.
/// </summary>
public class InvalidBeatmapFileException: Exception
{
	public InvalidBeatmapFileException(String message): base(message) {}
};

/// <summary>
/// Exception class that is thrown when there's problem loading required field.
/// </summary>
public class ProblematicRequiredFieldException: InvalidBeatmapFileException
{
	public ProblematicRequiredFieldException(String fieldName, String message)
	: base($"{message}: {fieldName}") {}
};

/// <summary>
/// Exception class that is thrown when required field is missing.
/// </summary>
public class MissingRequiredFieldException: ProblematicRequiredFieldException
{
	public MissingRequiredFieldException(String fieldName)
	: base(fieldName, "Missing required field") {}
};

/// <summary>
/// Exception class that is thrown when required field has invalid value.
/// </summary>
public class FieldInvalidValueException: ProblematicRequiredFieldException
{
	public FieldInvalidValueException(String fieldName, String additionalMessage)
	: base(fieldName, $"Invalid field ({additionalMessage})") {}
	public FieldInvalidValueException(String fieldName)
	: base(fieldName, "Inalid field value") {}
};

}
