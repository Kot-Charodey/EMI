using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using EMI;
using EMI.NGC;

namespace EMI.NetStream
{
    using Structures;

    public class NetStreamHost
    {
        private readonly NetStreamIndicators INDS;
        private readonly List<IRPCRemoveHandle> Handles = new List<IRPCRemoveHandle>();
        public bool IsOpen => Handles.Count > 0;
        private readonly Stream Stream;
        public readonly int ID;

        private NetStreamHost(Client client, Stream stream)
        {
            Stream = stream;
            ID = stream.GetHashCode();
            INDS = new NetStreamIndicators(ID);

            Handles.Add(client.LocalRPC.RegisterMethod(GetStreamInfo, INDS.GetStreamInfo));
            Handles.Add(client.LocalRPC.RegisterMethod(GetStreamLength, INDS.GetStreamLength));
            Handles.Add(client.LocalRPC.RegisterMethod(GetStreamPosition,INDS.GetStreamPosition));
            Handles.Add(client.LocalRPC.RegisterMethod(SetStreamPosition, INDS.SetStreamPosition));
            Handles.Add(client.LocalRPC.RegisterMethod(Flush, INDS.Flush));
            Handles.Add(client.LocalRPC.RegisterMethod(Read, INDS.Read));
            
            Handles.Add(client.LocalRPC.RegisterMethod(Seek, INDS.Seek));
            Handles.Add(client.LocalRPC.RegisterMethod(SetLength, INDS.SetLength));
            Handles.Add(client.LocalRPC.RegisterMethod(Write, INDS.Write));
            Handles.Add(client.LocalRPC.RegisterMethod(_Close, INDS.Close));
        }

        private NetStreamInfo GetStreamInfo()
        {
            return new NetStreamInfo(Stream);
        }

        private long GetStreamLength()
        {
            try
            {
                return Stream.Length;
            }
            catch
            {
                return -1;
            }
        }

        private long GetStreamPosition()
        {
            try
            {
                return Stream.Position;
            }
            catch
            {
                return -1;
            }
        }

        private bool SetStreamPosition(long position)
        {
            try
            {
                Stream.Position = position;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private FlushInfo Flush()
        {
            try
            {
                Stream.Flush();
                return new FlushInfo(true, GetStreamLength(), GetStreamPosition());
            }
            catch
            {
                return new FlushInfo(false, -1, -1);
            }
        }
        
        private ReadInfo Read(ReadInInfo readIn)
        {
            try
            {
                using (var buffer = new NGCArray(Math.Min(readIn.BufferSize - readIn.Offset, readIn.Count)))
                {
                    int len = Stream.Read(buffer.Bytes, readIn.Offset, readIn.Count);
                    return new ReadInfo(true, len, buffer.Bytes, Stream.Length, Stream.Position);
                }
            }
            catch
            {
                return new ReadInfo(false, -1, null, -1, -1);
            }
        }

        private SeekInfo Seek(SeekInInfo info)
        {
            try
            {
                var offset = Stream.Seek(info.offset, info.origin);
                return new SeekInfo(true, Stream.Length, Stream.Position, offset);
            }
            catch
            {
                return new SeekInfo(false, -1, -1, -1);
            }
        }

        private bool SetLength(long length)
        {
            try
            {
                Stream.SetLength(length);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private WriteInfo Write(WriteInInfo write)
        {
            try
            {
                Stream.Write(write.Buffer,write.Offset,write.Count);
                return new WriteInfo(true, Stream.Position, Stream.Length);
            }
            catch
            {
                return new WriteInfo(false, -1, -1);
            }
        }

        public static int Create(Client client, Stream stream)
        {
            var host = new NetStreamHost(client, stream);
            return host.ID;
        }

        private void _Close()
        {
            lock (Handles)
            {
                foreach (var host in Handles)
                {
                    host.Remove();
                }
                Handles.Clear();
            }
        }

        public void Close()
        {
            if (Handles.Count == 0)
            {
                throw new Exception("Connection is already closed!");
            }
            _Close();
        }
    }
}
