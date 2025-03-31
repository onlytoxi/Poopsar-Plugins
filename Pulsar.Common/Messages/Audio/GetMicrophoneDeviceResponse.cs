using ProtoBuf;
using Pulsar.Common.Messages.other;
using System;
using System.Collections.Generic;

namespace Pulsar.Common.Messages.Audio
{
    [ProtoContract]
    public class GetMicrophoneDeviceResponse : IMessage
    {
        [ProtoMember(1)]
        public List<Tuple<int, string>> DeviceInfos { get; set; }
    }
}
