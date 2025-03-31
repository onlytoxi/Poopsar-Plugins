using ProtoBuf;
using Pulsar.Common.Messages.other;

namespace Pulsar.Common.Messages.Administration.RegistryEditor
{
    [ProtoContract]
    public class DoCreateRegistryKey : IMessage
    {
        [ProtoMember(1)]
        public string ParentPath { get; set; }
    }
}
