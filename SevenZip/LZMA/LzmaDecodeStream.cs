namespace SevenZip
{
    using System;
    using System.IO;

    using SevenZip.Sdk.Compression.Lzma;

    /// <summary>
    /// 使用 LZMA 实时解压数据的流。
    /// </summary>
    public class LzmaDecodeStream : Stream
    {
        private readonly MemoryStream _buffer = new MemoryStream();
        private readonly Decoder _decoder = new Decoder();
        private readonly Stream _input;
        private byte[] _commonProperties;
        private bool _error;
        private bool _firstChunkRead;

        /// <summary>
        /// 初始化 LzmaDecodeStream 类的新实例。
        /// </summary>
        /// <param name="encodedStream">一个已压缩的流。</param>
        public LzmaDecodeStream(Stream encodedStream)
        {
            if (!encodedStream.CanRead)
            {
                throw new ArgumentException("The specified stream can not read.", "encodedStream");
            }
            _input = encodedStream;
        }

        /// <summary>
        /// 获取数据块大小。
        /// </summary>
        public int ChunkSize => (int) _buffer.Length;

        /// <summary>
        /// 获取一个值，指示当前流是否支持读取。
        /// </summary>
        public override bool CanRead => true;

        /// <summary>
        /// 获取一个值，指示当前流是否支持查找。
        /// </summary>
        public override bool CanSeek => false;

        /// <summary>
        /// 获取一个值，指示当前流是否支持写入。
        /// </summary>
        public override bool CanWrite => false;

        /// <summary>
        /// 获取输出流的字节长度。
        /// </summary>
        public override long Length
        {
            get
            {
                if (_input.CanSeek)
                {
                    return _input.Length;
                }

                return _buffer.Length;
            }
        }

        /// <summary>
        /// 获取或设置输出流中的位置。
        /// </summary>
        public override long Position
        {
            get
            {
                if (_input.CanSeek)
                {
                    return _input.Position;
                }
                return _buffer.Position;
            }
            set => throw new NotSupportedException();
        }

        private void ReadChunk()
        {
            long size;
            byte[] properties;
            try
            {
                properties = SevenZipExtractor.GetLzmaProperties(_input, out size);
            }
            catch (LzmaException)
            {
                _error = true;
                return;
            }
            if (!_firstChunkRead)
            {
                _commonProperties = properties;
            }
            if (_commonProperties[0] != properties[0] ||
                _commonProperties[1] != properties[1] ||
                _commonProperties[2] != properties[2] ||
                _commonProperties[3] != properties[3] ||
                _commonProperties[4] != properties[4])
            {
                _error = true;
                return;
            }
            if (_buffer.Capacity < (int) size)
            {
                _buffer.Capacity = (int) size;
            }
            _buffer.SetLength(size);
            _decoder.SetDecoderProperties(properties);
            _buffer.Position = 0;
            _decoder.Code(
                _input, _buffer, 0, size, null);
            _buffer.Position = 0;
        }

        /// <summary>
        /// 不执行任何操作。
        /// </summary>
        public override void Flush() {}

        /// <summary>
        /// 从当前流中读取字节序列，并在必要时解压数据。
        /// </summary>
        /// <param name="buffer">字节数组。</param>
        /// <param name="offset">buffer 中从零开始的字节偏移量，从此处开始存储从当前流中读取的数据。</param>
        /// <param name="count">从当前流中读取的最大字节数。</param>
        /// <returns>读入缓冲区的总字节数。</returns>        
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_error)
            {
                return 0;
            }

            if (!_firstChunkRead)
            {
                ReadChunk();
                _firstChunkRead = true;
            }
            int readCount = 0;
            while (count > _buffer.Length - _buffer.Position && !_error)
            {
                var buf = new byte[_buffer.Length - _buffer.Position];
                _buffer.Read(buf, 0, buf.Length);
                buf.CopyTo(buffer, offset);
                offset += buf.Length;
                count -= buf.Length;
                readCount += buf.Length;
                ReadChunk();
            }
            if (!_error)
            {
                _buffer.Read(buffer, offset, count);
                readCount += count;
            }
            return readCount;
        }

        /// <summary>
        /// 设置当前流中的位置。
        /// </summary>
        /// <param name="offset">相对于 origin 参数的字节偏移量。</param>
        /// <param name="origin">System.IO.SeekOrigin 类型的值，指示用于获取新位置的参考点。</param>
        /// <returns>当前流中的新位置。</returns>       
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// 设置当前流的长度。
        /// </summary>
        /// <param name="value">当前流的期望长度（以字节为单位）。</param>
        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// 向当前流写入字节序列。
        /// </summary>
        /// <param name="buffer">字节数组。</param>
        /// <param name="offset">buffer 中从零开始的字节偏移量，从此处开始存储从当前流中读取的数据。</param>
        /// <param name="count">从当前流中读取的最大字节数。</param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
    }
}
