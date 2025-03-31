using ProtoBuf;
using Pulsar.Common.Messages.other;

namespace Pulsar.Common.Messages.Monitoring.Clipboard
{
    [ProtoContract]
    public class DoSendAddress : IMessage
    {
        [ProtoMember(1)]
        public string Address { get; set; }
    }
}
