using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using CommandLine;

namespace LS2OVR.Inspect
{

internal class Options
{
	[Value(0, MetaName = "input", Required = true, HelpText = "Set the input filename/directory")]
	public String InputFile {get; set;}
	[Option('b', "beatmap", Required = false, HelpText = "Inspect specific beatmap number, 1-based index", Default = -1)]
	public Int32 BeatmapIndex {get; set;} = -1;
	[Option('m', "map", Required = false, HelpText = "Inspect hit points, may shows long output.")]
	public Boolean ShowMap {get; set;} = false;
	[Option('f', "file", Required = false, HelpText = "Inspect additional files.")]
	public Boolean ShowFiles {get; set;} = false;
};

class Program
{
	internal static void DumpMetadata(Metadata metadata)
	{
		Console.WriteLine("Metadata");
		Console.WriteLine("========");
		Console.WriteLine("Title: {0}", metadata.Title);
		Console.WriteLine("Artist: {0}", metadata.Artist ?? "(unknown)");
		Console.WriteLine("Source: {0}", metadata.Source ?? "(unknown)");

		if (metadata.Composers.Count > 0)
		{
			Console.WriteLine("Composers Info: {0}", metadata.Composers.Count);

			foreach (ComposerData c in metadata.Composers)
				Console.WriteLine("  {0}: {1}", c.Role, c.Name);
		}
		else
			Console.WriteLine("Composers Info: (none)");
		
		Console.WriteLine("Audio File: {0}", metadata.Audio ?? "(none)");
		Console.WriteLine("Artwork File: {0}", metadata.Artwork ?? "(none)");

		if (metadata.Tags != null && metadata.Tags.Length > 0)
			Console.WriteLine("Beatmap Tags: {0}", String.Join(" ", metadata.Tags));
		else
			Console.WriteLine("Beatmap Tags: (none)");
	}

	internal static void DumpBackgroundData(BackgroundInfo bg)
	{
		Console.WriteLine("    Main Part  : {0}", bg.Main);
		Console.WriteLine("    Left Part  : {0}", bg.Left);
		Console.WriteLine("    Right Part : {0}", bg.Right);
		Console.WriteLine("    Top Part   : {0}", bg.Top);
		Console.WriteLine("    Bottom Part: {0}", bg.Bottom);
	}

	internal static void DumpBeatmapData(BeatmapData data, Boolean dumpMap)
	{
		Console.WriteLine("  Difficulty: {0}", data.DifficultyName ?? $"{data.Star}☆");
		Console.WriteLine("  Star: {0} (Random {1})", data.Star, data.StarRandom);
		
		if (data.Background != null)
		{
			if (data.Background.Value.IsComplex())
			{
				Console.WriteLine("  Background: Complex background");
				DumpBackgroundData(data.Background.Value);
			}
			else
				Console.WriteLine("  Background: {0}", (String) data.Background.Value);
			
			if (data.BackgroundRandom.Value.IsComplex())
			{
				Console.WriteLine("  Background (Random): Complex background");
				DumpBackgroundData(data.BackgroundRandom.Value);
			}
			else
				Console.WriteLine("  Background: {0}", (String) data.BackgroundRandom.Value);
		}
		else
		{
			Console.WriteLine("  Background: (none)");
			Console.WriteLine("  Background (Random): (none)");
		}

		if (data.CustomUnitList != null && data.CustomUnitList.Count > 0)
		{
			Console.WriteLine("  Custom Unit Info: {0}", data.CustomUnitList.Count);

			foreach (CustomUnitInfo c in data.CustomUnitList)
				Console.WriteLine("    {0}: {1}", c.Position, c.Filename);
		}

		if (data.ScoreInfo != null)
			Console.WriteLine(
				"  Score Rank: C({0}) B({1}) A({2}) S({3})",
				data.ScoreInfo[0],
				data.ScoreInfo[1],
				data.ScoreInfo[2],
				data.ScoreInfo[3]
			);
		else
			Console.WriteLine("  Score Rank: (unknown)");
		
		if (data.ComboInfo != null)
			Console.WriteLine(
				"  Combo Rank: C({0}) B({1}) A({2}) S({3})",
				data.ComboInfo[0],
				data.ComboInfo[1],
				data.ComboInfo[2],
				data.ComboInfo[3]
			);
		else
			Console.WriteLine("  Combo Rank: (unknown)");

		Console.WriteLine("  Base Score/Tap: {0}", data.BaseScorePerTap);
		Console.WriteLine("  Initial Stamina: {0}", data.InitialStamina);
		Console.WriteLine("  Simultaneous Mark Meaningful: {0}", data.SimultaneousFlagProperlyMarked);
		Console.WriteLine("  Hit Points: {0}", data.MapData.Count);

		if (dumpMap)
		{
			UInt32 i = 0;
			foreach (BeatmapTimingMap hitPoint in data.MapData)
			{
				Console.WriteLine("    Hit Point #{0}", i++);
				Console.WriteLine("      Time: {0}", hitPoint.Time.ToString("0.000", CultureInfo.InvariantCulture));
				Console.WriteLine("      Lane: {0}", hitPoint.Position);
				Console.WriteLine("      Type: {0}", hitPoint.NoteType);

				if (hitPoint.NoteType == NoteMapType.LongNote)
					Console.WriteLine("      Duration: {0}", hitPoint.Length.ToString("0.000", CultureInfo.InvariantCulture));

				if (hitPoint.Attribute == 15)
				{
					String r = hitPoint.RedColor.ToString("0.##", CultureInfo.InvariantCulture);
					String g = hitPoint.GreenColor.ToString("0.##", CultureInfo.InvariantCulture);
					String b = hitPoint.BlueColor.ToString("0.##", CultureInfo.InvariantCulture);
					Console.WriteLine("      Attribute: rgb({0}, {1}, {2})", r, g, b);
				}
				else
					Console.WriteLine("      Attribute: {0}", hitPoint.Attribute);
				
				Console.WriteLine("      Swing Note: {0}", hitPoint.SwingNote);
				if (hitPoint.SwingNote)
					Console.WriteLine("      Group: {0}", hitPoint.NoteGroup);

				Console.Write("      Simultaneous Note: {0}", hitPoint.SimultaneousNote);
				Console.WriteLine(data.SimultaneousFlagProperlyMarked ? "" : " (unreliable)");
			}
		}
	}

	internal static void DumpBeatmapDataEarly(BeatmapData data, UInt32 index, Boolean dumpMap)
	{
		String beatmapStrHdrStr = $"Beatmap #{index}";
		String border = new String('-', beatmapStrHdrStr.Length);
		Console.WriteLine(beatmapStrHdrStr);
		Console.WriteLine(border);
		DumpBeatmapData(data, dumpMap);
		Console.WriteLine(border);
		Console.WriteLine();
	}

	static void Main(String[] args)
	{
		ParserResult<Options> result = Parser.Default.ParseArguments<Options>(args)
		.WithNotParsed((errors) => Environment.Exit(1));
		
		Options options = (result as Parsed<Options>).Value;

		try
		{
			Beatmap beatmap = new Beatmap(new FileStream(options.InputFile, FileMode.Open, FileAccess.Read, FileShare.Read));
			Console.WriteLine("Filename: {0}", options.InputFile);
			Console.WriteLine("Format Version: {0}", beatmap.BeatmapFormatVersion);
			Console.WriteLine();
			DumpMetadata(beatmap.BeatmapMetadata);

			if (options.BeatmapIndex == 0)
			{
				Console.WriteLine();

				// All beatmaps
				UInt32 i = 0;
				foreach (BeatmapData beatmapData in beatmap.BeatmapList)
					DumpBeatmapDataEarly(beatmapData, ++i, options.ShowMap);
			}
			else if (options.BeatmapIndex > 0)
			{
				Console.WriteLine();

				// Specific beatmaps
				if (options.BeatmapIndex <= beatmap.BeatmapList.Count)
					DumpBeatmapDataEarly(beatmap.BeatmapList[options.BeatmapIndex - 1], (UInt32) options.BeatmapIndex, options.ShowMap);
				else
				{
					Console.Error.WriteLine("No beatmap #{0}", options.BeatmapIndex);
					Console.Error.WriteLine();
				}
			}

			if (options.ShowFiles)
			{
				Console.WriteLine("Files embedded");
				Console.WriteLine("--------------");
				UInt32 i = 0;
				foreach (KeyValuePair<String, Byte[]> files in beatmap.FileDatabase)
				{
					Console.WriteLine("File #{0}", ++i);
					Console.WriteLine("  Filename: {0}", files.Key);
					Console.WriteLine("  Size: {0} Bytes", files.Value.Length);
				}
				Console.WriteLine();
			}
		}
		catch (Exception e) when (e is IOException || e is InvalidBeatmapFileException)
		{
			Console.Error.WriteLine(e.ToString());
			Environment.Exit(1);
		}
	}
};

}
