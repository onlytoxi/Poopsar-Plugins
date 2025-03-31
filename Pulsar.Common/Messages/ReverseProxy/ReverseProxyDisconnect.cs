using ProtoBuf;
using Pulsar.Common.Messages.other;

namespace Pulsar.Common.Messages.ReverseProxy
{
    [ProtoContract]
    public class ReverseProxyDisconnect : IMessage
    {
        [ProtoMember(1)]
        public int ConnectionId { get; set; }
    }
}
