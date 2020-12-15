using System;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Newtonsoft.Json;

namespace Rikarin.Network.ServiceBus.Kafka {
    // Kafka Consumer (1 group, 1 topic)
    internal class KafkaGroupManager {
        readonly IConsumer<Ignore, string> _consumer;
        readonly CancellationTokenSource _cancellationTokenSource = new();
        readonly KafkaServiceBus _serviceBus;

        public Type Group { get; }

        public KafkaGroupManager(
            KafkaServiceBus serviceBus,
            Type group
        ) {
            var config = new ConsumerConfig {
                BootstrapServers = serviceBus.Settings.Hostname,
                // GroupId = group.FullName.Replace('.', '_').ToLower(),
                GroupId = group.FullName,
                EnableAutoCommit = false,
                AutoOffsetReset = AutoOffsetReset.Earliest
            };

            Group = group;
            _serviceBus = serviceBus;
            _consumer = new ConsumerBuilder<Ignore, string>(config).Build();

            Setup();
        }

        public void SubscribeTopic(Type topic) {
            Console.WriteLine($"Subscribing to {topic.FullName} group {Group.FullName}");
            _consumer.Subscribe(topic.FullName);
        }

        public void UnsubscribeTopic(Type topic) {
            // TODO
        }

        void Setup() {
            var cancellationToken = _cancellationTokenSource.Token;

            // TODO: maybe exec the handler outside of Task.Run
            Task.Run(() => {
                System.Console.WriteLine("listening");
                while (!cancellationToken.IsCancellationRequested) {
                    try {
                        Console.WriteLine("consuming...");
                        var result = _consumer.Consume(cancellationToken);
                        Console.WriteLine("yummi yummi");
                        var message = (ICommand)JsonConvert.DeserializeObject(result.Message.Value, _serviceBus.JsonSerializerSettings);

                        if (message == null) {
                            throw new Exception("Message base type is not ICommand");
                        }

                        System.Console.WriteLine($"consumed message {result.Message.Value}");
                        
                        _serviceBus.HandleMessage(message);
                        _consumer.Commit();
                    } catch (ConsumeException e) {
                        // TODO: log exception
                        Console.WriteLine(e);
                    } catch (Exception e) {
                        Console.WriteLine(e);
                    }
                }

                Console.WriteLine($"Task for group=${Group} is ending...");
                // TODO: catch OperationCanceledException ??
                // TODO: commit even if exception was thrown?
            });
        }
    }
}