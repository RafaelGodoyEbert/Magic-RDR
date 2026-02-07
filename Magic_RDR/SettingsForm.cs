using Magic_RDR.RPF;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Magic_RDR
{
    public partial class SettingsForm : Form
    {
        [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        public SettingsForm()
        {
            InitializeComponent();
            checkBoxUseLastRPF.Checked = RPF6FileNameHandler.UseLastRPF;
            checkBoxEncryptTOC.Checked = RPF6FileNameHandler.EncryptTOC;
            checkBoxEncryptData.Checked = RPF6FileNameHandler.EncryptData;
            checkBoxShowLines.Checked = RPF6FileNameHandler.ShowLines;
            checkBoxShowPlusMinus.Checked = RPF6FileNameHandler.ShowPlusMinus;
            sortOrderComboBox.Text = RPF6FileNameHandler.Sorting.ToString();
            sortColumnComboBox.Text = RPF6FileNameHandler.SortColumn.ToString();
            checkBoxUseCustomColor.Checked = RPF6FileNameHandler.UseCustomColor;
            sizeModeComboBox.Text = RPF6FileNameHandler.ImageSizeMode.ToString();
            backgroundTextureComboBox.Text = RPF6FileNameHandler.TextureBackgroundColor.ToString().Replace("Color [", "").Replace("]", "");
            checkBoxDarkMode.Checked = RPF6FileNameHandler.DarkMode;
            SetTheme();
            
            // Adjust height to show the Save button which was cut off
            this.Height += 40; 
        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            RPF6FileNameHandler.SaveSettings();
            if (MessageBox.Show("Successfully saved settings !\n\nDo you want to restart ?", "Success", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                System.Windows.Forms.Application.Restart();
            else
                Close();
        }

        private void sortOrderComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            int order = sortOrderComboBox.SelectedIndex;
            SortOrder sortOrder;

            switch (order)
            {
                case 1:
                    sortOrder = SortOrder.Descending;
                    break;
                default:
                    sortOrder = SortOrder.Ascending;
                    break;
            }
            RPF6FileNameHandler.Sorting = sortOrder;
        }

        private void sortColumnComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            int order = sortColumnComboBox.SelectedIndex;
            string sortOrder;

            switch (order)
            {
                case 1:
                    sortOrder = "Type";
                    break;
                case 2:
                    sortOrder = "Size";
                    break;
                default:
                    sortOrder = "Name";
                    break;
            }
            RPF6FileNameHandler.SortColumn = sortOrder;
        }

        private void checkBoxUseCustomColor_CheckedChanged(object sender, EventArgs e)
        {
            RPF6FileNameHandler.UseCustomColor = checkBoxUseCustomColor.Checked;
        }

        private void checkBoxUseLastRPF_CheckedChanged(object sender, EventArgs e)
        {
            RPF6FileNameHandler.UseLastRPF = checkBoxUseLastRPF.Checked;
            RPF6FileNameHandler.LastRPFPath = MainForm.CurrentRPFFileName ?? "None";
        }

        private void checkBoxEncryptTOC_CheckedChanged(object sender, EventArgs e)
        {
            RPF6FileNameHandler.EncryptTOC = checkBoxEncryptTOC.Checked;
        }

        private void checkBoxEncryptData_CheckedChanged(object sender, EventArgs e)
        {
            RPF6FileNameHandler.EncryptData = checkBoxEncryptData.Checked;
        }
        
        private void checkBoxDarkMode_CheckedChanged(object sender, EventArgs e)
        {
            RPF6FileNameHandler.DarkMode = checkBoxDarkMode.Checked;
        }

        private void checkBoxShowPlusMinus_CheckedChanged(object sender, EventArgs e)
        {
            RPF6FileNameHandler.ShowPlusMinus = checkBoxShowPlusMinus.Checked;
        }

        private void checkBoxShowLines_CheckedChanged(object sender, EventArgs e)
        {
            RPF6FileNameHandler.ShowLines = checkBoxShowLines.Checked;
        }

        private void sizeModeComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            int order = sizeModeComboBox.SelectedIndex;
            PictureBoxSizeMode mode;

            switch (order)
            {
                case 1:
                    mode = PictureBoxSizeMode.Zoom;
                    break;
                case 2:
                    mode = PictureBoxSizeMode.CenterImage;
                    break;
                case 3:
                    mode = PictureBoxSizeMode.StretchImage;
                    break;
                default:
                    mode = PictureBoxSizeMode.AutoSize;
                    break;
            }
            RPF6FileNameHandler.ImageSizeMode = mode;
        }

        private void backgroundTextureComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            int index = backgroundTextureComboBox.SelectedIndex;
            Color background;

            switch (index)
            {
                case 0:
                    background = Color.Black;
                    break;
                case 1:
                    background = Color.White;
                    break;
                case 2:
                    background = Color.Red;
                    break;
                case 3:
                    background = Color.Green;
                    break;
                case 4:
                    background = Color.Blue;
                    break;
                default:
                    background = Color.Transparent;
                    break;
            }
            RPF6FileNameHandler.TextureBackgroundColor = background;
        }

        private void SetTheme()
        {
            if (RPF6FileNameHandler.DarkMode)
            {
                this.BackColor = Color.FromArgb(45, 45, 48);
                this.ForeColor = Color.White;
                ApplyThemeToControls(this.Controls, true);
                
                // Enable Dark Title Bar
                int useImmersiveDarkMode = 1;
                DwmSetWindowAttribute(this.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useImmersiveDarkMode, sizeof(int));
            }
        }

        private void ApplyThemeToControls(Control.ControlCollection controls, bool dark)
        {
            foreach (Control control in controls)
            {
                if (control is GroupBox groupBox)
                {
                    groupBox.ForeColor = dark ? Color.White : SystemColors.ControlText;
                    // Force simple paint to avoid white borders if possible, or we need to handle Paint event.
                    // WinForms GroupBoxes are notoriously hard to style borders.
                    // We can at least color the text.
                    if (dark)
                    {
                        groupBox.Paint -= GroupBox_Paint;
                        groupBox.Paint += GroupBox_Paint;
                    }
                }
                else if (control is Button button)
                {
                    button.BackColor = dark ? Color.FromArgb(60, 60, 60) : SystemColors.ButtonFace;
                    button.ForeColor = dark ? Color.White : SystemColors.ControlText;
                    button.FlatStyle = FlatStyle.Flat;
                    button.FlatAppearance.BorderColor = Color.FromArgb(100, 100, 100);
                    button.FlatAppearance.BorderSize = 1;
                }
                else if (control is CheckBox checkBox)
                {
                    checkBox.ForeColor = dark ? Color.White : SystemColors.ControlText;
                }
                else if (control is ComboBox comboBox)
                {
                    comboBox.BackColor = dark ? Color.FromArgb(30, 30, 30) : SystemColors.Window;
                    comboBox.ForeColor = dark ? Color.White : SystemColors.WindowText;
                    comboBox.FlatStyle = FlatStyle.Flat;
                }
                else if (control is Label label)
                {
                    label.ForeColor = dark ? Color.White : SystemColors.ControlText;
                }
                else if (control is Panel panel)
                {
                    panel.BackColor = dark ? Color.FromArgb(45, 45, 48) : SystemColors.Control;
                    panel.ForeColor = dark ? Color.White : SystemColors.ControlText;
                }

                if (control.HasChildren)
                {
                    ApplyThemeToControls(control.Controls, dark);
                }
            }
        }

        private void GroupBox_Paint(object sender, PaintEventArgs e)
        {
            if (!(sender is GroupBox box)) return;
            
            // Standard GroupBox renders a lighter border. To override it, we can redraw a border.
            // But just drawing over it often clashes with the text.
            // A common trick is to draw the border, but skip the text area.
            
            Graphics g = e.Graphics;
            Color borderColor = Color.FromArgb(100, 100, 100);
            Color contentColor = Color.White; // Text Color

            // Measure text size
            Size tSize = TextRenderer.MeasureText(box.Text, box.Font);

            // Bounds for border
            Rectangle borderRect = box.ClientRectangle;
            borderRect.Y += tSize.Height / 2;
            borderRect.Height -= tSize.Height / 2;

            // Clear the area where standard border might have painted white pixels (optional, risks flickering)
            // Instead, we just draw our solid border.
            
            // Draw custom border
            ControlPaint.DrawBorder(g, borderRect, borderColor, ButtonBorderStyle.Solid);
            
            // Re-draw the background behind text to "cut" the border
            Rectangle textRect = new Rectangle(6, 0, tSize.Width, tSize.Height);
            g.FillRectangle(new SolidBrush(box.BackColor), textRect);

            // Re-draw text because we might have painted over it or to ensure it's white
            TextRenderer.DrawText(g, box.Text, box.Font, new Point(6, 0), contentColor);
        }
    }
}
