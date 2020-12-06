using System.Threading.Tasks;

namespace Rikarin.Network.Communication {
    public interface IRpcClient {
        Task<object> Call(string typeName, object[] args);
    }
}