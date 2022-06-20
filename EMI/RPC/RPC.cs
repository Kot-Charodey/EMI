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
    /// Отвечает за регистрирование удалённых процедур для последующего вызова
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
        /// Указывает каким клиентам необходимо выполнить пересылку
        /// </summary>
        /// <param name="sendClient">какой клиент хочет произвевти пересылку</param>
        /// <returns></returns>
        public delegate Client[] ForwardingInfo(Client sendClient);
        /// <summary>
        /// Зарегестрированные функции - используется при вызове
        /// </summary>
        private readonly Dictionary<int, MicroFunc> RegisteredMethods = new Dictionary<int, MicroFunc>();
        private readonly Dictionary<int, ForwardingInfo> RegisteredForwarding = new Dictionary<int, ForwardingInfo>();

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

        /// <summary>
        /// Пытается получить функцию по айди - если не получиться вернёт null (потоко-безопасен)
        /// </summary>
        /// <param name="ID"></param>
        /// <returns></returns>
        internal ForwardingInfo TryGetRegisteredForwarding(int ID)
        {
            lock (this)
            {
                RegisteredForwarding.TryGetValue(ID, out var fun);
                return fun;
            }
        }

        /// <summary>
        /// Производит основную регистрацию метода (остаётся только написать микрофункцию)
        /// </summary>
        /// <param name="regName"></param>
        /// <param name="micro"></param>
        /// <returns></returns>
        private IRPCRemoveHandle RegisterMethodHelp(int regName, MicroFunc micro)
        {
            int id = regName;
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
                return new RemoveHandleMethod(id, micro, this);
            }
        }

        /// <summary>
        /// Регестрирует метод пересылки вызовов
        /// </summary>
        /// <param name="indicator">ссылка на функцию для вызова на указанных клиентах</param>
        /// <param name="info">функция будет вызываться при пересылке сообщений и должна вернуть список клиентов которым необходимо отправить пересылку</param>
        /// <returns></returns>
        public IRPCRemoveHandle RegisterForwarding(AIndicator indicator, ForwardingInfo info)
        {
            lock (this)
            {
                if (RegisteredForwarding.ContainsKey(indicator.ID))
                {
                    throw new AlreadyException("Forwarding has already been registered at this indicator");
                }

                RegisteredForwarding.Add(indicator.ID, info);
                return new RemoveHandleForwarding(indicator.ID, this);
            }
        }

        #region RegisterMethod
        /// <summary>
        /// Регистрирует метод для возможности вызвать его
        /// </summary>
        /// <param name="method">метод</param>
        /// <param name="indicator">имя ключа</param>
        public IRPCRemoveHandle RegisterMethod(RPCfunc method, Indicator.Func indicator)
        {
            return RegisterMethodHelp(indicator.ID, (IReleasableArray array) =>
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
        /// Регистрирует метод для возможности вызвать его
        /// </summary>
        /// <param name="method">метод</param>
        /// <param name="indicator">ссылка на метод</param>
        public IRPCRemoveHandle RegisterMethod<T1>(RPCfunc<T1> method, Indicator.Func<T1> indicator)
        {
            var packager = Packager.Create<T1>();
            return RegisterMethodHelp(indicator.ID, (IReleasableArray array) =>
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
        /// Регистрирует метод для возможности вызвать его
        /// </summary>
        /// <param name="method">метод</param>
        /// <param name="indicator">ссылка на метод</param>
        public IRPCRemoveHandle RegisterMethod<Tout>(RPCfuncOut<Tout> method, Indicator.FuncOut<Tout> indicator)
        {
            var @out = RPCReturn<Tout>.Create();
            return RegisterMethodHelp(indicator.ID, (IReleasableArray array) =>
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
        /// Регистрирует метод для возможности вызвать его
        /// </summary>
        /// <param name="method">метод</param>
        /// <param name="indicator">ссылка на метод</param>
        public IRPCRemoveHandle RegisterMethod<Tout, T1>(RPCfuncOut<Tout, T1> method, Indicator.FuncOut<Tout,T1> indicator)
        {
            var packager = Packager.Create<T1>();
            var @out = RPCReturn<Tout>.Create();
            return RegisterMethodHelp(indicator.ID, (IReleasableArray array) =>
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
        public class RemoveHandleMethod : IRPCRemoveHandle
        {
            private readonly int ID;
            private RPC RPC;
            private MicroFunc Micro;
            private bool IsRemoved = false;

            internal RemoveHandleMethod(int id, MicroFunc micro, RPC rpc)
            {
                ID = id;
                Micro = micro;
                RPC = rpc;
            }

            /// <summary>
            /// Удаляет метод из списка зарегестрированных (его больше нельзя будет вызвать)
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

        /// <summary>
        /// Позволяет удалить зарегистрированный метод
        /// </summary>
        public class RemoveHandleForwarding : IRPCRemoveHandle
        {
            private readonly int ID;
            private RPC RPC;
            private bool IsRemoved = false;

            internal RemoveHandleForwarding(int id, RPC rpc)
            {
                ID = id;
                RPC = rpc;
            }

            /// <summary>
            /// Удаляет метод из списка зарегестрированных (его больше нельзя бужет вызвать)
            /// </summary>
            public void Remove()
            {
                lock (RPC)
                {
                    if (IsRemoved)
                        throw new AlreadyException();
                    IsRemoved = true;

                    RPC.RegisteredForwarding.Remove(ID);
                    RPC = null;
                }
            }
        }
    }
}