﻿using System.Threading;
using System.Threading.Tasks;
using EMI.ProBuffer;
using SmartPackager;

namespace EMI.Indicators
{
    /// <summary>
    /// Ссылка на удалённый метод
    /// </summary>
    public sealed class Indicator : AIndicator
    {
        /// <summary>
        /// Создаёт новую ссылку на функцию
        /// </summary>
        /// <param name="methodName">имя метода на который ссылается ссылка (функция не будет вызвана если имя указано не полностью)(namespace.class.method)</param>
        public Indicator(string methodName) : base(methodName)
        {

        }

        /// <summary>
        /// Создаёт новую ссылку на функцию
        /// </summary>
        /// <param name="func"></param>
        public Indicator(RPCfunc func) : base(func)
        {

        }

        /// <summary>
        /// Вызвать удалённый метод на указанном клиенте
        /// </summary>
        /// <param name="client">клиент на котором будет вызван метод</param>
        /// <param name="token">токен отмены операции</param>
        /// <param name="type">тип вызова</param>
        /// <returns></returns>
        public async Task RCall(Client client, CancellationToken token=default, RCType type=RCType.Guaranteed)
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
    public sealed class Indicator<T1> : AIndicator
    {
        /// <summary>
        /// Создаёт новую ссылку на функцию
        /// </summary>
        /// <param name="methodName">имя метода на который ссылается ссылка (функция не будет вызвана если имя указано не полностью)(namespace.class.method)</param>
        public Indicator(string methodName) : base(methodName)
        {

        }

        /// <summary>
        /// Создаёт новую ссылку на функцию
        /// </summary>
        /// <param name="func"></param>
        public Indicator(RPCfunc<T1> func) : base(func)
        {

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
}