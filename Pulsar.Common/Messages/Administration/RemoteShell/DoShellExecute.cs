using ProtoBuf;
using Pulsar.Common.Messages.other;

namespace Pulsar.Common.Messages.Administration.RemoteShell
{
    [ProtoContract]
    public class DoShellExecute : IMessage
    {
        [ProtoMember(1)]
        public string Command { get; set; }
    }
}
