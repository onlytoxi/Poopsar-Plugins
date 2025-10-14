using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Pulsar.Server.Networking;

namespace Pulsar.Server.Plugins
{
    public interface IServerContext
    {
        Form MainForm { get; }
        PulsarServer Server { get; }
        void Log(string message);
        void AddClientContextMenuItem(string text, Action<IReadOnlyList<Client>> onClick);
        void AddClientContextMenuItem(string text, Icon icon, Action<IReadOnlyList<Client>> onClick);
        void AddClientContextMenuItem(string section, string text, Action<IReadOnlyList<Client>> onClick);
        void AddClientContextMenuItem(string section, string text, Icon icon, Action<IReadOnlyList<Client>> onClick);
        void AddClientContextMenuItem(string[] sections, string text, Action<IReadOnlyList<Client>> onClick);
        void AddClientContextMenuItem(string[] sections, string text, Icon icon, Action<IReadOnlyList<Client>> onClick);
        void AddClientContextMenuItemPath(string path, string text, Action<IReadOnlyList<Client>> onClick);
        void AddClientContextMenuItemPath(string path, string text, Icon icon, Action<IReadOnlyList<Client>> onClick);
        void ApplyTheme(Action<Form> apply);
        void ClearPluginMenuItems();
    }
}
