using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using SmartPackager;

namespace EMI
{
    using Network;
    using NGC;
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
        internal delegate IRPCReturn MicroFunc(INGCArray array);
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

#if DebugPro
        private readonly Dictionary<int, (string, int)> RegisteredMethodsName = new Dictionary<int, (string, int)>();
        private readonly Dictionary<int,string> RegisteredForwardingName = new Dictionary<int, string>();
#endif
        /// <summary>
        /// Получает список зарегистрированных функций
        /// </summary>
        /// <returns></returns>
        public KeyValuePair<int,(string,int)>[] GetRegisteredMethodsName()
        {
#if DebugPro
            return RegisteredMethodsName.ToArray();
#else
            throw new NotSupportedException();
#endif
        }

        /// <summary>
        /// Получает список зарегистрированных функций (Forwarding)
        /// </summary>
        /// <returns></returns>
        public KeyValuePair<int, string>[] GetRegisteredForwardingName()
        {
#if DebugPro
            return RegisteredForwardingName.ToArray();
#else
            throw new NotSupportedException();
#endif
        }

        /// <summary>
        /// Вызывается когда изменён список зарегистрированных методов
        /// </summary>
        public event Action OnChangedRegisteredMethods;
        /// <summary>
        /// Вызывается когда изменён список зарегистрированных методов
        /// </summary>
        public event Action OnChangedRegisteredMethodsForwarding;

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
        /// <param name="indicator"></param>
        /// <param name="micro"></param>
        /// <returns></returns>
        private IRPCRemoveHandle RegisterMethodHelp(AIndicator indicator, MicroFunc micro)
        {
            int id = indicator.ID;
            lock (this)
            {
                if (RegisteredMethods.ContainsKey(id))
                {
                    RegisteredMethods[id] += micro;
#if DebugPro
                    var dat = RegisteredMethodsName[id];
                    RegisteredMethodsName[id] = (dat.Item1, dat.Item2 + 1);
                    OnChangedRegisteredMethods?.Invoke();
#endif
                }
                else
                {
                    RegisteredMethods.Add(id, micro);
#if DebugPro
                    RegisteredMethodsName.Add(id, (indicator.Name,1));
                    OnChangedRegisteredMethods?.Invoke();
#endif
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
#if DebugPro
                RegisteredForwardingName.Add(indicator.ID, indicator.Name);
                OnChangedRegisteredMethodsForwarding?.Invoke();
#endif
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
            return RegisterMethodHelp(indicator, (INGCArray array) =>
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
            return RegisterMethodHelp(indicator, (INGCArray array) =>
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
            return RegisterMethodHelp(indicator, (INGCArray array) =>
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
            return RegisterMethodHelp(indicator, (INGCArray array) =>
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
                    {
                        deleg -= Micro;
#if DebugPro
                        var dat = RPC.RegisteredMethodsName[ID];
                        RPC.RegisteredMethodsName[ID] = (dat.Item1, dat.Item2 - 1);
                        RPC.OnChangedRegisteredMethods?.Invoke();
#endif
                    }
                    else
                    {
                        RPC.RegisteredMethods.Remove(ID);
#if DebugPro
                        RPC.RegisteredMethodsName.Remove(ID);
                        RPC.OnChangedRegisteredMethods?.Invoke();
#endif
                    }

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
#if DebugPro
                    RPC.RegisteredForwardingName.Remove(ID);
                    RPC.OnChangedRegisteredMethodsForwarding?.Invoke();
#endif
                    RPC = null;
                }
            }
        }
    }
}