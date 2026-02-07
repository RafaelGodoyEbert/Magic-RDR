using Magic_RDR.RPF;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Drawing;
using System.Runtime.InteropServices;
using static Magic_RDR.RPF6.RPF6TOC;

namespace Magic_RDR.Viewers
{
    public partial class StringTableViewerForm : Form
    {
        [DllImport("uxtheme.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
        public static extern int SetWindowTheme(IntPtr hWnd, string pszSubAppName, string pszSubIdList);

        private TOCSuperEntry Entry;
        private RPF6 RPF;
        private StringTable Table;
        private List<TextBox[]> EditTextBoxes;
        private ListViewItem SelectedItem;
        private bool IsResource;

        public StringTableViewerForm(TOCSuperEntry entry, RPF6 rpf)
        {
            this.InitializeComponent();
            this.Text = string.Format("MagicRDR - StringTable Viewer [{0}]", entry.Entry.Name);
            this.Entry = entry;
            this.RPF = rpf;
            this.InitData();
            this.entryCountLabel.Text = string.Format("{0} entries", Table.Entries.Count == 0 ? (int)Table.StringCount : Table.Entries.Count);
            SetTheme();
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        private void SetTheme()
        {
            if (RPF6FileNameHandler.DarkMode)
            {
                this.BackColor = Color.FromArgb(45, 45, 48);
                this.ForeColor = Color.White;
                ApplyThemeToControls(this.Controls);

                // Enable Dark Title Bar
                int useImmersiveDarkMode = 1;
                DwmSetWindowAttribute(this.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useImmersiveDarkMode, sizeof(int));
            }
        }

        private void ApplyThemeToControls(Control.ControlCollection controls)
        {
            foreach (Control control in controls)
            {
                if (control is ListView listView)
                {
                    listView.BackColor = Color.FromArgb(30, 30, 30);
                    listView.ForeColor = Color.White;
                    listView.OwnerDraw = true;
                    SetWindowTheme(listView.Handle, "DarkMode_Explorer", null);
                    
                    listView.Resize -= ListView_Resize;
                    listView.Resize += ListView_Resize;
                    ListView_Resize(listView, EventArgs.Empty);

                    listView.DrawColumnHeader += (sender, headerScore) =>
                    {
                        headerScore.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(45, 45, 48)), headerScore.Bounds);
                        headerScore.Graphics.DrawRectangle(new Pen(Color.FromArgb(60, 60, 60)), headerScore.Bounds);
                        TextRenderer.DrawText(headerScore.Graphics, listView.Columns[headerScore.ColumnIndex].Text, listView.Font, headerScore.Bounds, Color.White, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
                    };

                    listView.DrawItem += (sender, itemScore) =>
                    {
                        if (itemScore.Item.Selected)
                        {
                            itemScore.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(60, 60, 60)), itemScore.Bounds);
                        }
                        else
                        {
                            itemScore.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(30, 30, 30)), itemScore.Bounds);
                        }
                        itemScore.DrawText();
                    };

                    listView.DrawSubItem += (sender, subItemScore) =>
                    {
                        if (subItemScore.Item.Selected)
                        {
                            subItemScore.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(60, 60, 60)), subItemScore.Bounds);
                        }
                        else
                        {
                            subItemScore.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(30, 30, 30)), subItemScore.Bounds);
                        }
                        subItemScore.DrawText();
                    };
                }
                else if (control is MenuStrip menuStrip)
                {
                    menuStrip.BackColor = Color.FromArgb(45, 45, 48);
                    menuStrip.ForeColor = Color.White;
                    menuStrip.Renderer = new DarkThemeRenderer();
                    foreach (ToolStripItem item in menuStrip.Items)
                    {
                        item.BackColor = Color.FromArgb(45, 45, 48);
                        item.ForeColor = Color.White;
                    }
                }
                else if (control is ToolStrip toolStrip)
                {
                    toolStrip.BackColor = Color.FromArgb(45, 45, 48);
                    toolStrip.ForeColor = Color.White;
                    toolStrip.Renderer = new DarkThemeRenderer();
                    foreach (ToolStripItem item in toolStrip.Items)
                    {
                        item.BackColor = Color.FromArgb(45, 45, 48);
                        item.ForeColor = Color.White;
                    }
                }
                else if (control is TabControl tabControl)
                {
                    tabControl.Appearance = TabAppearance.Normal; 
                    tabControl.DrawMode = TabDrawMode.OwnerDrawFixed;
                    tabControl.Padding = new Point(16, 4); // Add some padding for comfort
                    tabControl.DrawItem -= TabControl_DrawItem; // Prevent Valid
                    tabControl.DrawItem += TabControl_DrawItem; // Add Handler
                    SetWindowTheme(tabControl.Handle, "DarkMode_Explorer", null);
                }
                else if (control is TabPage tabPage)
                {
                    tabPage.BackColor = Color.FromArgb(45, 45, 48);
                    tabPage.ForeColor = Color.White;
                }

                if (control.HasChildren)
                {
                    ApplyThemeToControls(control.Controls);
                }
            }
        }

        private void ListView_Resize(object sender, EventArgs e)
        {
            ListView lv = sender as ListView;
            if (lv == null || lv.Columns.Count == 0) return;

            int totalWidth = 0;
            // Sum width of all columns EXCEPT the last one
            for (int i = 0; i < lv.Columns.Count - 1; i++)
            {
                totalWidth += lv.Columns[i].Width;
            }

            int remaining = lv.ClientSize.Width - totalWidth;
            if (remaining > 50) 
            {
                    lv.Columns[lv.Columns.Count - 1].Width = remaining - 4;
            }
        }

        private void TabControl_DrawItem(object sender, DrawItemEventArgs e)
        {
            TabControl tc = sender as TabControl;
            TabPage page = tc.TabPages[e.Index];
            Rectangle tabBounds = tc.GetTabRect(e.Index);

            // Background of the tab header
            Color backColor = (e.State == DrawItemState.Selected) ? Color.FromArgb(60, 60, 60) : Color.FromArgb(45, 45, 48);
            using (SolidBrush brush = new SolidBrush(backColor))
            {
                e.Graphics.FillRectangle(brush, tabBounds);
            }

            // Text
            Color textColor = (e.State == DrawItemState.Selected) ? Color.White : Color.Gray;
            TextRenderer.DrawText(e.Graphics, page.Text, tc.Font, tabBounds, textColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            
            // Clean up focus rectangle
            if (e.State == DrawItemState.Selected)
            {
               e.DrawFocusRectangle();
            }
        }

        private class DarkThemeRenderer : ToolStripProfessionalRenderer
        {
            public DarkThemeRenderer() : base(new DarkThemeColorTable()) { }
        }

        private class DarkThemeColorTable : ProfessionalColorTable
        {
            public override Color MenuItemSelected => Color.FromArgb(60, 60, 60);
            public override Color MenuItemSelectedGradientBegin => Color.FromArgb(60, 60, 60);
            public override Color MenuItemSelectedGradientEnd => Color.FromArgb(60, 60, 60);
            public override Color MenuBorder => Color.FromArgb(45, 45, 48);
            public override Color MenuItemBorder => Color.FromArgb(60, 60, 60);
            public override Color MenuItemPressedGradientBegin => Color.FromArgb(45, 45, 48);
            public override Color MenuItemPressedGradientEnd => Color.FromArgb(45, 45, 48);
            public override Color ToolStripDropDownBackground => Color.FromArgb(45, 45, 48);
            public override Color ImageMarginGradientBegin => Color.FromArgb(45, 45, 48);
            public override Color ImageMarginGradientMiddle => Color.FromArgb(45, 45, 48);
            public override Color ImageMarginGradientEnd => Color.FromArgb(45, 45, 48);
            public override Color ButtonSelectedHighlight => Color.FromArgb(60, 60, 60);
            public override Color ButtonSelectedGradientBegin => Color.FromArgb(60, 60, 60);
            public override Color ButtonSelectedGradientEnd => Color.FromArgb(60, 60, 60);
            public override Color ButtonPressedGradientBegin => Color.FromArgb(45, 45, 48);
            public override Color ButtonPressedGradientEnd => Color.FromArgb(45, 45, 48);
            public override Color ButtonSelectedBorder => Color.FromArgb(60, 60, 60);
        }

        private void InitData()
        {
            if (Entry == null)
            {
                return;
            }

            MemoryStream memoryStream = new MemoryStream();
            RPF.TOC.ExtractFile(Entry, memoryStream);

            this.IsResource = !Entry.Entry.Name.EndsWith(".strtbl");
            Table = new StringTable(memoryStream, this.IsResource);

            LoadData();
        }

        private void LoadData()
        {
            StringBuilder stringBuilder = new StringBuilder();
            this.listView.BeginUpdate();

            if (this.IsResource)
            {
                this.listView.Columns.Add("Index", 50);
                this.listView.Columns.Add("Key", 100);
                this.listView.Columns.Add("Value", 2000);

                // Initialize the editing TextBoxes for each column
                this.EditTextBoxes = new List<TextBox[]>
                {
                    new TextBox[this.listView.Columns.Count]
                };

                for (int i = 0; i < this.listView.Columns.Count; i++)
                {
                    this.EditTextBoxes[0][i] = new TextBox();
                    this.EditTextBoxes[0][i].Visible = false;
                    this.EditTextBoxes[0][i].Parent = this.listView;
                    this.EditTextBoxes[0][i].KeyDown += EditTextBox_KeyDown;
                    this.EditTextBoxes[0][i].LostFocus += EditTextBox_LostFocus;
                }

                for (int index = 0; index < this.Table.Entries.Count; index++)
                {
                    var item = new ListViewItem(new string[] { index.ToString() ,"0x" + this.Table.Entries[index].Key.ToString("X8"), this.Table.Entries[index].StrValue });
                    this.listView.Items.Add(item);
                }
            }
            else
            {
                this.EditTextBoxes = new List<TextBox[]>();
                this.tabControl.TabPages.Remove(this.tabControl.TabPages[0]);

                for (int s = 0; s < this.Table.Segments.Length; s++)
                {
                    bool supportedLanguage = true;
                    var segment = this.Table.Segments[s];
                    this.tabControl.TabPages.Add(segment.Name);

                    var lv = new ListView()
                    {
                        Dock = DockStyle.Fill,
                        View = View.Details,
                        FullRowSelect = true,
                        LabelEdit = supportedLanguage,
                        Enabled = supportedLanguage
                    };

                    // Apply Theme Early
                    if (RPF6FileNameHandler.DarkMode)
                    {
                         lv.BackColor = Color.FromArgb(30, 30, 30);
                         lv.ForeColor = Color.White;
                         lv.OwnerDraw = true;
                         SetWindowTheme(lv.Handle, "DarkMode_Explorer", null);
                         lv.DrawColumnHeader += (sender, headerScore) =>
                         {
                              headerScore.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(45, 45, 48)), headerScore.Bounds);
                              headerScore.Graphics.DrawRectangle(new Pen(Color.FromArgb(60, 60, 60)), headerScore.Bounds);
                              TextRenderer.DrawText(headerScore.Graphics, lv.Columns[headerScore.ColumnIndex].Text, lv.Font, headerScore.Bounds, Color.White, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
                         };
                         lv.DrawItem += (sender, itemScore) =>
                         {
                              if (itemScore.Item.Selected)
                                   itemScore.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(60, 60, 60)), itemScore.Bounds);
                              else
                                   itemScore.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(30, 30, 30)), itemScore.Bounds);
                              itemScore.DrawText();
                         };
                         lv.DrawSubItem += (sender, subItemScore) =>
                         {
                              if (subItemScore.Item.Selected)
                                   subItemScore.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(60, 60, 60)), subItemScore.Bounds);
                              else
                                   subItemScore.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(30, 30, 30)), subItemScore.Bounds);
                              subItemScore.DrawText();
                         };
                    }

                    lv.Columns.Add("Index", 50);
                    lv.Columns.Add("Value", 3000);

                    // Initialize the editing TextBoxes for each column
                    this.EditTextBoxes.Add(new TextBox[lv.Columns.Count]);

                    for (int i = 0; i < lv.Columns.Count; i++)
                    {
                        this.EditTextBoxes[s][i] = new TextBox();
                        this.EditTextBoxes[s][i].Visible = false;
                        this.EditTextBoxes[s][i].Parent = lv;
                        this.EditTextBoxes[s][i].KeyDown += EditTextBox_KeyDown;
                        this.EditTextBoxes[s][i].LostFocus += EditTextBox_LostFocus;
                    }

                    lv.MouseDoubleClick += (sender, e) =>
                    {
                        int index = this.tabControl.SelectedIndex;
                        var clickedItem = lv.GetItemAt(e.X, e.Y);

                        if (clickedItem != null)
                        {
                            var subItem = clickedItem.GetSubItemAt(e.X, e.Y);
                            if (subItem != null)
                            {
                                int columnIndex = -1;
                                for (int i = 0; i < lv.Columns.Count; i++) //Find the column index by comparing sub-item text with column header text
                                {
                                    if (!Regex.IsMatch(subItem.Text, @"^\d+(\.\d+)?$"))
                                    {
                                        columnIndex = 1;
                                        break;
                                    }
                                }

                                if (columnIndex != -1)
                                {
                                    this.SelectedItem = clickedItem;
                                    this.EditTextBoxes[index][columnIndex].Text = subItem.Text;
                                    this.EditTextBoxes[index][columnIndex].Location = subItem.Bounds.Location;
                                    this.EditTextBoxes[index][columnIndex].Size = subItem.Bounds.Size;
                                    this.EditTextBoxes[index][columnIndex].Visible = true;
                                    this.EditTextBoxes[index][columnIndex].Focus();
                                }
                            }
                        }
                    };

                    for (int i = 0; i < segment.Strings.Count; i++)
                    {
                        var item = new ListViewItem(new string[] { i.ToString(), segment.Strings[i] });
                        lv.Items.Add(item);
                    }
                    this.tabControl.TabPages[s].Controls.Add(lv);
                }
            }
            this.listView.EndUpdate();
        }

        private void exportButton_Click(object sender, EventArgs e)
        {
            var sb = new StringBuilder();
            if (this.IsResource)
            {
                foreach (ListViewItem item in this.listView.Items)
                {
                    sb.AppendLine(string.Format("[{0}] - {1} - {2}", item.SubItems[0].Text, item.SubItems[1].Text, item.SubItems[2].Text));
                }
            }
            else
            {
                foreach (ListView lv in this.tabControl.SelectedTab.Controls)
                {
                    foreach (ListViewItem item in lv.Items)
                    {
                        sb.AppendLine(string.Format("[{0}] - {1}", item.SubItems[0].Text, item.SubItems[1].Text));
                    }
                }
            }

            if (string.IsNullOrEmpty(sb.ToString()))
            {
                MessageBox.Show("There's nothing to export !", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Title = "Export";
            dialog.Filter = "Plain Text (*.txt)|*.txt";
            dialog.FileName = Entry.Entry.Name.Replace(".xst", ".txt").Replace(".sst", ".txt").Replace(".strtbl", ".txt");

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                File.WriteAllText(dialog.FileName, sb.ToString());
                MessageBox.Show("Saved successfully !", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void rebuildButton_Click(object sender, EventArgs e)
        {
            StringTable table = this.MakeStringTable();
            if (table != null)
            {
                SaveFileDialog saveFileDialog = null;
                bool res = this.IsResource;

                if (res)
                {
                    saveFileDialog = new SaveFileDialog
                    {
                        Title = "Save StringTable",
                        FileName = this.Entry.Entry.Name,
                        Filter = "StringTable File (*.{0}st)|*.{0}st|All files (*.*)|*.*"
                    };
                    saveFileDialog.Filter = string.Format(saveFileDialog.Filter, AppGlobals.PlatformChar);
                }
                else
                {
                    saveFileDialog = new SaveFileDialog
                    {
                        Title = "Save StringTable",
                        FileName = this.Entry.Entry.Name,
                        Filter = "StringTable File (*.strtbl)|*.strtbl|All files (*.*)|*.*"
                    };
                }

                if (saveFileDialog.ShowDialog() != DialogResult.OK)
                    return;
                
                Stream xOut = File.Create(saveFileDialog.FileName);
                if (res)
                    table.WriteTable(xOut, res);
                else
                    this.Table.WriteTable(xOut, res);

                xOut.Flush();
                xOut.Close();
            }
            else
            {
                MessageBox.Show("An error occured while saving the file...", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            MessageBox.Show("Successfully saved & exported the file !\nDon't forget to import/replace it", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            this.Close();
        }

        private StringTable MakeStringTable()
        {
            StringTable st = new StringTable
            {
                TableModValue = 101U
            };
            
            if (this.IsResource)
            {
                foreach (ListViewItem item in this.listView.Items)
                {
                    st.AddEntry(item.SubItems[1].Text, item.SubItems[2].Text);
                }
            }
            else
            {
                var segments = this.Table.Segments;
                foreach (TabPage page in this.tabControl.TabPages)
                {
                    if (!IsLanguageEditable(page.Text))
                        continue;

                    var currentSegment = segments.FirstOrDefault(item => item.Name == page.Text);
                    if (currentSegment == null)
                    {
                        return null;
                    }

                    currentSegment.Strings.Clear();
                    foreach (ListView lv in page.Controls)
                    {
                        foreach (ListViewItem item in lv.Items)
                        {
                            currentSegment.Strings.Add(item.SubItems[1].Text);
                        }
                    }
                }
            }
            return st;
        }

        private bool IsLanguageEditable(string str)
        {
            switch (str)
            {
                case "English":
                case "Spanish":
                case "French":
                case "Spanish (Spain)":
                case "Spanish (Mexico)":
                case "Portuguese":
                case "German":
                case "Italian":
                case "Polish":
                case "Russian":
                    return true;
                case "Chinese (Traditional)":
                case "Chinese (Simplified)":
                case "Japanese":
                case "Korean":
                default:
                    return false;
            }
        }

        private void EditTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            int index = this.tabControl.SelectedIndex;

            if (e.KeyCode == Keys.Enter)
            {
                //Update the selected item's sub-item with the edited text
                for (int i = 0; i < this.EditTextBoxes[index].Length; i++)
                {
                    if (sender == this.EditTextBoxes[index][i])
                    {
                        this.SelectedItem.SubItems[i].Text = this.EditTextBoxes[index][i].Text;
                        this.EditTextBoxes[index][i].Visible = false;
                        break;
                    }
                }
            }
            else if (e.KeyCode == Keys.Escape)
            {
                for (int i = 0; i < this.EditTextBoxes[index].Length; i++)
                {
                    if (sender == this.EditTextBoxes[index][i])
                    {
                        this.EditTextBoxes[index][i].Visible = false;
                    }
                }
            }
        }

        private void EditTextBox_LostFocus(object sender, EventArgs e)
        {
            int index = this.tabControl.SelectedIndex;

            //Update the selected item's sub-item with the edited text when the TextBox loses focus
            for (int i = 0; i < this.EditTextBoxes[index].Length; i++)
            {
                if (sender == this.EditTextBoxes[index][i])
                {
                    this.SelectedItem.SubItems[i].Text = this.EditTextBoxes[index][i].Text;
                    this.EditTextBoxes[index][i].Visible = false;
                    break;
                }
            }
        }

        private void listView_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            int index = this.tabControl.SelectedIndex;
            ListViewItem clickedItem = this.listView.GetItemAt(e.X, e.Y);

            if (clickedItem != null)
            {
                ListViewItem.ListViewSubItem subItem = clickedItem.GetSubItemAt(e.X, e.Y);
                if (subItem != null)
                {
                    int columnIndex = -1;
                    for (int i = 0; i < this.listView.Columns.Count; i++) //Find the column index by comparing sub-item text with column header text
                    {
                        if (!subItem.Text.StartsWith("0x") && !Regex.IsMatch(subItem.Text, @"^\d+(\.\d+)?$"))
                        {
                            columnIndex = 2;
                            break;
                        }
                    }

                    if (columnIndex != -1)
                    {
                        this.SelectedItem = clickedItem;
                        this.EditTextBoxes[index][columnIndex].Text = subItem.Text;
                        this.EditTextBoxes[index][columnIndex].Location = subItem.Bounds.Location;
                        this.EditTextBoxes[index][columnIndex].Size = subItem.Bounds.Size;
                        this.EditTextBoxes[index][columnIndex].Visible = true;
                        this.EditTextBoxes[index][columnIndex].Focus();
                    }
                }
            }
        }

		private void ImportTextIntoListView(ListView lv, string[] lines)
		{
			bool hasThreeColumns = DetectThreeColumnFormat(lines);

			// For 3 columns: [index] - 0xKEY - VALUE
			Regex threeColRegex = new Regex(
				@"^\s*\[(\d+)\]\s*-\s*0x[0-9A-Fa-f]{8}\s*-\s*(.*)$",
				RegexOptions.Compiled);

			// For 2 columns: [index] - VALUE
			Regex twoColRegex = new Regex(
				@"^\s*\[(\d+)\]\s*-\s*(.*)$",
				RegexOptions.Compiled);

			// Build index -> item map once (performance critical) ---
			// key = index text from first column
			Dictionary<string, ListViewItem> indexMap = lv.Items
				.Cast<ListViewItem>()
				.Where(li => li.SubItems.Count > 0)
				.ToDictionary(li => li.SubItems[0].Text, li => li);

			int valueColumnIndex = Math.Max(0, lv.Columns.Count - 1);

			lv.BeginUpdate();

			try
			{
				foreach (string line in lines)
				{
					if (string.IsNullOrWhiteSpace(line))
						continue;

					string indexStr;
					string valueStr;

					if (hasThreeColumns)
					{
						Match m = threeColRegex.Match(line);
						if (!m.Success)
							continue;

						indexStr = m.Groups[1].Value;
						valueStr = m.Groups[2].Value;
					}
					else
					{
						Match m = twoColRegex.Match(line);
						if (!m.Success)
							continue;

						indexStr = m.Groups[1].Value;
						valueStr = m.Groups[2].Value;
					}

					// Fast lookup by index instead of linear search
					if (!indexMap.TryGetValue(indexStr, out ListViewItem item))
						continue;

					if (item.SubItems.Count > valueColumnIndex)
					{
						// Update last column (Value)
						item.SubItems[valueColumnIndex].Text = valueStr;
					}
				}
			}
			finally
			{
				lv.EndUpdate();
			}
		}

		private bool DetectThreeColumnFormat(string[] lines)
		{
			// Detect "[i] - 0xXXXXXXXX - ..." in any non-empty line
			Regex detectRegex = new Regex(
				@"^\s*\[(\d+)\]\s*-\s*0x[0-9A-Fa-f]{8}\s*-\s*",
				RegexOptions.Compiled);

			foreach (string line in lines)
			{
				if (string.IsNullOrWhiteSpace(line))
					continue;

				return detectRegex.IsMatch(line);
			}

			return false;
		}

		private void importButton_Click(object sender, EventArgs e)
		{
			OpenFileDialog dialog = new OpenFileDialog
			{
				Title = "Import",
				Filter = "Plain Text (*.txt)|*.txt",
				FileName = Entry.Entry.Name
			};

			if (dialog.ShowDialog() != DialogResult.OK)
				return;

			string[] lines = File.ReadAllLines(dialog.FileName, Encoding.UTF8);

			if (lines.Length == 0)
			{
				MessageBox.Show("File is empty, nothing to import.", "Import",
					MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}

			// Try to get ListView from currently selected tab
			ListView lv = null;

			if (this.tabControl.SelectedTab != null)
			{
				lv = this.tabControl.SelectedTab.Controls
					.OfType<ListView>()
					.FirstOrDefault();
			}

			// Fallback to main listView if nothing found
			if (lv == null)
				lv = this.listView;

			if (lv == null)
			{
				MessageBox.Show("No ListView available for import.", "Import",
					MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}

			ImportTextIntoListView(lv, lines);

			MessageBox.Show("Import finished. Now you can press Save.", "Import",
				MessageBoxButtons.OK, MessageBoxIcon.Information);
		}
	}
}
