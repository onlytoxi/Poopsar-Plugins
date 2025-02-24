using Quasar.Common.Enums;
using Quasar.Common.Messages;
using Quasar.Common.Messages.other;
using Quasar.Common.Messages.Preview;
using Quasar.Common.Messages.Webcam;
using Quasar.Common.Networking;
using Quasar.Common.Video.Codecs;
using Quasar.Server.Controls;
using Quasar.Server.Networking;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace Quasar.Server.Messages
{
    public class GetPreviewImageHandler : MessageProcessorBase<Bitmap>, IDisposable
    {
        public bool IsStarted { get; set; }

        private readonly object _syncLock = new object();
        private readonly PictureBox _box;
        private readonly Client _client;
        private UnsafeStreamCodec _codec;

        /// <summary>
        /// Used in lock statements to synchronize access to <see cref="LocalResolution"/> between UI thread and thread pool.
        /// </summary>
        private readonly object _sizeLock = new object();

        public GetPreviewImageHandler(Client client, PictureBox box) : base(true)
        {
            _box = box;
            _client = client;
            LocalResolution = box.Size;
        }

        /// <summary>
        /// The local resolution, see <seealso cref="LocalResolution"/>.
        /// </summary>
        private Size _localResolution;

        /// <summary>
        /// The local resolution in width x height. It indicates to which resolution the received frame should be resized.
        /// </summary>
        /// <remarks>
        /// This property is thread-safe.
        /// </remarks>
        public Size LocalResolution
        {
            get
            {
                lock (_sizeLock)
                {
                    return _localResolution;
                }
            }
            set
            {
                lock (_sizeLock)
                {
                    _localResolution = value;
                }
            }
        }

        public override bool CanExecute(IMessage message) => message is GetPreviewImageResponse;

        public override bool CanExecuteFrom(ISender sender) => _client.Equals(sender);

        public override void Execute(ISender sender, IMessage message)
        {
            switch (message)
            {
                case GetPreviewImageResponse frame:
                    Execute(sender, frame);
                    break;
            }
        }

        private void Execute(ISender client, GetPreviewImageResponse message)
        {
            lock (_syncLock)
            {

                if (_codec == null || _codec.ImageQuality != message.Quality || _codec.Monitor != message.Monitor || _codec.Resolution != message.Resolution)
                {
                    _codec?.Dispose();
                    _codec = new UnsafeStreamCodec(message.Quality, message.Monitor, message.Resolution);
                }

                using (MemoryStream ms = new MemoryStream(message.Image))
                {
                    try
                    {
                        Bitmap boxmap = new Bitmap(_codec.DecodeData(ms), LocalResolution);
                        OnReport(boxmap);

                        _box.Image = boxmap;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Error decoding image: " + ex.Message);
                    }

                }

                message.Image = null;

                //client.Send(new GetDesktop { Quality = message.Quality, DisplayIndex = message.Monitor });
            }
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
                lock (_syncLock)
                {
                    _codec?.Dispose();
                    IsStarted = false;
                }
            }
        }
    }
}