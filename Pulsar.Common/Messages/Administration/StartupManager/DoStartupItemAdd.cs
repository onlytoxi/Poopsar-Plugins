using ProtoBuf;
using Pulsar.Common.Messages.other;
using Pulsar.Common.Models;

namespace Pulsar.Common.Messages
{
    [ProtoContract]
    public class DoStartupItemAdd : IMessage
    {
        [ProtoMember(1)]
        public StartupItem StartupItem { get; set; }
    }
}
