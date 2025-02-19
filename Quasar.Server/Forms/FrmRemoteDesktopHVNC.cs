using Quasar.Server.Forms.DarkMode;
using System.Windows.Forms;

namespace Quasar.Server.Forms
{
    public partial class FrmRemoteDesktopHVNC : Form
    {
        public FrmRemoteDesktopHVNC()
        {
            InitializeComponent();
            DarkModeManager.ApplyDarkMode(this);
        }
    }
}
