using LiveSplit.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace LiveSplit.TimeAttackPause.UI.Components
{
    public partial class TimeAttackPauseSettings : UserControl
    {
        public LayoutMode Mode { get; set; }
        public bool EnableAutosave { get; set; }

        public TimeAttackPauseSettings()
        {
            InitializeComponent();
            EnableAutosave = true;
        }

        private void TimeAttackPauseSettings_Load(object sender, EventArgs e)
        {

        }

        private void tableLayoutPanel_Paint(object sender, PaintEventArgs e)
        {

        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void BrowseButton_Click(object sender, EventArgs e)
        {
            using (var d = new FolderBrowserDialog())
            {
                d.SelectedPath = DefaultSavePath ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                if (d.ShowDialog() == DialogResult.OK)
                {
                    DefaultSavePath = d.SelectedPath;
                    SaveFilePathTextBox.Text = DefaultSavePath;
                }
            }
        }

        private void SetDefaultSaveFileButton_Click(object sender, EventArgs e)
        {
            DefaultSavePath = SaveFilePathTextBox.Text;
            EnableAutosave = EnableAutosaveCheckBox.Checked;
        }

        public XmlNode GetSettings(XmlDocument document)
        {
            var parent = document.CreateElement("Settings");
            CreateSettingsNode(document, parent);
            return parent;
        }

        public void SetSettings(XmlNode node)
        {
            var element = (XmlElement)node;
            DefaultSavePath = SettingsHelper.ParseString(element["DefaultSavePath"]);
            var enableNode = element["EnableAutosave"];
            EnableAutosave = enableNode == null ? true : SettingsHelper.ParseBool(enableNode);
            // update UI
            SaveFilePathTextBox.Text = DefaultSavePath;
            EnableAutosaveCheckBox.Checked = EnableAutosave;
        }

        public int GetSettingsHashCode()
        {
            return CreateSettingsNode(null, null);
        }

        private int CreateSettingsNode(XmlDocument document, XmlElement parent)
        {
            return SettingsHelper.CreateSetting(document, parent, "Version", "1.0") ^
                SettingsHelper.CreateSetting(document, parent, "DefaultSavePath", DefaultSavePath) ^
                SettingsHelper.CreateSetting(document, parent, "EnableAutosave", EnableAutosave.ToString());
        }
    }
}
