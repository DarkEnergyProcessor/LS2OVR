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
	public List<ComposerData> Composers {get; set;}
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
	/// <exception cref="System.ArgumentNullException">Thrown if <paramref name="t"/> is null.</exception>
	public Metadata(String t)
	{
		Title = t ?? throw new ArgumentNullException("t");
		Artist = Source = Audio = Artwork = null;
		Composers = null;
		Tags = null;
	}

	/// <summary>
	/// Create new Metadata from specified NbtCompound.
	/// </summary>
	/// <param name="data">TAG_Compound data.</param>
	/// <exception cref="LS2OVR.MissingRequiredFieldException">Thrown if required field(s) is missing.</exception>
	/// <exception cref="LS2OVR.FieldInvalidValueException">Thrown if required field(s) has invalid value.</exception>
	public Metadata(NbtCompound data)
	{
		NbtString temp;
		Title = Util.GetRequiredStringField(data, "title");
		Composers = new List<ComposerData>();

		if (data.TryGet("artist", out temp))
			Artist = temp.StringValue;
		else
			Artist = null;
		if (data.TryGet("source", out temp))
			Source = temp.StringValue;
		else
			Source = null;
		if (data.TryGet("audio", out temp))
			Audio = temp.StringValue;
		else
			Audio = null;
		if (data.TryGet("artwork", out temp))
			Artwork = temp.StringValue;
		else
			Artwork = null;

		if (data.TryGet("composers", out NbtTag composersList))
		{
			if (composersList is NbtList composerNbtListTag && composerNbtListTag.ListType == NbtTagType.Compound)
			{
				foreach (NbtCompound composerData in composerNbtListTag.ToArray<NbtCompound>())
				{
					try
					{
						if (composerData.TryGet("role", out NbtString tempRole) && composerData.TryGet("name", out temp))
							Composers.Add(new ComposerData(tempRole.StringValue, temp.StringValue));
					}
					catch (InvalidCastException) {}
				}
			}
		}
		
		if (data.TryGet("tags", out NbtTag tagData))
		{
			if (tagData is NbtList tagDataList && tagDataList.ListType == NbtTagType.String)
			{
				List<String> tagsList = new List<String>();

				foreach (NbtString tag in tagDataList.ToArray<NbtString>())
					tagsList.Add(tag.StringValue);

				Tags = tagsList.ToArray();
			}
			else
				Tags = null;
		}
		else
			Tags = null;
	}

	public static explicit operator NbtCompound(Metadata self)
	{
		NbtCompound data = new NbtCompound("metadata");
		data.Add(new NbtString("title", self.Title));
		
		if (self.Artist != null)
			data.Add(new NbtString("artist", self.Artist));
		if (self.Source != null)
			data.Add(new NbtString("source", self.Source));
		if (self.Composers != null && self.Composers.Count > 0)
		{
			NbtList composerList = new NbtList("composers");

			foreach (ComposerData composer in self.Composers)
				composerList.Add((NbtCompound) composer);
			
			data.Add(composerList);
		}
		if (self.Audio != null)
			data.Add(new NbtString("audio", self.Audio));
		if (self.Artwork != null)
			data.Add(new NbtString("artwork", self.Artwork));
		if (self.Tags != null && self.Tags.Length > 0)
		{
			NbtList tagList = new NbtList("tags");

			foreach (String tag in self.Tags)
				tagList.Add(new NbtString(tag));
			
			data.Add(tagList);
		}

		return data;
	}
}

}
