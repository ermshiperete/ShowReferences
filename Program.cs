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
			string filename = null;
			if (args.Length > 0)
				filename = args[0];
			new Application().Run(new MainForm(filename));
		}
	}
}
