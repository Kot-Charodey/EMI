using EMI.MyException;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace EMI
{
    /// <summary>
    /// Позволяет создать синхронизированный клиент-серверный интерфейс
    /// </summary>
    public static class SyncInterface
    {
        private static AssemblyName AssemblySyncInterface = new AssemblyName("EMI.SyncInterface");

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
            if (!typeof(T).IsInterface)
            {
                throw new InvalidInterfaceException("A generic type is not an interface");
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
