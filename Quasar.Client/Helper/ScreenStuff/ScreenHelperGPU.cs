using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Device = SharpDX.Direct3D11.Device;

namespace Quasar.Client.Helper.ScreenStuff
{
    public static class ScreenHelperGPU
    {
        private static Device _device;
        private static OutputDuplication _outputDuplication;
        private static Texture2D _stagingTexture;
        private static int _currentScreenIndex = -1;
        private static string _currentDeviceName = string.Empty;

        private const int SRCCOPY = 0x00CC0020;
        private const int CURSOR_SHOWING = 0x00000001;

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct CURSORINFO
        {
            public int cbSize;
            public int flags;
            public IntPtr hCursor;
            public POINT ScreenPosition;
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetCursorInfo(out CURSORINFO pci);

        [DllImport("user32.dll")]
        private static extern bool DrawIcon(IntPtr hDC, int X, int Y, IntPtr hIcon);

        public static Bitmap CaptureScreen(int screenIndex)
        {
            if (screenIndex < 0 || screenIndex >= Screen.AllScreens.Length)
                throw new ArgumentOutOfRangeException(nameof(screenIndex), "Invalid screen index.");

            var screen = Screen.AllScreens[screenIndex];

            if (screenIndex != _currentScreenIndex || _device == null || _currentDeviceName != screen.DeviceName)
            {
                Cleanup();
                InitializeDirectX(screen);
                _currentScreenIndex = screenIndex;
                _currentDeviceName = screen.DeviceName;
            }

            return TryCapture(screen.Bounds);
        }

        private static Bitmap TryCapture(Rectangle screenBounds)
        {
            SharpDX.DXGI.Resource screenResource = null;
            try
            {
                _outputDuplication.AcquireNextFrame(500, out var frameInfo, out screenResource);
                using (var screenTexture = screenResource.QueryInterface<Texture2D>())
                {
                    _device.ImmediateContext.CopyResource(screenTexture, _stagingTexture);
                    return ConvertTextureToBitmap(_stagingTexture, screenBounds);
                }
            }
            catch (SharpDXException ex) when (ex.ResultCode.Code == SharpDX.DXGI.ResultCode.WaitTimeout.Result.Code)
            {
                return null;
            }
            finally
            {
                screenResource?.Dispose();
                _outputDuplication?.ReleaseFrame();
            }
        }

        private static void InitializeDirectX(Screen screen)
        {
            var flags = DeviceCreationFlags.BgraSupport;
            _device = new Device(SharpDX.Direct3D.DriverType.Hardware, flags);

            using (var factory = new Factory1())
            {
                Output1 output1 = null;
                foreach (var adapter in factory.Adapters1)
                {
                    foreach (var output in adapter.Outputs)
                    {
                        if (output.Description.DeviceName == screen.DeviceName)
                        {
                            output1 = output.QueryInterface<Output1>();
                            break;
                        }
                    }
                    if (output1 != null) break;
                }

                if (output1 == null)
                    throw new InvalidOperationException($"Could not find DXGI output for {screen.DeviceName}");

                _outputDuplication = output1.DuplicateOutput(_device);

                var textureDesc = new Texture2DDescription
                {
                    Width = screen.Bounds.Width,
                    Height = screen.Bounds.Height,
                    MipLevels = 1,
                    ArraySize = 1,
                    Format = Format.B8G8R8A8_UNorm,
                    SampleDescription = new SampleDescription(1, 0),
                    Usage = ResourceUsage.Staging,
                    BindFlags = BindFlags.None,
                    CpuAccessFlags = CpuAccessFlags.Read,
                    OptionFlags = ResourceOptionFlags.None
                };

                _stagingTexture = new Texture2D(_device, textureDesc);
                output1.Dispose();
            }
        }

        private static Bitmap ConvertTextureToBitmap(Texture2D texture, Rectangle screenBounds)
        {
            var deviceContext = _device.ImmediateContext;
            var dataBox = deviceContext.MapSubresource(texture, 0, MapMode.Read, SharpDX.Direct3D11.MapFlags.None);

            var bitmap = new Bitmap(screenBounds.Width, screenBounds.Height, PixelFormat.Format32bppArgb);
            var bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                                             ImageLockMode.WriteOnly, bitmap.PixelFormat);

            try
            {
                int rowPitch = dataBox.RowPitch;
                int bytesPerPixel = 4;
                int widthInBytes = bitmap.Width * bytesPerPixel;
                byte[] row = new byte[widthInBytes];

                for (int y = 0; y < bitmap.Height; y++)
                {
                    IntPtr sourcePtr = IntPtr.Add(dataBox.DataPointer, y * dataBox.RowPitch);
                    IntPtr destPtr = IntPtr.Add(bitmapData.Scan0, y * bitmapData.Stride);

                    Marshal.Copy(sourcePtr, row, 0, Math.Min(widthInBytes, dataBox.RowPitch));
                    Marshal.Copy(row, 0, destPtr, widthInBytes);
                }
            }
            finally
            {
                bitmap.UnlockBits(bitmapData);
                deviceContext.UnmapSubresource(texture, 0);
            }

            DrawCursorOnBitmap(bitmap, screenBounds);

            return bitmap;
        }

        private static void DrawCursorOnBitmap(Bitmap bitmap, Rectangle screenBounds)
        {
            var cursorInfo = new CURSORINFO { cbSize = Marshal.SizeOf(typeof(CURSORINFO)) };
            if (GetCursorInfo(out cursorInfo) && cursorInfo.flags == CURSOR_SHOWING)
            {
                // Calculate the cursor position relative to the screen bounds
                int cursorX = cursorInfo.ScreenPosition.X - screenBounds.X;
                int cursorY = cursorInfo.ScreenPosition.Y - screenBounds.Y;

                // Only draw the cursor if it's within the bounds of the bitmap
                if (cursorX >= 0 && cursorY >= 0 && cursorX < bitmap.Width && cursorY < bitmap.Height)
                {
                    using (Graphics g = Graphics.FromImage(bitmap))
                    {
                        IntPtr hdc = g.GetHdc();
                        DrawIcon(hdc, cursorX, cursorY, cursorInfo.hCursor);
                        g.ReleaseHdc(hdc);
                    }
                }
            }
        }

        private static void Cleanup()
        {
            SafeDispose(ref _stagingTexture);
            SafeDispose(ref _outputDuplication);
            SafeDispose(ref _device);
        }

        private static void SafeDispose<T>(ref T disposable) where T : class, IDisposable
        {
            if (disposable != null)
            {
                try { disposable.Dispose(); }
                catch { /* Suppress cleanup errors */ }
                disposable = null;
            }
        }

        public static Rectangle GetBounds(int screenIndex) => Screen.AllScreens[screenIndex].Bounds;
    }
}
