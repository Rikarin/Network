using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Confluent.Kafka;
using Newtonsoft.Json;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;

namespace Rikarin.Network.ServiceBus.Kafka {
    public class KafkaServiceBus : IServiceBus {
        delegate void RequestCallback(ICommand message);

        readonly IProducer<Null, string> _producer;
        readonly IServiceProvider _serviceProvider;

        readonly IDictionary<Type, KafkaGroupManager> _groups = new Dictionary<Type, KafkaGroupManager>(); // group -> group manager
        readonly IDictionary<Type, Type> _messages = new Dictionary<Type, Type>(); // message type -> topic
        readonly IDictionary<Type, Type> _handlers = new Dictionary<Type, Type>(); // message type -> handler type

        readonly IDictionary<Guid, RequestCallback> _requests = new Dictionary<Guid, RequestCallback>();

        internal JsonSerializerSettings JsonSerializerSettings => new JsonSerializerSettings {
            TypeNameHandling = TypeNameHandling.All
        };

        public ServiceBusSettings Settings { get; }

        public KafkaServiceBus(
            ServiceBusSettings settings,
            IServiceProvider serviceProvider
        ) {
            var config = new ProducerConfig {
                BootstrapServers = settings.Hostname
            };

            Settings = settings;
            _serviceProvider = serviceProvider;
            _producer = new ProducerBuilder<Null, string>(config).Build();
        }

        public void ConfigureGroup<T, U>() where T : IServiceGroup where U : IServiceTopic {
            ConfigureGroup(typeof(T), typeof(U));
        }

        public void ConfigureGroup(Type group, Type topic) {
            if (!_groups.TryGetValue(group, out var groupManager)) {
                groupManager = new KafkaGroupManager(this, group);
                _groups.Add(group, groupManager);
            }

            groupManager.SubscribeTopic(topic);
        }

        public void ConfigureTopic<T, U>() where T : IServiceTopic where U : ICommand {
            ConfigureTopic(typeof(T), typeof(U));
        }

        public void ConfigureTopic(Type topic, Type message) {
            _messages[message] = topic;
        }

        public void Register<T>() where T : IHandleMessage {
            Register(typeof(T));
        }

        public void Register(Type handler) {
            var interfaces = handler.GetInterfaces().Where(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IHandleMessage<>));
            var messages = interfaces.Select(x => x.GetGenericArguments().First());

            foreach (var message in messages) {
                _handlers.Add(message, handler);
            }
        }

        public Task<T> RequestAsync<T>(ICommand message, CancellationToken token = default(CancellationToken)) where T : ICommand {
            var tcr = new TaskCompletionSource<T>();

            _requests.Add(message.CorrelationId, (ICommand msg) => {
                tcr.SetResult((T)msg);
            });

            token.Register(() => {
                _requests.Remove(message.CorrelationId);
                tcr.TrySetCanceled();
            });

            PublishAsync(message);
            return tcr.Task;
        }

        public Task PublishAsync(ICommand message) {
            var data = JsonConvert.SerializeObject(message, JsonSerializerSettings);
            var topic = _messages.Single(x => x.Key == message.GetType()).Value.FullName;

            System.Console.WriteLine($"test {topic} data {data}");
            return _producer.ProduceAsync(topic, new Message<Null, string> {
                Value = data
            });
        }

        internal void HandleMessage(ICommand message) {
            if (_requests.TryGetValue(message.CorrelationId, out RequestCallback callback)) {
                callback(message);
                _requests.Remove(message.CorrelationId);

                return;
            }

            var handler = _handlers[message.GetType()];
            var context = new KafkaMessageHandlerContext(this, message);

            using (var scope = _serviceProvider.CreateScope()) {
                var instance = scope.ServiceProvider.GetService(handler);
                System.Console.WriteLine($"type {message.GetType()}");
                handler
                    .GetMethod("Handle", new Type[] { message.GetType(), typeof(IMessageHandlerContext) })
                    .Invoke(instance, new object[] { message, context });
            }
        }
    }
}