using System;
using System.Reflection;
using System.Collections.Generic;

using SmartPackager;

namespace EMI
{
    /// <summary>
    /// Позволяет производить удалённый вызов процедур
    /// </summary>
    public class RPC
    {
        /// <summary>
        /// Содержит в себе код по запуску функции
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="Packet">необработанный пакет данных</param>
        /// <returns>результат выполения (null) если нет или не надо возвращать</returns>
        private delegate byte[] MicroFunc(MethodHandle handle, byte[] Packet);
        /// <summary>
        /// Зарегестрированные функции - используется при вызове
        /// </summary>
        private readonly Dictionary<ushort, MicroFunc> RegisteredMethods = new Dictionary<ushort, MicroFunc>();
        /// <summary>
        /// позволяет по имени найти адресс функции для вызовов
        /// </summary>
        internal readonly Dictionary<string, ushort> RegisteredMethodsName = new Dictionary<string, ushort>();

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
            ushort id = 0;
            while (RegisteredMethods.ContainsKey(id))
            {
                if (id == ushort.MaxValue)
                    throw new MyException.RPCRegisterLimitException();
                id++;
            }
            return id;
        }

        /// <summary>
        /// Производит основную регистрацию метода (остаётся только написать микрофункцию)
        /// </summary>
        /// <param name="method"></param>
        /// <param name="micro"></param>
        /// <returns></returns>
        private RemoveHandle RegisterMethodHelp(Delegate method,MicroFunc micro)
        {
            string name = $"{method.Method.DeclaringType.FullName}.{method.Method.Name}";

            lock (this)
            {
                ushort id = GetMethodID();
                RegisteredMethods.Add(id, micro);
                RegisteredMethodsName.Add(name, id);
                return new RemoveHandle(id, name, this);
            }
        }

        #region RegisterMethod
        /// <summary>
        /// Регестрирует метод для возможности вызвать его
        /// </summary>
        /// <param name="method">метод</param>
        public RemoveHandle RegisterMethod(RPCfunc method)
        {
            return RegisterMethodHelp(method, (MethodHandle handle, byte[] Packet) =>
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
            return RegisterMethodHelp(method, (MethodHandle handle, byte[] Packet) =>
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
            return RegisterMethodHelp(method, (MethodHandle handle, byte[] Packet) =>
            {
                packager.UnPack(Packet, 0,out T1 t1);
                method(t1);
                return null;
            });
        }
        /// <summary>
        /// Регестрирует метод для возможности вызвать его
        /// </summary>
        /// <param name="method">метод</param>
        public RemoveHandle RegisterMethod<T1>(RPCfunc<MethodHandle,T1> method)
        {
            var packager = Packager.Create<T1>();
            return RegisterMethodHelp(method, (MethodHandle handle, byte[] Packet) =>
            {
                packager.UnPack(Packet, 0, out T1 t1);
                method(handle,t1);
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
            return RegisterMethodHelp(method, (MethodHandle handle, byte[] Packet) =>
            {
                return packager.PackUP(method());
            });
        }
        /// <summary>
        /// Регестрирует метод для возможности вызвать его
        /// </summary>
        /// <param name="method">метод</param>
        public RemoveHandle RegisterMethodReturned<Tout>(RPCfuncOut<Tout, MethodHandle> method)
        {
            var packager = Packager.Create<Tout>();
            return RegisterMethodHelp(method, (MethodHandle handle, byte[] Packet) =>
            {
                return packager.PackUP(method(handle));
            });
        }
        #endregion

        /// <summary>
        /// Позволяет удалить зарегестрированный метод
        /// </summary>
        public class RemoveHandle
        {
            private readonly ushort ID;
            private readonly string Name;
            private readonly RPC RPC;

            internal RemoveHandle(ushort id,string name, RPC rpc)
            {
                ID = id;
                Name = name;
                RPC = rpc;
            }

            /// <summary>
            /// Удаляет метод из списка зарегестрированных (его больше нельзя буджет вызвать)
            /// </summary>
            public void Remove()
            {
                RPC.RegisteredMethods.Remove(ID);
                RPC.RegisteredMethodsName.Remove(Name);
            }
        }
    }
}