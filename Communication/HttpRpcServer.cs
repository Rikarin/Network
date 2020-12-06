using System;
using Newtonsoft.Json;
using System.Text;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace Rikarin.Communication {
    class HttpRpcServer {
        readonly IServiceProvider _serviceProvider;

        public HttpRpcServer(IServiceProvider serviceProvider) {
            _serviceProvider = serviceProvider;
        }

        public void HandleRequest(string payload) {
            var output = JsonConvert.DeserializeObject(payload, new JsonSerializerSettings {
                TypeNameHandling = TypeNameHandling.All
            }) as object[];

            var interfaceType = Type.GetType(output[0].ToString());
            var method = interfaceType.GetMethod(output[1].ToString());

            var service = _serviceProvider.GetRequiredService(interfaceType);
            method.Invoke(service, output.Skip(2).ToArray());
        }
    }
}