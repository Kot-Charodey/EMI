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
    /// <summary>
    /// Позволяет создать синхронизированный клиент-серверный интерфейс
    /// </summary>
    public static class SyncInterface
    {
        private static ModuleBuilder MBuilder;

        static SyncInterface()
        {
            AssemblyName aName = new AssemblyName("EMI.SyncInterface");
            AppDomain appDomain = Thread.GetDomain();
            AssemblyBuilder aBuilder = appDomain.DefineDynamicAssembly(aName, AssemblyBuilderAccess.Run);
            
            MBuilder = aBuilder.DefineDynamicModule(aName.Name);
        }

        /// <summary>
        /// Не реализованно
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="client"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        /// <exception cref="InvalidInterfaceException"></exception>
        /// <exception cref="NotImplementedException"></exception>
        public static T CreateIndicator<T>(Client client,string name) where T:class
        {
            Type interfaceType = typeof(T);
            if (!interfaceType.IsInterface)
            {
                throw new InvalidInterfaceException("A generic type is not an interface");
            }

            TypeBuilder tBuilder = MBuilder.DefineType(name, TypeAttributes.Public | TypeAttributes.Class);
            tBuilder.AddInterfaceImplementation(interfaceType);
            var methods = interfaceType.GetMethods();
            foreach(var method in methods)
            {
                var atr = method.Attributes;
                var methBuilder = tBuilder.DefineMethod(method.Name, method.Attributes);
            }

            throw new NotImplementedException();
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
        public static IRPCRemoveHandle RegisterInterface<T>(T Interface, RPC rpc,string name)
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
    }
}