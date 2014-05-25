using System;
using System.IO;

namespace CheckSum.Helpers
{
    public class StreamWithProgress : Stream, IDisposable
    {
        public Stream InnerStream { get; private set; }

        private Action<long, long> ProgressFunction { get; set; }

        public StreamWithProgress(Stream innerStream, Action<long,long> progressFunction)
        {
            InnerStream = innerStream;
            ProgressFunction = progressFunction;
        }

        public override bool CanRead
        {
            get { return InnerStream.CanRead; }
        }

        public override bool CanSeek
        {
            get { return InnerStream.CanSeek; }
        }

        public override bool CanWrite
        {
            get { return InnerStream.CanWrite; }
        }

        public override void Flush()
        {
            InnerStream.Flush();
        }

        public override long Length
        {
            get { return InnerStream.Length; }
        }

        public override long Position
        {
            get { return InnerStream.Position; }
            set { InnerStream.Position = value; }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var r=InnerStream.Read(buffer, offset, count);
            ProgressFunction(InnerStream.Position, InnerStream.Length);
            return r;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return InnerStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            InnerStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            InnerStream.Write(buffer,offset,count);
        }

        void IDisposable.Dispose()
        {
            InnerStream.Dispose();
            base.Dispose();
        }
    }
}