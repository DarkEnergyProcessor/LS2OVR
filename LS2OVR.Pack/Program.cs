using System;
using System.Collections.Generic;
using System.IO;
using CommandLine;
using YamlDotNet.RepresentationModel;

namespace LS2OVR
{
namespace Pack
{

class Program
{
	internal class UnknownCompressionTypeException: InvalidBeatmapFileException
	{
		public UnknownCompressionTypeException(String message)
		: base($"Unknown compression: {message}") {}
	};

	internal static BeatmapCompressionType MapFromSupportedCompression(String comp)
	{
		if (comp.Equals("none", StringComparison.CurrentCultureIgnoreCase))
			return BeatmapCompressionType.None;
		else if (comp.Equals("gzip", StringComparison.CurrentCultureIgnoreCase))
			return BeatmapCompressionType.GZip;
		else if (comp.Equals("zlib", StringComparison.CurrentCultureIgnoreCase))
			return BeatmapCompressionType.ZLib;
		else
			throw new UnknownCompressionTypeException(comp);
	}

	internal class Options
	{
		[Value(0, MetaName = "input", Required = true, HelpText = "Set the input filename/directory")]
		public String InputFile {get; set;}
		[Option('d', "directory", Required = false, HelpText = "Set the directory where packer looks for files.")]
		public String DefaultDirectory {get; set;} = null;
		[Option('c', "compression", Required = false, HelpText = "Set the beatmap compression algorithm. GZip is default.")]
		public String Compression {get; set;} = null;
		[Value(1, MetaName = "output", Required = false, HelpText = "Set the output filename")]
		public String OutputFile {get; set;} = null;
	};
	
	static void Main(String[] args)
	{
		ParserResult<Options> result = Parser.Default.ParseArguments<Options>(args)
		.WithNotParsed((errors) => Environment.Exit(1));

		Options options = (result as Parsed<Options>).Value;

		try
		{
			String ls2ovrYml = null;
			String currentDir = options.DefaultDirectory;
			String outputFile = options.OutputFile;
			BeatmapCompressionType defaultCompression = BeatmapCompressionType.GZip;
			FileAttributes inputAttribute = File.GetAttributes(options.InputFile);

			if ((inputAttribute & FileAttributes.Directory) > 0)
			{
				String path = options.InputFile.TrimEnd('/').TrimEnd('\\');
				ls2ovrYml = path + "/.ls2ovr.yaml";
				currentDir = currentDir ?? path;

				if (File.Exists(ls2ovrYml) == false)
					ls2ovrYml = path + "/.ls2ovr.yml";
			}
			else
			{
				ls2ovrYml = options.InputFile;
				currentDir = currentDir ?? Path.GetDirectoryName(options.InputFile);
			}

			if (outputFile == null)
				outputFile = Path.GetFileName(Path.GetDirectoryName(ls2ovrYml)) + ".ls2ovr";
			
			if (options.Compression != null)
				defaultCompression = MapFromSupportedCompression(options.Compression);

			YamlStream yaml = new YamlStream();
			StreamReader yamlReader = File.OpenText(ls2ovrYml);
			yaml.Load(yamlReader);
			yamlReader.Close();

			YamlDocument doc = yaml.Documents[0];
			Metadata metadata = DEPLSConfig.ReadMetadataFromYaml(doc);
			Beatmap beatmap = new Beatmap(metadata)
			{
				BeatmapList = DEPLSConfig.ReadBeatmapDataFromYaml(doc, currentDir),
				FileDatabase = DEPLSConfig.ReadAdditionalFileFromYaml(doc, currentDir)
			};

			// Give metadata and beatmap data files a priority than additional files
			if (metadata.Audio != null)
				beatmap.FileDatabase[metadata.Audio] = File.ReadAllBytes(currentDir + '/' + metadata.Audio);
			if (metadata.Artwork != null)
				beatmap.FileDatabase[metadata.Artwork] = File.ReadAllBytes(currentDir + '/' + metadata.Artwork);
			
			Dictionary<String, Byte[]> customUnitFiles = new Dictionary<String, Byte[]>();
			foreach (BeatmapData beatmapData in beatmap.BeatmapList)
			{
				if (beatmapData.CustomUnitList != null)
				{
					foreach (CustomUnitInfo customUnit in beatmapData.CustomUnitList)
					{
						if (customUnitFiles.ContainsKey(customUnit.Filename) == false)
							customUnitFiles[customUnit.Filename] = File.ReadAllBytes(currentDir + '/' + customUnit.Filename);
					}
				}
			}
			
			// Merge custom unit files to FileDatabase, overwriting existing keys
			foreach (KeyValuePair<String, Byte[]> customUnitFile in customUnitFiles)
				beatmap.FileDatabase[customUnitFile.Key] = customUnitFile.Value;
			
			// Encode beatmap
			FileStream outputFileStream = new FileStream(outputFile, FileMode.Create, FileAccess.Write);
			beatmap.WriteTo(outputFileStream, defaultCompression);
		}
		// not good
		catch (Exception e)
		{
			Console.Error.WriteLine(e.ToString());

			if (
				e is IOException ||
				e is InvalidCastException ||
				e is InvalidBeatmapFileException
			)
				Environment.Exit(1);
			else
				// oh okay
				throw;
		}
	}
};

}
}
