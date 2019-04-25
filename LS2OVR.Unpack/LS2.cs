using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace LS2OVR.Unpack.LS2
{

using FactoryList = Dictionary<Byte[], Func<BinaryReader, Boolean, LS2Section>>;

internal class LS2Beatmap
{
	internal static readonly Byte[] LS2Signature = {108, 105, 118, 101, 115, 105, 109, 50};
	internal static readonly Byte[] LCLRFourCC = {76, 67, 76, 82};
	internal static readonly Byte[] BIMGFourCC = {66, 73, 77, 71};
	internal static readonly FactoryList SectionFactory = new FactoryList(new ByteArrayCompare())
	{
		// ADIO
		{ADIOSection.FourCC, ADIOSection.Load},
		// BIMG
		{BIMGFourCC, UIMGSection.Load},
		// BMPM
		{BMPMSection.FourCCBMPM, BMPMSection.LoadBMPM},
		// BMPT
		{BMPMSection.FourCCBMPT, BMPMSection.LoadBMPT},
		// COVR
		{COVRSection.FourCC, COVRSection.Load},
		// DATA
		{DATASection.FourCC, DATASection.Load},
		// LCLR
		{LCLRFourCC, ADIOSection.Load},
		// MTDT
		{MTDTSection.FourCC, MTDTSection.Load},
		// SCRI
		{SCRISection.FourCC, SCRISection.Load},
		// SRYL
		{SRYLSection.FourCC, SRYLSection.Load},
		// UIMG
		{UIMGSection.FourCC, UIMGSection.Load},
		// UNIT
		{UNITSection.FourCC, UNITSection.Load},
	};

	internal static List<LS2Section> FindSection(List<KeyValuePair<Byte[], LS2Section>> s, Byte[] fcc)
	{
		List<LS2Section> c = new List<LS2Section>();

		foreach (KeyValuePair<Byte[], LS2Section> a in s)
		{
			if (Util.ByteArrayEquals(a.Key, fcc))
				c.Add(a.Value);
		}

		return c;
	}

	public static Beatmap LS2ToLS2OVR(Stream input)
	{
		if (input == null)
			throw new ArgumentNullException("input");
		if (input.CanRead == false)
			throw new IOException("stream does not support reading");
		if (input.CanSeek == false)
			throw new IOException("stream does not support seeking");

		BinaryReader reader = new BinaryReader(input);

		if (Util.ByteArrayEquals(reader.ReadBytes(8), LS2Signature) == false)
			throw new InvalidBeatmapFileException("invalid LS2 beatmap");
		
		UInt16 sections = Util.ReadUInt16LE(reader);
		Byte fileFlags = reader.ReadByte();
		Byte stamina = reader.ReadByte();
		Byte background = (Byte) (fileFlags & 0x04);
		Boolean isV2 = (fileFlags & 0x80) > 0;
		UInt16 scorePerTap = Util.ReadUInt16LE(reader);

		BeatmapData beatmapData = new BeatmapData() {Star = 1, StarRandom = 1, MapData = new List<BeatmapTimingMap>()};
		beatmapData.BaseScorePerTap = scorePerTap;
		if (stamina <= 127)
			beatmapData.InitialStamina = stamina;
		if (background > 0)
			beatmapData.Background = beatmapData.BackgroundRandom = new BackgroundInfo(background);
		
		List<BeatmapTimingMap> maps = new List<BeatmapTimingMap>();
		List<KeyValuePair<Byte[], LS2Section>> sectionList = new List<KeyValuePair<Byte[], LS2Section>>();

		// Load sections
		for (Int32 i = 0; i < sections; i++)
		{
			Byte[] fourCC = reader.ReadBytes(4);

			try
			{
				LS2Section sect = SectionFactory[fourCC](reader, isV2);
				sectionList.Add(new KeyValuePair<Byte[], LS2Section>(fourCC, sect));

				if (sect is BMPMSection map)
					maps.AddRange((List<BeatmapTimingMap>) map);
			}
			catch (KeyNotFoundException)
			{
				throw new InvalidBeatmapFileException(String.Format(
					"Unknown section found {0},{1},{2},{3}",
					fourCC[0], fourCC[1], fourCC[2], fourCC[3]
				));
			}
		}

		// Sort maps
		maps.Sort((BeatmapTimingMap a, BeatmapTimingMap b) => a.Time.CompareTo(b.Time));
		for (Int32 i = 0; i < maps.Count; i++)
		{
			BeatmapTimingMap p = maps[i];

			if (i > 0)
			{
				BeatmapTimingMap prev = maps[(Int32) (i - 1)];

				if (Math.Abs(prev.Time - p.Time) <= 0.001 && prev.SimultaneousNote == false)
				{
					// BeatmapTimingMap is immutable.
					prev.SimultaneousNote = true;
					p.SimultaneousNote = true;
					beatmapData.MapData.RemoveAt((Int32) (i - 1));
					beatmapData.MapData.Add(prev);
				}
			}

			beatmapData.MapData.Add(p);
		}

		beatmapData.SimultaneousFlagProperlyMarked = true;
		
		// Metadata
		Metadata beatmapMetadata = new Metadata("unknown");
		try
		{
			MTDTSection metadata = FindSection(sectionList, MTDTSection.FourCC)[0] as MTDTSection;

			if (metadata.PreferredAudioFilename.Equals(String.Empty) == false)
				beatmapMetadata.Audio = metadata.PreferredAudioFilename;
			if (metadata.SongName.Equals(String.Empty) == false)
				beatmapMetadata.Title = metadata.SongName;
			if (metadata.ScoreInfo != null)
				beatmapData.ScoreInfo = metadata.ScoreInfo;
			if (metadata.ComboInfo != null)
				beatmapData.ComboInfo = metadata.ComboInfo;
			if (metadata.Star > 0)
				beatmapData.Star = beatmapData.StarRandom = metadata.Star;
			if (metadata.StarRandom > 0)
				beatmapData.StarRandom = metadata.StarRandom;
		}
		catch (Exception e) when (e is NullReferenceException || e is IndexOutOfRangeException)
		{
			if (isV2)
				throw new InvalidBeatmapFileException("missing MTDT section");
		}

		// Add file database
		Dictionary<String, Byte[]> fileList = new Dictionary<String, Byte[]>();
		foreach (LS2Section d in FindSection(sectionList, DATASection.FourCC))
		{
			DATASection data = d as DATASection;
			fileList.Add(data.Filename, data.Data);
		}

		// Cover data
		List<LS2Section> covrList = FindSection(sectionList, COVRSection.FourCC);
		if (covrList.Count > 0)
		{
			COVRSection coverInfo = covrList[0] as COVRSection;
			if (beatmapMetadata.Title.Equals("unknown"))
				beatmapMetadata.Title = coverInfo.Title;
			
			String coverImageName = "cover.png";
			while (fileList.ContainsKey(coverImageName))
				coverImageName = "cover-" + Program.RandomString(5) + ".png";
			
			beatmapMetadata.Artwork = coverImageName;
			fileList.Add(coverImageName, coverInfo.CoverData);
		}

		// Score information
		if (isV2 == false && beatmapData.ScoreInfo == null)
		{
			List<LS2Section> scri = FindSection(sectionList, SCRISection.FourCC);
			if (scri.Count > 0)
				beatmapData.ScoreInfo = scri[0] as SCRISection;
		}

		// Storyboard
		List<LS2Section> srylList = FindSection(sectionList, SRYLSection.FourCC);
		if (srylList.Count > 0)
		{
			SRYLSection sryl = srylList[0] as SRYLSection;
			fileList.Add("storyboard.lua", sryl.StoryboardData);
		}

		// Audio
		List<LS2Section> audioList = FindSection(sectionList, ADIOSection.FourCC);
		if (audioList.Count > 0)
		{
			ADIOSection audio = audioList[0] as ADIOSection;
			String audioFilename = beatmapMetadata.Audio;
			while (audioFilename == null || fileList.ContainsKey(audioFilename))
				audioFilename = "audio-" + Program.RandomString(5) + $".{audio.Extension}";
			
			beatmapMetadata.Audio = audioFilename;
			fileList.Add(audioFilename, audio.Data);
		}

		// TODO: Background, units, and LCLR

		Beatmap beatmap = new Beatmap(beatmapMetadata)
		{
			BeatmapList = new List<BeatmapData>() { beatmapData },
			FileDatabase = fileList
		};
		return beatmap;
	}
};

}
