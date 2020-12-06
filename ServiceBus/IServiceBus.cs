using System;
using System.Threading;
using System.Threading.Tasks;

namespace Rikarin.Network.ServiceBus {
    public interface IServiceBus {
        ServiceBusSettings Settings { get; }

        void ConfigureGroup(Type group, Type topic);
        void ConfigureGroup<T, U>() where T : IServiceGroup where U : IServiceTopic;

        void ConfigureTopic(Type topic, Type message);
        void ConfigureTopic<T, U>() where T : IServiceTopic where U : ICommand;

        void Register(Type handler);
        void Register<T>() where T : IHandleMessage;

        Task PublishAsync(ICommand message);
        Task<T> RequestAsync<T>(ICommand message, CancellationToken token = default(CancellationToken)) where T : ICommand;
    }
}