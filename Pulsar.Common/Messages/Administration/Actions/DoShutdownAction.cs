using ProtoBuf;
using Pulsar.Common.Enums;
using Pulsar.Common.Messages.other;

namespace Pulsar.Common.Messages.Administration.Actions
{
    [ProtoContract]
    public class DoShutdownAction : IMessage
    {
        [ProtoMember(1)]
        public ShutdownAction Action { get; set; }
    }
}
