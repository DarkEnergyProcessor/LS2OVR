using System;

namespace LS2OVR
{
	/// <summary>
	/// Exception class that is thrown when the file is invalid.
	/// </summary>
	public class InvalidBeatmapFileException: Exception
	{
		public InvalidBeatmapFileException(String message): base(message) {}
	}
}
