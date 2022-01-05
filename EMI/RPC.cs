using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using SmartPackager;

namespace EMI
{
    using Packet;
    using ProBuffer;
    using MyException;
    /// <summary>
    /// Позволяет производить удалённый вызов процедур
    /// </summary>
    public class RPC
    {
        /// <summary>
        /// Содержит в себе код по запуску функции
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="array">необработанный пакет данных</param>
        /// <param name="needReturn">нуж но ли проводить упаковку для функций которые возвращают результат</param>
        /// <param name="token"></param>
        /// <returns>результат выполения (null) если нет или не надо возвращать</returns>
        internal delegate Task<IReleasableArray> MicroFunc(MethodHandle handle, Array2Offser array, bool needReturn, CancellationToken token);
        /// <summary>
        /// Зарегестрированные функции - используется при вызове
        /// </summary>
        internal readonly MicroFunc[] RegisteredMethods = new MicroFunc[ushort.MaxValue];
        /// <summary>
        /// позволяет по имени найти адресс функции для вызовов
        /// </summary>
        internal readonly string[] RegisteredMethodsName = new string[ushort.MaxValue];

        //internal event Action<>
        internal RPC()
        {
        }

        /// <summary>
        /// Ищет свободный айди для регистрации функции
        /// </summary>
        /// <returns></returns>
        private ushort GetMethodID()
        {
            lock (this)
            {
                ushort id = 0;
                while (RegisteredMethods[id] != null)
                {
                    if (id == ushort.MaxValue)
                        throw new RegisterLimitException();
                    id++;
                }
                return id;
            }
        }

        /// <summary>
        /// Производит основную регистрацию метода (остаётся только написать микрофункцию)
        /// </summary>
        /// <param name="method"></param>
        /// <param name="micro"></param>
        /// <returns></returns>
        private RemoveHandle RegisterMethodHelp(Delegate method, MicroFunc micro)
        {
            string name = $"{method.Method.DeclaringType.FullName}.{method.Method.Name}";

            lock (this)
            {
                foreach(var n in RegisteredMethodsName) {
                    if (n == name)
                    {
                        throw new RPCRegisterNameException();
                    }
                }
                ushort id = GetMethodID();
                RegisteredMethods[id] = micro;
                RegisteredMethodsName[id] = name;
                return new RemoveHandle(id, this);
            }
        }

        

#pragma warning disable CS1998 // В асинхронном методе отсутствуют операторы await, будет выполнен синхронный метод
        #region RegisterMethod
        /// <summary>
        /// Регестрирует метод для возможности вызвать его
        /// </summary>
        /// <param name="method">метод</param>
        public RemoveHandle RegisterMethod(RPCfunc method)
        {
            return RegisterMethodHelp(method, async (MethodHandle handle, Array2Offser array, bool needReturn, CancellationToken _) =>
            {
                 method();
                 return null;
             });
        }
        /// <summary>
        /// Регестрирует метод для возможности вызвать его
        /// </summary>
        /// <param name="method">метод</param>
        public RemoveHandle RegisterMethod(RPCfunc<MethodHandle> method)
        {
            return RegisterMethodHelp(method, async (MethodHandle handle, Array2Offser array, bool needReturn, CancellationToken _) =>
             {
                 method(handle);
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
            return RegisterMethodHelp(method, async (MethodHandle handle, Array2Offser array, bool needReturn, CancellationToken _) =>
             {
                 packager.UnPack(array.Array.Bytes, array.Offset, out T1 t1);
                 method(t1);
                 return null;
             });
        }
        /// <summary>
        /// Регестрирует метод для возможности вызвать его
        /// </summary>
        /// <param name="method">метод</param>
        public RemoveHandle RegisterMethod<T1>(RPCfunc<MethodHandle, T1> method)
        {
            var packager = Packager.Create<T1>();
            return RegisterMethodHelp(method, async (MethodHandle handle, Array2Offser array, bool needReturn, CancellationToken _) =>
             {
                 packager.UnPack(array.Array.Bytes, array.Offset, out T1 t1);
                 method(handle, t1);
                 return null;
             });
        }
        #endregion
        #region RegisterMethodReturned
        /// <summary>
        /// Регестрирует метод для возможности вызвать его
        /// </summary>
        /// <param name="method">метод</param>
        public RemoveHandle RegisterMethodReturned<Tout>(RPCfuncOut<Tout> method)
        {
            var packager = Packager.Create<Tout>();
            return RegisterMethodHelp(method, async (MethodHandle handle, Array2Offser array, bool needReturn, CancellationToken token) =>
            {
                var data = method();

                if (needReturn)
                {
                    IReleasableArray bits = await handle.Client.MyArrayBufferSend.AllocateArrayAsync((int)packager.CalcNeedSize(data), token).ConfigureAwait(false);
                    if (token.IsCancellationRequested) return null;
                    packager.PackUP(bits.Bytes, Packagers.RPCAnswerSizeOf, data);
                    return bits;
                }
                else
                {
                    return null;
                }
            });
        }
        /// <summary>
        /// Регестрирует метод для возможности вызвать его
        /// </summary>
        /// <param name="method">метод</param>
        public RemoveHandle RegisterMethodReturned<Tout>(RPCfuncOut<Tout, MethodHandle> method)
        {
            var packager = Packager.Create<Tout>();
            return RegisterMethodHelp(method, async (MethodHandle handle, Array2Offser array, bool needReturn, CancellationToken token) =>
            {
                var data = method(handle);
            
                if (needReturn)
                {
                    IReleasableArray bits = await handle.Client.MyArrayBufferSend.AllocateArrayAsync((int)packager.CalcNeedSize(data), token).ConfigureAwait(false);
                    if (token.IsCancellationRequested) return null;
                    packager.PackUP(bits.Bytes, Packagers.RPCAnswerSizeOf, data);
                    return bits;
                }
                else
                {
                    return null;
                }
            });
        }
        #endregion
#pragma warning restore CS1998 // В асинхронном методе отсутствуют операторы await, будет выполнен синхронный метод

        /// <summary>
        /// Позволяет удалить зарегистрированный метод
        /// </summary>
        public class RemoveHandle
        {
            private readonly ushort ID;
            private readonly RPC RPC;

            internal RemoveHandle(ushort id, RPC rpc)
            {
                ID = id;
                RPC = rpc;
            }

            /// <summary>
            /// Удаляет метод из списка зарегестрированных (его больше нельзя буджет вызвать)
            /// </summary>
            public void Remove()
            {
                RPC.RegisteredMethods[ID] = null;
                RPC.RegisteredMethodsName[ID] = null;
            }
        }
    }
}