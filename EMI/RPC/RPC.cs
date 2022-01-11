using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using SmartPackager;

namespace EMI
{
    using Network;
    using ProBuffer;
    using Indicators;
    using MyException;
    using RPCInternal;
    /// <summary>
    /// Позволяет производить удалённый вызов процедур
    /// </summary>
    public class RPC
    {
        /// <summary>
        /// Содержит в себе код по запуску функции
        /// </summary>
        /// <param name="array">необработанный пакет данных</param>
        /// <returns></returns>
        internal delegate IRPCReturn MicroFunc(IReleasableArray array);
        /// <summary>
        /// Зарегестрированные функции - используется при вызове
        /// </summary>
        private readonly Dictionary<int, MicroFunc> RegisteredMethods = new Dictionary<int, MicroFunc>();

        internal RPC()
        {
        }

        /// <summary>
        /// Пытается получить функцию по айди - если не получиться вернёт null (потоко-безопасен)
        /// </summary>
        /// <param name="ID"></param>
        /// <returns></returns>
        internal MicroFunc TryGetRegisteredMethod(int ID)
        {
            lock (this)
            {
                RegisteredMethods.TryGetValue(ID, out var fun);
                return fun;
            }
        }

        internal static string GetDelegateName(Delegate deleg)
        {
            return $"{deleg.Method.DeclaringType.FullName}.{deleg.Method.Name}";
        }

        /// <summary>
        /// Производит основную регистрацию метода (остаётся только написать микрофункцию)
        /// </summary>
        /// <param name="method"></param>
        /// <param name="micro"></param>
        /// <returns></returns>
        private RemoveHandle RegisterMethodHelp(Delegate method, MicroFunc micro)
        {
            string name = GetDelegateName(method);
            int id = name.DeterministicGetHashCode();
            lock (this)
            {
                if (RegisteredMethods.ContainsKey(id))
                {
                    RegisteredMethods[id] += micro;
                }
                else
                {
                    RegisteredMethods.Add(id, micro);
                }
                return new RemoveHandle(id, micro, this);
            }
        }

        #region RegisterMethod
        /// <summary>
        /// Регестрирует метод для возможности вызвать его
        /// </summary>
        /// <param name="method">метод</param>
        public RemoveHandle RegisterMethod(RPCfunc method)
        {
            return RegisterMethodHelp(method, (IReleasableArray array) =>
            {
                try
                {
                    method();
                }
                catch (Exception e)
                {
                    Console.WriteLine("EMI RPC => " + e);
                }
                return null;
            });
        }

        /// <summary>
        /// Регестрирует метод для возможности вызвать его
        /// </summary>
        /// <param name="method">метод</param>
        public RemoveHandle RegisterMethod<T1>(RPCfunc<T1> method)
        {
            var packager = Packager.Create<T1>();
            return RegisterMethodHelp(method, (IReleasableArray array) =>
             {
                 packager.UnPack(array.Bytes, array.Offset, out T1 t1);
                 try
                 {
                     method(t1);
                 }
                 catch (Exception e)
                 {
                     Console.WriteLine("EMI RPC => " + e);
                 }
                 return null;
             });
        }
        #endregion
        #region RegisterMethodReturned
        /// <summary>
        /// Регестрирует метод для возможности вызвать его
        /// </summary>
        /// <param name="method">метод</param>
        public RemoveHandle RegisterMethod<Tout>(RPCfuncOut<Tout> method)
        {
            var @out = RPCReturn<Tout>.Create();
            return RegisterMethodHelp(method, (IReleasableArray array) =>
            {
                Tout data;
                try
                {
                    data = method();
                }
                catch (Exception e)
                {
                    data = default;
                    Console.WriteLine("EMI RPC => " + e);
                }
                @out.Set(data);
                return @out;
            });
        }

        /// <summary>
        /// Регестрирует метод для возможности вызвать его
        /// </summary>
        /// <param name="method">метод</param>
        public RemoveHandle RegisterMethod<Tout,T1>(RPCfuncOut<Tout,T1> method)
        {
            var packager = Packager.Create<T1>();
            var @out = RPCReturn<Tout>.Create();
            return RegisterMethodHelp(method, (IReleasableArray array) =>
            {
                packager.UnPack(array.Bytes, array.Offset, out T1 t1);
                Tout data;
                try
                {
                    data = method(t1);
                }
                catch (Exception e)
                {
                    data = default;
                    Console.WriteLine("EMI RPC => " + e);
                }
                @out.Set(data);
                return @out;
            });
        }
        #endregion

        /// <summary>
        /// Позволяет удалить зарегистрированный метод
        /// </summary>
        public class RemoveHandle
        {
            private readonly int ID;
            private RPC RPC;
            private MicroFunc Micro;
            private bool IsRemoved = false;

            internal RemoveHandle(int id, MicroFunc micro, RPC rpc)
            {
                ID = id;
                Micro = micro;
                RPC = rpc;
            }

            /// <summary>
            /// Удаляет метод из списка зарегестрированных (его больше нельзя буджет вызвать)
            /// </summary>
            public void Remove()
            {
                lock (RPC)
                {
                    if (IsRemoved)
                        throw new AlreadyException();
                    IsRemoved = true;

                    var deleg = RPC.RegisteredMethods[ID];
                    if (deleg.GetInvocationList().GetLength(0) > 1)
                        deleg -= Micro;
                    else
                        RPC.RegisteredMethods.Remove(ID);

                    RPC = null;
                    Micro = null;
                    
                }
            }
        }
    }
}