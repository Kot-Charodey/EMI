using System;
using System.Threading.Tasks;
using System.IO;

namespace EMI.NetStream
{
    using Structures;

    public class NetStreamRemote : Stream
    {
        private readonly Client Client;
        private readonly NetStreamIndicators INDS;
        private NetStreamInfo StreamInfo;
        private long StreamLength = -1;
        private long StreamPosition = -1;

        public override bool CanRead => StreamInfo.CanRead;

        public override bool CanSeek => StreamInfo.CanSeek;

        public override bool CanWrite => StreamInfo.CanWrite;

        public override long Length
        {
            get
            {
                if (StreamLength < 0)
                {
                    throw new NotSupportedException();
                }
                else
                {
                    return StreamLength;
                }
            }
        }

        public override long Position 
        { 
            get
            {
                if(StreamPosition < 0)
                {
                    throw new NotSupportedException();
                }
                else
                {
                    return StreamPosition;
                }
            }
            set
            {
                if (StreamPosition < 0)
                {
                    throw new NotSupportedException();
                }
                else
                {
                    var res = INDS.SetStreamPosition.RCall(value, Client).Result;
                    if(res == false)
                    {
                        throw new NotSupportedException();
                    }
                    else
                    {
                        StreamPosition = value;
                    }
                }
            }
        }

        public override void Flush()
        {
            var result = INDS.Flush.RCall(Client).Result;
            if (!result.Result)
            {
                throw new Exception();
            }
            else
            {
                StreamLength = result.Length;
                StreamPosition = result.Position;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            if (offset + count > buffer.Length)
                throw new IndexOutOfRangeException();

            var result = INDS.Read.RCall(new ReadInInfo(buffer.Length, offset, count), Client).Result;

            if (result.Result)
            {
                Buffer.BlockCopy(result.Buffer, 0, buffer, offset, count);
                StreamLength = result.Lenght;
                StreamPosition = result.Position;

                return result.ReadLen;
            }
            else
            {
                throw new Exception();
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            var result = INDS.Seek.RCall(new SeekInInfo(offset, origin), Client).Result;
            if (result.Result)
            {
                StreamPosition = result.Position;
                StreamLength = result.Length;
                return result.SeekPosition;
            }
            else
            {
                throw new Exception();
            }
        }

        public override void SetLength(long value)
        {
            var result = INDS.SetLength.RCall(value, Client).Result;
            StreamLength = value;
            if (!result)
            {
                throw new Exception();
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            var result = INDS.Write.RCall(new WriteInInfo(offset, count, buffer), Client).Result;
            if (result.Result)
            {
                StreamLength = result.Length;
                StreamPosition = result.Position;
            }
            else
            {
                throw new Exception();
            }
        }

        public override void Close()
        {
            INDS.Close.RCall(Client).Wait();
            base.Close();
        }

        private NetStreamRemote(Client client, int id)
        {
            INDS = new NetStreamIndicators(id);
            Client = client;
        }

        public static async Task<NetStreamRemote> Open(Client client, int id)
        {
            var stream = new NetStreamRemote(client, id);
            stream.StreamInfo = await stream.INDS.GetStreamInfo.RCall(client).ConfigureAwait(false);
            stream.StreamPosition = await stream.INDS.GetStreamPosition.RCall(client).ConfigureAwait(false);
            stream.StreamLength = await stream.INDS.GetStreamLength.RCall(client).ConfigureAwait(false);

            return stream;
        }
    }
}
