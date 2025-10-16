using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Pulsar.Server.Plugins;
using Pulsar.Server.Forms.DarkMode;
using Pulsar.Server.Utilities;

namespace Pulsar.Server.Forms
{
    public class NonResizableListView : ListView
    {
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x0201)
            {
                var hitTest = this.HitTest(new Point((int)m.LParam & 0xFFFF, (int)m.LParam >> 16));
                if (hitTest.Location == ListViewHitTestLocations.None)
                {
                    return;
                }
            }
            base.WndProc(ref m);
        }
    }

    public partial class FrmPluginManager : Form
    {
        private readonly PluginManager _pluginManager;
        private readonly List<PluginInfo> _pluginInfos = new List<PluginInfo>();
        private int _lastSortColumn = -1;
        private bool _sortAscending = true;
        private bool _toggleFromCheckbox;
        private bool _suppressItemCheck;
        
        private System.Windows.Forms.Button btnManage;
        private System.Windows.Forms.Button btnRefresh;
        private System.Windows.Forms.Button btnBrowse;
        private NonResizableListView listView;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.ColumnHeader colName;
        private System.Windows.Forms.ColumnHeader colVersion;
        private System.Windows.Forms.ColumnHeader colStatus;
        private System.Windows.Forms.ColumnHeader colType;
        private System.Windows.Forms.ColumnHeader colActions;

        internal FrmPluginManager(PluginManager pluginManager)
        {
            try
            {
                _pluginManager = pluginManager;
                InitializeComponent();
                LoadPlugins();
                SetupEventHandlers();
                
                this.ShowInTaskbar = true;
                this.TopMost = false;
                
                try
                {
                    var res = new System.ComponentModel.ComponentResourceManager(typeof(FrmSettings));
                    var settingsIcon = res.GetObject("$this.Icon") as Icon;
                    this.Icon = settingsIcon ?? this.Owner?.Icon ?? Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                    this.ShowIcon = true;
                }
                catch { }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing plugin manager: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw;
            }
        }

        private void InitializeComponent()
        {
            this.btnManage = new System.Windows.Forms.Button();
            this.btnRefresh = new System.Windows.Forms.Button();
            this.btnBrowse = new System.Windows.Forms.Button();
            this.listView = new NonResizableListView();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnClose = new System.Windows.Forms.Button();
            this.lblTitle = new System.Windows.Forms.Label();
            this.colName = new System.Windows.Forms.ColumnHeader();
            this.colVersion = new System.Windows.Forms.ColumnHeader();
            this.colStatus = new System.Windows.Forms.ColumnHeader();
            this.colType = new System.Windows.Forms.ColumnHeader();
            this.colActions = new System.Windows.Forms.ColumnHeader();
            this.SuspendLayout();
            
            this.btnManage.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left;
            this.btnManage.Location = new System.Drawing.Point(12, 45);
            this.btnManage.Name = "btnManage";
            this.btnManage.Size = new System.Drawing.Size(80, 30);
            this.btnManage.TabIndex = 0;
            this.btnManage.Text = "Manage";
            this.btnManage.UseVisualStyleBackColor = true;
            
            this.btnRefresh.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left;
            this.btnRefresh.Location = new System.Drawing.Point(100, 45);
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Size = new System.Drawing.Size(80, 30);
            this.btnRefresh.TabIndex = 1;
            this.btnRefresh.Text = "Refresh";
            this.btnRefresh.UseVisualStyleBackColor = true;
            
            this.btnBrowse.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            this.btnBrowse.Location = new System.Drawing.Point(580, 45);
            this.btnBrowse.Name = "btnBrowse";
            this.btnBrowse.Size = new System.Drawing.Size(120, 30);
            this.btnBrowse.TabIndex = 2;
            this.btnBrowse.Text = "Browse Plugins";
            this.btnBrowse.UseVisualStyleBackColor = true;
            
            this.listView.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.listView.CheckBoxes = true;
            this.listView.FullRowSelect = true;
            this.listView.GridLines = false;
            this.listView.Location = new System.Drawing.Point(12, 85);
            this.listView.Name = "listView";
            this.listView.Size = new System.Drawing.Size(688, 350);
            this.listView.TabIndex = 3;
            this.listView.UseCompatibleStateImageBehavior = false;
            this.listView.View = System.Windows.Forms.View.Details;
            this.listView.Sorting = System.Windows.Forms.SortOrder.None;
            this.listView.AllowColumnReorder = false;
            this.listView.OwnerDraw = true;
            this.listView.DrawColumnHeader += new System.Windows.Forms.DrawListViewColumnHeaderEventHandler(this.listView_DrawColumnHeader);
            this.listView.DrawItem += new System.Windows.Forms.DrawListViewItemEventHandler(this.listView_DrawItem);
            this.listView.DrawSubItem += new System.Windows.Forms.DrawListViewSubItemEventHandler(this.listView_DrawSubItem);
            this.listView.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(this.listView_ColumnClick);
            this.listView.MouseClick += new System.Windows.Forms.MouseEventHandler(this.listView_MouseClick);
            this.listView.DoubleClick += new System.EventHandler(this.listView_DoubleClick);
            this.listView.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            var rowHeightImages = new ImageList();
            rowHeightImages.ImageSize = new Size(1, 28);
            this.listView.SmallImageList = rowHeightImages;
            
            this.colName.Text = "Plugin Name";
            this.colName.Width = 320;
            
            this.colVersion.Text = "Version";
            this.colVersion.Width = 110;
            
            this.colStatus.Text = "Status";
            this.colStatus.Width = 110;
            
            this.colType.Text = "Type";
            this.colType.Width = 200;
            
            this.colActions.Text = "Actions";
            this.colActions.Width = 125;
            this.colActions.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            
            this.btnSave.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            this.btnSave.Location = new System.Drawing.Point(500, 450);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(120, 30);
            this.btnSave.TabIndex = 4;
            this.btnSave.Text = "Save & Restart";
            this.btnSave.UseVisualStyleBackColor = true;
            
            this.btnClose.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            this.btnClose.Location = new System.Drawing.Point(630, 450);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(80, 30);
            this.btnClose.TabIndex = 5;
            this.btnClose.Text = "Close";
            this.btnClose.UseVisualStyleBackColor = true;
            
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblTitle.Location = new System.Drawing.Point(12, 12);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(120, 21);
            this.lblTitle.TabIndex = 6;
            this.lblTitle.Text = "Plugin Manager";
            
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(720, 500);
            this.Controls.Add(this.lblTitle);
            this.Controls.Add(this.btnClose);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.listView);
            this.Controls.Add(this.btnBrowse);
            this.Controls.Add(this.btnRefresh);
            this.Controls.Add(this.btnManage);
            this.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
            this.MaximizeBox = true;
            this.MinimizeBox = true;
            this.MinimumSize = new System.Drawing.Size(600, 400);
            this.Name = "FrmPluginManager";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Plugin Manager";
            this.ResumeLayout(false);
            this.PerformLayout();

            this.listView.Columns.Add(this.colName);
            this.listView.Columns.Add(this.colVersion);
            this.listView.Columns.Add(this.colStatus);
            this.listView.Columns.Add(this.colType);
            this.listView.Columns.Add(this.colActions);
            
            this.listView.Resize += (s, e) => AdjustColumnWidths();
            this.Resize += (s, e) => AdjustColumnWidths();
            AdjustColumnWidths();

            try
            {
                DarkModeManager.ApplyDarkMode(this);
                ScreenCaptureHider.ScreenCaptureHider.Apply(this.Handle);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Dark mode application failed: {ex.Message}");
            }
        }

        private void AdjustColumnWidths()
        {
            try
            {
                int totalWidth = this.listView.ClientSize.Width;
                if (totalWidth <= 0) return;

                int versionWidth = 110;
                int statusWidth = 110;
                int actionsBaseWidth = this.colActions.Width > 0 ? this.colActions.Width : 100;
                int padding = 0;

                int fixedWithoutActions = versionWidth + statusWidth + padding;
                int remaining = Math.Max(120, totalWidth - fixedWithoutActions);

                int nameWidth = (int)(remaining * 0.6);
                int typeWidth = remaining - nameWidth;

                if (nameWidth < 160) nameWidth = 160;
                if (typeWidth < 120) typeWidth = 120;

                int usedWithoutActions = nameWidth + typeWidth + versionWidth + statusWidth + padding;
                int actionsWidth = Math.Max(actionsBaseWidth, totalWidth - usedWithoutActions);

                int sum = usedWithoutActions + actionsWidth;
                if (sum > totalWidth)
                {
                    int overflow = sum - totalWidth;
                    int reduceName = Math.Min(overflow, Math.Max(0, nameWidth - 120));
                    nameWidth -= reduceName;
                    overflow -= reduceName;
                    if (overflow > 0)
                    {
                        int reduceType = Math.Min(overflow, Math.Max(0, typeWidth - 100));
                        typeWidth -= reduceType;
                    }
                    usedWithoutActions = nameWidth + typeWidth + versionWidth + statusWidth + padding;
                    actionsWidth = Math.Max(actionsBaseWidth, totalWidth - usedWithoutActions);
                }

                this.colName.Width = nameWidth;
                this.colVersion.Width = versionWidth;
                this.colStatus.Width = statusWidth;
                this.colType.Width = typeWidth;
                this.colActions.Width = actionsWidth;
            }
            catch { }
        }

        private void SetupEventHandlers()
        {
            this.btnManage.Click += BtnManage_Click;
            this.btnRefresh.Click += (s, e) => LoadPlugins();
            this.btnBrowse.Click += BtnBrowse_Click;
            this.btnSave.Click += (s, e) => SaveChanges();
            this.btnClose.Click += (s, e) => this.Close();
            this.listView.ItemChecked += ListView_ItemChecked;
            this.listView.ItemCheck += ListView_ItemCheck_Filter;
            this.listView.MouseDown += ListView_MouseDownForCheck;
            _pluginManager.PluginsChanged += PluginManager_PluginsChanged;
        }

        private void ListView_MouseDownForCheck(object sender, MouseEventArgs e)
        {
            try
            {
                var hit = this.listView.HitTest(e.Location);
                _toggleFromCheckbox = (hit.Location == ListViewHitTestLocations.StateImage);
            }
            catch { _toggleFromCheckbox = false; }
        }

        private void ListView_ItemCheck_Filter(object sender, ItemCheckEventArgs e)
        {
            if (_suppressItemCheck)
            {
                return;
            }
            if (!_toggleFromCheckbox)
            {
                e.NewValue = e.CurrentValue;
            }
            _toggleFromCheckbox = false;
        }

        private void BtnManage_Click(object sender, EventArgs e)
        {
            try
            {
                var pluginsDir = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "Plugins");
                if (!Directory.Exists(pluginsDir))
                {
                    Directory.CreateDirectory(pluginsDir);
                }
                System.Diagnostics.Process.Start("explorer.exe", pluginsDir);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open plugins folder: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnBrowse_Click(object sender, EventArgs e)
        {
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "https://t.me/pulsarplugins",
                    UseShellExecute = true
                };
                System.Diagnostics.Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open plugin market: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ListView_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            if (e.Item?.Tag is PluginInfo pluginInfo)
            {
                pluginInfo.Enabled = e.Item.Checked;
                UpdatePluginStatus(pluginInfo);
            }
        }

        private void SaveChanges()
        {
            try
            {
                var changes = new List<string>();
                
                foreach (var pluginInfo in _pluginInfos)
                {
                    bool hasDll = File.Exists(pluginInfo.FilePath);
                    bool hasDisabled = File.Exists(pluginInfo.DisabledPath);
                    bool isCurrentlyEnabled = hasDll && !hasDisabled;
                    
                    if (pluginInfo.Enabled != isCurrentlyEnabled)
                    {
                        if (pluginInfo.Enabled)
                        {
                            if (!hasDll && hasDisabled)
                            {
                                File.Move(pluginInfo.DisabledPath, pluginInfo.FilePath);
                                changes.Add($"Enabled {pluginInfo.Name}");
                            }
                            else if (hasDll && hasDisabled)
                            {
                                try { File.Delete(pluginInfo.DisabledPath); } catch { }
                                changes.Add($"Enabled {pluginInfo.Name}");
                            }
                        }
                        else
                        {
                            if (hasDll && !hasDisabled)
                            {
                                File.Move(pluginInfo.FilePath, pluginInfo.DisabledPath);
                                changes.Add($"Disabled {pluginInfo.Name}");
                            }
                            else if (hasDll && hasDisabled)
                            {
                                try { File.Delete(pluginInfo.FilePath); } catch { }
                                changes.Add($"Disabled {pluginInfo.Name}");
                            }
                        }
                    }
                }
                
                // Always reload plugins and refresh the list, even if no changes
                try { _pluginManager.ReloadPlugins(); } catch { }
                LoadPlugins();

                if (changes.Count > 0)
                {
                    var result = MessageBox.Show($"Changes saved:\n{string.Join("\n", changes)}\n\nRestart Pulsar to apply changes?", 
                        "Plugin Manager", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    
                    if (result == DialogResult.Yes)
                    {
                        var currentExe = Application.ExecutablePath;
                        var startInfo = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = currentExe,
                            UseShellExecute = true
                        };
                        
                        System.Diagnostics.Process.Start(startInfo);
                        Application.Exit();
                    }
                }
                else
                {
                    MessageBox.Show("Plugins reloaded successfully.", 
                        "Plugin Manager", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save changes: {ex.Message}", 
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PluginManager_PluginsChanged(object sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => LoadPlugins()));
            }
            else
            {
                LoadPlugins();
            }
        }

        private void LoadPlugins()
        {
            try
            {
                this._suppressItemCheck = true;
                this.listView.BeginUpdate();
                this.listView.Items.Clear();
                _pluginInfos.Clear();

                var enabledPlugins = _pluginManager.Plugins.ToList();
                var pluginsDir = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "Plugins");
                
                if (!Directory.Exists(pluginsDir))
                {
                    return;
                }

                var allDlls = Directory.EnumerateFiles(pluginsDir, "*.*", SearchOption.TopDirectoryOnly)
                    .Where(f => f.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
                        || f.EndsWith(".dll.disabled", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(Path.GetFileName)
                    .ToList();

                var byBase = new Dictionary<string, (bool hasDll, bool hasDisabled, string dllPath, string disabledPath)>();
                foreach (var path in allDlls)
                {
                    bool isDisabled = path.EndsWith(".disabled", StringComparison.OrdinalIgnoreCase);
                    var basePath = isDisabled ? path.Substring(0, path.Length - 9) : path;
                    if (!string.Equals(Path.GetExtension(basePath), ".dll", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!byBase.TryGetValue(basePath, out var entry)) entry = (false, false, basePath, basePath + ".disabled");
                    if (isDisabled) entry.hasDisabled = true; else entry.hasDll = true;
                    byBase[basePath] = entry;
                }

                foreach (var kv in byBase.OrderBy(k => Path.GetFileName(k.Key)))
                {
                    var basePath = kv.Key;
                    var entry = kv.Value;
                    var pluginName = Path.GetFileNameWithoutExtension(basePath);

                    bool isEnabledFs = File.Exists(entry.dllPath) && !File.Exists(entry.disabledPath);
                    string loadPath = isEnabledFs && File.Exists(entry.dllPath) ? entry.dllPath : (File.Exists(entry.disabledPath) ? entry.disabledPath : entry.dllPath);
                    
                    var pluginInfo = new PluginInfo
                    {
                        Name = pluginName,
                        FilePath = entry.dllPath,
                        DisabledPath = entry.disabledPath,
                        Enabled = isEnabledFs,
                        Version = GetPluginVersion(loadPath),
                        Description = GetPluginDescription(loadPath),
                        Type = GetPluginType(loadPath)
                    };
                    _pluginInfos.Add(pluginInfo);
                }

                var sortedPlugins = _pluginInfos
                    .OrderBy(p => p.Enabled ? 0 : 1)
                    .ThenBy(p => p.Name)
                    .ToList();

                foreach (var plugin in sortedPlugins)
                {
                    bool enabledFs = false;
                    try { enabledFs = File.Exists(plugin.FilePath) && !File.Exists(plugin.DisabledPath); } catch { enabledFs = plugin.Enabled; }
                    var item = new ListViewItem(plugin.Name)
                    {
                        Tag = plugin,
                        Checked = enabledFs
                    };
                    item.SubItems.Add(plugin.Version);
                    item.SubItems.Add(enabledFs ? "Enabled" : "Disabled");
                    item.SubItems.Add(plugin.Type);
                    item.SubItems.Add("⚙");

                    if (!enabledFs)
                    {
                        item.ForeColor = Color.FromArgb(150, 150, 150);
                    }
                    else
                    {
                        item.ForeColor = Color.White;
                    }

                    this.listView.Items.Add(item);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Plugin manager load error: {ex.Message}");
            }
            finally
            {
                this.listView.EndUpdate();
                this._suppressItemCheck = false;
            }
        }

        private void UpdatePluginStatus(PluginInfo pluginInfo)
        {
            var item = this.listView.Items.Cast<ListViewItem>().FirstOrDefault(i => i.Tag == pluginInfo);
            if (item != null)
            {
                bool enabledFs = false;
                try { enabledFs = File.Exists(pluginInfo.FilePath) && !File.Exists(pluginInfo.DisabledPath); } catch { enabledFs = pluginInfo.Enabled; }
                item.SubItems[2].Text = enabledFs ? "Enabled" : "Disabled";
                item.ForeColor = enabledFs ? Color.White : Color.FromArgb(150, 150, 150);
                item.Checked = enabledFs;
            }
        }

        private string GetPluginVersion(string path)
        {
            try
            {
                if (path.EndsWith(".pulsar", StringComparison.OrdinalIgnoreCase))
                {
                    using (var fs = File.OpenRead(path))
                    using (var zip = new System.IO.Compression.ZipArchive(fs, System.IO.Compression.ZipArchiveMode.Read))
                    {
                        var dll = SelectFirstServerDll(zip) ?? zip.Entries.FirstOrDefault(e => e.FullName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase));
                        if (dll == null) return "Unknown";
                        using (var s = dll.Open())
                        using (var ms = new MemoryStream())
                        {
                            s.CopyTo(ms);
                            var asm = System.Reflection.Assembly.Load(ms.ToArray());
                            var v = asm.GetName().Version;
                            return v?.ToString() ?? "Unknown";
                        }
                    }
                }

                var assembly = System.Reflection.Assembly.LoadFrom(path);
                var version = assembly.GetName().Version;
                return version?.ToString() ?? "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }

        private string GetPluginDescription(string path)
        {
            try
            {
                if (path.EndsWith(".pulsar", StringComparison.OrdinalIgnoreCase))
                {
                    using (var fs = File.OpenRead(path))
                    using (var zip = new System.IO.Compression.ZipArchive(fs, System.IO.Compression.ZipArchiveMode.Read))
                    {
                        var dll = SelectFirstServerDll(zip) ?? zip.Entries.FirstOrDefault(e => e.FullName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase));
                        if (dll == null) return "Unknown plugin";
                        using (var s = dll.Open())
                        using (var ms = new MemoryStream())
                        {
                            s.CopyTo(ms);
                            var asm = System.Reflection.Assembly.Load(ms.ToArray());
                            var pluginTypes = asm.GetTypes().Where(t => typeof(IServerPlugin).IsAssignableFrom(t) && !t.IsAbstract);
                            foreach (var type in pluginTypes)
                            {
                                var instance = Activator.CreateInstance(type) as IServerPlugin;
                                if (instance != null)
                                {
                                    return $"Plugin: {instance.Name} v{instance.Version}";
                                }
                            }
                        }
                    }
                }

                var assembly = System.Reflection.Assembly.LoadFrom(path);
                var types = assembly.GetTypes().Where(t => typeof(IServerPlugin).IsAssignableFrom(t) && !t.IsAbstract);
                foreach (var type in types)
                {
                    var instance = Activator.CreateInstance(type) as IServerPlugin;
                    if (instance != null)
                    {
                        return $"Plugin: {instance.Name} v{instance.Version}";
                    }
                }
                return "Unknown plugin type";
            }
            catch
            {
                return "Invalid plugin";
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _pluginManager.PluginsChanged -= PluginManager_PluginsChanged;
            base.OnFormClosing(e);
        }

        private void listView_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            if (e.Column == _lastSortColumn)
            {
                _sortAscending = !_sortAscending;
            }
            else
            {
                _sortAscending = true;
            }
            
            _lastSortColumn = e.Column;
            SortListView(e.Column);
        }

        private void SortListView(int columnIndex)
        {
            this.listView.BeginUpdate();
            
            var items = new List<ListViewItem>();
            foreach (ListViewItem item in this.listView.Items)
            {
                items.Add(item);
            }
            
            items.Sort((x, y) =>
            {
                string xText = x.SubItems[columnIndex].Text;
                string yText = y.SubItems[columnIndex].Text;
                
                int result;
                
                switch (columnIndex)
                {
                    case 0:
                    case 1:
                    case 3:
                        result = string.Compare(xText, yText, StringComparison.OrdinalIgnoreCase);
                        break;
                    case 2:
                        bool xEnabled = xText.Equals("Enabled", StringComparison.OrdinalIgnoreCase);
                        bool yEnabled = yText.Equals("Enabled", StringComparison.OrdinalIgnoreCase);
                        result = xEnabled.CompareTo(yEnabled);
                        break;
                    default:
                        result = string.Compare(xText, yText, StringComparison.OrdinalIgnoreCase);
                        break;
                }
                
                return _sortAscending ? result : -result;
            });
            
            this.listView.Items.Clear();
            foreach (var item in items)
            {
                this.listView.Items.Add(item);
            }
            
            this.listView.EndUpdate();
            this.listView.Invalidate();
        }

        private void listView_MouseClick(object sender, MouseEventArgs e)
        {
            var hitTest = this.listView.HitTest(e.Location);
            if (hitTest.Item != null && hitTest.SubItem != null)
            {
                if (hitTest.SubItem == hitTest.Item.SubItems[4])
                {
                    if (hitTest.Item.Tag is PluginInfo pluginInfo)
                    {
                        ShowPluginDetails(pluginInfo);
                    }
                }
            }
        }

        private void ShowPluginDetails(PluginInfo pluginInfo)
        {
            var detailsForm = new Form
            {
                Text = $"Plugin Details - {pluginInfo.Name}",
                Size = new Size(500, 400),
                StartPosition = FormStartPosition.CenterParent,
                Owner = this,
                Icon = this.Icon
            };

            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White
            };

            var nameLabel = new Label
            {
                Text = $"Plugin Name: {pluginInfo.Name}",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Location = new Point(10, 10),
                Size = new Size(460, 25),
                ForeColor = Color.White
            };

            var versionLabel = new Label
            {
                Text = $"Version: {pluginInfo.Version}",
                Font = new Font("Segoe UI", 10),
                Location = new Point(10, 45),
                Size = new Size(460, 20),
                ForeColor = Color.White
            };

            var typeLabel = new Label
            {
                Text = $"Type: {pluginInfo.Type}",
                Font = new Font("Segoe UI", 10),
                Location = new Point(10, 70),
                Size = new Size(460, 20),
                ForeColor = Color.White
            };

            var statusLabel = new Label
            {
                Text = $"Status: {(pluginInfo.Enabled ? "Enabled" : "Disabled")}",
                Font = new Font("Segoe UI", 10),
                Location = new Point(10, 95),
                Size = new Size(460, 20),
                ForeColor = pluginInfo.Enabled ? Color.LightGreen : Color.LightCoral
            };

            var descriptionLabel = new Label
            {
                Text = $"Description: {pluginInfo.Description}",
                Font = new Font("Segoe UI", 10),
                Location = new Point(10, 120),
                Size = new Size(460, 60),
                ForeColor = Color.White
            };

            var uninstallButton = new Button
            {
                Text = "Uninstall",
                Location = new Point(10, 200),
                Size = new Size(100, 30),
                BackColor = Color.FromArgb(60, 60, 63),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            uninstallButton.Click += (s, ev) =>
            {
                var confirm = MessageBox.Show($"Remove {pluginInfo.Name}? This will delete the DLL.", "Confirm uninstall", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (confirm != DialogResult.Yes) return;
                try
                {
                    if (System.IO.File.Exists(pluginInfo.FilePath)) System.IO.File.Delete(pluginInfo.FilePath);
                    if (System.IO.File.Exists(pluginInfo.DisabledPath)) System.IO.File.Delete(pluginInfo.DisabledPath);
                    try { _pluginManager.ReloadPlugins(); } catch { }
                    LoadPlugins();
                    detailsForm.Close();
                }
                catch (Exception exDel)
                {
                    MessageBox.Show($"Failed to uninstall: {exDel.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            var closeButton = new Button
            {
                Text = "Close",
                Location = new Point(370, 200),
                Size = new Size(100, 30),
                BackColor = Color.FromArgb(60, 60, 63),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            closeButton.Click += (s, ev) => detailsForm.Close();

            panel.Controls.AddRange(new Control[] { nameLabel, versionLabel, typeLabel, statusLabel, descriptionLabel, uninstallButton, closeButton });
            detailsForm.Controls.Add(panel);

            detailsForm.ShowDialog();
        }

        private void listView_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Any(f => f.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".pulsar", StringComparison.OrdinalIgnoreCase)))
                {
                    e.Effect = DragDropEffects.Copy;
                    return;
                }
            }
            e.Effect = DragDropEffects.None;
        }

        private void listView_DragDrop(object sender, DragEventArgs e)
        {
            try
            {
                if (e.Data == null || !e.Data.GetDataPresent(DataFormats.FileDrop)) return;
                var files = ((string[])e.Data.GetData(DataFormats.FileDrop)) ?? new string[0];
                var dlls = files.Where(f => f.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)).ToList();
                var packages = files.Where(f => f.EndsWith(".pulsar", StringComparison.OrdinalIgnoreCase)).ToList();
                if (dlls.Count == 0 && packages.Count == 0) return;

                var pluginsDir = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath), "Plugins");
                if (!System.IO.Directory.Exists(pluginsDir)) System.IO.Directory.CreateDirectory(pluginsDir);

                int copied = 0;
                foreach (var src in dlls)
                {
                    try
                    {
                        var dest = System.IO.Path.Combine(pluginsDir, System.IO.Path.GetFileName(src));
                        if (string.Equals(src, dest, StringComparison.OrdinalIgnoreCase)) continue;
                        if (System.IO.File.Exists(dest))
                        {
                            var overwrite = MessageBox.Show($"{System.IO.Path.GetFileName(dest)} already exists. Overwrite?", "Confirm overwrite", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                            if (overwrite != DialogResult.Yes) continue;
                        }
                        System.IO.File.Copy(src, dest, true);
                        copied++;
                    }
                    catch (Exception exCopy)
                    {
                        MessageBox.Show($"Failed to copy {System.IO.Path.GetFileName(src)}: {exCopy.Message}", "Copy Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }

                foreach (var pkg in packages)
                {
                    try
                    {
                        PluginPackageImporter.Import(pkg, pluginsDir);
                        copied++;
                    }
                    catch (Exception exPkg)
                    {
                        MessageBox.Show($"Failed to import {System.IO.Path.GetFileName(pkg)}: {exPkg.Message}", "Import Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }

                if (copied > 0)
                {
                    try { _pluginManager.ReloadPlugins(); } catch { }
                    LoadPlugins();
                    MessageBox.Show($"Installed {copied} plugin(s).", "Plugins", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to install plugins: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void listView_DoubleClick(object sender, EventArgs e)
        {
            if (this.listView.SelectedItems.Count == 1)
            {
                var item = this.listView.SelectedItems[0];
                if (item?.Tag is PluginInfo pi)
                {
                    ShowPluginDetails(pi);
                }
            }
        }

        private void listView_DrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
        {
            bool isSorted = e.ColumnIndex == _lastSortColumn;
            
            Color backgroundColor = isSorted ? Color.FromArgb(70, 130, 180) : Color.FromArgb(60, 60, 63);
            Color textColor = isSorted ? Color.White : Color.White;
            
            Rectangle headerFill = e.Bounds;
            if (e.ColumnIndex == this.listView.Columns.Count - 1)
            {
                headerFill = new Rectangle(e.Bounds.X, e.Bounds.Y, this.listView.ClientSize.Width - e.Bounds.X, e.Bounds.Height);
            }
            using (SolidBrush brush = new SolidBrush(backgroundColor))
            {
                e.Graphics.FillRectangle(brush, headerFill);
            }
            
            var headerFlags = TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine |
                               (e.Header == this.colActions ? TextFormatFlags.HorizontalCenter : TextFormatFlags.Left);
            Rectangle headerBounds = e.Bounds;
            if (e.Header == this.colStatus)
            {
                headerBounds = new Rectangle(e.Bounds.X + 30, e.Bounds.Y, Math.Max(0, e.Bounds.Width - 30), e.Bounds.Height);
            }
            TextRenderer.DrawText(e.Graphics, e.Header.Text, e.Font, headerBounds, textColor, headerFlags);
            
            if (e.ColumnIndex == this.listView.Columns.Count - 1)
            {
                int right = this.listView.ClientSize.Width;
                int fillWidth = Math.Max(0, right - e.Bounds.Right);
                if (fillWidth > 0)
                {
                    using (SolidBrush brush = new SolidBrush(backgroundColor))
                    {
                        e.Graphics.FillRectangle(brush, new Rectangle(e.Bounds.Right, e.Bounds.Top, fillWidth, e.Bounds.Height));
                    }
                }
            }
        }

        private void listView_DrawItem(object sender, DrawListViewItemEventArgs e)
        {
            e.DrawDefault = true;
        }

        private void listView_DrawSubItem(object sender, DrawListViewSubItemEventArgs e)
        {
            bool? enabledFsOverride = null;
            if (e.ColumnIndex == 2 && e.Item?.Tag is PluginInfo tpi)
            {
                try { enabledFsOverride = File.Exists(tpi.FilePath) && !File.Exists(tpi.DisabledPath); } catch { enabledFsOverride = null; }
                if (enabledFsOverride.HasValue)
                {
                    e.SubItem.Text = enabledFsOverride.Value ? "Enabled" : "Disabled";
                }
            }

            Color textColor = (enabledFsOverride.HasValue && !enabledFsOverride.Value)
                ? Color.FromArgb(150, 150, 150)
                : e.Item.ForeColor;
            
            Rectangle rowFill = e.Bounds;
            if (e.ColumnIndex == this.listView.Columns.Count - 1)
            {
                rowFill = new Rectangle(e.Bounds.X, e.Bounds.Y, this.listView.ClientSize.Width - e.Bounds.X, e.Bounds.Height);
            }
            using (SolidBrush brush = new SolidBrush(e.Item.BackColor))
            {
                e.Graphics.FillRectangle(brush, rowFill);
            }
            
            var cellFlags = TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine |
                            (e.ColumnIndex == 4 ? TextFormatFlags.HorizontalCenter : TextFormatFlags.Left);
            Rectangle cellBounds = e.Bounds;
            if (e.ColumnIndex == 2)
            {
                cellBounds = new Rectangle(e.Bounds.X + 30, e.Bounds.Y, Math.Max(0, e.Bounds.Width - 30), e.Bounds.Height);
            }
            TextRenderer.DrawText(e.Graphics, e.SubItem.Text, e.Item.Font, cellBounds, textColor, cellFlags);

            if (e.ColumnIndex == this.listView.Columns.Count - 1)
            {
                int right = this.listView.ClientSize.Width;
                int fillWidth = Math.Max(0, right - e.Bounds.Right);
                if (fillWidth > 0)
                {
                    using (SolidBrush brush = new SolidBrush(e.Item.BackColor))
                    {
                        e.Graphics.FillRectangle(brush, new Rectangle(e.Bounds.Right, e.Bounds.Top, fillWidth, e.Bounds.Height));
                    }
                }
            }
        }

        private string GetPluginType(string path)
        {
            try
            {
                if (path.EndsWith(".pulsar", StringComparison.OrdinalIgnoreCase))
                {
                    using (var fs = File.OpenRead(path))
                    using (var zip = new System.IO.Compression.ZipArchive(fs, System.IO.Compression.ZipArchiveMode.Read))
                    {
                        var dll = SelectFirstServerDll(zip) ?? zip.Entries.FirstOrDefault(e => e.FullName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase));
                        if (dll == null) return "Unknown";
                        using (var s = dll.Open())
                        using (var ms = new MemoryStream())
                        {
                            s.CopyTo(ms);
                            var asm = System.Reflection.Assembly.Load(ms.ToArray());
                            var typesInPackage = asm.GetTypes();
                            bool hasServerPluginPkg = typesInPackage.Any(t => typeof(IServerPlugin).IsAssignableFrom(t) && !t.IsAbstract);
                            bool hasClientPluginPkg = typesInPackage.Any(t => t.Name.Contains("Client") || t.Name.Contains("ClientModule"));
                            if (hasServerPluginPkg && hasClientPluginPkg) return "Server & Client";
                            if (hasServerPluginPkg) return "Server";
                            if (hasClientPluginPkg) return "Client";
                            return "Unknown";
                        }
                    }
                }

                var assembly = System.Reflection.Assembly.LoadFrom(path);
                var types = assembly.GetTypes();
                
                bool hasServerPlugin = types.Any(t => typeof(IServerPlugin).IsAssignableFrom(t) && !t.IsAbstract);
                bool hasClientPlugin = types.Any(t => t.Name.Contains("Client") || t.Name.Contains("ClientModule"));
                
                if (hasServerPlugin && hasClientPlugin)
                    return "Server & Client";
                else if (hasServerPlugin)
                    return "Server";
                else if (hasClientPlugin)
                    return "Client";
                else
                    return "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }

        private static System.IO.Compression.ZipArchiveEntry SelectFirstServerDll(System.IO.Compression.ZipArchive zip)
        {
            try
            {
                var manifest = zip.Entries.FirstOrDefault(e => e.FullName.Equals("plugin.json", StringComparison.OrdinalIgnoreCase));
                if (manifest == null) return null;
                using (var r = new StreamReader(manifest.Open()))
                {
                    var text = r.ReadToEnd();
                    var idx = text.IndexOf("\"server\"", StringComparison.OrdinalIgnoreCase);
                    if (idx < 0) return null;
                    var start = text.IndexOf('[', idx);
                    if (start < 0) return null;
                    var end = text.IndexOf(']', start);
                    if (end < 0) return null;
                    var slice = text.Substring(start + 1, end - start - 1);
                    int pos = 0;
                    while (pos < slice.Length)
                    {
                        var q1 = slice.IndexOf('"', pos);
                        if (q1 < 0) break;
                        var q2 = slice.IndexOf('"', q1 + 1);
                        if (q2 < 0) break;
                        var rel = slice.Substring(q1 + 1, q2 - q1 - 1).Trim();
                        var fname = Path.GetFileName(rel);
                        var entry = zip.Entries.FirstOrDefault(e => Path.GetFileName(e.FullName).Equals(fname, StringComparison.OrdinalIgnoreCase));
                        if (entry != null) return entry;
                        pos = q2 + 1;
                    }
                }
            }
            catch { }
            return null;
        }

        private class PluginInfo
        {
            public string Name { get; set; }
            public string FilePath { get; set; }
            public string DisabledPath { get; set; }
            public bool Enabled { get; set; }
            public string Version { get; set; }
            public string Description { get; set; }
            public string Type { get; set; }
        }
    }
}