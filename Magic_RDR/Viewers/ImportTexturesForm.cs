using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using static Magic_RDR.Viewers.TextureViewerForm;
using static Magic_RDR.RPF.Texture;
using System.Linq;
using System.IO;
using System.Drawing;

namespace Magic_RDR.Viewers
{
    public partial class ImportTexturesForm : Form
    {
        public DialogResult DialogResultValue { get; private set; }
        private TextureInfo[] DefaultTextures;
        private List<string> CorrectTexturesPaths;
        private List<string> AddedTexturesPaths;
        private string EntryName;
        private int TotalCorrectTextures = 0;

        public ImportTexturesForm(string currentFileName)
        {
            this.InitializeComponent();

            this.CorrectTexturesPaths = new List<string>();
            this.AddedTexturesPaths = new List<string>();
            this.EntryName = currentFileName;
            this.DefaultTextures = XTD_TextureDictionary.TexInfos;
            this.validateButton.Enabled = false;

            this.UpdateListView();
            SetTheme();
        }

        [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
        [System.Runtime.InteropServices.DllImport("uxtheme.dll", ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hWnd, string pszSubAppName, string pszSubIdList);
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        private void SetTheme()
        {
            if (Magic_RDR.RPF.RPF6FileNameHandler.DarkMode)
            {
                this.BackColor = System.Drawing.Color.FromArgb(45, 45, 48);
                this.ForeColor = System.Drawing.Color.White;

                listView.BackColor = System.Drawing.Color.FromArgb(30, 30, 30);
                listView.ForeColor = System.Drawing.Color.White;
                listView.OwnerDraw = true;
                SetWindowTheme(listView.Handle, "DarkMode_Explorer", null);
                listView.DrawColumnHeader += ListView_DrawColumnHeader;
                listView.DrawItem += ListView_DrawItem;
                listView.DrawSubItem += ListView_DrawSubItem;

                richTextBox1.BackColor = System.Drawing.Color.FromArgb(30, 30, 30);
                richTextBox1.ForeColor = System.Drawing.Color.White;
                
                ApplyThemeToControls(this.Controls);

                int useImmersiveDarkMode = 1;
                DwmSetWindowAttribute(this.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useImmersiveDarkMode, sizeof(int));
            }
        }

        private void ListView_DrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
        {
            e.Graphics.FillRectangle(new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(45, 45, 48)), e.Bounds);
            e.Graphics.DrawRectangle(new System.Drawing.Pen(System.Drawing.Color.FromArgb(60, 60, 60)), e.Bounds);
            TextRenderer.DrawText(e.Graphics, e.Header.Text, ((ListView)sender).Font, e.Bounds, System.Drawing.Color.White, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
        }

        private void ListView_DrawItem(object sender, DrawListViewItemEventArgs e)
        {
            if (e.Item.Selected) e.Graphics.FillRectangle(new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(60, 60, 60)), e.Bounds);
            else e.Graphics.FillRectangle(new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(30, 30, 30)), e.Bounds);
            e.DrawText();
        }

        private void ListView_DrawSubItem(object sender, DrawListViewSubItemEventArgs e)
        {
             if (e.Item.Selected) e.Graphics.FillRectangle(new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(60, 60, 60)), e.Bounds);
             else e.Graphics.FillRectangle(new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(30, 30, 30)), e.Bounds);
             e.DrawText();
        }

        private void ApplyThemeToControls(Control.ControlCollection controls)
        {
             foreach (Control control in controls)
             {
                 if (control is ToolStrip toolStrip)
                 {
                     toolStrip.Renderer = new DarkThemeRenderer();
                     toolStrip.BackColor = System.Drawing.Color.FromArgb(45, 45, 48);
                     toolStrip.ForeColor = System.Drawing.Color.White;
                 }
                 else if (control is Label label)
                 {
                     label.ForeColor = System.Drawing.Color.White;
                     label.BackColor = System.Drawing.Color.FromArgb(45, 45, 48);
                 }
                 else if (control is Panel panel)
                 {
                     panel.BackColor = System.Drawing.Color.FromArgb(45, 45, 48);
                     panel.ForeColor = System.Drawing.Color.White;
                 }
                 else if (control is SplitContainer split)
                 {
                     split.BackColor = System.Drawing.Color.FromArgb(45, 45, 48);
                 }

                 if (control.HasChildren) ApplyThemeToControls(control.Controls);
             }
        }

        private class DarkThemeRenderer : ToolStripProfessionalRenderer
        {
            public DarkThemeRenderer() : base(new DarkThemeColorTable()) { }
        }

        private class DarkThemeColorTable : ProfessionalColorTable
        {
            public override System.Drawing.Color MenuItemSelected => System.Drawing.Color.FromArgb(60, 60, 60);
            public override System.Drawing.Color MenuItemSelectedGradientBegin => System.Drawing.Color.FromArgb(60, 60, 60);
            public override System.Drawing.Color MenuItemSelectedGradientEnd => System.Drawing.Color.FromArgb(60, 60, 60);
            public override System.Drawing.Color MenuBorder => System.Drawing.Color.FromArgb(45, 45, 48);
            public override System.Drawing.Color MenuItemBorder => System.Drawing.Color.FromArgb(60, 60, 60);
            public override System.Drawing.Color MenuItemPressedGradientBegin => System.Drawing.Color.FromArgb(45, 45, 48);
            public override System.Drawing.Color MenuItemPressedGradientEnd => System.Drawing.Color.FromArgb(45, 45, 48);
            public override System.Drawing.Color ToolStripDropDownBackground => System.Drawing.Color.FromArgb(45, 45, 48);
            public override System.Drawing.Color ImageMarginGradientBegin => System.Drawing.Color.FromArgb(45, 45, 48);
            public override System.Drawing.Color ImageMarginGradientMiddle => System.Drawing.Color.FromArgb(45, 45, 48);
            public override System.Drawing.Color ImageMarginGradientEnd => System.Drawing.Color.FromArgb(45, 45, 48);
            public override System.Drawing.Color ButtonSelectedHighlight => System.Drawing.Color.FromArgb(60, 60, 60);
            public override System.Drawing.Color ButtonSelectedGradientBegin => System.Drawing.Color.FromArgb(60, 60, 60);
            public override System.Drawing.Color ButtonSelectedGradientEnd => System.Drawing.Color.FromArgb(60, 60, 60);
            public override System.Drawing.Color ButtonPressedGradientBegin => System.Drawing.Color.FromArgb(45, 45, 48);
            public override System.Drawing.Color ButtonPressedGradientEnd => System.Drawing.Color.FromArgb(45, 45, 48);
            public override System.Drawing.Color ButtonSelectedBorder => System.Drawing.Color.FromArgb(60, 60, 60);
            public override System.Drawing.Color ToolStripBorder => System.Drawing.Color.FromArgb(45, 45, 48);
        }

        private void addTextureButton_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Title = "Open",
                Filter = "Direct Draw Surface File|*.dds"
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                if (!AddedTexturesPaths.Contains(dialog.FileName))
                {
                    this.AddedTexturesPaths.Add(dialog.FileName);
                    this.UpdateTextBox();
                }
            }
        }

        private void addDirectoryButton_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    string[] files = Directory.GetFiles(dialog.SelectedPath, "*.dds");
                    foreach (string file in files)
                    {
                        try
                        {
                            if (!AddedTexturesPaths.Contains(file))
                            {
                                this.AddedTexturesPaths.Add(file);
                                this.UpdateTextBox();
                            }
                        }
                        catch
                        {
                            break;
                        }
                    }
                }
            }
        }

        private void validateButton_Click(object sender, EventArgs e)
        {
            try
            {
                string currentPath = AppDomain.CurrentDomain.BaseDirectory;
                string exportPath = Path.Combine(currentPath, "Temp");
 
                //Copy the selected valid DDS to the temp folder
                foreach (var file in this.CorrectTexturesPaths)
                {
                    File.Copy(file, exportPath + "\\" + this.EntryName.Replace(".wtd", "") + "\\" + Path.GetFileName(file), true);
                }
                this.DialogResultValue = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occured :\n\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateListView()
        {
            List<ListViewItem> list = new List<ListViewItem>();
            foreach (var texture in this.DefaultTextures)
            {
                if (texture == null)
                {
                    continue;
                }

                string newTextureName = texture.TextureName;
                if (newTextureName.Contains("/"))
                {
                    newTextureName = newTextureName.Substring(newTextureName.LastIndexOf("/") + 1);
                }
                if (newTextureName.Contains(":"))
                {
                    newTextureName = newTextureName.Substring(newTextureName.LastIndexOf(":") + 1);
                }

                var item = new ListViewItem();
                item.Text = newTextureName;
                item.SubItems.Add(string.Format("{0}x{1}", texture.Width, texture.Height));
                item.SubItems.Add(texture.PixelFormat.ToString());
                list.Add(item);
            }
            this.columnTexture.Text = string.Format("Texture ({0})", this.DefaultTextures.Length);
            this.listView.BeginUpdate();
            this.listView.Items.Clear();
            this.listView.Items.AddRange(list.ToArray());
            this.listView.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            this.listView.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
            this.listView.EndUpdate();
        }

        private void UpdateTextBox()
        {
            var sb = new StringBuilder();
            foreach (var texture in this.AddedTexturesPaths)
            {
                if (!sb.ToString().Contains(texture))
                {
                    var matchingItems = this.listView.Items.Cast<ListViewItem>().
                        Where(item => item.SubItems.Cast<ListViewItem.ListViewSubItem>()
                        .Any(subitem => subitem.Text.Equals(Path.GetFileName(texture))));

                    if (matchingItems.Any())
                    {
                        sb.AppendLine("FOUND : " + texture);
                        this.CorrectTexturesPaths.Add(texture);
                        this.validateButton.Enabled = true;
                        this.TotalCorrectTextures++;
                    }
                    else sb.AppendLine("NOT FOUND : " + texture);
                }
            }
            this.richTextBox1.Text = sb.ToString();
            this.correctTextureLabel.Text = "Correct Textures : " + TotalCorrectTextures.ToString();
            this.addedTextureLabel.Text = "Added Textures : " + AddedTexturesPaths.Count.ToString();

            // Color specific strings
            this.ColorText("FOUND", Color.Green);
            this.ColorText("NOT FOUND", Color.Red);
        }

        private void ColorText(string searchText, Color color)
        {
            int start = 0;
            while (start < this.richTextBox1.TextLength)
            {
                int foundStart = this.richTextBox1.Find(searchText, start, RichTextBoxFinds.None);
                if (foundStart == -1)
                {
                    break;
                }

                this.richTextBox1.SelectionStart = foundStart;
                this.richTextBox1.SelectionLength = searchText.Length;
                this.richTextBox1.SelectionColor = color;

                start = foundStart + searchText.Length;
            }
        }

        private void ImportTexturesForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (this.TotalCorrectTextures > 0)
                this.DialogResultValue = DialogResult.OK;
            else
                this.DialogResultValue = DialogResult.Cancel;
        }
    }
}
