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

		[Option]
		public bool OneLine { get; set; }

		[Option]
		public bool NoUI { get; set; }

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

