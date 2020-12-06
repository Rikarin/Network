using System;
using System.Threading.Tasks;

namespace Rikarin.Network.ServiceBus.Kafka {
    public class KafkaMessageHandlerContext : IMessageHandlerContext {
        readonly KafkaServiceBus _serivceBus;
        readonly ICommand _receivedMessage;

        internal KafkaMessageHandlerContext(
            KafkaServiceBus serviceBus,
            ICommand receivedMessage
        ) {
            _serivceBus = serviceBus;
            _receivedMessage = receivedMessage;
        }

        public Task PublishAsync(ICommand message) {
            return _serivceBus.PublishAsync(message);
        }
    }
}