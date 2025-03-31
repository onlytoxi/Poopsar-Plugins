using ProtoBuf;
using Pulsar.Common.Messages.other;

namespace Pulsar.Common.Messages
{
    [ProtoContract]
    public class GetDebugLog : IMessage
    {
        [ProtoMember(1)]
        public string Log { get; set; }
    }
}
