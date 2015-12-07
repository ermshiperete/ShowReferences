// Copyright (c) 2015 SIL International
// This software is licensed under the MIT License (http://opensource.org/licenses/MIT)
using System;
using Eto.Forms;

namespace ShowReferences.Commands
{
	public class OpenAssemblyCommand: Command
	{
		public OpenAssemblyCommand()
		{
			MenuText = "Open";
			ToolBarText = "Open";
			ToolTip = "Open an assembly";
			Shortcut = Application.Instance.CommonModifier | Keys.O;
		}

		protected override void OnExecuted(EventArgs e)
		{
			base.OnExecuted(e);

			using (var dlg = new OpenFileDialog())
			{
				dlg.MultiSelect = false;
				if (dlg.ShowDialog(Application.Instance.MainForm) == DialogResult.Ok)
				{
					var mainForm = Application.Instance.MainForm as MainForm;
					if (mainForm != null)
					{
						mainForm.LoadAssembly(dlg.FileName);
					}
				}
			}
		}
	}
}

