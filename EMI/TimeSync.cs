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
            var array = Client.MyArrayBuffer.AllocateArray(Packagers.Tics.SizeOf);
            var data = new Packagers.Tics();

            data.PacketHeader.PacketType = PacketType.TimeSync;
            data.PacketHeader.TimeSyncType = TimeSyncType.Ticks;
            data.Tiks = ticks;

            Packagers.PTics.PackUP(array.Bytes, 0, data);
            Client.MyNetworkClient.Send(array, true);
        }

        protected override long GetTicks()
        {
            var data = Client.TimerSyncInputTick.Get();
            Packagers.PLong.UnPack(data.Array.Bytes, data.Offset, out var data2);
            data.Array.Release();
            return data2;
        }

        protected override void SendIntegrations(ushort count)
        {
            var array = Client.MyArrayBuffer.AllocateArray(Packagers.Integ.SizeOf);
            var data = new Packagers.Integ();

            data.PacketHeader.PacketType = PacketType.TimeSync;
            data.PacketHeader.TimeSyncType = TimeSyncType.Integ;
            data.Integration = count;

            Packagers.PInteg.PackUP(array.Bytes, 0, data);
            Client.MyNetworkClient.Send(array, true);
        }

        protected override ushort GetIntegrations()
        {
            var data = Client.TimerSyncInputInteg.Get();
            Packagers.PUshort.UnPack(data.Array.Bytes, data.Offset, out var data2);
            data.Array.Release();
            return data2;
        }
#pragma warning restore CS1591 // Отсутствует комментарий XML для открытого видимого типа или члена
    }
}
