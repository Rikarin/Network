using System.Threading.Tasks;

namespace Rikarin.Network.ServiceBus {
    public interface IMessageHandlerContext {
        Task PublishAsync(ICommand message);
    }
}