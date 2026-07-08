namespace SevenZip
{
    using System;
    using System.IO;

    using SevenZip.Sdk.Compression.Lzma;

    /// <summary>
    /// 使用 LZMA 实时压缩数据的流。
    /// </summary>
    public class LzmaEncodeStream : Stream
    {
        private const int MAX_BUFFER_CAPACITY = 1 << 30; //1 GB
        private readonly MemoryStream _buffer = new MemoryStream();
        private readonly int _bufferCapacity = 1 << 18; //256 KB
        private readonly bool _ownOutput;
        private bool _disposed;
        private Encoder _lzmaEncoder;
        private Stream _output;

        /// <summary>
        /// 初始化 LzmaEncodeStream 类的新实例。
        /// </summary>
        public LzmaEncodeStream()
        {
            _output = new MemoryStream();
            _ownOutput = true;
            Init();
        }

        /// <summary>
        /// 初始化 LzmaEncodeStream 类的新实例。
        /// </summary>
        /// <param name="bufferCapacity">缓冲区大小。大小越大，压缩效果越好。</param>
        public LzmaEncodeStream(int bufferCapacity)
        {
            _output = new MemoryStream();
            _ownOutput = true;
            if (bufferCapacity > MAX_BUFFER_CAPACITY)
            {
                throw new ArgumentException("Too large capacity.", "bufferCapacity");
            }
            _bufferCapacity = bufferCapacity;
            Init();
        }

        /// <summary>
        /// 初始化 LzmaEncodeStream 类的新实例。
        /// </summary>
        /// <param name="outputStream">支持写入的输出流。</param>
        public LzmaEncodeStream(Stream outputStream)
        {
            if (!outputStream.CanWrite)
            {
                throw new ArgumentException("The specified stream can not write.", "outputStream");
            }
            _output = outputStream;
            Init();
        }

        /// <summary>
        /// 初始化 LzmaEncodeStream 类的新实例。
        /// </summary>
        /// <param name="outputStream">支持写入的输出流。</param>
        /// <param name="bufferCapacity">缓冲区大小。大小越大，压缩效果越好。</param>
        public LzmaEncodeStream(Stream outputStream, int bufferCapacity)
        {
            if (!outputStream.CanWrite)
            {
                throw new ArgumentException("The specified stream can not write.", "outputStream");
            }
            _output = outputStream;
            if (bufferCapacity > 1 << 30)
            {
                throw new ArgumentException("Too large capacity.", "bufferCapacity");
            }
            _bufferCapacity = bufferCapacity;
            Init();
        }

        /// <summary>
        /// 获取一个值，指示当前流是否支持读取。
        /// </summary>
        public override bool CanRead => false;

        /// <summary>
        /// 获取一个值，指示当前流是否支持查找。
        /// </summary>
        public override bool CanSeek => false;

        /// <summary>
        /// 获取一个值，指示当前流是否支持写入。
        /// </summary>
        public override bool CanWrite
        {
            get
            {
                DisposedCheck();
                return _buffer.CanWrite;
            }
        }

        /// <summary>
        /// 获取输出流的字节长度。
        /// </summary>
        public override long Length
        {
            get
            {
                DisposedCheck();

                if (_output.CanSeek)
                {
                    return _output.Length;
                }

                return _buffer.Position;
            }
        }

        /// <summary>
        /// 获取或设置输出流中的位置。
        /// </summary>
        public override long Position
        {
            get
            {
                DisposedCheck();

                if (_output.CanSeek)
                {
                    return _output.Position;
                }

                return _buffer.Position;
            }
            set => throw new NotSupportedException();
        }

        private void Init()
        {
            _buffer.Capacity = _bufferCapacity;
            SevenZipCompressor.LzmaDictionarySize = _bufferCapacity;
            _lzmaEncoder = new Encoder();
            SevenZipCompressor.WriteLzmaProperties(_lzmaEncoder);
        }

        /// <summary>
        /// 检查该类是否已被释放。
        /// </summary>
        /// <exception cref="System.ObjectDisposedException" />
        private void DisposedCheck()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException("SevenZipExtractor");
            }
        }

        private void WriteChunk()
        {
            _lzmaEncoder.WriteCoderProperties(_output);
            long streamSize = _buffer.Position;
            if (_buffer.Length != _buffer.Position)
            {
                _buffer.SetLength(_buffer.Position);
            }
            _buffer.Position = 0;
            for (int i = 0; i < 8; i++)
            {
                _output.WriteByte((byte) (streamSize >> (8*i)));
            }
            _lzmaEncoder.Code(_buffer, _output, -1, -1, null);
            _buffer.Position = 0;
        }

        /// <summary>
        /// 将 LzmaEncodeStream 转换为 LzmaDecodeStream 以读取数据。
        /// </summary>
        /// <returns></returns>
        public LzmaDecodeStream ToDecodeStream()
        {
            DisposedCheck();
            Flush();
            if (_output.CanSeek)
            {
                _output.Position = 0;
            }
            return new LzmaDecodeStream(_output);
        }

        /// <summary>
        /// 清除此流的所有缓冲区，并使所有缓冲数据被压缩并写入。
        /// </summary>
        public override void Flush()
        {
            DisposedCheck();
            if (_buffer.Position > 0)
            {
                WriteChunk();
            }
        }

        /// <summary>
        /// 释放 LzmaEncodeStream 使用的所有非托管资源。
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    try
                    {
                        Flush();
                    }
                    finally
                    {
                        _buffer.Close();
                        if (_ownOutput)
                        {
                            _output?.Dispose();
                        }
                        _output = null;
                    }
                }
                _disposed = true;
            }
        }

        /// <summary>
        /// 从当前流中读取字节序列，并按读取的字节数推进流中的位置。
        /// </summary>
        /// <param name="buffer">字节数组。</param>
        /// <param name="offset">buffer 中从零开始的字节偏移量，从此处开始存储从当前流中读取的数据。</param>
        /// <param name="count">从当前流中读取的最大字节数。</param>
        /// <returns>读入缓冲区的总字节数。</returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            DisposedCheck();
            throw new NotSupportedException();
        }

        /// <summary>
        /// 设置当前流中的位置。
        /// </summary>
        /// <param name="offset">相对于 origin 参数的字节偏移量。</param>
        /// <param name="origin">System.IO.SeekOrigin 类型的值，指示用于获取新位置的参考点。</param>
        /// <returns>当前流中的新位置。</returns>
        public override long Seek(long offset, SeekOrigin origin)
        {
            DisposedCheck();
            throw new NotSupportedException();
        }

        /// <summary>
        /// 设置当前流的长度。
        /// </summary>
        /// <param name="value">当前流的期望长度（以字节为单位）。</param>
        public override void SetLength(long value)
        {
            DisposedCheck();
            throw new NotSupportedException();
        }

        /// <summary>
        /// 向当前流写入字节序列，并在必要时进行压缩。
        /// </summary>
        /// <param name="buffer">字节数组。</param>
        /// <param name="offset">buffer 中从零开始的字节偏移量，从此处开始存储从当前流中读取的数据。</param>
        /// <param name="count">从当前流中读取的最大字节数。</param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            DisposedCheck();
            int dataLength = Math.Min(buffer.Length - offset, count);
            while (_buffer.Position + dataLength >= _bufferCapacity)
            {
                int length = _bufferCapacity - (int) _buffer.Position;
                _buffer.Write(buffer, offset, length);
                offset = length + offset;
                dataLength -= length;
                WriteChunk();
            }
            _buffer.Write(buffer, offset, dataLength);
        }
    }
}
