using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Text;
using System.Net.Http;

namespace Rikarin.Communication {
    class HttpRpcClient {
        public string Hostname { get; }

        public HttpRpcClient(string hostname) {
            Hostname = hostname;
        }

        public async Task<object> Call(string typeName, object[] args) {
            var payload = JsonConvert.SerializeObject(args, new JsonSerializerSettings {
                TypeNameHandling = TypeNameHandling.All
            });

            var content = new StringContent(payload, Encoding.UTF8, "application/json");
            var result = await new HttpClient().PostAsync($"{Hostname}/rpc", content);

            var returnType = Type.GetType(typeName);
            return JsonConvert.DeserializeObject(await result.Content.ReadAsStringAsync(), returnType);
        }
    }
}