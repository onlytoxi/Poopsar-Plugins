using ProtoBuf;
using Pulsar.Common.Messages.other;

namespace Pulsar.Common.Messages.Webcam
{
    [ProtoContract]
    public class GetAvailableWebcamsResponse : IMessage
    {
        [ProtoMember(1)]
        public string[] Webcams { get; set; }
    }
}
