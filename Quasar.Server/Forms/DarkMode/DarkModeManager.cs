using DarkModeForms;
using Quasar.Server.Models;
using System.Windows.Forms;

namespace Quasar.Server.Forms.DarkMode
{
    public class DarkModeManager
    {
        private static readonly DarkModeCS.DisplayMode lightMode = DarkModeCS.DisplayMode.ClearMode;
        private static readonly DarkModeCS.DisplayMode darkMode = DarkModeCS.DisplayMode.DarkMode;
        public static void ApplyDarkMode(Form form)
        {
            bool isDarkModeChecked = Settings.DarkMode;

            DarkModeCS _ = new DarkModeCS(form)
            {
                ColorMode = isDarkModeChecked ? darkMode : lightMode,
                ColorizeIcons = false,
            };
        }
    }
}
