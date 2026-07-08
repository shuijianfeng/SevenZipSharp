namespace SevenZip
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Runtime.InteropServices;
    
    using System.Runtime.InteropServices.Marshalling;
#if UNMANAGED

    /// <summary>
    /// 具有 DisposeStream 属性的类。
    /// </summary>
    internal class DisposeVariableWrapper
    {
        public bool DisposeStream { protected get; set; }

        protected DisposeVariableWrapper(bool disposeStream) { DisposeStream = disposeStream; }
    }

    /// <summary>
    /// 在 InStreamWrapper 中使用的流包装器
    /// </summary>
    internal class StreamWrapper : DisposeVariableWrapper, IDisposable
    {
        /// <summary>
        /// 与流关联的文件名（用于修正日期）
        /// </summary>
        private readonly string _fileName;

        private readonly DateTime _fileTime;

        /// <summary>
        /// 用于读取、写入和定位的工作流。
        /// </summary>
        private Stream _baseStream;

        /// <summary>
        /// 初始化 StreamWrapper 类的新实例
        /// </summary>
        /// <param name="baseStream">用于读取、写入和定位的工作流</param>
        /// <param name="fileName">与流关联的文件名（用于修正属性）</param>
        /// <param name="time">文件最后写入时间（用于修正属性）</param>
        /// <param name="disposeStream">指示是否释放 baseStream</param>
        protected StreamWrapper(Stream baseStream, string fileName, DateTime time, bool disposeStream) 
            : base(disposeStream)
        {
            _baseStream = baseStream;
            _fileName = fileName;
            _fileTime = time;
        }

        /// <summary>
        /// 初始化 StreamWrapper 类的新实例
        /// </summary>
        /// <param name="baseStream">用于读取、写入和定位的工作流</param>
        /// <param name="disposeStream">指示是否释放 baseStream</param>
        protected StreamWrapper(Stream baseStream, bool disposeStream)
            : base(disposeStream)
        {
            _baseStream = baseStream;            
        }

        /// <summary>
        /// 获取用于读取、写入和定位的工作流。
        /// </summary>
        protected Stream BaseStream => _baseStream;

        #region IDisposable Members

        /// <summary>
        /// 清理使用的所有资源并修正文件属性。
        /// </summary>
        public void Dispose()
        {
            if (_baseStream != null && DisposeStream)
            {               
                try
                {
                    _baseStream.Dispose();
                }
                catch (ObjectDisposedException) { }
                _baseStream = null;                                
            }    
            
            if (!string.IsNullOrEmpty(_fileName) && File.Exists(_fileName))
            {
                try
                {
                    File.SetLastWriteTime(_fileName, _fileTime);
                    File.SetLastAccessTime(_fileName, _fileTime);
                    File.SetCreationTime(_fileName, _fileTime);
                }
                catch (ArgumentOutOfRangeException) {}
            }

            GC.SuppressFinalize(this);
        }

        #endregion

        public virtual void Seek(long offset, SeekOrigin seekOrigin, IntPtr newPosition)
        {
            if (BaseStream != null)
            {
                long position = BaseStream.Seek(offset, seekOrigin);
                if (newPosition != IntPtr.Zero)
                {
                    Marshal.WriteInt64(newPosition, position);
                }
            }            
        }
    }

    /// <summary>
    /// 用于流读取操作的 IInStream 包装器。
    /// </summary>
    /// 
    [GeneratedComClass]
    internal sealed unsafe partial class InStreamWrapper : StreamWrapper, ISequentialInStream, IInStream
    {
        /// <summary>
        /// 初始化 InStreamWrapper 类的新实例。
        /// </summary>
        /// <param name="baseStream">用于写入数据的流</param>
        /// <param name="disposeStream">指示是否释放 baseStream</param>
        public InStreamWrapper(Stream baseStream, bool disposeStream) : base(baseStream, disposeStream) { }

        #region ISequentialInStream Members

        /// <summary>
        /// 从流中读取数据。
        /// </summary>
        /// <param name="data">数据数组。</param>
        /// <param name="size">数组大小。</param>
        /// <returns>读取的字节数。</returns>
        public int Read(byte[] data, uint size)
        {
            int readCount = 0;
            if (BaseStream != null)
            {
                readCount = BaseStream.Read(data, 0, (int) size);
                if (readCount > 0)
                {
                    BytesRead?.Invoke(this, new IntEventArgs(readCount));
                }
            }
            return readCount;
        }

        #endregion

        /// <summary>
        /// 当从源读取了 IntEventArgs.Value 个字节时发生。
        /// </summary>
        public event EventHandler<IntEventArgs> BytesRead;
    }

    /// <summary>
    /// 用于流写入操作的 IOutStream 包装器。
    /// </summary>
    /// 
    [GeneratedComClass]
    internal sealed unsafe partial class OutStreamWrapper : StreamWrapper, ISequentialOutStream, IOutStream
    {
        /// <summary>
        /// 初始化 OutStreamWrapper 类的新实例
        /// </summary>
        /// <param name="baseStream">用于写入数据的流</param>
        /// <param name="fileName">文件名（用于修正属性）</param>
        /// <param name="time">文件创建时间（用于修正属性）</param>
        /// <param name="disposeStream">指示是否释放 baseStream</param>
        public OutStreamWrapper(Stream baseStream, string fileName, DateTime time, bool disposeStream) :
            base(baseStream, fileName, time, disposeStream) {}

        /// <summary>
        /// 初始化 OutStreamWrapper 类的新实例
        /// </summary>
        /// <param name="baseStream">用于写入数据的流</param>
        /// <param name="disposeStream">指示是否释放 baseStream</param>
        public OutStreamWrapper(Stream baseStream, bool disposeStream) :
            base(baseStream, disposeStream) {}

        #region IOutStream Members

        public int SetSize(long newSize)
        {
            BaseStream.SetLength(newSize);
            return 0;
        }

        #endregion

        #region ISequentialOutStream Members

        /// <summary>
        /// 将数据写入流
        /// </summary>
        /// <param name="data">数据数组</param>
        /// <param name="size">数组大小</param>
        /// <param name="processedSize">已写入的字节数</param>
        /// <returns>成功则返回零</returns>
        public int Write(byte[] data, uint size, IntPtr processedSize)
        {
            BaseStream.Write(data, 0, (int) size);
            if (processedSize != IntPtr.Zero)
            {
                Marshal.WriteInt32(processedSize, (int) size);
            }
            BytesWritten?.Invoke(this, new IntEventArgs((int) size));
            return 0;
        }

        #endregion

        /// <summary>
        /// 当写入了 IntEventArgs.Value 个字节时发生。
        /// </summary>
        public event EventHandler<IntEventArgs> BytesWritten;
    }

    /// <summary>
    /// 多卷流包装器基类。
    /// </summary>
    internal class MultiStreamWrapper : DisposeVariableWrapper, IDisposable
    {
        protected readonly Dictionary<int, KeyValuePair<long, long>> StreamOffsets = new Dictionary<int, KeyValuePair<long, long>>();

        protected readonly List<Stream> Streams = new List<Stream>();
        protected int CurrentStream;
        protected long Position;
        protected long StreamLength;

        /// <summary>
        /// 初始化 MultiStreamWrapper 类的新实例。
        /// </summary>
        /// <param name="dispose">如果请求则执行 Dispose()。</param>
        protected MultiStreamWrapper(bool dispose) : base(dispose) {}

        /// <summary>
        /// 获取输入数据的总长度。
        /// </summary>
        public long Length => StreamLength;

        #region IDisposable Members

        /// <summary>
        /// 清理使用的所有资源并修正文件属性。
        /// </summary>
        public virtual void Dispose()
        {
            if (DisposeStream)
            {
                foreach (Stream stream in Streams)
                {
                    try
                    {
                        stream.Dispose();
                    }
                    catch (ObjectDisposedException) {}
                }
                Streams.Clear();
            }
            GC.SuppressFinalize(this);
        }

        #endregion

        protected static string VolumeNumber(int num)
        {
            var prefix = num switch
            {
                < 10 => ".00",
                < 100 => ".0",
                _ => "."
            };
            return prefix + num.ToString(CultureInfo.InvariantCulture);
        }

        private int StreamNumberByOffset(long offset)
        {
            foreach (int number in StreamOffsets.Keys)
            {
                if (StreamOffsets[number].Key <= offset &&
                    StreamOffsets[number].Value >= offset)
                {
                    return number;
                }
            }
            return -1;
        }

        public void Seek(long offset, SeekOrigin seekOrigin, IntPtr newPosition)
        {
            long absolutePosition;
            switch (seekOrigin) {
                case SeekOrigin.Begin:
                    absolutePosition = offset;
                    break;
                case SeekOrigin.Current:
                    absolutePosition = Position + offset;
                    break;
                case SeekOrigin.End:
                    absolutePosition = Length + offset;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(seekOrigin));
            }
            CurrentStream = StreamNumberByOffset(absolutePosition);
            long delta = Streams[CurrentStream].Seek(
                absolutePosition - StreamOffsets[CurrentStream].Key, SeekOrigin.Begin);
            Position = StreamOffsets[CurrentStream].Key + delta;
            if (newPosition != IntPtr.Zero)
            {
                Marshal.WriteInt64(newPosition, Position);
            }
        }
    }

    /// <summary>
    /// 用于多卷流读取操作的 IInStream 包装器。
    /// </summary>
    /// 
    [GeneratedComClass]
    internal sealed unsafe partial class InMultiStreamWrapper : MultiStreamWrapper, ISequentialInStream, IInStream
    {
        /// <summary>
        /// 初始化 InMultiStreamWrapper 类的新实例。
        /// </summary>
        /// <param name="fileName">归档文件名。</param>
        /// <param name="dispose">如果请求则执行 Dispose()。</param>
        public InMultiStreamWrapper(string fileName, bool dispose) :
            base(dispose)
        {
            string baseName = fileName.Substring(0, fileName.Length - 4);
            int i = 0;
            while (File.Exists(fileName))
            {
                Streams.Add(new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 65536, FileOptions.SequentialScan));
                long length = Streams[i].Length;
                StreamOffsets.Add(i++, new KeyValuePair<long, long>(StreamLength, StreamLength + length));
                StreamLength += length;
                fileName = baseName + VolumeNumber(i + 1);
            }
        }

        #region ISequentialInStream Members

        /// <summary>
        /// 从流中读取数据。
        /// </summary>
        /// <param name="data">数据数组。</param>
        /// <param name="size">数组大小。</param>
        /// <returns>读取的字节数。</returns>
        public int Read(byte[] data, uint size)
        {
            var readSize = (int) size;
            int readCount = Streams[CurrentStream].Read(data, 0, readSize);
            readSize -= readCount;
            Position += readCount;
            while (readCount < (int) size)
            {
                if (CurrentStream == Streams.Count - 1)
                {
                    return readCount;
                }
                CurrentStream++;
                Streams[CurrentStream].Seek(0, SeekOrigin.Begin);
                int count = Streams[CurrentStream].Read(data, readCount, readSize);
                readCount += count;
                readSize -= count;
                Position += count;
            }
            return readCount;
        }

        #endregion
    }

    /// <summary>
    /// 用于多卷流写入操作的 IOutStream 包装器。
    /// </summary>
    /// 
    [GeneratedComClass]
    internal sealed unsafe partial class OutMultiStreamWrapper : MultiStreamWrapper, ISequentialOutStream, IOutStream
    {
        private readonly string _archiveName;
        private readonly long _volumeSize;
        private long _overallLength;

        /// <summary>
        /// 初始化 OutMultiStreamWrapper 类的新实例。
        /// </summary>
        /// <param name="archiveName">归档名称。</param>
        /// <param name="volumeSize">卷大小。</param>
        public OutMultiStreamWrapper(string archiveName, long volumeSize) :
            base(true)
        {
            _archiveName = archiveName;
            _volumeSize = volumeSize;
            CurrentStream = -1;
            NewVolumeStream();
        }

        #region IOutStream Members

        public int SetSize(long newSize)
        {
            return 0;
        }

        #endregion

        #region ISequentialOutStream Members

        public int Write(byte[] data, uint size, IntPtr processedSize)
        {
            int offset = 0;
            var originalSize = (int) size;
            Position += size;
            _overallLength = Math.Max(Position + 1, _overallLength);
            while (size > _volumeSize - Streams[CurrentStream].Position)
            {
                var count = (int) (_volumeSize - Streams[CurrentStream].Position);
                Streams[CurrentStream].Write(data, offset, count);
                size -= (uint) count;
                offset += count;
                NewVolumeStream();
            }
            Streams[CurrentStream].Write(data, offset, (int) size);
            if (processedSize != IntPtr.Zero)
            {
                Marshal.WriteInt32(processedSize, originalSize);
            }
            return 0;
        }

        #endregion

        public override void Dispose()
        {
            int lastIndex = Streams.Count - 1;
            Streams[lastIndex].SetLength(lastIndex > 0? Streams[lastIndex].Position : _overallLength);
            base.Dispose();
        }

        private void NewVolumeStream()
        {
            CurrentStream++;
            Streams.Add(new FileStream(_archiveName + VolumeNumber(CurrentStream + 1), FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 65536, FileOptions.SequentialScan));
            Streams[CurrentStream].SetLength(_volumeSize);
            StreamOffsets.Add(CurrentStream, new KeyValuePair<long, long>(0, _volumeSize - 1));
        }
    }
    [GeneratedComClass]
    internal sealed unsafe partial class FakeOutStreamWrapper : ISequentialOutStream, IDisposable
    {
        #region IDisposable Members

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        #endregion

        #region ISequentialOutStream Members

        /// <summary>
        /// 除了调用 BytesWritten 事件外什么也不做
        /// </summary>
        /// <param name="data">数据数组</param>
        /// <param name="size">数组大小</param>
        /// <param name="processedSize">已写入的字节数</param>
        /// <returns>成功则返回零</returns>
        public int Write(byte[] data, uint size, IntPtr processedSize)
        {
            BytesWritten?.Invoke(this, new IntEventArgs((int) size));
            if (processedSize != IntPtr.Zero)
            {
                Marshal.WriteInt32(processedSize, (int) size);
            }
            return 0;
        }

        #endregion

        /// <summary>
        /// 当写入了 IntEventArgs.Value 个字节时发生
        /// </summary>
        public event EventHandler<IntEventArgs> BytesWritten;
    }
#endif
}
