// Copyright (c) 2015 SIL International
// This software is licensed under the MIT License (http://opensource.org/licenses/MIT)
using System;
using Eto.Forms;

namespace ShowReferences
{
	class MainClass
	{
		[STAThread]
		public static void Main(string[] args)
		{
			var app = new Application();
			var options = Options.ParseCommandLineArgs(args);
			if (options.NoUI)
			{
				new MainForm(options.Filenames, options.All);
				return;
			}

			string filename = null;
			if (options.Filenames != null && options.Filenames.Count > 0)
				filename = options.Filenames[0];

			app.Run(new MainForm(filename));
		}
	}
}
