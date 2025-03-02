using ProtoBuf;
using Quasar.Common.Messages.other;
using Quasar.Common.Video;

namespace Quasar.Common.Messages.Monitoring.RemoteDesktop
{
    [ProtoContract]
    public class GetDesktopResponse : IMessage
    {
        [ProtoMember(1)]
        public byte[] Image { get; set; }

        [ProtoMember(2)]
        public int Quality { get; set; }

        [ProtoMember(3)]
        public int Monitor { get; set; }

        [ProtoMember(4)]
        public Resolution Resolution { get; set; }

        [ProtoMember(5)]
        public bool Renew { get; set; }

        [ProtoMember(6)]
        public long Timestamp { get; set; }

        [ProtoMember(7)]
        public bool IsLastRequestedFrame { get; set; }
    }
}
