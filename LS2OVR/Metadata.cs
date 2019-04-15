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
	struct ComposerData
	{
		public String role;
		public String name;

		public ComposerData(String r, String n)
		{
			role = r ?? throw new ArgumentNullException("r");
			name = n ?? throw new ArgumentNullException("n");
		}
	};

	struct Metadata
	{
		public String title;
		public String artist;
		public String source;
		public ComposerData[] composers;
		public String audio;
		public String artwork;
		public String[] tags;

		public Metadata(String t)
		{
			title = t ?? throw new ArgumentNullException("t");
			artist = source = audio = artwork = null;
			composers = null;
			tags = null;
		}

		public Metadata(NbtCompound data)
		{
			title = data.Get<NbtString>("title").StringValue;
			NbtString temp;

			if (data.TryGet<NbtString>("artist", out temp))
				artist = temp.StringValue;
			else
				artist = null;
			if (data.TryGet<NbtString>("source", out temp))
				source = temp.StringValue;
			else
				source = null;
			if (data.TryGet<NbtString>("audio", out temp))
				audio = temp.StringValue;
			else
				audio = null;
			if (data.TryGet<NbtString>("artwork", out temp))
				artwork = temp.StringValue;
			else
				artwork = null;

			composers = null;
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
					composers = composerDataList.ToArray();
			}
			
			if (data.TryGet<NbtList>("tags", out NbtList tagData))
			{
				try
				{
					List<String> tagsList = new List<string>();

					foreach (NbtString tag in tagData.ToArray<NbtString>())
						tagsList.Add(tag.StringValue);

					tags = tagsList.ToArray();
				}
				catch (InvalidCastException)
				{
					tags = null;
				}
			}
			else
				tags = null;
		}

		public NbtCompound ToNbt()
		{
			NbtCompound data = new NbtCompound("metadata");
			data.Add(new NbtString("title", title));

			return data;
		}
	}
}
