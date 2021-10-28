namespace EMI
{
    internal enum PacketType:byte
    {
        SndClose,
        SndSimple,//просто вызвать функцию (не гарантированно)
        SndGuaranteed,//просто вызвать функцию (гарантированно)
        SndGuaranteedRtr,//результат данного пакета следует вернуть
        SndGuaranteedSegmented,
        SndGuaranteedRtrSegmented,//требует вернуть результат
        SndGuaranteedReturned,//результат выполнения функции
        SndGuaranteedSegmentedReturned,//результат выполнения функции (жирные данные)
        SndFullyReceivedSegmentPackage,//пакет доставлен
        ReqGetPkg,
        ReqGetPkgSegmented,
        ReqPing0,
        ReqPing1,
        ReqConnection0,//client -> server #1
        ReqConnection1,//server -> client #2 (hesh)
        ReqConnection2,//client -> server #3 (hesh)
    }
}
