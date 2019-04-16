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
		[Option('d', "directory", Required = false, HelpText = "Set the directory where packer looks for files.")]
		public String DefaultDirectory {get; set;} = null;
		[Option('o', "output", Required = false, HelpText = "Set the output filename.")]
		public String OutputFile {get; set;} = null;
	};

	static void Main(String[] args)
	{
		Parser.Default.ParseArguments<Options>(args);
	}
};

}
}
