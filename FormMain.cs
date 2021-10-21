using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Text.RegularExpressions;
 
namespace Exploreader
{

    public partial class FormMain : Form
    {
    #region Global Variable Declaration:
        //string gSelectedPathAndFileName;
        string gSelectedFileName;
        bool gTextContentChanged = false;
        bool gNewFile = false;
        string gTempString;
        int gStartPos;
        int gCurrentPos;
        int gNextStartPos = 0;
        int sortOrderIndex = 0;
        SortOrder ListSortOrder = SortOrder.Ascending;
    #endregion


        public FormMain(string igSelectedFile)
        {
            gSelectedFileName = igSelectedFile;
            InitializeComponent();
        }
        private void FormMain_Load(object sender, EventArgs e)
        {
            try
            {
                dataGridView1.Enabled = false;
                folderTreeView1.InitFolderTreeView();//Folder initiate    

                SetAutoSave("Init"); //Initializing AutoSave button.

                if (gSelectedFileName == "-")
                {
                    //Initializing the browsepad as a new file was being created:
                    OpenFolder(Environment.GetFolderPath(Environment.SpecialFolder.Desktop)); //file list initially load to desktop
                    CreateAndOpenNewFile("Init");
                }
                else
                {
                    ////Opening a doubleClicked File.
                    FileInfo fi = new FileInfo(gSelectedFileName);
                    OpenFolder(fi.DirectoryName);
                    folderTreeView1.DrillToFolder(fi.DirectoryName);
                    ////Searching the DataGridView and selecting the correct row. 
                    for (int i = 0; i < dataGridView1.RowCount; i++)
                    {
                        if (dataGridView1.Rows[i].Cells[6].FormattedValue.ToString() == fi.FullName)
                        {
                            dataGridView1.CurrentCell = dataGridView1[0, i];
                            dataGridView1.CurrentCell.Selected = true;
                        }
                    }
                }
                this.toolStripContainerMain.BottomToolStripPanel.Visible = false;
                this.panelFind.Visible = false;

                string tip = "Click on the above Link button to go to the selected folder" + "\n"
                + "Currently selected folder:\n\t" + lblLink.Text;
                ToolTip tp = new ToolTip();
                tp.ToolTipTitle = "Current selection=" + lblLink.Text;
                tp.ToolTipIcon = ToolTipIcon.Info;
                tp.SetToolTip(this.folderTreeView1, tip);
                tp.ShowAlways = true;
            }
            catch (Exception ex)
            {
                ErrMsg(ex, "Form Load");
            }
        }

    #region Folder tree view related items:
        //When ever a folder is selected, reload the FileList
        private void folderTreeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            //folderTreeView1.CheckBoxes = false;
            OpenFolder(this.folderTreeView1.GetSelectedNodePath().ToString());
        }  
        private void OpenFolder(string lFolderPath)
        {
            try
            {
                if (lFolderPath.Length < 2) //Defaulting to Desktop.
                    lFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

                dataGridView1.Enabled = false;
                this.folderTreeView1.Tag = lFolderPath;
                PopulateExts(this.folderTreeView1.Tag.ToString());
                //PopulateDataGrid(".*", this.folderTreeView1.Tag.ToString());
                PopulateDataGrid(Properties.Settings.Default.FileTypeFilterString.ToString(), this.folderTreeView1.Tag.ToString(), sortOrderIndex, ListSortOrder);
            }
            catch (Exception ex)
            {
                ErrMsg(ex, "Open Folder error");
            }
        }

        //populate Exts when a folder is selected.
        private void PopulateExts(string lPath)
        {
            try
            {
                this.dataGridView1.Enabled = false;
                this.comboBoxExtList.Enabled = false;
                this.comboBoxExtList.Items.Clear();

                string[] AvailableExts;
                //string[] filesArray = Directory.GetFiles(@lPath,"*.*");

                DirectoryInfo DirInfo = new DirectoryInfo(lPath);//file list initially load to desktop
                lblLink.Text = DirInfo.FullName.ToString();
                FileInfo[] FI;
                FI = DirInfo.GetFiles("*.*"); 
                lblAppStatus.Text = " Has " + FI.Length.ToString() + " files.";//status update.

                AvailableExts = new string[FI.Length];

                for (int i = 0; i < FI.Length; i++) // Populating all available extensions array:
                    AvailableExts[i] = FI[i].Extension;

                string[] DistinctExts = GetDistinctValues(AvailableExts); // Distinct Extensions array.

                for (int i = 0; i < DistinctExts.Length; i++)
                    this.comboBoxExtList.Items.Add(DistinctExts[i].ToString());

                this.comboBoxExtList.Sorted = true;
                //initializing the combobox to .*
                this.comboBoxExtList.Items.Add(".*");
                //this.comboBoxExtList.SelectedIndex = 0;

                if (!comboBoxExtList.Items.Contains(Properties.Settings.Default.FileTypeFilterString.ToString()))
                    this.comboBoxExtList.Items.Add(Properties.Settings.Default.FileTypeFilterString.ToString());

                this.comboBoxExtList.SelectedItem = Properties.Settings.Default.FileTypeFilterString.ToString();
                
                // lblAppStatus.Text = "Exts count:" + comboBoxExtList.Items.Count.ToString(); //Status update
                this.comboBoxExtList.DropDownHeight = comboBoxExtList.Items.Count * 25;
                this.comboBoxExtList.Enabled = true;
            }
            catch (Exception ex)
            {
                ErrMsg(ex, "While Populating Extension List");
            }
        }
        //Populate Data Grid View:
        private void PopulateDataGrid(string ext,string lPath,int sortIndex,SortOrder ListSortOrder2)
        {
            try
            {
                this.dataGridView1.Enabled = false;
                DirectoryInfo DirInfo = new DirectoryInfo(lPath);//file list initially load to desktop
                FileInfo[] FI;
                FI = DirInfo.GetFiles("*" + ext.ToString());

                ////Using List<> to allow for sorting in data grid view.
                List<FileInfo> listFI = new List<FileInfo>();
                foreach(FileInfo tempFI in FI)
                    listFI.Add(tempFI);
                       
                ////Sorting the file list
                    switch (sortIndex)
                    {
                        case 0:
                            lblAppStatus.Text = sortIndex.ToString() + " Sorted by Name";
                            listFI.Sort(delegate(FileInfo f1, FileInfo f2) { return f1.Name.CompareTo(f2.Name); });
                            break;
                        case 1:
                            lblAppStatus.Text = sortIndex.ToString() + " Sorted by Length";
                            listFI.Sort(delegate(FileInfo f1, FileInfo f2) { return f1.Length.CompareTo(f2.Length); });
                            break;
                        case 6:
                            lblAppStatus.Text = sortIndex.ToString() + " Sorted by FullName";
                            listFI.Sort(delegate(FileInfo f1, FileInfo f2) { return f1.FullName.CompareTo(f2.FullName); });
                            break;
                        case 7:
                            lblAppStatus.Text = sortIndex.ToString() + " Sorted by Extension";
                            listFI.Sort(delegate(FileInfo f1, FileInfo f2) { return f1.Extension.CompareTo(f2.Extension); });
                            break;
                        case 8:
                            lblAppStatus.Text = sortIndex.ToString() + " Sorted by Creation Time";
                            listFI.Sort(delegate(FileInfo f1, FileInfo f2) { return f1.CreationTime.CompareTo(f2.CreationTime); });
                            break;
                        case 10:
                            lblAppStatus.Text = sortIndex.ToString() + " Sorted by Last Access Time";
                            listFI.Sort(delegate(FileInfo f1, FileInfo f2) { return f1.LastAccessTime.CompareTo(f2.LastAccessTime); });
                            break;
                        case 12:
                            lblAppStatus.Text = sortIndex.ToString() + " Sorted by Last Write time";
                            listFI.Sort(delegate(FileInfo f1, FileInfo f2) { return f1.LastWriteTime.CompareTo(f2.LastWriteTime); });
                            break;
                        default:
                            lblAppStatus.Text = "default sort by Name";
                            listFI.Sort(delegate(FileInfo f1, FileInfo f2) {return f1.Name.CompareTo(f2.Name);});
                            break;
                    }

                    if (ListSortOrder2 == SortOrder.Descending)
                        listFI.Reverse();

                
                dataGridView1.DataSource = listFI;
                if (ListSortOrder == SortOrder.Ascending)
                    dataGridView1.Columns[sortIndex].HeaderCell.SortGlyphDirection = SortOrder.Ascending;
                else
                    dataGridView1.Columns[sortIndex].HeaderCell.SortGlyphDirection = SortOrder.Descending;
                
                int[] validColumns = new int[] { 0, 1, 7, 8, 10,12 };
                for (int i = 0; i < dataGridView1.Columns.Count; i++)
                    if (!((IList<int>)validColumns).Contains(i))
                        dataGridView1.Columns[i].Visible = false;
                        
                ////replaced this (!validColumns.Contains(i)) with above for .net 2.0 framework compatiability.
                                       

                dataGridView1.Refresh();
                dataGridView1.Enabled = true;
             }
            catch (Exception ex)
            { ErrMsg(ex, "While Populating the the File List Grid."); }
        }


    #endregion
        
        //Handling All Error Messages in this section of Code:
        private void ErrMsg(Exception ex,string lErrSource)
        {
            string msg;

            msg = "Error message: " + ex.Message
                + "\n" + ex.HelpLink 
                + "\nTarget Site: " + ex.TargetSite.Name.ToString();

            if (ex.Message.Contains("OutOfMemory"))
                msg = "Out of memory exception is caused by trying to open a large file of size:"
                    + (Convert.ToInt64(dataGridView1.SelectedCells[1].Value)/1000).ToString()
                    + " KB.\n    -> If this machine has enough memory to handle files of such size, Close all other programs and try again \n    -> Increase the memory of the machine to handle files of such large size."
                    + "\n    -> Also please note that the Browsepad was designed to handle text file which usually does not grow very large.\n\n"
                    + msg.ToString();

            MessageBox.Show(msg, "Source: " + lErrSource +"/"+ ex.Source + "    -Browsepad", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
         }

        //on extension selected event, re-populate filelist.
        private void comboBoxExtList_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.dataGridView1.Enabled = false;
            Properties.Settings.Default.FileTypeFilterString = this.comboBoxExtList.SelectedItem.ToString();
            PopulateDataGrid(Properties.Settings.Default.FileTypeFilterString.ToString(), this.folderTreeView1.Tag.ToString(), sortOrderIndex, ListSortOrder);
        }

    #region Data grid View related procedures
        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            lblAppStatus.Text = "Opening...";
            this.dataGridView1_SelectionChanged(sender, e);
        }
        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            if (dataGridView1.Enabled == true)
            {
                lblAppStatus.Text = "Opening...";
                this.dataGridView1.Tag = dataGridView1.SelectedCells[0].Value.ToString();
                gNewFile = false;
                FileSelectionChanged();
                gCurrentPos = 0;
                gStartPos = 0;
                gNextStartPos = 0;
            }
        }
        private void FileSelectionChanged()
        { 
        //Task1: Save current file if Auto Save selected
        //Task2: Getting filename from data Grid and loading that file in text box.
            try
            {
                DialogResult dr;
                if (gTextContentChanged == true)
                {
                    if (Properties.Settings.Default.AutoSaveSetting == true)
                        SaveCurrentlyLoadedFile(this.textBoxContent.Tag.ToString(), false,"FileSelectionChanged");
                    else
                        {
                            dr = MessageBox.Show("Do you want to save the file '" + this.textBoxContent.Tag.ToString() + "'?", "Save File? - Browsepad", MessageBoxButtons.YesNo, MessageBoxIcon.Stop);
                            if (dr == DialogResult.Yes)
                                SaveCurrentlyLoadedFile(this.textBoxContent.Tag.ToString(), gNewFile, "FileSelectionChanged");
                            else 
                                lblAppStatus.Text = "You chose not to save the file.";
                        }
                }

                if (dataGridView1.Enabled == true)
                    ReadTextFile(dataGridView1.SelectedCells[6].Value.ToString());
            }
            catch (Exception ex)
            {
                ErrMsg(ex, "While File selection changing.");
            }
        }
       //Try's to read the text stream and returns the text.
        private void ReadTextFile(string lFilepath)
        { 
            if (!System.IO.File.Exists(lFilepath))
                throw new Exception("There is a problem opening file: '" + lFilepath + "', File Doesnot exist.");

            try
            {
                System.IO.StreamReader reader = new System.IO.StreamReader(lFilepath);
                StringBuilder sb = new StringBuilder();
                sb.Append(reader.ReadToEnd());
                this.textBoxContent.Text = sb.ToString();
                //this.gSelectedPathAndFileName = lFilepath;
                this.textBoxContent.Tag = lFilepath;
                this.Text = lFilepath+" - Browsepad";
                lblAppStatus.Text = "File Opened:"+this.dataGridView1.Tag.ToString(); //status update
                reader.Close();
                SetFileChangedTo(false);
            }

            catch (Exception ex)
            {
                ErrMsg(ex,"While Reading TextFile");
                textBoxContent.Clear();
                //this.gSelectedPathAndFileName = "";
                this.textBoxContent.Tag = "";
                this.Text = "Browsepad by Challa";
                this.lblAppStatus.Text = "Error Opening the file:" + this.textBoxContent.Tag.ToString() +"!! ->"+ ex.Message;
                CreateAndOpenNewFile("Init");
            }
        }
 
    #endregion

        private void lblLink_Click(object sender, EventArgs e)
        {
            string windir = Environment.GetEnvironmentVariable("WINDIR");
            System.Diagnostics.Process prc = new System.Diagnostics.Process();
            prc.StartInfo.FileName = windir + @"\explorer.exe";
            prc.StartInfo.Arguments = lblLink.Text;
            prc.Start();
        }
        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveCurrentlyLoadedFile(this.textBoxContent.Tag.ToString(), gNewFile,"SaveButton");
         }
        private void textBoxContent_TextChanged(object sender, EventArgs e)
        {
            SetFileChangedTo(true);
        }
        private void SetFileChangedTo(bool ChangedYesNo)
        {
            if (ChangedYesNo == true)
            {
                gTextContentChanged = true;
                this.toolStripButtonSaveNow.Text = "Save Now *";
                this.saveToolStripMenuItem.Text = "Save Now *";
            }
            else
            {
                gTextContentChanged = false;
                this.toolStripButtonSaveNow.Text = "Save Now";
                this.saveToolStripMenuItem.Text = "Save Now";
            }

        }
        private void autoSaveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetAutoSave("Toggle");
         }
        private void SetAutoSave(string InitOrToggle)
        {
            if (InitOrToggle == "Init")
            {
                //Auto save Check box: enabling and disabling.
                if (Properties.Settings.Default.AutoSaveSetting == true)
                {
                    this.autoSaveToolStripMenuItem.Image = Properties.Resources.CheckedCheckbox;
                    this.toolStripButtonAutoSave.Image = Properties.Resources.CheckedCheckbox;
                }
                else
                {
                    this.autoSaveToolStripMenuItem.Image = Properties.Resources.EmptyCheckbox;
                    this.toolStripButtonAutoSave.Image = Properties.Resources.EmptyCheckbox;
                }
            }
            else if (InitOrToggle == "Toggle")
            {
                if (Properties.Settings.Default.AutoSaveSetting == false)
                {
                    this.autoSaveToolStripMenuItem.Image = Properties.Resources.CheckedCheckbox;
                    this.toolStripButtonAutoSave.Image = Properties.Resources.CheckedCheckbox;
                    Properties.Settings.Default.AutoSaveSetting = true;
                }
                else
                {
                    this.autoSaveToolStripMenuItem.Image = Properties.Resources.EmptyCheckbox;
                    this.toolStripButtonAutoSave.Image = Properties.Resources.EmptyCheckbox;
                    Properties.Settings.Default.AutoSaveSetting = false;
                }
            }
        }
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
        private void FormMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            DialogResult dr = new DialogResult();
            if (gTextContentChanged == true)
            {
                if (Properties.Settings.Default.AutoSaveSetting == true)
                    SaveCurrentlyLoadedFile(this.textBoxContent.Tag.ToString(), false,"FormClosing");
                else
                {
                    dr = MessageBox.Show("Do you want to save the file '" + this.textBoxContent.Tag.ToString() + "'?", "Save File? - Browsepad", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Stop);
                    if (dr == DialogResult.Yes)
                    {
                        SaveCurrentlyLoadedFile(this.textBoxContent.Tag.ToString(), gNewFile,"FormClosing");
                        e.Cancel = false;
                    }
                    else if (dr == DialogResult.No)
                        e.Cancel = false;
                    else e.Cancel = true;
                }
            }
        }
        private void SaveCurrentlyLoadedFile(string lFullFileName, bool lNewFile, string lCallSource)
        {
            try
            {
                DialogResult dr = DialogResult.No;

                if (lNewFile || lFullFileName.Length == 0)
                {
                    SaveFileDialog saveFileDialog1 = new SaveFileDialog();
                    saveFileDialog1.Filter = "Text files(*.txt)|*.txt|All files(*.*)|*.*";
                    saveFileDialog1.FileName = lFullFileName;
                    saveFileDialog1.Title = "Save currently loaded File - Browsepad";
                    dr = saveFileDialog1.ShowDialog();
                    lFullFileName = saveFileDialog1.FileName.ToString();
                }
                
                if (dr != DialogResult.Cancel)
                {
                    this.toolStripProgressBar1.Value = 10;
                    //Streamwriter code from http://www.dreamincode.net/forums/showtopic64662.htm
                    StreamWriter sw = new StreamWriter(lFullFileName);
                    this.toolStripProgressBar1.Value = 40;
                    sw.WriteLine(textBoxContent.Text);
                    this.toolStripProgressBar1.Value = 80;
                    sw.Close();
                    this.toolStripProgressBar1.Value = 100;
                    SetFileChangedTo(false);
                    gNewFile = false;
                    this.lblAppStatus.Text = lFullFileName + " Saved.";
                }
                //timer1.Stop();
                //timer1.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Source: " + ex.Source + "    -Browsepad", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }
        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CreateAndOpenNewFile("NewFile");
        }
        // saves the current file, if any and initiates the new file name and gives it a name
        private void CreateAndOpenNewFile(string InitOrNewFile)
        {
            string lFileName = "Browsepad_File_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt";
            if (InitOrNewFile == "Init") //Just create new
            {
                this.dataGridView1.Tag = lFileName;
                this.textBoxContent.Tag = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\" + lFileName;
            }
            if (InitOrNewFile == "NewFile") //Save current file and create new
            {
                if (this.textBoxContent.Tag.ToString().Length > 0)
                    SaveCurrentlyLoadedFile(this.textBoxContent.Tag.ToString(), false,"CreateNewFile");

                if (lblLink.Text.Length <= 1)
                {
                    this.dataGridView1.Tag = lFileName;
                    this.textBoxContent.Tag = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\" + lFileName; ;
                }
                else
                    this.textBoxContent.Tag = lblLink.Text + "\\" + lFileName;
            }

            textBoxContent.Clear();
            SetFileChangedTo(false);
            gNewFile = true;
            this.Text = this.textBoxContent.Tag.ToString() + "[NEW FILE] - Browsepad";
            this.lblAppStatus.Text = "New file, Not Saved Yet!";
        }
        private void openFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            DialogResult dr; 
            dr = ofd.ShowDialog();
            if (dr != DialogResult.Cancel)
            {
                this.textBoxContent.Tag = Path.GetFullPath(ofd.FileName);//gSelectedPathAndFileName = Path.GetFullPath(ofd.FileName);
                this.folderTreeView1.Tag = Directory.GetCurrentDirectory();
                this.dataGridView1.Tag = Path.GetFileName(ofd.FileName);
                OpenFolder(this.folderTreeView1.Tag.ToString());
                ReadTextFile(this.textBoxContent.Tag.ToString());
            }
        }
        private void openFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            fbd.ShowDialog();
            OpenFolder(fbd.SelectedPath);
            //folderTreeView1.InitFolderTreeView();
        }
        private void openDesktopToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFolder(Environment.GetFolderPath(Environment.SpecialFolder.Desktop));
        }
        private void openMyDocumentsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFolder(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
        }
        private void openCToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFolder("C:\\");
        }
        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveCurrentlyLoadedFile("", true,"SaveAsButton");
        }
        private void folderTreeView1_MouseHover(object sender, EventArgs e)
        {
        }
    #region Edit Menu Functions:
        private void undoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (textBoxContent.CanUndo)
                    this.textBoxContent.Undo();
            }
            catch (Exception ex)
            {
                ErrMsg(ex, "No More Undo's available");
            }
        }
        private void cutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            textBoxContent.Cut();
        }
        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            textBoxContent.Copy();
        }
        private void pasteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            textBoxContent.Paste();
        }
        private void timeDateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.textBoxContent.Paste("<"+DateTime.Now.ToString() + ">");
        }
        private void selectAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            textBoxContent.SelectAll();
        }
    #endregion
        private void toolStripButtonReadingView_Click(object sender, EventArgs e)
        {
            if (toolStripButtonReadingView.Text == "Reading View")
            {
                this.splitContainerMain.Panel1Collapsed = true;
                toolStripButtonReadingView.Text = "Normal View";
                this.toolStripButtonReadingView.Image = Properties.Resources.NormalView;
                this.readingViewToolStripMenuItem.Image = Properties.Resources.NormalView;
            }
            else
            {
                this.splitContainerMain.Panel1Collapsed = false;
                toolStripButtonReadingView.Text = "Reading View";
                this.toolStripButtonReadingView.Image = Properties.Resources.ReadingView;
                this.readingViewToolStripMenuItem.Image = Properties.Resources.ReadingView;
            }
        }
        private void wordWrapToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.toolStripProgressBar1.Value = 40;
            if (textBoxContent.WordWrap == true)
            {
                textBoxContent.WordWrap = false;
                this.wordWrapToolStripMenuItem.Image = Properties.Resources.EmptyCheckbox;
                this.toolStripButtonWordWrap.Image = Properties.Resources.EmptyCheckbox;
                this.toolStripProgressBar1.Value = 70;
            }
            else
            {
                textBoxContent.WordWrap = true;
                this.wordWrapToolStripMenuItem.Image = Properties.Resources.CheckedCheckbox;
                this.toolStripButtonWordWrap.Image = Properties.Resources.CheckedCheckbox;
                this.toolStripProgressBar1.Value = 70;
            }
            this.toolStripProgressBar1.Value = 100;
        }
        private void toUpperToolStripMenuItem_Click(object sender, EventArgs e)
        {
            textBoxContent.SelectedText = textBoxContent.SelectedText.ToUpper();
        }
        private void toLowerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            textBoxContent.SelectedText = textBoxContent.SelectedText.ToLower();
        }
        private void zoomInToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (textBoxContent.Font.SizeInPoints < 60)
                this.textBoxContent.Font = new Font(textBoxContent.Font.Name, textBoxContent.Font.SizeInPoints + 1);
        }
        private void zoomOutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (textBoxContent.Font.SizeInPoints >= 6)
                this.textBoxContent.Font = new Font(textBoxContent.Font.Name, textBoxContent.Font.SizeInPoints - 1);
        }
        private void LFCRLFToolStripMenuItem_Click(object sender, EventArgs e)
        {
            toolStripProgressBar1.ProgressBar.Value = 20;
            StringBuilder sb = new StringBuilder();
            sb.Append(textBoxContent.Text);
            if (this.LFCRLFToolStripMenuItem.Text == "LF -> CRLF")
            {
                toolStripProgressBar1.ProgressBar.Value = 50;
                sb.Replace("\n", "\r\n");
                this.LFCRLFToolStripMenuItem.Text = "CRLF -> LF";
            }
            else
            {
                toolStripProgressBar1.ProgressBar.Value = 50;
                sb.Replace("\r\n", "\n");
                this.LFCRLFToolStripMenuItem.Text = "LF -> CRLF";
            }
            toolStripProgressBar1.ProgressBar.Value = 70;
            textBoxContent.Text = sb.ToString();
            toolStripProgressBar1.ProgressBar.Value = 100;
      
        }
        private void FontToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FontDialog fd = new FontDialog();
            fd.ShowDialog();
            //Properties.Settings.Default.textfont = fd.Font;
            textBoxContent.Font = fd.Font;
        }
    #region Find & Replace related functions:
        private void findToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.panelFind.Visible = true;

            if (this.textBoxContent.SelectedText.Length > 0)
            {
                textBoxFind.Text  = this.textBoxContent.SelectedText.ToString();
            }
            else if (textBoxFind.Text.Length == 0)
            {
                buttonFindNext.Enabled = false;
                buttonReplace.Enabled = false;
                buttonReplaceAll.Enabled = false;
                textBoxFind.Focus();
            }
            else
            {
                buttonFindNext.Enabled = true;
                buttonReplace.Enabled = true;
                buttonReplaceAll.Enabled = true;
                buttonFindNext.Focus();
            }
            //Call search procedure here. 
            //buttonFind_Click(sender, e);
            //buttonFindNext_Click(sender, e);
        }
        private void buttonCancel_Click(object sender, EventArgs e)
        {
            this.panelFind.Visible = false;

            gCurrentPos = 0;
            gNextStartPos = 0;
            gStartPos = 0;

            buttonFindNext.Enabled = false;
            buttonReplace.Enabled = false;
            buttonReplaceAll.Enabled = false;
            this.findNextToolStripMenuItem.Enabled = false;
            this.toolStripMenuItemReplace.Enabled = false;
        }
        private void textBoxFind_TextChanged(object sender, EventArgs e)
        {
            enableFindRelatedButtons();
        }
        private void buttonFindNext_Click(object sender, EventArgs e)
        {   
            try
            {
                findOrReplace("find");
            }
            catch (Exception ex)
            {
                ErrMsg(ex, "While Searching the File");
                gCurrentPos = 0;
                gStartPos = 0;
                gNextStartPos = 0;
            }
        }
        private void buttonReplace_Click(object sender, EventArgs e)
        {
            try
            {
                findOrReplace("replace");
                //int FileLen = this.textBoxContent.Text.Length;
                //if (FileLen < 1)
                //{
                //    MessageBox.Show("File '" + this.textBoxContent.Tag.ToString() + "' doesn't contain any text.", "Zero File Length exception . - Browsepad.", MessageBoxButtons.OK, MessageBoxIcon.Information);
                //    return;
                //}

                //int searchStringLength = this.textBoxFind.Text.Length;
                //if (searchStringLength > 0)
                //{   //checking for case matching 
                //    StringComparison searchType;
                //    if (checkBoxMatchCase.Checked) searchType = StringComparison.Ordinal;
                //    else searchType = StringComparison.OrdinalIgnoreCase;

                //    if (gNextStartPos == 0)
                //        gStartPos = this.textBoxContent.SelectionStart; //Getting the current caret position.
                //    else if (gNextStartPos == -1)
                //        gStartPos = 0;
                //    else gStartPos = gNextStartPos;

                //    gCurrentPos = textBoxContent.Text.IndexOf(textBoxFind.Text, gStartPos, searchType);

                //    switch (gCurrentPos)
                //    {
                //        case -1:
                //            gNextStartPos = gCurrentPos + 1 + searchStringLength;
                //            break;
                //        default:
                //            gNextStartPos = gCurrentPos + searchStringLength;
                //            break;
                //    }
                //    if (gCurrentPos != -1)
                //    {
                //        this.textBoxContent.Focus();
                //        this.textBoxContent.Select(gCurrentPos, searchStringLength);
                //        textBoxContent.SelectedText = textBoxReplace.Text;
                //        this.textBoxContent.ScrollToCaret();
                //    }
                //    else
                //    {
                //        MessageBox.Show("End of File reached!! \n\nClicking the 'Find Next' button again will automatically \nstart searching from the begining of the file.", "EOF reached     -Browsepad", MessageBoxButtons.OK, MessageBoxIcon.Information);
                //        gNextStartPos = -1;
                //    }
                //}
                //else this.textBoxFind.Focus();
            }
            catch (Exception ex)
            {
                ErrMsg(ex, "While Replacing");
                gCurrentPos = 0;
                gStartPos = 0;
                gNextStartPos = 0;
            }

        }
        private void buttonReplaceAll_Click(object sender, EventArgs e)
        {
            if (this.textBoxFind.Text.Length > 0)
            {
                RegexOptions opt = new RegexOptions();
                if (checkBoxMatchCase.Checked == false)
                    opt = RegexOptions.IgnoreCase;

                MatchCollection mc = Regex.Matches(textBoxContent.Text, textBoxFind.Text, opt);
                textBoxContent.Text = Regex.Replace(textBoxContent.Text, textBoxFind.Text, textBoxReplace.Text, opt);
                MessageBox.Show("Found " + mc.Count + " occurences of '" + textBoxFind.Text + "' and replaced with '" + textBoxReplace.Text + "'.");
            }
            else
                MessageBox.Show("Enter the text you want to replace in 'Find What' text box", "Search text Empty - Browsepad", MessageBoxButtons.OK, MessageBoxIcon.Hand);

        }
        private void enableFindRelatedButtons()
        {
            if (textBoxFind.Text.Length > 0)
            {
                buttonFindNext.Enabled = true;
                buttonReplace.Enabled = true;
                buttonReplaceAll.Enabled = true;
                this.findNextToolStripMenuItem.Enabled = true;
                this.toolStripMenuItemReplace.Enabled = true;
            }
            else
            {
                buttonFindNext.Enabled = false;
                buttonReplace.Enabled = false;
                buttonReplaceAll.Enabled = false;
                this.findNextToolStripMenuItem.Enabled = false;
                this.toolStripMenuItemReplace.Enabled = false;
            }
        }
        private void findOrReplace(string task)
        {
            try
            {
                int FileLen = this.textBoxContent.Text.Length;
                if (FileLen < 1)
                {
                    MessageBox.Show("File '"
                        + this.textBoxContent.Tag.ToString()
                        + "' doesn't contain any text.", "Zero File Length exception. - Browsepad.",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                int searchStringLength = this.textBoxFind.Text.Length;
                if (searchStringLength > 0)
                {   //checking for case matching 
                    StringComparison searchType;
                    if (checkBoxMatchCase.Checked) searchType = StringComparison.Ordinal;
                    else searchType = StringComparison.OrdinalIgnoreCase;

                    //debug 
                    //labelfind.Text = "selectionstart:" + textBoxContent.SelectionStart.ToString() + " - Nextstartposition:" + gNextStartPos.ToString();

                    if (gNextStartPos == 0 || gNextStartPos == -1)
                        gStartPos = 0;  //Getting the current caret position.
                    else if (gNextStartPos > 0 && this.textBoxContent.SelectionStart == 0)
                        gStartPos = gNextStartPos;
                    else
                        gStartPos = this.textBoxContent.SelectionStart + searchStringLength;

                    gCurrentPos = textBoxContent.Text.IndexOf(textBoxFind.Text, gStartPos, searchType);

                    switch (gCurrentPos)
                    {
                        case -1:
                            gNextStartPos = 0;
                            break;
                        default:
                            gNextStartPos = gCurrentPos + 1;
                            break;
                    }
                    if (gCurrentPos != -1)
                    {
                            this.textBoxContent.Focus();
                            this.textBoxContent.Select(gCurrentPos, searchStringLength);
                            if (task == "replace") 
                                textBoxContent.SelectedText = textBoxReplace.Text;
                            this.textBoxContent.ScrollToCaret();
                    }
                    else
                    {
                        MessageBox.Show("End of File reached!! \n\nClicking the 'Find Next' button will automatically \nstart searching from the begining of the file.", "EOF reached     -Browsepad", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        gNextStartPos = -1;
                    }
                }
                else this.textBoxFind.Focus();
            }
            catch (Exception ex)
            {
                ErrMsg(ex, "While Searching the File");
                gCurrentPos = 0;
                gStartPos = 0;
                gNextStartPos = 0;
            }
        }

    #endregion
        private void dataGridView1_ColumnHeaderMouseDoubleClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            sortOrderIndex = e.ColumnIndex;
            if (ListSortOrder == SortOrder.Ascending)
                ListSortOrder = SortOrder.Descending;
            else
                ListSortOrder = SortOrder.Ascending;

            this.dataGridView1.Enabled = false;
            PopulateDataGrid(Properties.Settings.Default.FileTypeFilterString.ToString(), this.folderTreeView1.Tag.ToString(), sortOrderIndex, ListSortOrder);
            //Properties.Settings.Default.SortOrder;
            //dataGridView1.SortedColumn = dataGridView1.Columns[0];
            
            //dgv.DataGridViewColumnSortMode.Automatic;
            //dataGridView1.Sort(dataGridView1.Columns[e.ColumnIndex], ListSortDirection.Ascending);
            //dataGridView1.Columns[e.ColumnIndex].SortMode = DataGridViewColumnSortMode.Automatic;
            //dataGridView1.Columns[e.ColumnIndex].HeaderCell.SortGlyphDirection = SortOrder.Ascending;
        }

    #region Handling Escape Key Down event:
        private void textBoxContent_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyData == Keys.Escape)
                if (panelFind.Visible)
                    buttonCancel_Click(sender, e);
        }
        private void textBoxFind_KeyDown(object sender, KeyEventArgs e)
        {

            if (e.KeyData == Keys.Escape)
            {
                if (panelFind.Visible)
                    buttonCancel_Click(sender, e);
            }
            else if (e.KeyData == Keys.Enter)
                buttonFindNext_Click(sender,e);
        }
        private void textBoxReplace_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyData == Keys.Escape)
                if (panelFind.Visible)
                    buttonCancel_Click(sender, e);
        }
    #endregion 

    #region Trivial functionality:
      #region Copy path button:
        private void copyFolderPathToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(this.folderTreeView1.Tag.ToString());
        }
        private void copyFilePathToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(this.textBoxContent.Tag.ToString());
        }
        private void copyFileNameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(this.dataGridView1.Tag.ToString());
        }
      #endregion
        private void toolStripButtonRefresh_Click(object sender, EventArgs e)
        {
            folderTreeView1.InitFolderTreeView();
            folderTreeView1.DrillToFolder(folderTreeView1.Tag.ToString());
        }
        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AboutBox1 ab1 = new AboutBox1();
            ab1.Show();
        }
        private void lblLink_MouseEnter(object sender, EventArgs e)
        {
            gTempString = lblAppStatus.Text;
            this.lblAppStatus.Text = "Click this link to open Folder.";
        }
        private void lblLink_MouseLeave(object sender, EventArgs e)
        {
            lblAppStatus.Text = gTempString;
            gTempString = "";
        }

    #endregion

        public T[] GetDistinctValues<T>(T[] array) 
        { 
            List<T> tmp = new List<T>(); 
            for (int i = 0; i < array.Length; i++) 
            { 
                if (tmp.Contains(array[i]))            
                    continue; 
                tmp.Add(array[i]); 
            } 
            return tmp.ToArray(); 
        }

    }

     //public class myReverserClass : IComparer
     //{
     //    // Calls CaseInsensitiveComparer.Compare with the parameters reversed.
     //    int IComparer.Compare(Object x, Object y)
     //    {
     //        return ((new CaseInsensitiveComparer()).Compare(y, x));
     //    }
     //}
}


