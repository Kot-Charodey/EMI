using TimeSync;

namespace EMI
{
    using Packet;
    /// <summary>
    /// целое длинное число которое представляет тики времени (разрядность должна совпадать у экземпляров запущеных на разных машинах)
    /// </summary>
    public abstract class TimerSync : TimeLongSync
    {
        internal Client Client;

#pragma warning disable CS1591 // Отсутствует комментарий XML для открытого видимого типа или члена
        protected override void SendTicks(long ticks)
        {
            var array = Client.MyArrayBufferSend.AllocateArray(Packagers.TicksSizeOf);

            Packagers.Ticks.PackUP(array.Bytes, 0,
                new PacketHeader(PacketType.TimeSync, (byte)TimeSyncType.Ticks),
                ticks);
            Client.MyNetworkClient.Send(array, true);
            array.Release();
        }

        protected override long GetTicks()
        {
            var data = Client.TimerSyncInputTick.Get();
            Packagers.Ticks.UnPack(data.Array.Bytes, data.Offset,out _, out var ticks);
            data.Array.Release();
            return ticks;
        }

        protected override void SendIntegrations(ushort count)
        {
            var array = Client.MyArrayBufferSend.AllocateArray(Packagers.IntegSizeOf);

            Packagers.Integ.PackUP(array.Bytes, 0, 
                new PacketHeader(PacketType.TimeSync, (byte)TimeSyncType.Integ),
                count);
            Client.MyNetworkClient.Send(array, true);
            array.Release();
        }

        protected override ushort GetIntegrations()
        {
            var data = Client.TimerSyncInputInteg.Get();
            Packagers.Integ.UnPack(data.Array.Bytes, data.Offset,out _, out var integ);
            data.Array.Release();
            return integ;
        }
#pragma warning restore CS1591 // Отсутствует комментарий XML для открытого видимого типа или члена
    }
}
