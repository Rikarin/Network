using System;

namespace Rikarin.Network.ServiceBus {
    public abstract class Command : ICommand {
        public Guid CorrelationId { get; }

        public Command() {
            CorrelationId = Guid.NewGuid();
        }
    }
}