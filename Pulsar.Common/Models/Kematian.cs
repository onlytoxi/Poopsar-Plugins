using Pulsar.Common.Messages.other;

namespace Pulsar.Common.Models
{
    public class KematianZipMessage : IMessage
    {
        public byte[] ZipFile { get; set; }
    }
}