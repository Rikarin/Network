namespace Rikarin.Network.ServiceBus {
    public class ServiceBusSettings {
        public string Hostname { get; }

        public ServiceBusSettings(string hostname) {
            Hostname = hostname;
        }
    }
}