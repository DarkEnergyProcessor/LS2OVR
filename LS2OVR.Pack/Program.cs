using System;
using CommandLine;

namespace LS2OVR
{
namespace Pack
{

class Program
{
	internal class Options
	{
		[Value(0, MetaName = "input", Required = true, HelpText = "Set the input filename/directory")]
		public String InputFile {get; set;}
		[Option('d', "directory", Required = false, HelpText = "Set the directory where packer looks for files.")]
		public String DefaultDirectory {get; set;} = null;
		[Value(1, MetaName = "output", Required = false, HelpText = "Set the output filename")]
		public String OutputFile {get; set;} = null;
	};
	
	static void Main(String[] args)
	{
		ParserResult<Options> result = Parser.Default.ParseArguments<Options>(args)
		.WithNotParsed((errors) => Environment.Exit(1));
	}
};

}
}
