using System.Threading;
using System.Threading.Tasks;
using EMI.ProBuffer;
using SmartPackager;

namespace EMI.Indicators
{
    /// <summary>
    /// Позволяет создать ссылку на удалённый метод
    /// </summary>
    public static class Indicators
    {
        /// <summary>
        /// Позволяет создать ссылку на удалённый метод
        /// </summary>
        /// <param name="fucnc"></param>
        /// <returns></returns>
        public static Func Create(RPCfunc fucnc)
        {
            return new Func(RPC.GetDelegateName(fucnc).DeterministicGetHashCode());
        }
        /// <summary>
        /// Позволяет создать ссылку на удалённый метод
        /// </summary>
        /// <param name="fucnc"></param>
        /// <returns></returns>
        public static Func<T> Create<T>(RPCfunc<T> fucnc)
        {
            return new Func<T>(RPC.GetDelegateName(fucnc).DeterministicGetHashCode());
        }
        /// <summary>
        /// Позволяет создать ссылку на удалённый метод
        /// </summary>
        /// <param name="fucnc"></param>
        /// <returns></returns>
        public static FuncOut<TOut> Create<TOut>(RPCfuncOut<TOut> fucnc)
        {
            return new FuncOut<TOut>(RPC.GetDelegateName(fucnc).DeterministicGetHashCode());
        }
        /// <summary>
        /// Позволяет создать ссылку на удалённый метод
        /// </summary>
        /// <param name="fucnc"></param>
        /// <returns></returns>
        public static FuncOut<TOut,T1> Create<TOut,T1>(RPCfuncOut<TOut,T1> fucnc)
        {
            return new FuncOut<TOut,T1>(RPC.GetDelegateName(fucnc).DeterministicGetHashCode());
        }

        /// <summary>
        /// Ссылка на удалённый метод
        /// </summary>
        public sealed class Func : AIndicator
        {
            internal Func(int id)
            {
                ID = id;
            }

            /// <summary>
            /// Вызвать удалённый метод на указанном клиенте
            /// </summary>
            /// <param name="client">клиент на котором будет вызван метод</param>
            /// <param name="token">токен отмены операции</param>
            /// <param name="type">тип вызова</param>
            /// <returns></returns>
            public async Task RCall(Client client, CancellationToken token = default, RCType type = RCType.Guaranteed)
            {
                await RCallLow(client, type, token).ConfigureAwait(false);
            }

#pragma warning disable CS1591 // Отсутствует комментарий XML для открытого видимого типа или члена
            protected internal override int Size => 0;

            protected internal override void PackUp(IReleasableArray array)
            {

            }

            protected internal override void UnPack(IReleasableArray array)
            {

            }
#pragma warning restore CS1591 // Отсутствует комментарий XML для открытого видимого типа или члена
        }

        /// <summary>
        /// Ссылка на удалённый метод
        /// </summary>
        public sealed class Func<T1> : AIndicator
        {
            internal Func(int id)
            {
                ID = id;
            }

            /// <summary>
            /// Вызвать удалённый метод на указанном клиенте
            /// </summary>
            /// <param name="t1">параметр функции</param>
            /// <param name="client">клиент на котором будет вызван метод</param>
            /// <param name="token">токен отмены операции</param>
            /// <param name="type">тип вызова</param>
            /// <returns></returns>
            public async Task RCall(T1 t1, Client client, CancellationToken token = default, RCType type = RCType.Guaranteed)
            {
                Dt1 = t1;
                await RCallLow(client, type, token).ConfigureAwait(false);
            }

#pragma warning disable CS1591 // Отсутствует комментарий XML для открытого видимого типа или члена
            protected internal override int Size => (int)DPackUp.CalcNeedSize(Dt1);

            private readonly static Packager.M<T1> DPackUp = Packager.Create<T1>();
            private T1 Dt1;

            protected internal override void PackUp(IReleasableArray array)
            {
                DPackUp.PackUP(array.Bytes, array.Offset, Dt1);
            }

            protected internal override void UnPack(IReleasableArray array)
            {

            }
#pragma warning restore CS1591 // Отсутствует комментарий XML для открытого видимого типа или члена
        }

        /// <summary>
        /// Ссылка на удалённый метод
        /// </summary>
        public sealed class FuncOut<TOut> : AIndicator
        {
            internal FuncOut(int id)
            {
                ID = id;
            }

            /// <summary>
            /// Вызвать удалённый метод на указанном клиенте
            /// </summary>
            /// <param name="client">клиент на котором будет вызван метод</param>
            /// <param name="token">токен отмены операции</param>
            /// <param name="type">тип вызова</param>
            /// <returns></returns>
            public async Task<TOut> RCall(Client client, CancellationToken token = default, RCType type = RCType.ReturnWait)
            {
                Dtout = default;
                await RCallLow(client, type, token).ConfigureAwait(false);
                return Dtout;
            }

#pragma warning disable CS1591 // Отсутствует комментарий XML для открытого видимого типа или члена
            protected internal override int Size => 0;

            private readonly static Packager.M<TOut> DUnPack = Packager.Create<TOut>();
            private TOut Dtout;

            protected internal override void PackUp(IReleasableArray array)
            {
            }

            protected internal override void UnPack(IReleasableArray array)
            {
                DUnPack.UnPack(array.Bytes, array.Offset, out Dtout);
            }
#pragma warning restore CS1591 // Отсутствует комментарий XML для открытого видимого типа или члена
        }

        /// <summary>
        /// Ссылка на удалённый метод
        /// </summary>
        public sealed class FuncOut<TOut, T1> : AIndicator
        {
            internal FuncOut(int id)
            {
                ID = id;
            }

            /// <summary>
            /// Вызвать удалённый метод на указанном клиенте
            /// </summary>
            /// <param name="t1">параметр функции</param>
            /// <param name="client">клиент на котором будет вызван метод</param>
            /// <param name="token">токен отмены операции</param>
            /// <param name="type">тип вызова</param>
            /// <returns></returns>
            public async Task<TOut> RCall(T1 t1, Client client, CancellationToken token = default, RCType type = RCType.ReturnWait)
            {
                Dt1 = t1;
                Dtout = default;
                await RCallLow(client, type, token).ConfigureAwait(false);
                return Dtout;
            }

#pragma warning disable CS1591 // Отсутствует комментарий XML для открытого видимого типа или члена
            protected internal override int Size => (int)DPackUp.CalcNeedSize(Dt1);

            private readonly static Packager.M<TOut> DUnPack = Packager.Create<TOut>();
            private TOut Dtout;
            private readonly static Packager.M<T1> DPackUp = Packager.Create<T1>();
            private T1 Dt1;

            protected internal override void PackUp(IReleasableArray array)
            {
                DPackUp.PackUP(array.Bytes, array.Offset, Dt1);
            }

            protected internal override void UnPack(IReleasableArray array)
            {
                DUnPack.UnPack(array.Bytes, array.Offset, out Dtout);
            }
#pragma warning restore CS1591 // Отсутствует комментарий XML для открытого видимого типа или члена
        }
    }
}