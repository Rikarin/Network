using System.Threading.Tasks;

namespace Rikarin.Network.ServiceBus {
    public interface IHandleMessage { }

    public interface IHandleMessage<T> : IHandleMessage where T : ICommand {
        Task Handle(T message, IMessageHandlerContext context);
    }
}