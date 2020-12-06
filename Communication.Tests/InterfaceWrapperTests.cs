using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Rikarin.Network.Communication;

namespace Rikarin.Network.Communication.Tests {
    [TestClass]
    public class InterfaceWrapperTests {
        [TestMethod]
        public async Task Test1() {
            var rpcClient = new RpcClientMock();
            var wrapper = new InterfaceWrapper(rpcClient);

            var mocked = wrapper.CreateInstance<IFooBar>();

            await mocked.DoSomething(Guid.NewGuid(), "foo bar");
            await mocked.GetName(42);
            await mocked.GetValue(rpcClient);
            mocked.Nothing();
        }
    }

    class RpcClientMock : IRpcClient {
        public Task<object> Call(string typeName, object[] args) {
            Console.WriteLine($"Called typeName={typeName}");
            return Task.FromResult(new object());
        }
    }

    public interface IFooBar {
        Task DoSomething(Guid guid, String name);
        Task<string> GetName(long id);
        Task<int> GetValue(IRpcClient client);
        void Nothing();
    }
}