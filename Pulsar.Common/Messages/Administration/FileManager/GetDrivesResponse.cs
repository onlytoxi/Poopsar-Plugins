using ProtoBuf;
using Pulsar.Common.Messages.other;
using Pulsar.Common.Models;

namespace Pulsar.Common.Messages.Administration.FileManager
{
    [ProtoContract]
    public class GetDrivesResponse : IMessage
    {
        [ProtoMember(1)]
        public Drive[] Drives { get; set; }
    }
}
