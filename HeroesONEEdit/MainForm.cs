using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using HeroesONELib;

namespace HeroesONEEdit
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private string filename;
        private HeroesONEFile file;

        private void Form1_Load(object sender, EventArgs e)
        {
            string[] args = Environment.GetCommandLineArgs();
            if (args.Length == 1)
                file = new HeroesONEFile();
            else
                LoadFile(args[1]);
        }

        private void LoadFile(string filename)
        {
#if !DEBUG
			try
            {
#endif
                file = new HeroesONEFile(filename);
#if !DEBUG
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message);
                return;
            }
#endif
			switch (file.Type)
			{
				case ArchiveType.Heroes:
					heroesToolStripMenuItem_Click(this, EventArgs.Empty);
					break;
				case ArchiveType.Shadow060:
					shadow060ToolStripMenuItem_Click(this, EventArgs.Empty);
					break;
				case ArchiveType.Shadow050:
					shadow050ToolStripMenuItem_Click(this, EventArgs.Empty);
					break;
                case ArchiveType.HeroesE3:
                    heroesE3ToolStripMenuItem_Click(this, EventArgs.Empty);
                    break;
                case ArchiveType.HeroesPreE3:
                    heroesPreE3ToolStripMenuItem_Click(this, EventArgs.Empty);
                    break;
            }
            this.filename = filename;
            listView1.Items.Clear();
            imageList1.Images.Clear();
            listView1.BeginUpdate();
            foreach (HeroesONEFile.File item in file.Files)
            {
                imageList1.Images.Add(GetIcon(item.Name));
                listView1.Items.Add(item.Name, imageList1.Images.Count - 1);
            }
            listView1.EndUpdate();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog a = new OpenFileDialog()
            {
                DefaultExt = "one",
                Filter = "ONE Files|*.one|All Files|*.*"
            };
            if (a.ShowDialog() == DialogResult.OK)
                LoadFile(a.FileName);
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(filename))
                saveAsToolStripMenuItem_Click(sender, e);
            else
                file.Save(filename, heroesToolStripMenuItem.Checked ? ArchiveType.Heroes : heroesE3ToolStripMenuItem.Checked ? ArchiveType.HeroesE3 : heroesPreE3ToolStripMenuItem.Checked ? ArchiveType.HeroesPreE3 : shadow060ToolStripMenuItem.Checked ? ArchiveType.Shadow060 : ArchiveType.Shadow050);
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog a = new SaveFileDialog() { Filter = "ONE Files|*.one|All Files|*.*" })
                if (a.ShowDialog() == DialogResult.OK)
                {
					file.Save(a.FileName, heroesToolStripMenuItem.Checked ? ArchiveType.Heroes : heroesE3ToolStripMenuItem.Checked ? ArchiveType.HeroesE3 : heroesPreE3ToolStripMenuItem.Checked ? ArchiveType.HeroesPreE3 : shadow060ToolStripMenuItem.Checked ? ArchiveType.Shadow060 : ArchiveType.Shadow050);
                    this.filename = a.FileName;
                }
        }

        private void extractAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
			using (FolderBrowserDialog a = new FolderBrowserDialog() { ShowNewFolderButton = true })
			{
				if (filename != null)
					a.SelectedPath = Path.GetDirectoryName(filename);
				if (a.ShowDialog(this) == DialogResult.OK)
					foreach (HeroesONEFile.File item in file.Files)
						File.WriteAllBytes(Path.Combine(a.SelectedPath, item.Name), item.Data);
			}
        }

        ListViewItem selectedItem;
        private void listView1_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                selectedItem = listView1.GetItemAt(e.X, e.Y);
                if (selectedItem != null)
                    contextMenuStrip1.Show(listView1, e.Location);
            }
        }

		private void addFilesToolStripMenuItem_Click(object sender, EventArgs e)
		{
			using (OpenFileDialog a = new OpenFileDialog()
			{
				Filter = "All Files|*.*",
				Multiselect = true
			})
				if (a.ShowDialog() == DialogResult.OK)
					AddFiles(a.FileNames);
		}

		private void extractToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (selectedItem == null) return;
			using (SaveFileDialog a = new SaveFileDialog()
			{
				Filter = "All Files|*.*",
				FileName = selectedItem.Text
			})
				if (a.ShowDialog() == DialogResult.OK)
					File.WriteAllBytes(a.FileName, file.Files[listView1.Items.IndexOf(selectedItem)].Data);
		}

        private void replaceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (selectedItem == null) return;
            int i = listView1.Items.IndexOf(selectedItem);
            string fn = file.Files[i].Name;
            using (OpenFileDialog a = new OpenFileDialog()
            {
                Filter = "All Files|*.*",
                FileName = fn
            })
				if (a.ShowDialog() == DialogResult.OK)
				{
					file.Files[i] = new HeroesONEFile.File(a.FileName);
					file.Files[i].Data = file.Files[i].Data;
					file.Files[i].Name = fn;
				}
        }

        private void insertToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (selectedItem == null) return;
            using (OpenFileDialog a = new OpenFileDialog()
            {
                Filter = "All Files|*.*",
                Multiselect = true
            })
				if (a.ShowDialog() == DialogResult.OK)
				{
					int i = listView1.Items.IndexOf(selectedItem);
					foreach (string item in a.FileNames)
					{
						file.Files.Insert(i, new HeroesONEFile.File(item));
						file.Files[i].Data = file.Files[i].Data;
						i++;
					}
					listView1.Items.Clear();
					imageList1.Images.Clear();
					listView1.BeginUpdate();
					for (int j = 0; j < file.Files.Count; j++)
					{
						imageList1.Images.Add(GetIcon(file.Files[j].Name));
						listView1.Items.Add(file.Files[j].Name, j);
					}
					listView1.EndUpdate();
				}
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (selectedItem == null) return;
            int i = listView1.Items.IndexOf(selectedItem);
            file.Files.RemoveAt(i);
            listView1.Items.RemoveAt(i);
            imageList1.Images.RemoveAt(i);
        }

        private string oldName;
        private void listView1_BeforeLabelEdit(object sender, LabelEditEventArgs e)
        {
            oldName = e.Label;
        }

        private void listView1_AfterLabelEdit(object sender, LabelEditEventArgs e)
        {
            if (oldName == e.Label) return;
            foreach (HeroesONEFile.File item in file.Files)
            {
                if (item.Name.Equals(e.Label, StringComparison.OrdinalIgnoreCase))
                {
                    e.CancelEdit = true;
                    MessageBox.Show("This name is being used by another file.");
                    return;
                }
            }
            if (e.Label.IndexOfAny(Path.GetInvalidFileNameChars()) > -1)
            {
                e.CancelEdit = true;
                MessageBox.Show("This name contains invalid characters.");
                return;
            }
            file.Files[e.Item].Name = e.Label;
            imageList1.Images[e.Item] = GetIcon(e.Label).ToBitmap();
        }

        private void listView1_ItemActivate(object sender, EventArgs e)
        {
            string fp = Path.Combine(Path.GetTempPath(), file.Files[listView1.SelectedIndices[0]].Name);
            File.WriteAllBytes(fp, file.Files[listView1.SelectedIndices[0]].Data);
            System.Diagnostics.Process.Start(fp);
        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            file.Files = new List<HeroesONEFile.File>();
            listView1.Items.Clear();
            imageList1.Images.Clear();
        }

        private void listView1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.All;
        }

        private void listView1_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
				AddFiles((string[])e.Data.GetData(DataFormats.FileDrop, true));
        }

		private void AddFiles(string[] files)
		{
			int i = file.Files.Count;
			foreach (string item in files)
			{
				bool found = false;
				for (int j = 0; j < file.Files.Count; j++)
					if (file.Files[j].Name.Equals(Path.GetFileName(item), StringComparison.OrdinalIgnoreCase))
					{
						found = true;
						file.Files[j] = new HeroesONEFile.File(item);
						file.Files[j].Data = file.Files[j].Data;
					}
				if (found) continue;
				file.Files.Add(new HeroesONEFile.File(item));
				file.Files[i].Data = file.Files[i].Data;
				imageList1.Images.Add(GetIcon(file.Files[i].Name));
				listView1.Items.Add(file.Files[i].Name, i);
				i++;
			}
		}

        private void listView1_ItemDrag(object sender, ItemDragEventArgs e)
        {
            string fn = Path.Combine(Path.GetTempPath(), file.Files[listView1.SelectedIndices[0]].Name);
            File.WriteAllBytes(fn, file.Files[listView1.SelectedIndices[0]].Data);
            DoDragDrop(new DataObject(DataFormats.FileDrop, new string[] { fn }), DragDropEffects.All);
        }

        private Dictionary<string, Icon> iconstore = new Dictionary<string, Icon>();

        [System.Runtime.InteropServices.DllImport("shell32.dll")]
        private static extern IntPtr ExtractIconA(int hInst, string lpszExeFileName, int nIconIndex);

        private Icon GetIcon(string file)
        {
            string iconpath = "C:\\Windows\\system32\\shell32.dll,0";
            string ext = file.IndexOf('.') > -1 ? file.Substring(file.LastIndexOf('.')) : file;
            if (iconstore.ContainsKey(ext.ToLowerInvariant()))
                return iconstore[ext.ToLowerInvariant()];
            Microsoft.Win32.RegistryKey k = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(ext);
            if (k == null)
                k = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey("*");
            k = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey((string)k.GetValue("", "*"));
            if (k != null)
            {
                k = k.OpenSubKey("DefaultIcon");
                if (k != null)
                    iconpath = (string)k.GetValue("", "C:\\Windows\\system32\\shell32.dll,0");
            }
            int iconind = 0;
            if (iconpath.LastIndexOf(',') > iconpath.LastIndexOf('.'))
            {
                iconind = int.Parse(iconpath.Substring(iconpath.LastIndexOf(',') + 1));
                iconpath = iconpath.Remove(iconpath.LastIndexOf(','));
            }
            try
            {
                return iconstore[ext.ToLowerInvariant()] = Icon.FromHandle(ExtractIconA(0, iconpath, iconind));
            }
            catch (Exception)
            {
                return iconstore[ext.ToLowerInvariant()] = Icon.FromHandle(ExtractIconA(0, "C:\\Windows\\system32\\shell32.dll", 0));
            }
        }

        private void quitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

		private void heroesToolStripMenuItem_Click(object sender, EventArgs e)
		{
			heroesToolStripMenuItem.Checked = true;
			shadow050ToolStripMenuItem.Checked = shadow060ToolStripMenuItem.Checked = heroesE3ToolStripMenuItem.Checked = heroesPreE3ToolStripMenuItem.Checked = false;
		}

		private void shadow060ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			shadow060ToolStripMenuItem.Checked = true;
			heroesToolStripMenuItem.Checked = shadow050ToolStripMenuItem.Checked = heroesE3ToolStripMenuItem.Checked = heroesPreE3ToolStripMenuItem.Checked = false;
		}

		private void shadow050ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			shadow050ToolStripMenuItem.Checked = true;
			heroesToolStripMenuItem.Checked = shadow060ToolStripMenuItem.Checked = heroesE3ToolStripMenuItem.Checked = heroesPreE3ToolStripMenuItem.Checked = false;
		}

        private void heroesE3ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            heroesE3ToolStripMenuItem.Checked = true;
            shadow050ToolStripMenuItem.Checked = shadow060ToolStripMenuItem.Checked = heroesToolStripMenuItem.Checked = heroesPreE3ToolStripMenuItem.Checked = false;
        }

        private void heroesPreE3ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            heroesPreE3ToolStripMenuItem.Checked = true;
            shadow050ToolStripMenuItem.Checked = shadow060ToolStripMenuItem.Checked = heroesToolStripMenuItem.Checked = heroesE3ToolStripMenuItem.Checked = false;
        }
    }
}