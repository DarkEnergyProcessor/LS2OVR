using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using YamlDotNet.RepresentationModel;

namespace LS2OVR.Pack
{

internal class DEPLSConfig
{
	internal static Dictionary<String, YamlScalarNode> KeyMapping = new Dictionary<String, YamlScalarNode>();
	public static YamlScalarNode Key(String k)
	{
		try
		{
			return KeyMapping[k];
		}
		catch (KeyNotFoundException)
		{
			return KeyMapping[k] = new YamlScalarNode(k);
		}
	}

	internal static String GetYamlValue(YamlMappingNode map, String key)
	{
		try
		{
			return ((YamlScalarNode) map.Children[Key(key)]).Value;
		}
		catch (Exception e) when (e is InvalidCastException || e is KeyNotFoundException)
		{
			return null;
		}
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="document"></param>
	/// <returns></returns>
	/// <exception cref="System.ArgumentNullException"></exception>
	/// <exception cref="System.InvalidCastException"></exception>
	public static Metadata ReadMetadataFromYaml(YamlDocument document)
	{
		if (document == null)
			throw new ArgumentNullException("document");

		YamlMappingNode map = (YamlMappingNode) document.RootNode;
		String songTitle = ((YamlScalarNode) map[Key("title")]).Value;

		Metadata metadata = new Metadata(songTitle)
		{
			Artist = GetYamlValue(map, "artist"),
			Source = GetYamlValue(map, "source"),
			Audio = GetYamlValue(map, "audio"),
			Artwork = GetYamlValue(map, "artwork")
		};

		if (map.Children.TryGetValue(Key("composers"), out YamlNode composers) && composers is YamlSequenceNode composerList)
		{
			try
			{
				List<ComposerData> composerData = new List<ComposerData>();

				foreach (YamlSequenceNode info in composerList)
				{
					if (info.Children.Count >= 2)
					{
						try
						{
							composerData.Add(new ComposerData(((YamlScalarNode) info[0]).Value, ((YamlScalarNode) info[1]).Value));
						}
						catch (InvalidCastException) {}
					}
				}

				if (composerData.Count > 0)
					metadata.Composers = composerData;
			}
			catch (InvalidCastException) {}
		}

		String tags = GetYamlValue(map, "tags");
		if (tags != null && tags.Equals(String.Empty) == false)
			metadata.Tags = tags.Split(' ', StringSplitOptions.RemoveEmptyEntries);
		
		return metadata;
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="document"></param>
	/// <returns></returns>
	/// <exception cref="System.ArgumentNullException"></exception>
	/// <exception cref="System.InvalidCastException"></exception>
	public static List<BeatmapData> ReadBeatmapDataFromYaml(YamlDocument document, String currentDirectory)
	{
		if (document == null)
			throw new ArgumentNullException("document");
		
		if (currentDirectory == null)
			throw new ArgumentNullException("currentDirectory");
		
		List<BeatmapData> beatmapDataList = new List<BeatmapData>();
		YamlMappingNode map = (YamlMappingNode) document.RootNode;
		YamlSequenceNode beatmapList = (YamlSequenceNode) map[Key("beatmaps")];

		foreach (YamlMappingNode beatmap in beatmapList)
		{
			BeatmapData beatmapData = new BeatmapData()
			{
				Star = Byte.Parse(((YamlScalarNode) beatmap[Key("star")]).Value),
				StarRandom = Byte.Parse(((YamlScalarNode) beatmap[Key("star-random")]).Value),
				DifficultyName = GetYamlValue(beatmap, "difficulty-name"),
				Background = TryGetBackground(beatmap, "background"),
				ScoreInfo = TryGetIntArrayInfo(beatmap, "score-info"),
				ComboInfo = TryGetIntArrayInfo(beatmap, "combo-info")
			};

			if (beatmapData.Background != null)
				beatmapData.BackgroundRandom = TryGetBackground(beatmap, "background-random") ?? beatmapData.Background;
			
			if (beatmap.Children.TryGetValue(Key("custom-unit-list"), out YamlNode customUnit))
			{
				if (customUnit is YamlSequenceNode customUnitList)
				{
					List<CustomUnitInfo> customUnitInfos = new List<CustomUnitInfo>();

					foreach (YamlNode customUnitInfoTemp in customUnitList)
					{
						if (customUnitInfoTemp is YamlMappingNode customUnitInfo)
						{
							if (
								// Position field
								customUnitInfo.Children.TryGetValue(Key("position"), out YamlNode positionNode) &&
								positionNode is YamlScalarNode positionScalarNode &&
								Byte.TryParse(positionScalarNode.Value, out Byte position) &&
								position > 0 && position <= 9 &&
								// Unit filename field
								customUnitInfo.Children.TryGetValue(Key("unit-filename"), out YamlNode unitFilenameNode) &&
								unitFilenameNode is YamlScalarNode unitFilename
							)
								customUnitInfos.Add(new CustomUnitInfo(position, unitFilename.Value));
						}
					}

					if (customUnitInfos.Count > 0)
						beatmapData.CustomUnitList = customUnitInfos;
				}
			}

			// Read beatmap.
			// It expect same output as `livesim2.exe -dump`
			String beatmapFilename = GetYamlValue(beatmap, "beatmap-file") ?? throw new MissingRequiredFieldException("beatmap-file");
			StreamReader beatmapFile = File.OpenText(currentDirectory + '/' + beatmapFilename);
			List<SIFBeatmapData> sifBeatmapData = (List<SIFBeatmapData>) (new JsonSerializer()).Deserialize(beatmapFile, typeof(List<SIFBeatmapData>));
			sifBeatmapData.Sort((SIFBeatmapData a, SIFBeatmapData b) => a.timing_sec.CompareTo(b.timing_sec));
			List<BeatmapTimingMap> beatmapDatas = new List<BeatmapTimingMap>();

			// Simultaneous note marking
			for (Int32 i = 0; i < sifBeatmapData.Count; i++)
			{
				BeatmapTimingMap timingMap = (BeatmapTimingMap) sifBeatmapData[i];

				if (i > 0)
				{
					if (Math.Abs(sifBeatmapData[i - 1].timing_sec - sifBeatmapData[i].timing_sec) < 0.001)
					{
						BeatmapTimingMap prev = beatmapDatas[i - 1];
						prev.SimultaneousNote = true;
						timingMap.SimultaneousNote = true;
						beatmapDatas.RemoveAt(i - 1);
						beatmapDatas.Add(prev);
					}
				}

				beatmapDatas.Add(timingMap);
			}
			
			beatmapData.SimultaneousFlagProperlyMarked = true;
			beatmapData.MapData = beatmapDatas;

			beatmapDataList.Add(beatmapData);
		}

		return beatmapDataList;
	}

	internal static BackgroundInfo? TryGetBackground(YamlMappingNode map, String key)
	{
		if (map.Children.TryGetValue(Key(key), out YamlNode background))
		{
			if (background is YamlScalarNode)
			{
				YamlScalarNode bg = background as YamlScalarNode;

				if (Int32.TryParse(bg.Value, out Int32 backgroundID))
					return new BackgroundInfo(backgroundID);
				else
				{
					try
					{
						return new BackgroundInfo(bg.Value);
					}
					catch (FormatException)
					{
						return null;
					}
				}
			}
			else if (background is YamlMappingNode bg)
			{
				String main;

				if (bg.Children.TryGetValue(Key("main"), out YamlNode temp) && temp is YamlScalarNode mainBg)
					main = mainBg.Value;
				else
					return null;

				return new BackgroundInfo(
					main,
					GetYamlValue(bg, "left"),
					GetYamlValue(bg, "right"),
					GetYamlValue(bg, "top"),
					GetYamlValue(bg, "bottom")
				);
			}
		}

		return null;
	}

	internal static Int32[] TryGetIntArrayInfo(YamlMappingNode map, String key)
	{
		if (map.Children.TryGetValue(Key(key), out YamlNode temp) && temp is YamlSequenceNode info && info.Children.Count >= 4)
		{
			Int32[] arrayData = new Int32[4];

			for (Int32 i = 0; i < 4; i++)
			{
				if (info[i] is YamlScalarNode temp2 && Int32.TryParse(temp2.Value, out Int32 infoValue))
				{
					if (i > 0)
					{
						if (arrayData[i] <= arrayData[i - 1])
							return null;
						else
							arrayData[i] = infoValue;
					}
				}
				else
					return null;
			}
			
			return arrayData;
		}

		return null;
	}
};

}
