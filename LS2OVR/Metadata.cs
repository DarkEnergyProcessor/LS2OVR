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
using fNbt;

namespace LS2OVR
{
	/// <summary>
	/// Structure which holds song metadata.
	/// </summary>
	public struct Metadata
	{
		/// <summary>
		/// Song name.
		/// </summary>
		public String Title {get; set;}
		/// <summary>
		/// Song artist.
		/// </summary>
		public String Artist {get; set;}
		/// <summary>
		/// Song source. Either anime name, album name, etc.
		/// </summary>
		public String Source {get; set;}
		/// <summary>
		/// List of composers data.
		/// </summary>
		public ComposerData[] Composers {get; set;}
		/// <summary>
		/// Audio file name.
		/// </summary>
		public String Audio {get; set;}
		/// <summary>
		/// Song artwork file name.
		/// </summary>
		public String Artwork {get; set;}
		/// <summary>
		/// List of song tags.
		/// </summary>
		public String[] Tags {get; set;}

		/// <summary>
		/// Create new Metadata with specified title and other data set to null
		/// </summary>
		/// <param name="t">Song title.</param>
		public Metadata(String t)
		{
			Title = t ?? throw new ArgumentNullException("t");
			Artist = Source = Audio = Artwork = null;
			Composers = null;
			Tags = null;
		}

		public Metadata(NbtCompound data)
		{
			Title = data.Get<NbtString>("title").StringValue;
			NbtString temp;

			if (data.TryGet<NbtString>("artist", out temp))
				Artist = temp.StringValue;
			else
				Artist = null;
			if (data.TryGet<NbtString>("source", out temp))
				Source = temp.StringValue;
			else
				Source = null;
			if (data.TryGet<NbtString>("audio", out temp))
				Audio = temp.StringValue;
			else
				Audio = null;
			if (data.TryGet<NbtString>("artwork", out temp))
				Artwork = temp.StringValue;
			else
				Artwork = null;

			Composers = null;
			if (data.TryGet<NbtList>("composers", out NbtList composersList))
			{
				List<ComposerData> composerDataList = new List<ComposerData>();
				Boolean isOK = true;

				foreach(NbtCompound composerData in composersList.ToArray<NbtCompound>()) 
				{
					NbtString tempRole, tempName;
					if (composerData.TryGet<NbtString>("role", out tempRole) && composerData.TryGet<NbtString>("name", out tempName))
						composerDataList.Add(new ComposerData(tempRole.StringValue, tempName.StringValue));
					else
					{
						isOK = false;
						break;
					}
				}

				if (isOK)
					Composers = composerDataList.ToArray();
			}
			
			if (data.TryGet<NbtList>("tags", out NbtList tagData))
			{
				try
				{
					List<String> tagsList = new List<string>();

					foreach (NbtString tag in tagData.ToArray<NbtString>())
						tagsList.Add(tag.StringValue);

					Tags = tagsList.ToArray();
				}
				catch (InvalidCastException)
				{
					Tags = null;
				}
			}
			else
				Tags = null;
		}

		public NbtCompound ToNbt()
		{
			NbtCompound data = new NbtCompound("metadata");
			data.Add(new NbtString("title", Title));

			return data;
		}
	}
}
