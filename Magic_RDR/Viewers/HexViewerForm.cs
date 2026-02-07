using Be.Windows.Forms;
using Magic_RDR.RPF;
using System;
using System.Windows.Forms;
using System.Drawing;
using static Magic_RDR.RPF6.RPF6TOC;

namespace Magic_RDR.Viewers
{
    public partial class HexViewerForm : Form
    {
        public HexViewerForm(TOCSuperEntry entry)
        {
            InitializeComponent();
            Text = string.Format("MagicRDR - Simple Hex Viewer [{0}]", entry.Entry.Name);
            charCountLabel.Text = string.Format("{0} bytes", entry.Entry.AsFile.SizeInArchive);

            var file = entry.Entry.AsFile;
            RPFFile.RPFIO.Position = file.GetOffset();

            byte[] data;
            if (file.FlagInfo.IsResource)
                data = ResourceUtils.ResourceInfo.GetDataFromResourceBytes(RPFFile.RPFIO.ReadBytes(file.SizeInArchive));
            else if (file.FlagInfo.IsCompressed)
            {
                if (AppGlobals.Platform == AppGlobals.PlatformEnum.Switch)
                    data = DataUtils.DecompressZStandard(RPFFile.RPFIO.ReadBytes(file.SizeInArchive));
                else
                    data = DataUtils.DecompressDeflate(RPFFile.RPFIO.ReadBytes(file.SizeInArchive), file.FlagInfo.GetTotalSize());
            }
            else data = RPFFile.RPFIO.ReadBytes(file.SizeInArchive);

            try
            {
                var byteProvider = new DynamicByteProvider(data);
                hexBox.ByteProvider = byteProvider;
            }
            catch (Exception ex) { 
                MessageBox.Show("An error occured while reading file :\n\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
            }
            SetTheme();
        }

        [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
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
                if (control is Be.Windows.Forms.HexBox hexBox)
                {
                    hexBox.BackColor = Color.FromArgb(30, 30, 30);
                    hexBox.ForeColor = Color.White;
                    hexBox.SelectionBackColor = Color.FromArgb(100, 70, 70, 70); // Semi-transparent grey
                    hexBox.SelectionForeColor = Color.White;
                    hexBox.ShadowSelectionColor = Color.FromArgb(100, 70, 70, 70);
                    // HexBox might have specific properties for offsets etc.
                    // hexBox.LineInfoForeColor = Color.DarkGray; // If available
                }

                if (control.HasChildren)
                {
                    ApplyThemeToControls(control.Controls);
                }
            }
        }
    }
}
