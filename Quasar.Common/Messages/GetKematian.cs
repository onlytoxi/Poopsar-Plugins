using ProtoBuf;

namespace Quasar.Common.Messages
{
    [ProtoContract]
    public class GetKematian : IMessage
    {
        [ProtoMember(1)]
        public byte[] ZipFile { get; set; }
    }
}
