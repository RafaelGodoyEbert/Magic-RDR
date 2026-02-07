using Magic_RDR.Models;
using Magic_RDR.RPF;
using Magic_RDR.Viewers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using System.Windows.Forms;
using static Magic_RDR.RPF.RPF6FileNameHandler;
using static Magic_RDR.RPF6.RPF6TOC;

namespace Magic_RDR
{
    public partial class MainForm : Form
    {
        private Stream CurrentRPFStream;
        private Dictionary<int, Tuple<bool, bool, string, string>> SupportedRSCTypes = new Dictionary<int, Tuple<bool, bool, string, string>>();
        private ListViewItem LastHoveredItem;
        public TOCSuperEntry CurrentDirectory;
        public RPF6 CurrentRPF;
        public static bool CommandLine = false;
        public static string CurrentRPFFileName;
        public static bool HasJustEditedTexture = false, HasJustEditedRegularFile = false;

        public MainForm(string rpfPath)
        {
            if (rpfPath == string.Empty)
            {
                InitializeComponent();
                SetTheme();
                var lviSorter = new ListViewColumnSorter();
                lviSorter.SortOrder = Sorting;
                switch (SortColumn)
                {
                    case "Type":
                        lviSorter.SortColumn = 1;
                        break;
                    case "Size":
                        lviSorter.SortColumn = 2;
                        break;
                    default:
                        lviSorter.SortColumn = 0;
                        break;
                }
                listView.ListViewItemSorter = lviSorter;
                treeView.ShowPlusMinus = ShowPlusMinus;
                treeView.ShowLines = ShowLines;
                SupportedRSCTypes.Add(1, new Tuple<bool, bool, string, string>(true, false, "String Table", "Frag"));

                if (UseLastRPF && File.Exists(LastRPFPath))
                {
                    LoadRPF(LastRPFPath);
                }

				//Remove temp files
				var files = Directory.GetFiles(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "*.*", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    if ((file.Contains("mat") && file.EndsWith(".png")) || file.EndsWith(".awc") || file.EndsWith(".wav"))
                        File.Delete(file);
                }

				var exportPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Temp");
                files = Directory.GetFiles(exportPath);
                foreach (var file in files)

                {
                    string filename = Path.GetFileName(file);
                    if (File.Exists(file) && !filename.EndsWith(".exe") && !filename.EndsWith(".ini"))
                    {
                        try { File.Delete(file); } catch { }
                    }
                }

				var subDirectories = Directory.GetDirectories(exportPath);
                foreach (var subDirectory in subDirectories)
                {
                    Directory.Delete(subDirectory, true);
                }

				try
				{
					var tempDir = Path.GetTempPath();
					var tempWavFiles = Directory.GetFiles(tempDir, "*.wav", SearchOption.TopDirectoryOnly);

					foreach (var file in tempWavFiles)
					{
						try { File.Delete(file); } catch { }
					}
				}
				catch { }
			}
            else
            {
                CommandLine = true;
                LoadRPF(rpfPath);
            }
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "Open";
            openFileDialog.Filter = "RPF Files|*.rpf";

            if (openFileDialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }
            LoadRPF(openFileDialog.FileName);
        }

        public void LoadRPF(string fileLoc, bool reload = false)
        {
            if (!reload && CurrentRPF != null && MessageBox.Show("Are you sure you want to close the current RPF?\r\n\r\nAll unsaved progress will be lost.", "Close", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
            {
                return;
            }

            if (UseLastRPF)
            {
                LastRPFPath = fileLoc;
                SaveSettings();
            }

            CurrentDirectory = null;
            CurrentRPFFileName = fileLoc;
            CurrentRPFStream = File.Open(fileLoc, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);

            VerifyGameVersion();
            if (!CommandLine)
            {
                Text = string.Format("MagicRDR | {0} | {1}", AppGlobals.PlatformString, fileLoc);
                fileNameStatusLabel.Text = string.Format("Loading : {0}", CurrentRPFFileName);
            }       
            LoadRPF();
        }

        private void VerifyGameVersion()
        {
            byte[] buffer = new byte[4];
            CurrentRPFStream.Position = 8;
            CurrentRPFStream.Read(buffer, 0, buffer.Length);

            if (BitConverter.ToUInt32(buffer, 0) == 0)
                AppGlobals.SetPlatform(AppGlobals.PlatformEnum.Xbox);
            else
                AppGlobals.SetPlatform(AppGlobals.PlatformEnum.Switch);

            CurrentRPFStream.Position = 0;
        }

        public void LoadRPF()
        {
            NewWorkForm newWorkForm = new NewWorkForm(CurrentRPFStream);

            if (!newWorkForm.Done)
                newWorkForm.ShowDialog();

            if (newWorkForm.OpenRPFException != null)
            {
                fileNameStatusLabel.Text = "Made by Im Foxxyyy";
                currentDirectoryLabel.Text = "";

                if (CurrentRPF != null)
                {
                    CurrentRPF.CloseAllStreams();
                    CurrentRPF = null;
                    CurrentDirectory = null;
                }
                CurrentRPFStream.Close();
                CurrentRPFStream = null;

                MessageBox.Show(string.Format("An error has occurred:\r\n{0}\r\n\r\n{1}", newWorkForm.OpenRPFException.Message, newWorkForm.OpenRPFException.StackTrace), "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
            }
            else
            {
                if (CurrentRPF != null)
                {
                    CurrentRPF.CloseAllStreams();
                }
                CurrentRPF = newWorkForm.OpenRPF6Return;

                if (!CommandLine)
                {
                    try
                    {
                        LoadDirectory(CurrentRPF.TOC.SuperEntries[0], true);
                        treeView.Enabled = true;
                        treeView.ImageList = imageList1;
                        treeView.ImageIndex = 0;
                        treeView.ShowRootLines = true;
                        listView.Enabled = true;
                        listView.SmallImageList = imageList1;
                        splitContainer1.Enabled = true;
                        searchBox.Visible = true;
                        UpdateInfoLabels();
                    }
                    catch (Exception ex)
                    {
                        Text = "MagicRDR | Xbox 360";
                        fileNameStatusLabel.Text = "Made by Im Foxxyyy";
                        currentDirectoryLabel.Text = "";

                        if (CurrentRPF != null)
                        {
                            CurrentRPF.CloseAllStreams();
                            CurrentRPF = null;
                            CurrentDirectory = null;
                        }
                        CurrentRPFStream.Close();
                        CurrentRPFStream = null;
                        MessageBox.Show("An error has occurred while reading RPF :\n\n" + ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
                    }
                }
            }
            newWorkForm.Dispose();
        }

        private void SetListViewItemIcon(ListViewItem item)
        {
            if (!item.Text.Contains("."))
            {
                item.ImageIndex = 5;
                return;
            }
            int index = item.Text.LastIndexOf(".");
            string extension = item.Text.Substring(index, item.Text.Length - index);

            switch (extension)
            {
                case ".awc":
                    item.ImageIndex = 3;
                    break;
                case ".cutbin":
                case ".bik":
                    item.ImageIndex = 2;
                    break;
                case ".xtd":
                case ".wtd":
                case ".dds":
                case ".xsf":
                case ".wsf":
                case ".xtx":
                case ".wtx":
                case ".xedt":
                case ".wedt":
                    item.ImageIndex = 1;
                    break;
                case ".csv":
                case ".tr":
                case ".xml":
                case ".refgroup":
                case ".txt":
                    item.ImageIndex = 4;
                    break;
                case ".xvd":
                case ".wvd":
                case ".xsi":
                case ".wsi":
                    item.ImageIndex = 6;
                    break;
                case ".xtb":
                case ".wtb":
                case ".xbd":
                case ".wbd":
                    item.ImageIndex = 7;
                    break;
                default:
                    item.ImageIndex = 5;
                    break;
            }

            if (RPF6FileNameHandler.DarkMode)
            {
                item.BackColor = Color.FromArgb(30, 30, 30);
                item.ForeColor = Color.White;
            }
        }

        public void LoadDirectory(TOCSuperEntry super, bool firstTime = false)
        {
            if (super == null)
            {
                MessageBox.Show("Directory was removed or is invalid", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            CurrentDirectory = super;
            listView.Items.Clear();

            if (!super.IsDir)
                throw new Exception("Entry was not a directory");

            super.GetPathArray();
            WorkForm workForm = new WorkForm(CurrentRPF, super);
            if (!workForm.Done)
                workForm.ShowDialog();

            if (workForm.LoadDirectoryException != null)
            {
                MessageBox.Show(string.Format("An error occurred while loading directory:\r\n{0}\r\n{1}", workForm.LoadDirectoryException.Message, workForm.LoadDirectoryException.StackTrace), "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
            }
            else
            {
                var items = workForm.ItemsToAddToListView;
                for (int i = 0; i < items.Count; i++)
                {
                    SetListViewItemIcon(items[i]);
                }
                listView.BeginUpdate();
                listView.Items.AddRange(items.ToArray());

                int totalWidth = listView.Width;
                int averageColumnWidth = totalWidth / listView.Columns.Count;
                foreach (ColumnHeader column in listView.Columns)
                {
                    column.Width = averageColumnWidth;
                }
                listView.EndUpdate();

                if (firstTime)
                {
                    var entries = CurrentRPF.TOC.SuperEntries;
                    treeView.BeginUpdate();
                    treeView.Nodes.Clear();

                    for (int i = 0; i < entries.Count; i++)
                    {
                        if (!entries[i].IsDir) continue;

                        TreeNode node = new TreeNode();
                        if (entries[i].Entry.Name != "root")
                        {
                            node.Text = entries[i].Entry.Name;
                            SearchAndAdd(entries[i].Entry.GetPath(), entries[i].Entry.Name, entries[i].SuperParent.GetPath(), entries[i]);
                        }
                        else
                            treeView.Nodes.Add(entries[i].GetPath(), "root");
                    }
                    treeView.Nodes[0].Expand();
                    treeView.EndUpdate();
                }
            }
            UpdateInfoLabels();
            workForm.Dispose();
        }

        public void SearchAndAdd(string path, string name, string parent, TOCSuperEntry entry = null)
        {
            TreeNode[] list = treeView.Nodes.Find(parent, true);
            if (list.Length != 0)
            {
                if (list.Length == 1)
                {
                    list[0].Nodes.Add(path, name);
                    return;
                }

                for (int i = 0; i < list.Length; i++)
                {
                    if (list[i].Parent.Text != entry.Entry.Parent.Parent.Name)
                        continue;
                    list[i].Nodes.Add(path, name);
                }
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (CurrentRPF == null)
            {
                MessageBox.Show("You must have a valid RPF opened.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (MessageBox.Show("Are you sure you want to close the current file ?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            treeView.Nodes.Clear();
            listView.Items.Clear();
            CurrentRPF.CloseAllStreams();
            CurrentRPF = null;
            CurrentDirectory = null;

            if (CurrentRPFStream != null)
            {
                CurrentRPFStream.Close();
                CurrentRPFStream = null;
            }

            treeView.ImageList = null;
            treeView.ShowRootLines = false;
            treeView.Enabled = false;
            listView.SmallImageList = null;
            listView.Enabled = false;
            splitContainer1.Enabled = false;
            searchBox.Visible = false;
            fileNameStatusLabel.Text = "Made by Im Foxxyyy";
            fileNameStatusLabel.Image = Properties.Resources.accept;
            currentDirectoryLabel.Text = "";
            currentDirectoryLabel.Image = null;
        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (CurrentRPF != null && MessageBox.Show("Are you sure you want to close the current file?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
            {
                return;
            }

            if (CurrentRPF != null)
                CurrentRPF.CloseAllStreams();
            if (CurrentRPFStream != null)
                CurrentRPFStream.Close();

            CurrentRPF = new RPF6();
            TOCSuperEntry super = new RPF6.RPF6TOC.TOCSuperEntry();
            DirectoryEntry directoryEntry = new RPF6.RPF6TOC.DirectoryEntry();
            super.Entry = directoryEntry;
            directoryEntry.Name = "root";
            CurrentRPF.TOC.SuperEntries.Add(super);
            CurrentRPF.Header.DirectoryCount = 1;

            LoadDirectory(CurrentRPF.TOC.SuperEntries[0], true);
            UpdateInfoLabels();

            treeView.Enabled = true;
            treeView.ImageList = imageList1;
            treeView.ImageIndex = 0;
            treeView.ShowRootLines = true;
            listView.Enabled = true;
            listView.SmallImageList = imageList1;
            splitContainer1.Enabled = true;
            searchBox.Visible = true;
        }

        public void saveToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (!CommandLine)
            {
                if (CurrentRPF == null)
                {
                    MessageBox.Show("You must have a valid RPF opened.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (MessageBox.Show("Are you sure you want to save the current RPF?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                {
                    return;
                }
            }

            string fullPath = string.Empty;
            if (CurrentDirectory != null)
                fullPath = CurrentDirectory.Entry.GetPath();

            string tempFileName = Path.GetTempFileName();
            Stream xOut = File.Open(tempFileName, FileMode.Open, FileAccess.ReadWrite);
            SaveRPF(xOut);

            CurrentRPFStream?.Close();

            xOut.Close();
            File.Delete(CurrentRPFFileName);
            File.Move(tempFileName, string.Format("{0}{1}{2}", Path.GetDirectoryName(CurrentRPFFileName), Path.DirectorySeparatorChar, Path.GetFileName(CurrentRPFFileName)));

            LoadRPF(CurrentRPFFileName, CommandLine);
        }

        public void asNewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (CurrentRPF == null && !CommandLine)
            {
                MessageBox.Show("You must have a valid RPF opened.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            else if (CommandLine && Program.SaveRPFPath == string.Empty)
            {
                Console.WriteLine("Select a destination to save the .RPF");
            }

            SaveFileDialog saveFileDialog = null;
            if (Program.SaveRPFPath == string.Empty)
            {
                saveFileDialog = new SaveFileDialog();
                saveFileDialog.Title = "Save as new";
                saveFileDialog.Filter = "RPF Files|*.rpf|All Files|*.*";

                if (saveFileDialog.ShowDialog() != DialogResult.OK)
                    return;
            }

            string fullPath = string.Empty;
            if (CurrentDirectory != null)
                fullPath = CurrentDirectory.Entry.GetPath();

            Stream xOut = File.Create(saveFileDialog == null ? Program.SaveRPFPath : saveFileDialog.FileName, 262144);
            SaveRPF(xOut);
            xOut.Close();

            if (!CommandLine)
            {
                TOCSuperEntry super = fullPath == string.Empty ? null : GetEntryByPath(fullPath, CurrentRPF.TOC.SuperEntries[0]);
                if (super != null)
                    LoadDirectory(super);
                else
                    LoadDirectory(CurrentRPF.TOC.SuperEntries[0]);
            }
        }

        private void SaveRPF(Stream xOut)
        {
            CurrentRPF.Header.Encrypted = RPF6FileNameHandler.EncryptTOC;
            NewWorkForm newWorkForm = new NewWorkForm(xOut, CurrentRPF);

            if (!newWorkForm.Done)
                newWorkForm.ShowDialog();

            if (newWorkForm.SaveRPFException != null)
                MessageBox.Show(string.Format("An error occurred while trying to save RPF:\r\n{0}\r\n\r\n{1}", newWorkForm.SaveRPFException.Message, newWorkForm.SaveRPFException.StackTrace), "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
            
            if (!CommandLine)
                MessageBox.Show("Successfully saved !", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

            newWorkForm.Dispose();
        }

        private void fileOptions_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            accessDirectoryMenuItem.Enabled = false;
            if (CurrentRPF == null)
            {
                importButton.Enabled = false;
                importDirectoryButton.Enabled = false;
                createDirectoryButton.Enabled = false;
                copyPathButton.Enabled = false;
                extractFileButton.Enabled = false;
                viewFilePropertiesButton.Enabled = false;
                extractResourceButton.Enabled = false;
                replaceFileButton.Enabled = false;
                removeFileButton.Enabled = false;
                removeDirectoryButton.Enabled = false;
                extractTheseFilesButton.Enabled = false;
                viewHexButton.Enabled = false;
                return;
            }
            importButton.Enabled = createDirectoryButton.Enabled = importDirectoryButton.Enabled = CurrentDirectory.Write && !CurrentDirectory.DoesHaveParentMarkedNotToBeWritten;
            extractTheseFilesButton.Enabled = CurrentDirectory.Children.Count > 0 && CurrentDirectory.Write && !CurrentDirectory.DoesHaveParentMarkedNotToBeWritten;
            removeDirectoryButton.Enabled = CurrentDirectory != null;

            if (listView.SelectedItems.Count > 0)
            {
                ListViewItem selectedItem = listView.SelectedItems[0];
                if (selectedItem.Tag is TOCSuperEntry tag)
                {
                    copyPathButton.Enabled = true;
                    extractFileButton.Enabled = tag.Write && !tag.DoesHaveParentMarkedNotToBeWritten;
                    viewFilePropertiesButton.Enabled = !tag.IsDir && tag.Write && !tag.DoesHaveParentMarkedNotToBeWritten;
                    extractResourceButton.Enabled = !tag.IsDir && !tag.DoesHaveParentMarkedNotToBeWritten && tag.Entry.AsFile.FlagInfo.IsResource;
                    viewHexButton.Enabled = !tag.IsDir && !tag.DoesHaveParentMarkedNotToBeWritten;

                    if (!tag.Write || tag.DoesHaveParentMarkedNotToBeWritten)
                    {
                        replaceFileButton.Enabled = false;
                        replaceFileButton.Text = tag.IsDir ? "Replace Directory" : "Replace File";
                    }
                    if (!tag.IsDir && tag.CustomDataStream != null && tag.IsFileFromRPF)
                    {
                        replaceFileButton.Text = "Un-Replace";
                        replaceFileButton.Enabled = true;
                    }
                    else if (!tag.IsDir && tag.CustomDataStream != null && !tag.IsFileFromRPF)
                    {
                        replaceFileButton.Text = "Replace File";
                        replaceFileButton.Enabled = false;
                    }
                    else
                    {
                        replaceFileButton.Text = tag.IsDir ? "Replace Directory" : "Replace File";
                        replaceFileButton.Enabled = !tag.IsDir && (tag.Write && !tag.DoesHaveParentMarkedNotToBeWritten);
                    }

                    if (!tag.IsDir)
                    {
                        removeFileButton.Text = tag.Write ? "Remove File" : "Un-Remove File";
                        extractFileButton.Text = "Extract File";

                        if (Searched)
                            accessDirectoryMenuItem.Enabled = true;
                    }

                    if (!tag.DoesHaveParentMarkedNotToBeWritten)
                        removeFileButton.Enabled = true;
                    else
                        removeFileButton.Enabled = false;
                }
                else
                {
                    copyPathButton.Enabled = false;
                    removeFileButton.Enabled = false;
                    extractFileButton.Enabled = false;
                    viewFilePropertiesButton.Enabled = false;
                    replaceFileButton.Enabled = false;
                    extractResourceButton.Enabled = false;
                    viewHexButton.Enabled = false;
                }
            }
            else
            {
                replaceFileButton.Text = "Replace File";
                removeFileButton.Text = "Remove File";
                extractFileButton.Text = "Extract File";
                replaceFileButton.Enabled = false;
                removeFileButton.Enabled = false;
                extractFileButton.Enabled = false;
                viewFilePropertiesButton.Enabled = false;
                extractResourceButton.Enabled = false;
                copyPathButton.Enabled = false;
                viewHexButton.Enabled = false;
            }

            if (CurrentDirectory != null)
            {
                removeDirectoryButton.Text = CurrentDirectory.Write ? "Remove Directory" : "Un-Remove Directory";
            }

            if (!CurrentDirectory.DoesHaveParentMarkedNotToBeWritten)
                removeDirectoryButton.Enabled = true;
            else
                removeDirectoryButton.Enabled = false;
        }

        private void copyPathButton_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem selectedItem in listView.SelectedItems)
            {
                if (selectedItem.Tag is RPF6.RPF6TOC.TOCSuperEntry tag)
                {
                    try
                    {
                        Clipboard.SetText(tag.GetPath());
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(string.Format("Could not copy path:\r\n{0}", ex.Message), "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
        }

        private void extractResourceButton_Click(object sender, EventArgs e)
        {
            if (listView.SelectedItems.Count <= 0)
            {
                return;
            }

            ListViewItem selectedItem = listView.SelectedItems[0];
            if (selectedItem.Tag is RPF6.RPF6TOC.TOCSuperEntry tag)
            {
                MemoryStream memoryStream = new MemoryStream();
                CurrentRPF.TOC.ExtractFile(tag, memoryStream);

                if (ResourceUtils.ResourceInfo.IsResourceStream(memoryStream))
                {
                    byte[] dataFromStream = ResourceUtils.ResourceInfo.GetDataFromStream(memoryStream);

                    SaveFileDialog saveFileDialog = new SaveFileDialog();
                    saveFileDialog.Title = "Export";
                    saveFileDialog.DefaultExt = Path.GetExtension(tag.Entry.Name);
                    saveFileDialog.Filter = saveFileDialog.DefaultExt == "" ? "" : saveFileDialog.DefaultExt.ToUpper() + " Files (*." + saveFileDialog.DefaultExt + ")|*." + saveFileDialog.DefaultExt;
                    saveFileDialog.FileName = string.Format("{0}_unpacked", tag.Entry.Name);

                    if (saveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        Stream stream = File.Create(saveFileDialog.FileName);
                        stream.Write(dataFromStream, 0, dataFromStream.Length);
                        stream.Flush();
                        stream.Close();
                        SystemSounds.Asterisk.Play();
                        MessageBox.Show("Successfully saved file !", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                else
                {
                    MessageBox.Show(string.Format("{0} was not a valid resource file.", tag.Entry.Name), "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
                }
            }
        }

        private void extractFileButton_Click(object sender, EventArgs e)
        {
            if (listView.SelectedItems.Count <= 0)
            {
                return;
            }

            listView.BeginUpdate();
            ListViewItem selectedItem = listView.SelectedItems[0];

            if (selectedItem.Tag is RPF6.RPF6TOC.TOCSuperEntry tag)
            {
                if (!tag.IsDir)
                {
                    SaveFileDialog saveFileDialog = new SaveFileDialog();
                    saveFileDialog.Title = string.Format("Export {0}", tag.Entry.Name);
                    saveFileDialog.DefaultExt = Path.GetExtension(tag.Entry.Name);
                    saveFileDialog.Filter = saveFileDialog.DefaultExt == "" ? "" : saveFileDialog.DefaultExt.ToUpper() + " Files (*." + saveFileDialog.DefaultExt + ")|*." + saveFileDialog.DefaultExt;
                    saveFileDialog.FileName = tag.Entry.Name;

                    if (saveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        try
                        {
                            Stream xOut = File.Create(saveFileDialog.FileName);
                            CurrentRPF.TOC.ExtractFile(tag, xOut);
                            xOut.Close();
                            SystemSounds.Asterisk.Play();
                            MessageBox.Show("Successfully saved file !", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(string.Format("Could not export file.\r\nError:\r\n\r\n{0}\r\n\r\nStack Trace: {1}", ex.Message, ex.StackTrace), "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
                        }
                    }
                }
                else
                {
                    FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
                    if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                    {
                        WorkForm workForm = new WorkForm(CurrentRPF, tag, folderBrowserDialog.SelectedPath);
                        if (!workForm.Done)
                        {
                            workForm.ShowDialog();
                        }
                        if (workForm.ExportFilesException != null)
                        {
                            MessageBox.Show(string.Format("An error occurred while exporting directory:\r\n{0}\r\n\r\n{1}", workForm.ExportFilesException.Message, workForm.ExportFilesException.StackTrace), "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
                        }
                        workForm.Dispose();
                    }
                }
            }
            listView.EndUpdate();
        }

        private void createDirectoryButton_Click(object sender, EventArgs e)
        {
            NameDirectoryForm nameDirectoryForm = new NameDirectoryForm();
            nameDirectoryForm.ShowDialog();

            if (string.IsNullOrEmpty(nameDirectoryForm.NewDirectoryName))
            {
                return;
            }

            string inputPath = nameDirectoryForm.NewDirectoryName.Replace("\\", "/");
            string[] directories = inputPath.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            TOCSuperEntry currentContext = CurrentDirectory;

            foreach (string dirName in directories)
            {
                uint dirHash = DataUtils.GetHash(dirName);
                TOCSuperEntry existingChild = currentContext.AllChildren.FirstOrDefault(x => (int)x.Entry.NameOffset == (int)dirHash && !x.Entry.IsFile);

                if (existingChild == null)
                {
                    // Create new directory
                    AddName(dirName);
                    SaveNames();

                    TOCSuperEntry child = new TOCSuperEntry
                    {
                        SuperParent = currentContext
                    };
                    
                    DirectoryEntry directoryEntry = new DirectoryEntry
                    {
                        Name = dirName,
                        Parent = currentContext.Entry.AsDirectory
                    };
                    
                    child.Entry = directoryEntry;
                    CurrentRPF.TOC.SuperEntries.Add(child);
                    CurrentRPF.Header.DirectoryCount++; 
                    currentContext.AddChild(child);

                    // Update TreeView
                    string newPath = currentContext.Entry.Name == "root" && currentContext.Entry.Parent == null
                        ? "root/" + dirName // Or just confirm how root path is handled. Usually root is key "root"
                        : currentContext.Entry.GetPath() + "/" + dirName;
                    
                    // Fix path consistency if generic GetPath() behaves differently
                    if (currentContext.Entry.Name == "root" && currentContext.Entry.Parent == null)
                    {
                         // Special handling if GetPath() returns empty for root or similar
                         // Assuming GetPath() works, but let's stick to the previous logic style
                         SearchAndAdd(currentContext.Entry.GetPath() + "/" + dirName, dirName, "root", child);
                    }
                    else
                    {
                        SearchAndAdd(currentContext.Entry.GetPath() + "/" + dirName, dirName, currentContext.Entry.GetPath(), child);
                    }

                    currentContext = child;
                }
                else
                {
                    // Directory already exists, navigate into it
                    currentContext = existingChild;
                }
            }

            LoadDirectory(CurrentDirectory);
            MessageBox.Show("Directory structure created successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void viewFilePropertiesButton_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem selectedItem in listView.SelectedItems)
            {
                if (selectedItem.Tag is TOCSuperEntry entry)
                {
                    PropertiesForm propertiesForm = new PropertiesForm(entry);
                    propertiesForm.ShowDialog();

                    if (propertiesForm.ModifiedProperties)
                    {
                        LoadDirectory(CurrentDirectory);
                    }
                }
            }
        }

        private void importButton_Click(object sender, EventArgs e) => DoFileImportReplace();

        public void DoFileImportReplace(bool cursor = false, string importFileLoc = null, string replaceFileLoc = null, bool replaceTab = false, bool skipChecks = false)
        {
            ListViewItem blvi = null;
            TOCSuperEntry selectedItemSuper = CommandLine ? null : GetSelectedItemSuper(cursor, out blvi);
            TOCSuperEntry selectedReplaceFile = null;
            TOCSuperEntry selectedFile;

            if ((replaceFileLoc != null && CurrentDirectory.DoesHaveEntry(Path.GetFileName(replaceFileLoc))))
            {
                selectedReplaceFile = CurrentDirectory.GetChild(Path.GetFileName(replaceFileLoc));
                selectedFile = selectedReplaceFile;
                replaceTab = true;
            }
            else selectedFile = selectedItemSuper;

            NewImportReplaceForm importReplaceForm = new NewImportReplaceForm(replaceTab, CurrentDirectory, selectedFile, selectedReplaceFile, importFileLoc, replaceFileLoc, skipChecks);
            if (!importReplaceForm.IsDisposed)
                importReplaceForm.ShowDialog();

            if (importReplaceForm.TOCResult == null)
                return;
            if (importReplaceForm.Mode == NewImportReplaceForm.FileMode.Import)
                CurrentDirectory.AddChild(importReplaceForm.TOCResult);

            else if (importReplaceForm.TOCResult != null)
            {
                selectedFile.CustomDataStream?.Close();
                selectedFile.CustomDataStream = importReplaceForm.TOCResult.CustomDataStream;
                selectedFile.Entry = importReplaceForm.TOCResult.Entry;
                selectedFile.OldEntry = importReplaceForm.TOCResult.OldEntry;
                selectedFile.ReadBackFromRPF = false;

                if (blvi != null)
                {
                    blvi.Tag = selectedFile;
                }
            }

            if (!CommandLine)
                fileNameStatusLabel.Text = "Updating...";

            if (replaceFileLoc == null)
                CurrentRPF.Header.FileCount++; //Have to make sure this doesn't interfer when saving

            if (!skipChecks)
                LoadDirectory(CurrentDirectory); //Don't reload directory everytime we import/replace a file
        }

        private TOCSuperEntry GetSelectedItemSuper(bool mouseCoords, out ListViewItem blvi)
        {
            TOCSuperEntry tocSuperEntry = null;
            ListViewItem betterListViewItem = null;

            if (!mouseCoords)
            {
                if ((uint)listView.SelectedItems.Count > 0U)
                {
                    betterListViewItem = listView.SelectedItems[0];
                }
            }
            else
            {
                Point location = listView.PointToClient(Cursor.Position);
                betterListViewItem = listView.GetItemAt(location.X, location.Y);
            }

            blvi = betterListViewItem;
            if (betterListViewItem != null)
            {
                object tag = betterListViewItem.Tag;
                if (tag is object[] v)
                {
                    tocSuperEntry = (TOCSuperEntry)v[0];
                    if (tocSuperEntry.IsDir)
                    {
                        tocSuperEntry = null;
                    }
                }
                else if (tag is TOCSuperEntry entry)
                {
                    tocSuperEntry = entry;
                }
            }
            return tocSuperEntry;
        }

        private void replaceFileButton_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem selectedItem in listView.SelectedItems)
            {
                if (selectedItem.Tag is TOCSuperEntry tag)
                {
                    if (!tag.IsDir && tag.CustomDataStream != null && tag.IsFileFromRPF)
                    {
                        tag.CustomDataStream = null;
                        tag.ReadBackFromRPF = true;
                        tag.Entry = tag.OldEntry;
                    }
                    else DoFileImportReplace(replaceTab: true);
                }
            }
            fileNameStatusLabel.Text = "Updating...";
            LoadDirectory(CurrentDirectory);
        }

        private void removeFileButton_Click(object sender, EventArgs e)
        {
            int selectedItemsCount = listView.SelectedItems.Count;
            ListViewItem[] selectedItems = new ListViewItem[selectedItemsCount];

            if (selectedItemsCount <= 0)
                return;
            else if (selectedItemsCount == 1)
                selectedItems[0] = listView.SelectedItems[0];
            else
                listView.SelectedItems.CopyTo(selectedItems, 0);

            listView.BeginUpdate();
            foreach (var items in selectedItems)
            {
                if (items.Tag is TOCSuperEntry tag)
                {
                    bool markedNotToBeWritten = tag.DoesHaveParentMarkedNotToBeWritten;

                    if (!markedNotToBeWritten && tag.Write)
                        tag.MarkAsNotToBeWritten(false, CurrentRPF.TOC.SuperEntries);
                    else if (tag.Write && !tag.DoesHaveParentMarkedNotToBeWritten)
                        tag.MarkAsNotToBeWritten(false, CurrentRPF.TOC.SuperEntries);
                    else if (!tag.Write && !markedNotToBeWritten)
                        tag.MarkAsNotToBeWritten(true, CurrentRPF.TOC.SuperEntries);

                    items.BackColor = tag.Write ? (tag.CustomDataStream == null ? Color.Empty : Color.FromArgb(150, 50, byte.MaxValue, 50)) : Color.FromArgb(150, byte.MaxValue, 50, 50);
                }
            }
            listView.EndUpdate();
        }

        bool Searched = false;
        private void searchBox_Enter(object sender, EventArgs e)
        {
            if (searchBox.Text == "Search files...")
            {
                searchBox.Text = "";
                searchBox.ForeColor = Color.Black;
            }
        }

        private void searchBox_Leave(object sender, EventArgs e)
        {
            if (searchBox.Text == "")
            {
                searchBox.Text = "Search files...";
                searchBox.ForeColor = Color.Silver;

                if (Searched)
                {
                    Searched = false;
                    LoadDirectory(CurrentDirectory);
                }
            }
        }

        private void searchBox_TextChanged(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(searchBox.Text) || CurrentRPF == null)
                return;

            if (searchBox.Text.Length <= 1)
                return;

            List<ListViewItem> list = new List<ListViewItem>();
            var entries = CurrentRPF.TOC.SuperEntries;

            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].IsDir) continue;
                if (!entries[i].Entry.Name.Contains(searchBox.Text)) continue;

                ListViewItem listViewItem = new ListViewItem();
                listViewItem.Text = entries[i].Entry.Name;
                listViewItem.Tag = entries[i];
                listViewItem.SubItems.Add(WorkForm.GetFileEntryInfo(entries[i].Entry.AsFile));
                listViewItem.SubItems.Add(entries[i].Entry.AsFile.SizeInArchive.ToString());
                SetListViewItemIcon(listViewItem);
                list.Add(listViewItem);
            }
            listView.BeginUpdate();
            listView.Items.Clear();
            listView.Items.AddRange(list.ToArray());
            int totalWidth = listView.Width;
            int averageColumnWidth = totalWidth / listView.Columns.Count;
            foreach (ColumnHeader column in listView.Columns)
            {
                column.Width = averageColumnWidth;
            }
            listView.EndUpdate();
            UpdateInfoLabels();
            Searched = true;
        }

        private void extractTheseFilesButton_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            if (folderBrowserDialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            WorkForm workForm = new WorkForm(CurrentRPF, CurrentDirectory, folderBrowserDialog.SelectedPath);
            if (!workForm.Done)
            {
                workForm.ShowDialog();
                MessageBox.Show(string.Format("Successfully exported directory and {0} files", CurrentDirectory.Children.Count), "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            if (workForm.ExportFilesException != null)
            {
                MessageBox.Show(string.Format("An error occurred while exporting directory:\r\n{0}\r\n\r\n{1}", workForm.ExportFilesException.Message, workForm.ExportFilesException.StackTrace), "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
            }
            workForm.Dispose();
        }

        private void accessDirectoryMenuItem_Click(object sender, EventArgs e)
        {
            var item = listView.SelectedItems[0];
            if (item == null)
            {
                return;
            }

            if (item.Tag is TOCSuperEntry tag)
            {
                TOCSuperEntry entry = GetEntryByPath(tag.GetPath(), CurrentRPF.TOC.SuperEntries[0]).SuperParent;
                LoadDirectory(entry);

                TreeNode[] list = treeView.Nodes.Find(entry.Entry.GetPath(), true);
                if (list.Length != 0)
                {
                    if (list.Length == 1)
                    {
                        list[0].EnsureVisible();
                        return;
                    }

                    for (int i = 0; i < list.Length; i++)
                    {
                        if (list[i].Parent.Text != entry.Entry.Parent.Name)
                            continue;
                        list[i].EnsureVisible();
                    }
                }
            }
        }

        private void viewHexButton_Click(object sender, EventArgs e)
        {
            var item = listView.SelectedItems[0];
            if (item == null)
            {
                return;
            }

            if (item.Tag is TOCSuperEntry tag)
            {
                HexViewerForm viewer = new HexViewerForm(tag);
                viewer.Show();
            }
        }

        private void UpdateInfoLabels()
        {
            fileNameStatusLabel.Text = string.Format("File Count: {0} | Directory Count: {1}", CurrentRPF.Header.FileCount, CurrentRPF.Header.DirectoryCount);
            currentDirectoryLabel.Text = string.Format("Directory: {0} ({1} {2})", CurrentDirectory == null ? "root" : CurrentDirectory.Entry.Name, listView.Items.Count, listView.Items.Count <= 1 ? "file" : "files");
            fileNameStatusLabel.Image = Properties.Resources.application_form;
            currentDirectoryLabel.Image = Properties.Resources.application_form;
        }

        private void MainForm_DragDrop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                return;
            }

            string[] data = (string[])e.Data.GetData(DataFormats.FileDrop);
            int num1 = ((IEnumerable<string>)data).Count((x => Path.GetExtension(x) == ".rpf"));
            if (num1 > 1)
            {
                MessageBox.Show("You cannot open multiple RPFs at the same time !", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else if (num1 == 1)
            {
                if (data.Length > 1)
                {
                    MessageBox.Show("You cannot open an RPF and other files !", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else LoadRPF(data[0]);
            }
            else if (CurrentRPF != null)
            {
                GetSelectedItemSuper(true, out ListViewItem blvi);
                for (int index = 0; index < data.Length; ++index)
                {
                    DoFileImportReplace(data.Length <= 1, data[index], data[index]);
                }
            }
        }

        private void MainForm_DragEnter(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                return;
            }

            string[] data = (string[])e.Data.GetData(DataFormats.FileDrop);
            int num = ((IEnumerable<string>)data).Count((x => Path.GetExtension(x) == ".rpf"));

            if (num > 1)
                e.Effect = DragDropEffects.None;
            else if (num == 1)
                e.Effect = data.Length <= 1 ? DragDropEffects.Link : DragDropEffects.None;
            else if (CurrentRPF != null)
                e.Effect = DragDropEffects.Copy;
        }

        private void creditsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            StringBuilder message = new StringBuilder();
            message.AppendLine("Made by Mars (Im Foxxyyy)");
            message.AppendLine("Thanks to XBLToothPick/revelations/Shvab/OAleex\n");
            message.AppendLine("GitHub : https://github.com/Foxxyyy");
            message.AppendLine("Discord : #imfoxxyyy");
            MessageBox.Show(message.ToString(), "Credits", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void hashStripMenuItem_Click(object sender, EventArgs e)
        {
            HashGeneratorForm generator = new HashGeneratorForm();
            generator.Show();
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SettingsForm settings = new SettingsForm();
            settings.ShowDialog();
        }

        private void listView_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            ListViewItem item = listView.GetItemAt(e.X, e.Y);
            bool nSwitch = AppGlobals.Platform == AppGlobals.PlatformEnum.Switch;

            if (item == null)
            {
                return;
            }

            if (item.Tag is TOCSuperEntry tag)
            {
                if (tag.IsDir) //How is this possible lmao
                {
                    return;
                }

                if (tag.CustomDataStream != null)
                {
                    MessageBox.Show("You have to save the RPF before viewing this file", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                FileEntry file = tag.Entry.AsFile;
                byte rscType = file.ResourceType;

                if (rscType == 133 && (tag.Entry.Name.EndsWith(".xvd") || tag.Entry.Name.EndsWith(".wvd")))
                {
                    fileNameStatusLabel.Text = string.Format("Loading {0}... This operation can take time.", tag.Entry.Name);
                    System.Windows.Forms.Application.DoEvents();

                    VolumeViewerForm modelViewer = new VolumeViewerForm(tag);
                    modelViewer.Show();

                    UpdateInfoLabels();
                    LoadDirectory(tag.SuperParent, false); //In case we just edited this file, reload its directory to see changes
                }
                else if (rscType == 138 || tag.Entry.Name.EndsWith(".strtbl") || (rscType == 1 && (tag.Entry.Name.EndsWith("st") || tag.Entry.Name.EndsWith("xfd") || tag.Entry.Name.EndsWith("xft"))))
                {
                    if (tag.Entry.Name.EndsWith(".xst") || tag.Entry.Name.EndsWith(".sst") || tag.Entry.Name.EndsWith(".strtbl"))
                    {
                        StringTableViewerForm viewer = new StringTableViewerForm(tag, CurrentRPF);
                        viewer.Show();
                        return;
                    }
                    FragViewerForm modelViewer = new FragViewerForm(tag);
                    modelViewer.Show();
                    LoadDirectory(tag.SuperParent, false); //In case we just edited this file, reload its directory to see changes
                }
                else if (file.FlagInfo.IsResource && rscType == 2 || tag.Entry.Name.EndsWith(".xsc"))
                {
                    fileNameStatusLabel.Text = string.Format("Loading {0}... This operation can take time.", tag.Entry.Name);
                    System.Windows.Forms.Application.DoEvents();

                    ScriptViewerForm scriptViewer = new ScriptViewerForm(tag);
                    if (!scriptViewer.IsDisposed)
                    {
                        scriptViewer.Show();
                    }
                    UpdateInfoLabels();
                }
                else if (rscType == 134 && tag.Entry.Name.EndsWith(".xsi") || tag.Entry.Name.EndsWith(".wsi"))
                {
                    if (nSwitch)
                    {
                        MessageBox.Show("You can't view this file yet, sorry.\n\nWorking on it !", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    SectorViewerForm scriptViewer = new SectorViewerForm(tag);
                    scriptViewer.Show();
                }
                else if (tag.Entry.Name.EndsWith(".xbd") /*|| tag.Entry.Name.EndsWith(".xtb")*/)
                {
                    if (nSwitch)
                    {
                        MessageBox.Show("You can't view this file yet, sorry.\n\nWorking on it !", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    BoundViewerForm collisionViewer = new BoundViewerForm(tag);
                    collisionViewer.Show();
                }
                else if (tag.Entry.Name.EndsWith(".fxc") || tag.Entry.Name.EndsWith(".nvn"))
                {
                    ShaderViewerForm shaderViewer = new ShaderViewerForm(tag);
                    if (!shaderViewer.IsDisposed)
                    {
                        shaderViewer.Show();
                    }
                }
                else if (tag.Entry.Name.EndsWith(".awc"))
                {
                    fileNameStatusLabel.Text = string.Format("Loading {0}... This operation can take time.", tag.Entry.Name);
                    System.Windows.Forms.Application.DoEvents();

                    AudioViewerForm audioViewer = new AudioViewerForm(tag);
                    if (!audioViewer.IsDisposed)
                    {
                        audioViewer.Show();
                    }
                    UpdateInfoLabels();
                }
                else if (rscType == 10 || IsTextureFile(tag.Entry.Name))
                {
                    TextureViewerForm textureViewer = new TextureViewerForm(tag);
                    textureViewer.ShowDialog();

                    if (HasJustEditedTexture)
                    {
                        LoadDirectory(tag.SuperParent, false); //Reload the directory to see changes
                        HasJustEditedTexture = false;
                    }
                }
                else if (!file.FlagInfo.IsResource && ResourceUtils.IsTextFile(tag.Entry.Name))
                {
                    RPFFile.RPFIO.Position = file.GetOffset();

                    byte[] data = null;
                    if (file.FlagInfo.IsCompressed)
                    {
                        if (nSwitch)
                            data = DataUtils.DecompressZStandard(RPFFile.RPFIO.ReadBytes(file.SizeInArchive));
                        else
                            data = DataUtils.DecompressDeflate(RPFFile.RPFIO.ReadBytes(file.SizeInArchive), file.FlagInfo.GetTotalSize());
                    }
                    else data = RPFFile.RPFIO.ReadBytes(file.SizeInArchive);

                    if (data != null)
                    {
                        TextViewerForm textViewer = new TextViewerForm(tag, data);
                        textViewer.ShowDialog();
                    }

                    if (HasJustEditedRegularFile)
                    {
                        LoadDirectory(tag.SuperParent, false); //Reload the directory to see changes
                        HasJustEditedRegularFile = false;
                    }
                }
            }
        }

        private bool IsTextureFile(string entryName)
        {
            if (!entryName.Contains("."))
                return false;

            string extension = entryName.Substring(entryName.LastIndexOf("."));
            switch (extension)
            {
                case ".dds":
                case ".xsf":
                case ".xtd":
                case ".xtx":
                case ".wsf":
                case ".wtd":
                case ".wtx":
                    return true;
                default:
                    return false;
            }
        }

        private void importDirectoryButton_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.Description = "Select a folder to import";
            dialog.ShowNewFolderButton = false;

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                string[] files = Directory.GetFiles(dialog.SelectedPath);

                if (files.Length <= 0)
                {
                    MessageBox.Show("There's nothing to import in that folder", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                else if (MessageBox.Show(string.Format("Found {0} file(s)\nAre you sure you want to import/replace all of them?", files.Length), "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                {
                    return;
                }

                foreach (string file in files)
                {
                    string test = Path.GetFileName(file);
                    if (CurrentDirectory.DoesHaveEntry(Path.GetFileName(file)))
                        DoFileImportReplace(replaceFileLoc: file, skipChecks: true);
                    else
                        DoFileImportReplace(importFileLoc: file, skipChecks: true);

                    fileNameStatusLabel.Text = string.Format("Importing {0}... ({1}/{2})", file, Array.IndexOf(files, file), files.Length);
                    fileNameStatusLabel.Invalidate();
                    System.Windows.Forms.Application.DoEvents();
                }
                UpdateInfoLabels();
                LoadDirectory(CurrentDirectory);
                MessageBox.Show(string.Format("Done ! {0} file(s) imported", files.Length), "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void removeDirectoryButton_Click(object sender, EventArgs e)
        {
            if (CurrentDirectory == null)
            {
                return;
            }

            bool markedNotToBeWritten = CurrentDirectory.DoesHaveParentMarkedNotToBeWritten;
            if (!markedNotToBeWritten && CurrentDirectory.Write)
                CurrentDirectory.MarkAsNotToBeWritten(false, CurrentRPF.TOC.SuperEntries);
            else if (CurrentDirectory.Write && !CurrentDirectory.DoesHaveParentMarkedNotToBeWritten)
                CurrentDirectory.MarkAsNotToBeWritten(false, CurrentRPF.TOC.SuperEntries);
            else if (!CurrentDirectory.Write && !markedNotToBeWritten)
                CurrentDirectory.MarkAsNotToBeWritten(true, CurrentRPF.TOC.SuperEntries);

            treeView.SelectedNode.BackColor = CurrentDirectory.Write ? (CurrentDirectory.CustomDataStream == null ? Color.Empty : Color.FromArgb(150, 50, byte.MaxValue, 50)) : Color.FromArgb(150, byte.MaxValue, 50, 50);
            List<TreeNode> nodes = new List<TreeNode>();
            nodes.AddRange(GetAllNodes(treeView.SelectedNode));

            foreach (TreeNode node in nodes)
            {
                node.BackColor = CurrentDirectory.Write ? (CurrentDirectory.CustomDataStream == null ? Color.Empty : Color.FromArgb(150, 50, byte.MaxValue, 50)) : Color.FromArgb(150, byte.MaxValue, 50, 50);
            }
            LoadDirectory(CurrentDirectory.SuperParent, true);
        }

        public static List<TreeNode> GetAllNodes(TreeNode node)
        {
            List<TreeNode> nodes = new List<TreeNode>();
            nodes.Add(node);
            foreach (TreeNode child in node.Nodes)
            {
                nodes.AddRange(GetAllNodes(child));
            }
            return nodes;
        }

        public static bool StaticModelShouldBeLoaded(string modelName)
        {
            if (modelName.Contains("_vlow") ||
                modelName.Contains("_med") ||
                modelName.Contains("props") ||
                modelName.EndsWith(".xsi") ||
                modelName.EndsWith(".xbd") ||
                modelName.Contains("ultralowlod") ||
                modelName == "armadillo01x.xvd")
                return false;
            return true;
        }

        public static List<TOCSuperEntry> GetAllStaticModelEntries(TOCSuperEntry entry)
        {
            List<TOCSuperEntry> entries = new List<TOCSuperEntry>();
            foreach (TOCSuperEntry child in entry.AllChildren)
            {
                if (!child.IsDir)
                {
                    if (StaticModelShouldBeLoaded(child.Entry.Name))
                        entries.Add(child);
                }
                else entries.AddRange(GetAllStaticModelEntries(child));
            }
            return entries;
        }

        private void listView_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.A)
            {
                foreach (ListViewItem item in this.listView.Items)
                {
                    item.Selected = true;
                }
            }
            else if (e.KeyCode == Keys.Delete)
            {
                removeFileButton_Click(null, null);
            }
        }

        private void treeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (treeView.SelectedNode == null)
                return;

            if (e.Action != TreeViewAction.Unknown)
            {
                if (e.Node.IsExpanded)
                    e.Node.Collapse();
                else
                    e.Node.Expand();
                string node = treeView.SelectedNode.FullPath;

                if (string.IsNullOrEmpty(node))
                    return;

                LoadDirectory(GetEntryByPath(node.Replace("\\", "/"), CurrentRPF.TOC.SuperEntries[0]));
            }
        }

        private void treeView_MouseUp(object sender, MouseEventArgs e)
        {
            var node = treeView.GetNodeAt(e.Location);
            var hitTest = treeView.HitTest(e.Location);

            if (hitTest.Location == TreeViewHitTestLocations.Indent)
            {
                if (node.IsExpanded)
                    node.Collapse();
                else
                    node.Expand();
            }
        }

        private void listView_MouseMove(object sender, MouseEventArgs e)
        {
            ListViewItem item = listView.GetItemAt(e.X, e.Y);
            if (item != null)
            {
                if (item != LastHoveredItem)
                {
                    //Mouse is over a different item, revert the previous item's appearance.
                    RestoreItemAppearance(LastHoveredItem);

                    //Change the appearance of the current item.
                    ChangeItemAppearance(item);

                    LastHoveredItem = item;
                }
            }
            else
            {
                //Mouse is not over any item, revert the previous item's appearance.
                RestoreItemAppearance(LastHoveredItem);
                LastHoveredItem = null;
            }
        }

        private void ChangeItemAppearance(ListViewItem item)
        {
            if (item.BackColor == Color.FromArgb(255, 50, byte.MaxValue, 50) || item.BackColor == Color.FromArgb(150, byte.MaxValue, 50, 50))
            {
                return;
            }

            //Change the appearance of the item when the mouse hovers over it.
            item.BackColor = Color.LightBlue;
            item.ForeColor = Color.Black;
        }

        private void RestoreItemAppearance(ListViewItem item)
        {
            if (item != null)
            {
                if (item.BackColor == Color.FromArgb(255, 50, byte.MaxValue, 50) || item.BackColor == Color.FromArgb(150, byte.MaxValue, 50, 50))
                {
                    return;
                }

                //Restore the original appearance of the item.
                if (RPF6FileNameHandler.DarkMode)
                {
                    item.BackColor = Color.FromArgb(30, 30, 30);
                    item.ForeColor = Color.White;
                }
                else
                {
                    item.BackColor = SystemColors.Control;
                    item.ForeColor = Color.Black;
                }
            }
        }

        private void listView_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            //Toggle the sorting order if the same column is clicked again.
            if (e.Column == ((ListViewColumnSorter)listView.ListViewItemSorter).SortColumn)
            {
                if (((ListViewColumnSorter)listView.ListViewItemSorter).SortOrder == SortOrder.Ascending)
                    ((ListViewColumnSorter)listView.ListViewItemSorter).SortOrder = SortOrder.Descending;
                else
                    ((ListViewColumnSorter)listView.ListViewItemSorter).SortOrder = SortOrder.Ascending;
            }
            else
            {
                //Set the new column to sort.
                ((ListViewColumnSorter)listView.ListViewItemSorter).SortColumn = e.Column;
                ((ListViewColumnSorter)listView.ListViewItemSorter).SortOrder = SortOrder.Ascending;
            }

            //Sort the items based on the selected column and order.
            listView.BeginUpdate();
            listView.Sort();
            listView.EndUpdate();
        }

        private void changeSAVLanguageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (AppGlobals.Platform != AppGlobals.PlatformEnum.Switch)
            {
                MessageBox.Show("This option is only available for the Nintendo Switch version.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            bool openSavedPath = false;
            if (Directory.Exists(SAVFilePath) && File.Exists(SAVFilePath + "\\PREBOOT.SAV"))
            {
                if (MessageBox.Show(string.Format(".SAV directory found :\n\n{0}\n\nWould you like to open this directory ?", SAVFilePath), "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    openSavedPath = true;
                }
            }
            
            if (!openSavedPath)
            {
                FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog
                {
                    Description = "Select the emulator save directory"
                };

                DialogResult result = folderBrowserDialog.ShowDialog();
                if (result == DialogResult.OK)
                {
                    var files = Directory.GetFiles(folderBrowserDialog.SelectedPath);
                    string bootSavFile = files.FirstOrDefault(file => Path.GetFileName(file) == "PREBOOT.SAV");

                    if (bootSavFile == null)
                    {
                        MessageBox.Show("Needed .SAV files missing or corrupted", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                    SAVFilePath = folderBrowserDialog.SelectedPath;
                }
                else
                {
                    return;
                }
            }

            SaveSettings();
            SAVForm form = new SAVForm(SAVFilePath);
            form.ShowDialog();
        }

        private void reloadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.CurrentRPF == null)
                return;
            else if (MessageBox.Show("Are you sure you want to close the current RPF?\r\n\r\nAll unsaved progress will be lost.", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                return;
            else
                this.LoadRPF(CurrentRPFFileName, true);
        }

        [System.Runtime.InteropServices.DllImport("uxtheme.dll", ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        public static extern int SetWindowTheme(IntPtr hWnd, string pszSubAppName, string pszSubIdList);

        [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19;
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        private void SetTheme()
        {
            if (RPF6FileNameHandler.DarkMode)
            {
                this.BackColor = Color.FromArgb(45, 45, 48);
                this.ForeColor = Color.White;
                ApplyThemeToControls(this.Controls, true);
                
                // Special handling for MenuStrip to remove white borders/gradients
                menuStrip.Renderer = new DarkThemeRenderer();
                fileOptions.Renderer = new DarkThemeRenderer();
                
                // Dark Scrollbars attempt
                SetWindowTheme(treeView.Handle, "DarkMode_Explorer", null);
                SetWindowTheme(listView.Handle, "DarkMode_Explorer", null);

                // Enable Dark Title Bar (Windows 10/11)
                int useImmersiveDarkMode = 1;
                DwmSetWindowAttribute(this.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useImmersiveDarkMode, sizeof(int));
                
                // Handle ListView Resize to eliminate white header space
                listView.Resize -= ListView_Resize;
                listView.Resize += ListView_Resize;
                // Force initial resize
                ListView_Resize(listView, EventArgs.Empty);
            }
        }

        private void ListView_Resize(object sender, EventArgs e)
        {
            // Auto-resize the LAST column in the view to fill the space
            // Note: The visual order depends on DisplayIndex, but we usually just resize the last logical column 
            // OR the one that makes sense (Size/Name).
            // Here we just resize the last one in the collection as it's typically the right-most.
            if (listView.Columns.Count > 0)
            {
                int totalWidth = 0;
                // Sum width of all columns EXCEPT the last one
                for (int i = 0; i < listView.Columns.Count - 1; i++)
                {
                    totalWidth += listView.Columns[i].Width;
                }
                
                // Calculate remaining space
                int remaining = listView.ClientSize.Width - totalWidth;
                
                // Apply ONLY if it makes sense (don't shrink too small)
                if (remaining > 100) 
                {
                    listView.Columns[listView.Columns.Count - 1].Width = remaining - 4; // -4 for margin safety
                }
            }
        }

        private void ApplyThemeToControls(Control.ControlCollection controls, bool dark)
        {
            foreach (Control control in controls)
            {
                if (control is MenuStrip menuStrip)
                {
                    menuStrip.BackColor = Color.FromArgb(45, 45, 48);
                    menuStrip.ForeColor = Color.White;
                    foreach (ToolStripItem item in menuStrip.Items)
                    {
                        item.BackColor = Color.FromArgb(45, 45, 48);
                        item.ForeColor = Color.White;
                        if (item is ToolStripMenuItem menuItem)
                        {
                            foreach (ToolStripItem dropDownItem in menuItem.DropDownItems)
                            {
                                dropDownItem.BackColor = Color.FromArgb(45, 45, 48);
                                dropDownItem.ForeColor = Color.White;
                            }
                        }
                    }
                }
                else if (control is StatusStrip statusStrip)
                {
                    statusStrip.BackColor = Color.FromArgb(43, 43, 43); // Dark Grey instead of Blue
                    statusStrip.ForeColor = Color.White;
                    foreach (ToolStripItem item in statusStrip.Items)
                    {
                        item.BackColor = Color.FromArgb(43, 43, 43);
                        item.ForeColor = Color.White;
                    }
                }
                else if (control is TextBox textBox)
                {
                    if (dark)
                    {
                        textBox.BackColor = Color.FromArgb(30, 30, 30);
                        textBox.ForeColor = Color.White;
                        textBox.BorderStyle = BorderStyle.FixedSingle;
                    }
                    else
                    {
                        textBox.BackColor = SystemColors.Window;
                        textBox.ForeColor = SystemColors.WindowText;
                        textBox.BorderStyle = BorderStyle.Fixed3D;
                    }
                }
                else if (control is ListView listView)
                {
                    if (dark)
                    {
                        listView.BackColor = Color.FromArgb(30, 30, 30);
                        listView.ForeColor = Color.White;
                        listView.OwnerDraw = true;
                        
                        listView.DrawColumnHeader += (sender, headerScore) =>
                        {
                            headerScore.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(45, 45, 48)), headerScore.Bounds);
                            headerScore.Graphics.DrawRectangle(new Pen(Color.FromArgb(60, 60, 60)), headerScore.Bounds);
                            TextRenderer.DrawText(headerScore.Graphics, listView.Columns[headerScore.ColumnIndex].Text, listView.Font, headerScore.Bounds, Color.White, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
                        };
                        
                        // Fix for the white space to the right of columns:
                        // WinForms ListView doesn't expose a simple way to paint the 'empty' header space.
                        // However, since we are doing OwnerDraw, the main background is handled.
                        // The 'header' background for the empty space remains system default.
                        // One workaround is to change the Container color, but ListView header is a child.
                        // For now, ensuring the main list background is dark is the priority.
                        // (We partly fixed background by enabling WM_ERASEBKGND in ListViewNF)

                        listView.DrawItem += (sender, itemScore) => 
                        {
                            // Custom draw item to control selection color
                            if (itemScore.Item.Selected)
                            {
                                itemScore.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(60, 60, 60)), itemScore.Bounds);
                            }
                            else
                            {
                                itemScore.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(30, 30, 30)), itemScore.Bounds);
                            }
                            // We don't draw text here, DrawSubItem handles it usually, or we let default handle the rest if possible.
                            // However, ListView 'Details' view uses DrawSubItem.
                        };

                        listView.DrawSubItem += (sender, subItemScore) => 
                        { 
                            if (subItemScore.Item.Selected) 
                            {
                                subItemScore.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(70, 70, 70)), subItemScore.Bounds);
                            }
                            else
                            {
                                subItemScore.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(30, 30, 30)), subItemScore.Bounds);
                            }

                            var textBrush = Brushes.White;
                            // Draw image if it's the first column
                            if (subItemScore.ColumnIndex == 0)
                            {
                                if (listView.SmallImageList != null && subItemScore.Item.ImageIndex >= 0 && subItemScore.Item.ImageIndex < listView.SmallImageList.Images.Count)
                                {
                                    Image img = listView.SmallImageList.Images[subItemScore.Item.ImageIndex];
                                    subItemScore.Graphics.DrawImage(img, subItemScore.Bounds.Left + 2, subItemScore.Bounds.Top + (subItemScore.Bounds.Height - img.Height) / 2);
                                }
                                TextRenderer.DrawText(subItemScore.Graphics, subItemScore.Item.Text, listView.Font, new Rectangle(subItemScore.Bounds.Left + 20, subItemScore.Bounds.Top, subItemScore.Bounds.Width - 20, subItemScore.Bounds.Height), Color.White, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
                            }
                            else
                            {
                                TextRenderer.DrawText(subItemScore.Graphics, subItemScore.SubItem.Text, listView.Font, subItemScore.Bounds, Color.White, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
                            }
                        };
                    }
                    else
                    {
                        listView.BackColor = SystemColors.Window;
                        listView.ForeColor = SystemColors.WindowText;
                        listView.OwnerDraw = false;
                        // Events will still be attached but OwnerDraw=false disables them. 
                        // Cleaning up events is harder with anonymous delegates, but OwnerDraw=false is sufficient.
                    }
                }
                else if (control is TreeView treeView)
                {
                    if (dark)
                    {
                        treeView.BackColor = Color.FromArgb(30, 30, 30);
                        treeView.ForeColor = Color.White;
                        treeView.LineColor = Color.White;
                    }
                    else
                    {
                        treeView.BackColor = SystemColors.Window;
                        treeView.ForeColor = SystemColors.WindowText;
                        treeView.LineColor = SystemColors.WindowText; // Or default
                    }
                }
                else if (control is Button button)
                {
                    button.BackColor = dark ? Color.FromArgb(60, 60, 60) : SystemColors.ButtonFace;
                    button.ForeColor = dark ? Color.White : SystemColors.ControlText;
                    button.FlatStyle = FlatStyle.Flat;
                    button.FlatAppearance.BorderColor = Color.FromArgb(100, 100, 100);
                }
                else if (control is SplitContainer split)
                {
                    split.BackColor = dark ? Color.FromArgb(45, 45, 48) : SystemColors.Control;
                    ApplyThemeToControls(split.Panel1.Controls, dark);
                    ApplyThemeToControls(split.Panel2.Controls, dark);
                    continue; // Recursion handled
                }

                if (control.HasChildren)
                {
                    ApplyThemeToControls(control.Controls, dark);
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
        }
    }
}
