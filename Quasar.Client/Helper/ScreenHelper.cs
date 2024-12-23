using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using Quasar.Client.Utilities;
using System.Runtime.InteropServices;


namespace Quasar.Client.Helper
{
    public static class ScreenHelper
    {
        const Int32 CURSOR_SHOWING = 0x00000001;

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct CURSORINFO
        {
            public int cbSize;
            public int flags;
            public IntPtr hCursor;
            public POINT ptScreenPos;
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetCursorInfo(out CURSORINFO pci);

        [DllImport("user32.dll")]
        public static extern bool DrawIcon(IntPtr hDC, int X, int Y, IntPtr hIcon);

        public static Bitmap CaptureScreen(int screenNumber)
        {
            Rectangle bounds = Screen.AllScreens[screenNumber].Bounds;
            Bitmap result;
            try
            {
                Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);
                using (Graphics graphics = Graphics.FromImage(bitmap))
                {
                    graphics.CopyFromScreen(bounds.X, bounds.Y, 0, 0, bounds.Size, CopyPixelOperation.SourceCopy);

                    CURSORINFO cursorinfo;
                    cursorinfo.cbSize = Marshal.SizeOf(typeof(CURSORINFO));
                    if (GetCursorInfo(out cursorinfo))
                    {
                        if (cursorinfo.flags == CURSOR_SHOWING)
                        {
                            DrawIcon(graphics.GetHdc(), cursorinfo.ptScreenPos.x - bounds.X, cursorinfo.ptScreenPos.y - bounds.Y, cursorinfo.hCursor);
                            graphics.ReleaseHdc();
                        }
                    }
                    result = bitmap;
                }
            }
            catch
            {
                result = new Bitmap(bounds.Width, bounds.Height);
            }
            return result;
        }

        public static Rectangle GetBounds(int screenNumber)
        {
            return Screen.AllScreens[screenNumber].Bounds;
        }
    }
}
