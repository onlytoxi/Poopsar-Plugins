using ProtoBuf;
using Pulsar.Common.Messages.other;
using Pulsar.Common.Models;

namespace Pulsar.Common.Messages.Administration.TCPConnections
{
    [ProtoContract]
    public class GetConnectionsResponse : IMessage
    {
        [ProtoMember(1)]
        public TcpConnection[] Connections { get; set; }
    }
}
