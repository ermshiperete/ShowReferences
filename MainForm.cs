// Copyright (c) 2015 SIL International
// This software is licensed under the MIT License (http://opensource.org/licenses/MIT)
using System;
using Eto.Forms;
using Eto.Drawing;
using ShowReferences.Commands;
using System.Reflection;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Text;

namespace ShowReferences
{
	public class MainForm: Form
	{
		private TreeView _treeView;
		private TreeItem _treeItems;

		public MainForm()
		{
			Title = "Show Assembly References";
			ClientSize = new Size(400, 600);
			Resizable = true;
			Menu = new MenuBar {
				Items = {
					new ButtonMenuItem
					{ 
						Text = "&File",
						Items =
						{ 
							new OpenAssemblyCommand(),
						}
					}
				},
				QuitItem = new Command((sender, e) => Application.Instance.Quit()) {
					MenuText = "Exit",
					Shortcut = Application.Instance.CommonModifier | Keys.Q
				}
			};

			var layout = new DynamicLayout { DefaultSpacing = new Size(5, 5), Padding = new Padding(10) };
			_treeItems = new TreeItem {
				Text = "Root"
			};
			_treeView = new TreeView {
				Size = new Size(350, 550),
				DataStore = _treeItems
			};
			if (Platform.Supports<ContextMenu>())
			{
				var menu = new ContextMenu();
				var item = new ButtonMenuItem { Text = "Version" };
				item.Click += OnShowItemVersion;
				menu.Items.Add(item);

				_treeView.ContextMenu = menu;
			}
			layout.AddRow(_treeView);
			Content = layout;
		}

		private delegate object VersionDelegate<T>(T attr);

		private static string GetVersion<T>(Assembly assembly, VersionDelegate<T> a) where T: class
		{
			var attr = assembly.GetCustomAttributes(typeof(T), true).FirstOrDefault() as T;
			return attr != null ? a(attr).ToString() : string.Empty;
		}

		private void OnShowItemVersion(object sender, EventArgs e)
		{
			var item = _treeView.SelectedItem as TreeItem;
			if (item == null)
				return;
			var asm = item.Tag as Assembly;
			if (asm == null)
				return;

			var bldr = new StringBuilder();
			bldr.AppendFormat("Location:  {0}", asm.Location);
			bldr.AppendLine();
			bldr.AppendFormat("Version: {0}", asm.GetName().Version);
			bldr.AppendLine();
			bldr.AppendFormat("File Version: {0}", GetVersion<AssemblyFileVersionAttribute>(asm, (a) => a.Version));
			bldr.AppendLine();
			bldr.AppendFormat("Assembly Version: {0}", GetVersion<AssemblyVersionAttribute>(asm, (a) => a.Version));
			bldr.AppendLine();
			bldr.AppendFormat("Informational Version: {0}",
				GetVersion<AssemblyInformationalVersionAttribute>(asm, (a) => a.InformationalVersion));
			bldr.AppendLine();
			bldr.AppendFormat("Assembly Title: {0}", GetVersion<AssemblyTitleAttribute>(asm, (a) => a.Title));
			bldr.AppendLine();
			bldr.AppendFormat("File Description: {0}", GetVersion<AssemblyDescriptionAttribute>(asm, (a) => a.Description));
			bldr.AppendLine();
			bldr.AppendFormat("ProductName: {0}", GetVersion<AssemblyProductAttribute>(asm, (a) => a.Product));
			bldr.AppendLine();
			bldr.AppendFormat("Copyright: {0}", GetVersion<AssemblyCopyrightAttribute>(asm, (a) => a.Copyright));
			bldr.AppendLine();
			bldr.AppendFormat("AssemblyCompany: {0}", GetVersion<AssemblyCompanyAttribute>(asm, (a) => a.Company));
			bldr.AppendLine();
			bldr.AppendFormat("Trademark: {0}", GetVersion<AssemblyTrademarkAttribute>(asm, (a) => a.Trademark));

			MessageBox.Show(bldr.ToString(), asm.GetName().Name);
		}

		private string BaseDir { get; set; }

		public void LoadAssembly(string fileName)
		{
			BaseDir = Path.GetDirectoryName(fileName);
			var assemblyName = AssemblyName.GetAssemblyName(fileName);
			AddReferences(_treeItems, assemblyName);
			_treeView.RefreshData();
		}

		private Assembly TryLoadAssembly(AssemblyName assemblyName, string extension)
		{
			Assembly asm = null;
			try
			{
				asm = Assembly.LoadFrom(Path.Combine(BaseDir, assemblyName.Name + extension));
			}
			catch (FileNotFoundException)
			{
			}
			return asm;
		}

		private Assembly TryLoadAssembly(AssemblyName assemblyName)
		{
			try
			{
				return Assembly.Load(assemblyName);
			}
			catch (FileNotFoundException)
			{
				return TryLoadAssembly(assemblyName, ".dll") ?? TryLoadAssembly(assemblyName, ".exe");
			}
		}

		private void AddReferences(TreeItem treeItem, AssemblyName assemblyName)
		{
			Assembly asm = TryLoadAssembly(assemblyName);
			string append = string.Empty;
			if (asm == null)
				append = " (not available)";
			else if (asm.GlobalAssemblyCache)
				append = " (GAC)";

			var item = new TreeItem {
				Text = assemblyName.Name + append,
				Expanded = false,
				Tag = asm
			};
			treeItem.Children.Add(item);

			if (asm == null || asm.GlobalAssemblyCache)
				return;

			foreach (var child in asm.GetReferencedAssemblies().OrderBy(n => n.Name))
			{
				AddReferences(item, child);
			}
		}
	}
}

