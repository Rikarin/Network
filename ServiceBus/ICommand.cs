using System;

namespace Rikarin.Network.ServiceBus {
    public interface ICommand {
        Guid CorrelationId { get; }
        // DateTime CreateTime { get; }
    }
}