using System;
using System.Collections.Generic;
using System.IO;
using CommandLine;
using Newtonsoft.Json;
using YamlDotNet.RepresentationModel;

namespace LS2OVR.Unpack
{

internal struct SIFBeatmapData
{
#pragma warning disable 0649
	public Double timing_sec;
	public Byte position;
	public Int32 effect;
	public Double effect_value;
	public UInt32 notes_attribute;
	public Int32 notes_level;
#pragma warning restore 0649
};

internal class Options
{
	[Value(0, MetaName = "input", Required = true, HelpText = "Set the input filename")]
	public String InputFile {get; set;}
	[Option("no-files", Required = false, Default = false, HelpText = "Do not extract additional files except required files")]
	public Boolean NoAdditionalFiles {get; set;}
	[Option("no-custom-unit", Required = false, Default = false, HelpText = "Do not write any custom unit information, including files")]
	public Boolean NoCustomUnit {get; set;}
	[Option("create-directory", Required = false, Default = false, HelpText = "Create output directory if necessary")]
	public Boolean CreateDirectory {get; set;}
	[Value(1, MetaName = "output-dir", Required = false, Default = ".", HelpText = "Set the output directory")]
	public String OutputDir {get; set;}
};

class Program
{
	internal static String StringListForRandom = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
	internal static Random RNGForString = new Random();
	internal static String RandomString(Int32 length)
	{
		if (length < 0)
			throw new ArgumentOutOfRangeException("length");
		else if (length == 0)
			return String.Empty;
		
		List<Char> strList = new List<Char>();
		for (Int32 i = 0; i < length; i++)
			strList.Add(StringListForRandom[RNGForString.Next(0, StringListForRandom.Length)]);
		
		return new String(strList.ToArray());
	}

	internal static void AddIfNotExist<T>(List<T> list, T value)
	{
		if (list.Contains(value) == false)
			list.Add(value);
	}

	internal static void AddBackgroundToYaml(YamlMappingNode map, List<String> file, String key, BackgroundInfo bg)
	{
		if (bg.IsComplex())
		{
			YamlMappingNode backgroundNode = new YamlMappingNode() {{"main", bg.Main}};
			AddIfNotExist(file, bg.Main);
			if (bg.IsValidLeftRightBackground())
			{
				backgroundNode.Add("left", bg.Left);
				AddIfNotExist(file, bg.Left);
				backgroundNode.Add("right", bg.Right);
				AddIfNotExist(file, bg.Right);
			}
			if (bg.IsValidTopBottomBackground())
			{
				backgroundNode.Add("top", bg.Top);
				AddIfNotExist(file, bg.Top);
				backgroundNode.Add("bottom", bg.Bottom);
				AddIfNotExist(file, bg.Bottom);
			}

			map.Add(key, backgroundNode);
		}
		else
		{
			if (bg.BackgroundNumber == 0)
			{
				AddIfNotExist(file, bg.Main);
				map.Add(key, bg.Main);
			}
			else
				map.Add(key, bg.BackgroundNumber.ToString());
		}
	}

	internal static YamlSequenceNode CreateRankList(Int32[] list)
	{
		return new YamlSequenceNode() {
			list[0].ToString(),
			list[1].ToString(),
			list[2].ToString(),
			list[3].ToString(),
		};
	}

	static void Main(String[] args)
	{
		ParserResult<Options> result = Parser.Default.ParseArguments<Options>(args)
		.WithNotParsed((errors) => Environment.Exit(1));
		
		Options options = (result as Parsed<Options>).Value;

		try
		{
			FileStream inputFile = new FileStream(options.InputFile, FileMode.Open, FileAccess.Read, FileShare.Read);
			MemoryStream inputData = new MemoryStream();
			Beatmap beatmap = null;
			Byte[] signatureTest = new Byte[8];
			String outputDir = options.OutputDir.TrimEnd('/').TrimEnd('\\');
			
			if (options.CreateDirectory)
				Directory.CreateDirectory(outputDir);

			// Differentiate between LS2 and LS2OVR beatmap
			inputFile.CopyTo(inputData);
			inputData.Seek(0, SeekOrigin.Begin);
			inputData.Read(signatureTest, 0, 8);
			inputData.Seek(0, SeekOrigin.Begin);
			if (LS2.Util.ByteArrayEquals(LS2.LS2Beatmap.LS2Signature, signatureTest))
				beatmap = LS2.LS2Beatmap.LS2ToLS2OVR(inputData);
			else
				beatmap = new Beatmap(inputData);
			
			// Write metadata
			Metadata beatmapMetadata = beatmap.BeatmapMetadata;
			List<String> filesToExtract = new List<String>();
			YamlMappingNode rootNode = new YamlMappingNode
			{
				{new YamlScalarNode("title"), new YamlScalarNode(beatmapMetadata.Title)}
			};
			
			if (beatmapMetadata.Artist != null)
				rootNode.Add("artist", beatmapMetadata.Artist);
			if (beatmapMetadata.Source != null)
				rootNode.Add("source", beatmapMetadata.Source);
			if (beatmapMetadata.Composers != null)
			{
				YamlSequenceNode composerList = new YamlSequenceNode();

				foreach (ComposerData c in beatmapMetadata.Composers)
					composerList.Add(new YamlSequenceNode() {c.Role, c.Name});
				
				rootNode.Add("composers", composerList);
			}
			if (beatmapMetadata.Audio != null)
			{
				AddIfNotExist(filesToExtract, beatmapMetadata.Audio);
				rootNode.Add("audio", beatmapMetadata.Audio);
			}
			if (beatmapMetadata.Artwork != null)
			{
				AddIfNotExist(filesToExtract, beatmapMetadata.Artwork);
				rootNode.Add("artwork", beatmapMetadata.Artwork);
			}
			if (beatmapMetadata.Tags != null && beatmapMetadata.Tags.Length > 0)
				rootNode.Add("tags", String.Join(' ', beatmapMetadata.Tags));
			
			YamlSequenceNode beatmapList = new YamlSequenceNode();
			Int32 beatmapNumber = 1;

			foreach (BeatmapData beatmapData in beatmap.BeatmapList)
			{
				YamlMappingNode beatmapNode = new YamlMappingNode();
				String beatmapFilename = $"beatmap-{beatmapNumber}.json";

				// Generate new name if it's already used
				while (beatmap.FileDatabase != null && beatmap.FileDatabase.ContainsKey(beatmapFilename))
					beatmapFilename = $"beatmap-{beatmapNumber}-" + RandomString(RNGForString.Next(beatmapNumber, beatmapNumber + 3)) + ".json";
				
				// Write node
				if (beatmapData.DifficultyName != null)
					beatmapNode.Add("difficulty-name", beatmapData.DifficultyName);

				beatmapNode.Add("star", beatmapData.Star.ToString());
				beatmapNode.Add("star-random", beatmapData.Star.ToString());

				if (beatmapData.Background != null)
				{
					AddBackgroundToYaml(beatmapNode, filesToExtract, "background", beatmapData.Background.Value);
					
					if (beatmapData.BackgroundRandom != null)
						AddBackgroundToYaml(beatmapNode, filesToExtract, "background-random", beatmapData.BackgroundRandom.Value);
				}

				if (options.NoCustomUnit == false && beatmapData.CustomUnitList != null && beatmapData.CustomUnitList.Count > 0)
				{
					YamlSequenceNode customUnitInfoNode = new YamlSequenceNode();

					foreach (CustomUnitInfo unitInfo in beatmapData.CustomUnitList)
					{
						AddIfNotExist(filesToExtract, unitInfo.Filename);
						
						customUnitInfoNode.Add(new YamlMappingNode() {
							{"position", unitInfo.Position.ToString()},
							{"unit-filename", unitInfo.Filename}
						});
					}

					beatmapNode.Add("custom-unit-list", customUnitInfoNode);
				}

				if (beatmapData.ScoreInfo != null)
					beatmapNode.Add("score-rank", CreateRankList(beatmapData.ScoreInfo));
				if (beatmapData.ComboInfo != null)
					beatmapNode.Add("combo-rank", CreateRankList(beatmapData.ComboInfo));
				
				if (beatmapData.BaseScorePerTap > 0)
					beatmapNode.Add("score-per-tap", beatmapData.BaseScorePerTap.ToString());
				if (beatmapData.InitialStamina > 0)
					beatmapNode.Add("stamina", beatmapData.InitialStamina.ToString());
				
				beatmapNode.Add("beatmap-file", beatmapFilename);

				// Deserialize beatmap to SIF beatmap representation
				List<SIFBeatmapData> sifBeatmapDataList = new List<SIFBeatmapData>();
				foreach (BeatmapTimingMap p in beatmapData.MapData)
				{
					SIFBeatmapData data = new SIFBeatmapData() {
						timing_sec = p.Time,
						position = p.Position,
						effect_value = 2,
						notes_level = 1,
						notes_attribute =
							p.Attribute |
							(p.Attribute == 15 ? (
								(((UInt32) (p.BlueColor * 511.0)) << 5) |
								(((UInt32) (p.GreenColor * 511.0)) << 14) |
								(((UInt32) (p.RedColor * 511.0)) << 23)
							) : 0)
					};

					switch (p.NoteType)
					{
						case NoteMapType.NormalNote:
						{
							data.effect = 1;
							break;
						}
						case NoteMapType.TokenNote:
						{
							data.effect = 2;
							break;
						}
						case NoteMapType.LongNote:
						{
							data.effect = 3;
							data.effect_value = p.Length;
							break;
						}
						case NoteMapType.StarNote:
						{
							data.effect = 4;
							break;
						}
					}

					if (p.SwingNote)
					{
						data.effect += 10;
						data.notes_level = p.NoteGroup;
					}

					sifBeatmapDataList.Add(data);
				}

				FileStream beatmapOutput = new FileStream($"{outputDir}/{beatmapFilename}", FileMode.Create, FileAccess.Write, FileShare.None);
				StreamWriter beatmapWriter = new StreamWriter(beatmapOutput);
				(new JsonSerializer()).Serialize(beatmapWriter, sifBeatmapDataList.ToArray());
				beatmapWriter.Flush();
				beatmapOutput.Close();

				beatmapList.Add(beatmapNode);
				beatmapNumber++;
			}

			rootNode.Add("beatmaps", beatmapList);

			YamlSequenceNode fileList = null;
			// Enumerate files
			if (beatmap.FileDatabase != null && beatmap.FileDatabase.Count > 0)
			{

				if (options.NoAdditionalFiles)
				{
					// Specific files
					if (filesToExtract.Count > 0)
					{
						fileList = new YamlSequenceNode();
						foreach (String filename in filesToExtract)
						{
							if (beatmap.FileDatabase.TryGetValue(filename, out Byte[] data))
							{
								File.WriteAllBytes($"{outputDir}/{filename}", data);
								fileList.Add(filename);
							}
						}
					}
				}
				else
				{
					// All additional files
					fileList = new YamlSequenceNode();
					foreach (KeyValuePair<String, Byte[]> file in beatmap.FileDatabase)
					{
						File.WriteAllBytes($"{outputDir}/{file.Key}", file.Value);
						if (filesToExtract.Contains(file.Key) == false)
							fileList.Add(file.Key);
					}
				}
			}

			if (fileList != null)
				rootNode.Add("additional-files", fileList);
			
			// have to use full name to prevent confusion
			YamlDotNet.Serialization.Serializer serializer = new YamlDotNet.Serialization.Serializer();
			FileStream output = new FileStream($"{outputDir}/.ls2ovr.yaml", FileMode.Create, FileAccess.Write, FileShare.None);
			StreamWriter writer = new StreamWriter(output) {NewLine = "\n"};
			writer.WriteLine("---");
			serializer.Serialize(writer, rootNode);
			writer.Flush();
			output.Close();
		}
		catch (Exception e) when (
			e is IOException ||
			e is InvalidBeatmapFileException
		)
		{
			Console.Error.WriteLine(e.ToString());
			Environment.Exit(1);
		}
	}
};

}
