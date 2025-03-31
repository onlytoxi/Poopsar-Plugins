using ProtoBuf;
using Pulsar.Common.Messages.other;
using Pulsar.Common.Models;

namespace Pulsar.Common.Messages.FunStuff
{
    [ProtoContract]
    public class DoHideTaskbar : IMessage
    {
        [ProtoMember(1)]
        public string Message { get; set; }
    }
}
