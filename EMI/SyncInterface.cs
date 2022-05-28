using EMI.MyException;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Reflection.Emit;

namespace EMI
{
    using Indicators;
    using System.Linq.Expressions;

    /// <summary>
    /// Создаёт синхронизированный клиент-серверный интерфейс
    /// </summary>
    public static class SyncInterfaceOLD
    {
        private readonly static ModuleBuilder MBuilder = InitModuleBuilder();
        private readonly static MethodInfo[] RunCodes = typeof(SyncInterfaceRunCode).GetMethods();

        private static ModuleBuilder InitModuleBuilder()
        {
            AssemblyName aName = new AssemblyName("EMI.SyncInterface");
            AppDomain appDomain = Thread.GetDomain();
            AssemblyBuilder aBuilder = appDomain.DefineDynamicAssembly(aName, AssemblyBuilderAccess.Run);

            return aBuilder.DefineDynamicModule(aName.Name);
        }

        /// <summary>
        /// Создаёт ссылку на удалённый класс
        /// </summary>
        /// <typeparam name="T">Интерфейс который наследуется классом</typeparam>
        /// <param name="client"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public static T CreateIndicator<T>(Client client, string name) where T : class
        {
            Type interfaceType = typeof(T);
            if (!interfaceType.IsInterface)
            {
                //throw new InvalidInterfaceException("A generic type is not an interface");
            }

            TypeBuilder tBuilder = MBuilder.DefineType(name, TypeAttributes.Public | TypeAttributes.Class);
            RefAction InitFull = new RefAction();
            tBuilder.AddInterfaceImplementation(interfaceType);
            
            var methods = interfaceType.GetMethods();


            foreach (var method in methods)
            {
                var atr = method.Attributes;
                var paramInfoTypes = method.GetParameters();
                var paramTypes = (from infoType in paramInfoTypes select infoType.ParameterType).ToArray();
                var attr = MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig | MethodAttributes.NewSlot;
                var methodBuilder = tBuilder.DefineMethod(method.Name, attr, method.CallingConvention, method.ReturnType, paramTypes);

                MethodInfo methodCompile;
                int GenericArgumentsCount = paramTypes.Length;

                if (method.ReturnType == typeof(void))
                {
                    var funcType = (from code in RunCodes
                                    where code.Name == "Func" &&
                                    code.GetGenericArguments().Length == GenericArgumentsCount
                                    select code).First();
                    if (funcType.IsGenericMethod)
                        methodCompile = funcType.MakeGenericMethod(paramTypes);
                    else
                        methodCompile = funcType;
                }
                else
                {
                    var funcType = typeof(SyncInterfaceRunCode).GetMethod("FuncOut");
                    Type[] mgmTypes = new Type[paramTypes.Length + 1];
                    mgmTypes[0] = method.ReturnType;
                    for (int i = 0; i < paramTypes.Length; i++)
                        mgmTypes[i + 1] = paramTypes[i];
                    methodCompile = funcType.MakeGenericMethod(mgmTypes);
                }
                methodCompile.Invoke(null, new object[] { client, methodBuilder, tBuilder, InitFull});
                tBuilder.DefineMethodOverride(methodBuilder, method);
            }
            var SyncClass = (T)Activator.CreateInstance(tBuilder.CreateType());
            InitFull.Action();
            return SyncClass;
        }

        /// <summary>
        /// Не реализованно
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Interface"></param>
        /// <param name="rpc"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public static IRPCRemoveHandle RegisterInterface<T>(T Interface, RPC rpc, string name)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Позволяет удалить зарегистрированный интерфейс
        /// </summary>
        public class RemoveHandleSyncInterface : IRPCRemoveHandle
        {
            private bool IsRemoved = false;
            private RPC RPC;

            internal RemoveHandleSyncInterface(RPC rpc)
            {
                RPC = rpc;
            }

            /// <summary>
            /// Удаляет интерфейс из списка зарегестрированных (его больше нельзя будет вызвать)
            /// </summary>
            public void Remove()
            {
                lock (RPC)
                {
                    if (IsRemoved)
                        throw new AlreadyException();
                    IsRemoved = true;

                    RPC = null;
                    throw new NotImplementedException();
                }

            }
        }

        internal class RefAction
        {
            public Action Action;
        }
    }

    internal static class SyncInterfaceRunCode
    {
        private static string NameOF(MethodBuilder builder, string headerName) =>
            $"SyncInterface::{headerName}::{builder.Module.FullyQualifiedName}.{builder.Name}";

        public static void Func(Client client, MethodBuilder builder, TypeBuilder typeBuilder, SyncInterfaceOLD.RefAction initFull)
        {
            var func = new Indicator.Func(NameOF(builder, nameof(Func)));
            RPCfunc code = () => func.RCall(client).Wait();
            FieldBuilder codeField = typeBuilder.DefineField(code.ToString(), code.GetType(), FieldAttributes.Private | FieldAttributes.Static);
            var il = builder.GetILGenerator();
            var m = code.GetType().GetMethod("Invoke");

            //il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldsfld, codeField);
            il.Emit(OpCodes.Callvirt, m);
            il.Emit(OpCodes.Ret);

            initFull.Action += () => codeField.DeclaringType.GetField(code.ToString(), BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, code);
        }

        public static void Func<T1>(Client client, MethodBuilder builder, TypeBuilder typeBuilder)
        {
            var func = new Indicator.Func<T1>(NameOF(builder, $"{nameof(Func)}<{nameof(T1)}>"));
            RPCfunc code = () => Console.WriteLine("out put");  //func.RCall(t1, client).Wait();
            var il = builder.GetILGenerator();
            //il.Emit(OpCodes.Ldarg,0);
            //il.Emit(OpCodes.Constrained, code.Method);
            il.Emit(OpCodes.Callvirt, code.Method);
        }


        public static void FuncOut<T0>(Client client, MethodBuilder builder, TypeBuilder typeBuilder)
        {
            var func = new Indicator.FuncOut<T0>(NameOF(builder, $"{nameof(FuncOut)}<{nameof(T0)}>"));
            RPCfuncOut<T0> code = () => func.RCall(client).Result;
            var call = Expression.Call(code.Method);
            Expression.Return(Expression.Label(typeof(T0)), call);
            Expression.Lambda(call).CompileToMethod(builder);
        }


        public static void FuncOut<T0, T1>(Client client, MethodBuilder builder, TypeBuilder typeBuilder)
        {
            var func = new Indicator.FuncOut<T0, T1>(NameOF(builder, $"{nameof(FuncOut)}<{nameof(T0)},{nameof(T1)}>"));
            RPCfuncOut<T0, T1> code = (t1) => func.RCall(t1, client).Result;
            var p1 = Expression.Parameter(typeof(T1));
            var call = Expression.Call(code.Method, p1);
            Expression.Return(Expression.Label(typeof(T0)), call);
            Expression.Lambda(call).CompileToMethod(builder);
        }
    }
}