using Magic_RDR.RPF;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms;
using System.Xml;
using System.Runtime.InteropServices;

namespace Magic_RDR
{
    public partial class TextViewerForm : Form
    {
        [DllImport("uxtheme.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
        public static extern int SetWindowTheme(IntPtr hWnd, string pszSubAppName, string pszSubIdList);

        public RPF6.RPF6TOC.TOCSuperEntry Entry;
        public byte[] FileData;
        private string OriginalFileContent;

        public TextViewerForm(RPF6.RPF6TOC.TOCSuperEntry entry, byte[] data)
        {
            InitializeComponent();
            Entry = entry;
            FileData = data;
            Text = string.Format("MagicRDR - TextViewer [{0}]", entry.Entry.Name);

            textBox.Text = Encoding.UTF8.GetString(data);
            OriginalFileContent = textBox.Text;
            saveButton.Enabled = !entry.Entry.Name.EndsWith(".dat");

            charCountLabel.Text = string.Format("{0} characters, {1} lines", textBox.Text.Length, textBox.LinesCount);
            zoomLabel.Text = string.Format("Zoom {0}%", textBox.Zoom);
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
                if (control is MenuStrip menuStrip)
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
                else if (control is FastColoredTextBoxNS.FastColoredTextBox fctb)
                {
                    // Softer background for better contrast
                    fctb.BackColor = Color.FromArgb(30, 30, 30);
                    fctb.ForeColor = Color.FromArgb(220, 220, 220); // Off-white text

                    // Gutter styling
                    fctb.IndentBackColor = Color.FromArgb(40, 40, 42);
                    fctb.LineNumberColor = Color.FromArgb(100, 100, 100);
                    
                    // Selection and Current Line
                    fctb.SelectionColor = Color.FromArgb(60, 0, 122, 204); // VS-style selection blue
                    fctb.CurrentLineColor = Color.FromArgb(40, 255, 255, 255); // Subtle highlighting

                    // Apply Scrollbar Theme
                    SetWindowTheme(fctb.Handle, "DarkMode_Explorer", null);

                    // ... (previous simple styles)
                    
                    // Manually override XML styles using the SyntaxHighlighter if possible, 
                    // or just creating new styles.
                    // Since we can't easily access the internal default styles, we'll try to apply a custom one if Language is XML.
                    if (fctb.Language == FastColoredTextBoxNS.Language.XML)
                    {
                        // Create readable styles for Dark Mode
                        FastColoredTextBoxNS.TextStyle tagStyle = new FastColoredTextBoxNS.TextStyle(Brushes.DodgerBlue, null, FontStyle.Regular);
                        FastColoredTextBoxNS.TextStyle attrNameStyle = new FastColoredTextBoxNS.TextStyle(Brushes.LightSkyBlue, null, FontStyle.Regular);
                        FastColoredTextBoxNS.TextStyle attrValueStyle = new FastColoredTextBoxNS.TextStyle(Brushes.LightSalmon, null, FontStyle.Regular);
                        FastColoredTextBoxNS.TextStyle commentStyle = new FastColoredTextBoxNS.TextStyle(Brushes.LightGreen, null, FontStyle.Italic); // Green for comments
                        
                        // We need to apply these. FCTB doesn't expose a simple "SetStyle" for language.
                        // We would typically clear styles and re-add regex.
                        // But simply setting the default style handles non-matched text.
                        // The user complaint is likely about the default "Blue" for tags being too dark.
                        
                        // Let's try to update the syntax highlighter by recreating it or accessing properties if public.
                        // Assuming standard FCTB usage:
                        fctb.SyntaxHighlighter.AttributeValueStyle = attrValueStyle;
                        fctb.SyntaxHighlighter.AttributeStyle = attrNameStyle;
                        fctb.SyntaxHighlighter.TagBracketStyle = tagStyle;
                        fctb.SyntaxHighlighter.TagNameStyle = tagStyle;
                        fctb.SyntaxHighlighter.CommentStyle = commentStyle;
                    }
                }
                else if (control is StatusStrip statusStrip)
                {
                    statusStrip.BackColor = Color.FromArgb(43, 43, 43);
                    statusStrip.ForeColor = Color.White;
                    foreach (ToolStripItem item in statusStrip.Items)
                    {
                        item.BackColor = Color.FromArgb(43, 43, 43);
                        item.ForeColor = Color.White;
                    }
                }

                if (control.HasChildren)
                {
                    ApplyThemeToControls(control.Controls);
                }
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

        private void exportButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(textBox.Text) || Entry == null)
            {
                MessageBox.Show("There's nothing to export...", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Title = "Export";
            dialog.FileName = Entry.Entry.Name;

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                File.WriteAllText(dialog.FileName, textBox.Text);
                MessageBox.Show("Successfully exported !", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            if (Entry == null)
                return;

            if (textBox.Text == OriginalFileContent)
            {
                MessageBox.Show("No need to save, you didn't change anything...", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (MessageBox.Show("This will overwrite the current file\n\nContinue ?", "Error", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
            {
                return;
            }

            RPF6.RPF6TOC.TOCSuperEntry NewEntry = new RPF6.RPF6TOC.TOCSuperEntry();
            byte[] data = Encoding.UTF8.GetBytes(textBox.Text);

            NewEntry.CustomDataStream = new MemoryStream(data);
            NewEntry.OldEntry = Entry.Entry;
            NewEntry.ReadBackFromRPF = false;

            RPF6.RPF6TOC.FileEntry fileEntry = new RPF6.RPF6TOC.FileEntry()
            {
                FlagInfo = new ResourceUtils.FlagInfo()
            };
            fileEntry.FlagInfo.Flag1 = Entry.Entry.AsFile.FlagInfo.Flag1;
            fileEntry.FlagInfo.Flag2 = Entry.Entry.AsFile.FlagInfo.Flag2;
            if (Entry.Entry.AsFile.FlagInfo.IsCompressed)
            {
                fileEntry.FlagInfo.IsCompressed = true;
            }

            byte[] temp;
            if (AppGlobals.Platform == AppGlobals.PlatformEnum.Switch)
                temp = DataUtils.CompressZStandard(data);
            else
                temp = DataUtils.Compress(data, 9);

            fileEntry.FlagInfo.SetTotalSize(data.Length, 0);
            fileEntry.SizeInArchive = temp.Length;
            fileEntry.NameOffset = Entry.Entry.NameOffset;
            NewEntry.Entry = fileEntry;
            NewEntry.Entry.AsFile.KeepOffset = new long?(NewEntry.OldEntry.AsFile.GetOffset());

            RPF6.RPF6TOC.ReplaceEntry(Entry, NewEntry);
            MessageBox.Show("Successfully saved file !\nMake sure to also save the RPF to see changes", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            MainForm.HasJustEditedRegularFile = true;
            this.Close();
        }

        private void helpButton_Click(object sender, EventArgs e)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Ctrl + C to copy");
            sb.AppendLine("Ctrl + V to paste");
            sb.AppendLine("Ctrl + Z to undo");
            sb.AppendLine("Ctrl + Y to redo");
            sb.AppendLine("Ctrl + F to search");
            sb.AppendLine("Alt + Mouse Wheel to zoom");
            MessageBox.Show(sb.ToString(), "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void toolStripComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            int index = toolStripComboBox1.SelectedIndex;
            FastColoredTextBoxNS.Language language;

            switch (index)
            {
                case 1:
                    language = FastColoredTextBoxNS.Language.CSharp;
                    break;
                case 2:
                    language = FastColoredTextBoxNS.Language.VB;
                    break;
                case 3:
                    language = FastColoredTextBoxNS.Language.HTML;
                    break;
                case 4:
                    language = FastColoredTextBoxNS.Language.SQL;
                    break;
                case 5:
                    language = FastColoredTextBoxNS.Language.PHP;
                    break;
                case 6:
                    language = FastColoredTextBoxNS.Language.JS;
                    break;
                case 7:
                    language = FastColoredTextBoxNS.Language.Lua;
                    break;
                default:
                    language = FastColoredTextBoxNS.Language.XML;
                    break;
            }
            textBox.Language = language;
            string backup = textBox.Text;
            textBox.Text = "";
            textBox.Text = backup;
            textBox.Refresh();
        }

        private void textBox_ZoomChanged(object sender, EventArgs e)
        {
            zoomLabel.Text = string.Format("Zoom {0}%", textBox.Zoom);
        }

        private void textBox_TextChanged(object sender, FastColoredTextBoxNS.TextChangedEventArgs e)
        {
            charCountLabel.Text = string.Format("{0} characters, {1} lines", textBox.Text.Length, textBox.LinesCount);
        }

        private void zoomLabel_Click(object sender, EventArgs e)
        {
            textBox.Zoom = 100;
        }
    }
}
