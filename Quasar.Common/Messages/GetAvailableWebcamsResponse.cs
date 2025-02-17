using ProtoBuf;

namespace Quasar.Common.Messages
{
    [ProtoContract]
    public class GetAvailableWebcamsResponse : IMessage
    {
        [ProtoMember(1)]
        public string[] Webcams { get; set; }
    }
}
