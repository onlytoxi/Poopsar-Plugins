using ProtoBuf;
using Pulsar.Common.Messages.other;
using Pulsar.Common.Models;

namespace Pulsar.Common.Messages.FunStuff.GDI
{
    [ProtoContract]
    public class DoPixelCorrupt : IMessage
    {
        [ProtoMember(1)]
        public string Message { get; set; }
    }
}