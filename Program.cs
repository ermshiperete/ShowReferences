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
			new Application().Run(new MainForm());
		}
	}
}
