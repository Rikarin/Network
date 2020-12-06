using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Reflection.Emit;
using System.Collections.Generic;

namespace Rikarin.Network.Communication {
    public class InterfaceWrapper {
        readonly IDictionary<string, ModuleBuilder> _builder = new Dictionary<string, ModuleBuilder>();
        readonly IDictionary<Type, Type> _types = new Dictionary<Type, Type>();
        readonly IRpcClient _rpcClient;

        public InterfaceWrapper(IRpcClient rpcClient) {
            _rpcClient = rpcClient;
        }

        public T CreateInstance<T>() {
            return (T)Activator.CreateInstance(GenerateInterfaceType<T>(), _rpcClient);
        }

        Type GenerateInterfaceType<T>() {
            var sourceType = typeof(T);
            var originalAssemblyName = sourceType.Assembly.GetName().Name;

            Type newType;
            if (_types.TryGetValue(sourceType, out newType)) {
                return newType;
            }

            if (!sourceType.IsInterface) {
                throw new ArgumentException("Type T is not an interface");
            }

            ModuleBuilder moduleBuilder;
            if (!_builder.TryGetValue(originalAssemblyName, out moduleBuilder)) {
                var newAssemblyName = new AssemblyName(Guid.NewGuid() + "." + originalAssemblyName);
                var dynamicAssembly = AssemblyBuilder.DefineDynamicAssembly(newAssemblyName, AssemblyBuilderAccess.Run);
                moduleBuilder = dynamicAssembly.DefineDynamicModule(newAssemblyName.Name);

                _builder.Add(originalAssemblyName, moduleBuilder);
            }

            var typeBuilder = moduleBuilder.DefineType(
                sourceType.FullName,
                TypeAttributes.Public | TypeAttributes.Class,
                typeof(object),
                new[] { sourceType }
            );

            var rpcClientField = typeBuilder.DefineField("_rpcClient", typeof(IRpcClient), FieldAttributes.Private);

            // Generate contructor
            var ctor = typeBuilder.DefineConstructor(
                MethodAttributes.Public,
                CallingConventions.Standard | CallingConventions.HasThis,
                new Type[] { typeof(IRpcClient) }
            );

            var ctorBody = ctor.GetILGenerator();
            ctorBody.Emit(OpCodes.Ldarg_0); // push this
            ctorBody.Emit(OpCodes.Ldarg_1); // push ctor arg 0
            ctorBody.Emit(OpCodes.Stfld, rpcClientField); // set field
            ctorBody.Emit(OpCodes.Ret);

            // Generate methods
            var interfaces = new List<Type>();
            IEnumerable<Type> subList = new[] { sourceType };
            while (subList.Count() != 0) {
                interfaces.AddRange(subList);
                subList = subList.SelectMany(i => i.GetInterfaces());
            }

            interfaces = interfaces.Distinct().ToList();
            var call = typeof(IRpcClient).GetMethod("Call");

            foreach (var method in interfaces.SelectMany(y => y.GetMethods())) {
                if (method.ReturnType != typeof(void) && !typeof(Task).IsAssignableFrom(method.ReturnType)) {
                    Console.WriteLine(method.ReturnType);
                    throw new ArgumentException("Interface methods should return void or Task");
                }

                var newMethod = typeBuilder.DefineMethod(
                    method.Name,
                    method.Attributes ^ MethodAttributes.Abstract,
                    method.CallingConvention,
                    method.ReturnType,
                    method.ReturnParameter.GetRequiredCustomModifiers(),
                    method.ReturnParameter.GetOptionalCustomModifiers(),
                    method.GetParameters().Select(p => p.ParameterType).ToArray(),
                    method.GetParameters().Select(p => p.GetRequiredCustomModifiers()).ToArray(),
                    method.GetParameters().Select(p => p.GetOptionalCustomModifiers()).ToArray()
                );

                bool hasReturnValue = method.ReturnType != typeof(void);
                var methodBody = newMethod.GetILGenerator();

                // Create array
                var arr = methodBody.DeclareLocal(typeof(object[]));
                methodBody.Emit(OpCodes.Ldc_I4, method.GetParameters().Length + 2);
                methodBody.Emit(OpCodes.Newarr, typeof(object));
                methodBody.Emit(OpCodes.Stloc, arr);

                // Push Interface Name
                methodBody.Emit(OpCodes.Ldloc, arr);
                methodBody.Emit(OpCodes.Ldc_I4, 0);
                methodBody.Emit(OpCodes.Ldstr, sourceType.FullName);
                methodBody.Emit(OpCodes.Box, typeof(string));
                methodBody.Emit(OpCodes.Stelem_Ref);

                // Push Method Name
                methodBody.Emit(OpCodes.Ldloc, arr);
                methodBody.Emit(OpCodes.Ldc_I4, 1);
                methodBody.Emit(OpCodes.Ldstr, method.Name);
                methodBody.Emit(OpCodes.Box, typeof(string));
                methodBody.Emit(OpCodes.Stelem_Ref);

                // Push args to the array
                for (int i = 0; i < method.GetParameters().Length; i++) {
                    var paramType = method.GetParameters()[i].ParameterType;

                    methodBody.Emit(OpCodes.Ldloc, arr);
                    methodBody.Emit(OpCodes.Ldc_I4, i + 2);

                    methodBody.Emit(OpCodes.Ldarg, i + 1);
                    if (!paramType.IsClass) {
                        methodBody.Emit(OpCodes.Box, paramType);
                    }

                    methodBody.Emit(OpCodes.Stelem_Ref);
                }
                
                // Call
                methodBody.Emit(OpCodes.Ldarg_0);
                methodBody.Emit(OpCodes.Ldfld, rpcClientField);
                methodBody.Emit(OpCodes.Ldstr, method.ReturnType.ToString());
                methodBody.Emit(OpCodes.Ldloc, arr);
                methodBody.Emit(OpCodes.Call, call);

                if (!hasReturnValue) {
                    methodBody.Emit(OpCodes.Pop);
                }
                methodBody.Emit(OpCodes.Ret);
            }

            newType = typeBuilder.CreateType();
            _types.Add(sourceType, newType);

            return newType;
        }
    }
}