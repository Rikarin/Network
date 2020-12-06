namespace Rikarin.Network.ServiceBus {
    public class Saga<T> where T : ContainSagaData {
        protected T Data { get; private set; }

        protected virtual void Configure() {

        }

        protected void MarkAsComplete() {

        }
    }
}