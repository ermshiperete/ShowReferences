// Copyright (c) 2015 SIL International
// This software is licensed under the MIT License (http://opensource.org/licenses/MIT)
using System;
using System.Collections.Generic;
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
		private int _keyCount;
		private readonly TreeView _treeView;
		private readonly TreeView _reverseTreeView;
		private readonly TreeItem _treeItems;
		private readonly TreeItem _reverseTreeItems;
		private readonly Dictionary<string, TreeItem> _processedItems;
		private readonly Dictionary<string, List<string>> _allAssemblies;
		private readonly Dictionary<string, TreeItem> _reverseItems;
		private readonly TableLayout _assemblyDetails;

		public MainForm()
		{
			_processedItems = new Dictionary<string, TreeItem>();
			_allAssemblies = new Dictionary<string, List<string>>();
			_reverseItems = new Dictionary<string, TreeItem>();

			Title = "Show Assembly References";
			ClientSize = new Size(600, 600);
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
				Text = "Root",
				Key = TreeItemKey
			};
			_treeView = new TreeView {
				Size = new Size(200, 550),
				DataStore = _treeItems
			};
			_treeView.SelectionChanged += TreeViewOnSelectionChanged;
			_treeView.Expanding += TreeViewOnExpanding;
			if (Platform.Supports<ContextMenu>())
			{
				var menu = new ContextMenu();
				var item = new ButtonMenuItem { Text = "Version" };
				item.Click += OnShowItemVersion;
				menu.Items.Add(item);

				_treeView.ContextMenu = menu;
			}
			var details = new DynamicLayout { DefaultSpacing = new Size(5, 5), Padding = new Padding(10) };
			_assemblyDetails = new TableLayout(2, 11) { Padding = new Padding(10), Spacing = new Size(5, 5) };
			_assemblyDetails.Add(new Panel { Content = new Label { Text = "Location:"}}, 0, 0);
			_assemblyDetails.Add(new Panel { Content = new Label() }, 1, 0);
			_assemblyDetails.Add(new Panel { Content = new Label { Text = "Version:" } }, 0, 1);
			_assemblyDetails.Add(new Panel { Content = new Label() }, 1, 1);
			_assemblyDetails.Add(new Panel { Content = new Label { Text = "File Version:" } }, 0, 2);
			_assemblyDetails.Add(new Panel { Content = new Label() }, 1, 2);
			_assemblyDetails.Add(new Panel { Content = new Label { Text = "Assembly Version:" } }, 0, 3);
			_assemblyDetails.Add(new Panel { Content = new Label() }, 1, 3);
			_assemblyDetails.Add(new Panel { Content = new Label { Text = "Informational Version:" } }, 0, 4);
			_assemblyDetails.Add(new Panel { Content = new Label() }, 1, 4);
			_assemblyDetails.Add(new Panel { Content = new Label { Text = "Assembly Title:" } }, 0, 5);
			_assemblyDetails.Add(new Panel { Content = new Label() }, 1, 5);
			_assemblyDetails.Add(new Panel { Content = new Label { Text = "File Description:" } }, 0, 6);
			_assemblyDetails.Add(new Panel { Content = new Label() }, 1, 6);
			_assemblyDetails.Add(new Panel { Content = new Label { Text = "Product Name:" } }, 0, 7);
			_assemblyDetails.Add(new Panel { Content = new Label() }, 1, 7);
			_assemblyDetails.Add(new Panel { Content = new Label { Text = "Copyright:" } }, 0, 8);
			_assemblyDetails.Add(new Panel { Content = new Label() }, 1, 8);
			_assemblyDetails.Add(new Panel { Content = new Label { Text = "Company:" } }, 0, 9);
			_assemblyDetails.Add(new Panel { Content = new Label() }, 1, 9);
			_assemblyDetails.Add(new Panel { Content = new Label { Text = "Trademark:" } }, 0, 10);
			_assemblyDetails.Add(new Panel { Content = new Label() }, 1, 10);

			details.AddRow(_assemblyDetails);
			details.AddRow(new Panel { Content = new Label {Text = "Assemblies that reference this assembly:"}});
			_reverseTreeItems = new TreeItem { Text = "Root", Key = TreeItemKey };
			_reverseTreeView = new TreeView { DataStore = _reverseTreeItems };
			details.AddRow(_reverseTreeView);

			layout.AddRow(_treeView, details);
			Content = layout;
		}

		private string TreeItemKey
		{
			get { return _keyCount++.ToString(); }
		}
		private void TreeViewOnExpanding(object sender, TreeViewItemCancelEventArgs treeViewItemCancelEventArgs)
		{
			var item = treeViewItemCancelEventArgs.Item as LazyTreeItem;
			if (item != null)
			{
				var newItem = AddReferences((TreeItem)item.Parent, new AssemblyName(GetAssemblyName(item)), false);
				newItem.Expanded = true;
				_treeView.RefreshItem(item.Parent);
			}
		}

		private void EmptyAssemblyDetails()
		{
			for (int i = 0; i < 11; i++)
			{
				((Label) ((Panel) _assemblyDetails.Rows[i].Cells[1].Control).Content).Text = string.Empty;
			}
		}

		private void TreeViewOnSelectionChanged(object sender, EventArgs eventArgs)
		{
			EmptyAssemblyDetails();
			var item = _treeView.SelectedItem as TreeItem;
			if (item == null)
				return;
			var asm = item.Tag as Assembly;
			if (asm == null)
				return;

			((Label)((Panel) _assemblyDetails.Rows[0].Cells[1].Control).Content).Text = asm.Location;
			((Label)((Panel) _assemblyDetails.Rows[1].Cells[1].Control).Content).Text = asm.GetName().Version.ToString();
			((Label)((Panel) _assemblyDetails.Rows[2].Cells[1].Control).Content).Text = GetVersion<AssemblyFileVersionAttribute>(asm, (a) => a.Version);
			((Label)((Panel) _assemblyDetails.Rows[3].Cells[1].Control).Content).Text = GetVersion<AssemblyVersionAttribute>(asm, (a) => a.Version);
			((Label)((Panel) _assemblyDetails.Rows[4].Cells[1].Control).Content).Text = GetVersion<AssemblyInformationalVersionAttribute>(asm, (a) => a.InformationalVersion);
			((Label)((Panel) _assemblyDetails.Rows[5].Cells[1].Control).Content).Text = GetVersion<AssemblyTitleAttribute>(asm, (a) => a.Title);
			((Label)((Panel) _assemblyDetails.Rows[6].Cells[1].Control).Content).Text = GetVersion<AssemblyDescriptionAttribute>(asm, (a) => a.Description);
			((Label)((Panel) _assemblyDetails.Rows[7].Cells[1].Control).Content).Text = GetVersion<AssemblyProductAttribute>(asm, (a) => a.Product);
			((Label)((Panel) _assemblyDetails.Rows[8].Cells[1].Control).Content).Text = GetVersion<AssemblyCopyrightAttribute>(asm, (a) => a.Copyright);
			((Label)((Panel) _assemblyDetails.Rows[9].Cells[1].Control).Content).Text = GetVersion<AssemblyCompanyAttribute>(asm, (a) => a.Company);
			((Label)((Panel)_assemblyDetails.Rows[10].Cells[1].Control).Content).Text = GetVersion<AssemblyTrademarkAttribute>(asm, (a) => a.Trademark);

			_reverseTreeItems.Children.Clear();

			if (!_reverseItems.ContainsKey(asm.GetName().Name))
			{
				_reverseItems.Clear();
				ProcessReverseTree();
			}

			var reverseItem = _reverseItems[asm.GetName().Name];
			reverseItem.Expanded = true;
			_reverseTreeItems.Children.Add(reverseItem);
			_reverseTreeView.RefreshData();
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
			_reverseItems.Clear();
			_processedItems.Clear();
			_treeItems.Children.Clear();
			var assemblyName = AssemblyName.GetAssemblyName(fileName);
			LoadAllReferences(assemblyName);
			AddReferences(_treeItems, assemblyName, false);
			_treeItems.Children.First().Expanded = true;
			_treeView.RefreshData();
			ProcessReverseTree();
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

		private ITreeItem CloneTreeItem(TreeItem parent, bool deepClone)
		{
			var clone = deepClone ? new TreeItem() : new TreeItem(parent.Children);
			clone.Key = TreeItemKey;
			clone.Text = parent.Text;
			clone.Expanded = parent.Expanded;
			clone.Tag = parent.Tag;

			if (!deepClone)
				return clone;

			foreach (TreeItem child in parent.Children.OrderBy(n => n.Text))
			{
				clone.Children.Add(CloneTreeItem(child, true));
			}
			return clone;
		}

		private void LoadAllReferences(AssemblyName assemblyName)
		{
			if (_allAssemblies.ContainsKey(assemblyName.Name))
				return;

			var asm = TryLoadAssembly(assemblyName);
			var list = new List<string>();
			_allAssemblies[assemblyName.Name] = list;
			if (asm == null || asm.GlobalAssemblyCache)
				return;

			foreach (var child in asm.GetReferencedAssemblies())
			{
				list.Add(child.Name);
				LoadAllReferences(child);
			}
		}

		private TreeItem AddReferences(TreeItem treeItem, AssemblyName assemblyName, bool makeLazy, int level = 0)
		{
			for (int i = 0; i < level; i++) Console.Write("    ");
			Console.WriteLine(assemblyName.Name);

			if (_processedItems.ContainsKey(assemblyName.Name))
			{
				var processedItem = _processedItems[assemblyName.Name];
				var lazyItem = processedItem as LazyTreeItem;
				if (lazyItem == null)
				{
					treeItem.Children.Add(CloneTreeItem(processedItem, false));
					return processedItem;
				}
			}

			var asm = TryLoadAssembly(assemblyName);
			string append = string.Empty;
			if (asm == null)
				append = " (not available)";
			else if (asm.GlobalAssemblyCache)
				append = " (GAC)";

			var item = makeLazy ? new LazyTreeItem() : new TreeItem();
			item.Key = TreeItemKey;
			item.Text = assemblyName.Name + append;
			item.Expanded = false;
			item.Tag = asm;
			if (!makeLazy && _processedItems.ContainsKey(assemblyName.Name))
			{
				ReplaceChild(treeItem, item);
			}
			else
				treeItem.Children.Add(item);
			_processedItems[assemblyName.Name] = item;

			if (asm == null || asm.GlobalAssemblyCache)
				return item;

			if (makeLazy)
			{
				var references = asm.GetReferencedAssemblies();
				if (references.Any())
					item.Children.Add(new LazyTreeItem { Text = references.First().Name });
			}
			else
			{
				foreach (var child in asm.GetReferencedAssemblies().OrderBy(n => n.Name))
				{
					AddReferences(item, child, true, level + 1);
				}
			}
			return item;
		}

		private static void ReplaceChild(TreeItem parent, TreeItem child)
		{
			for (int i = 0; i < parent.Count; i++)
			{
				if (parent.Children[i].Text == child.Text)
				{
					parent.Children[i] = child;
					return;
				}
			}
		}

		private TreeItem GetOrCreateReverseItem(string assemblyName)
		{
			var assemblyItem = _reverseItems.ContainsKey(assemblyName) ? _reverseItems[assemblyName] : new TreeItem { Text = assemblyName, Key = TreeItemKey };
			_reverseItems[assemblyName] = assemblyItem;
			return assemblyItem;
		}

		private static string GetAssemblyName(TreeItem item)
		{
			var assembly = item.Tag as Assembly;
			var assemblyName = assembly == null ? item.Text : assembly.GetName().Name;
			return assemblyName;
		}

		private void ProcessReverseTree()
		{
			foreach (var kv in _allAssemblies)
			{
				var reverseItem = GetOrCreateReverseItem(kv.Key);
				foreach (var childItem in kv.Value)
				{
					var assemblyItem = GetOrCreateReverseItem(childItem);
					assemblyItem.Children.Add(reverseItem);
					_reverseItems[assemblyItem.Text] = assemblyItem;
				}
			}

			//_treeItems.Children.Add(new TreeItem {Text = "--------------------------------------"});
			//foreach (var child in _reverseItems.Values.OrderBy(n => n.Text))
			//{
			//	_treeItems.Children.Add(CloneTreeItem(child, true));
			//}
		}
	}
}

