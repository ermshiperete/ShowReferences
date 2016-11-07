// Copyright (c) 2016 SIL International
// This software is licensed under the MIT License (http://opensource.org/licenses/MIT)
using System;
using CommandLine;
using System.Collections.Generic;

namespace ShowReferences
{
	public class Options
	{
		public static Options Current { get; private set; }

		[Option("oneline", HelpText = "Output everything on one line instead of tree-like output")]
		public bool OneLine { get; set; }

		[Option("noui", HelpText = "Only output to console, don't show UI")]
		public bool NoUI { get; set; }

		[Option("all", HelpText = "Show references of all referenced assemblies")]
		public bool All { get; set; }

		[ValueList(typeof(List<string>))]
		public List<string> Filenames { get; set; }

		[ParserState]
		public IParserState LastParserState { get; set; }

		public static Options ParseCommandLineArgs(string[] args)
		{
			var options = new Options();

			if (Parser.Default.ParseArgumentsStrict(args, options))
			{
				Current = options;
				return options;
			}
			// CommandLineParser automagically handles displaying help
			return null;
		}
	}
}

