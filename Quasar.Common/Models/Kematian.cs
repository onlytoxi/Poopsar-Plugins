using Quasar.Common.Messages;

namespace Quasar.Common.Models
{
    public class KematianZipMessage : IMessage
    {
        public byte[] ZipFile { get; set; }
    }
}