using System;

using TimeSync;
using SmartPackager;

namespace EMI
{
    /// <summary>
    /// целое длинное число которое представляет тики времени (разрядность как в TimeSpan)
    /// </summary>
    public abstract class TimerSync:TimeLongSync
    {
        internal Client Client;
        private Packager.M<long> PTicks = Packager.Create<long>();
        private Packager.M<ushort> PIntegr = Packager.Create<ushort>();


#pragma warning disable CS1591 // Отсутствует комментарий XML для открытого видимого типа или члена
        protected override void SendTicks(long ticks)
        {
            byte[] bytes = PTicks.PackUP(ticks);
            //TODO send
        }

        protected override long GetTicks()
        {
            byte[] bytes= Client.TimerSyncInputTick.Get();
            PTicks.UnPack(bytes, 0, out var data);
            return data;
        }

        protected override void SendIntegrations(ushort count)
        {
            byte[] bytes = PIntegr.PackUP(count);
            //TODO send
        }

        protected override ushort GetIntegrations()
        {
            byte[] bytes = Client.TimerSyncInputInteg.Get();
            PIntegr.UnPack(bytes, 0, out var data);
            return data;
        }
#pragma warning restore CS1591 // Отсутствует комментарий XML для открытого видимого типа или члена
    }

    internal class TimerBuiltInSync : TimerSync
    {
        public override long Ticks => DateTime.UtcNow.Ticks;
    }
}
