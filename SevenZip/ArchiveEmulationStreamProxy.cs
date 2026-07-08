namespace SevenZip
{
    using System;
    using System.IO;

    /// <summary>
    /// 用于模拟流的归档部分的 Stream 扩展类。
    /// </summary>
    internal class ArchiveEmulationStreamProxy : Stream, IDisposable
    {
        private readonly bool _leaveOpen;

        /// <summary>
        /// 初始化 ArchiveEmulationStream 类的新实例。
        /// </summary>
        /// <param name="stream">要包装的流。</param>
        /// <param name="offset">流的偏移量。</param>
        /// <param name="leaveOpen">操作完成后是否应关闭该流。</param>
        public ArchiveEmulationStreamProxy(Stream stream, int offset, bool leaveOpen = false)
        {
            Source = stream;
            Offset = offset;
            Source.Position = offset;

            _leaveOpen = leaveOpen;
        }

        /// <summary>
        /// 获取文件偏移量。
        /// </summary>
        public int Offset { get; }

        /// <summary>
        /// 源包装流。
        /// </summary>
        public Stream Source { get; }

        /// <inheritdoc />
        public override bool CanRead => Source.CanRead;

        /// <inheritdoc />
        public override bool CanSeek => Source.CanSeek;

        /// <inheritdoc />
        public override bool CanWrite => Source.CanWrite;

        /// <inheritdoc />
        public override void Flush()
        {
            Source.Flush();
        }

        /// <inheritdoc />
        public override long Length => Source.Length - Offset;

        /// <inheritdoc />
        public override long Position
        {
            get => Source.Position - Offset;
            set => Source.Position = value;
        }

        /// <inheritdoc />
        public override int Read(byte[] buffer, int offset, int count)
        {
            return Source.Read(buffer, offset, count);
        }

        /// <inheritdoc />
        public override long Seek(long offset, SeekOrigin origin)
        {
            return Source.Seek(origin == SeekOrigin.Begin ? offset + Offset : offset,
                origin) - Offset;
        }

        /// <inheritdoc />
        public override void SetLength(long value)
        {
            Source.SetLength(value);
        }

        /// <inheritdoc />
        public override void Write(byte[] buffer, int offset, int count)
        {
            Source.Write(buffer, offset, count);
        }

        /// <inheritdoc />
        public new void Dispose()
        {
            if (!_leaveOpen)
            {
                Source.Dispose();
            }
        }

        /// <inheritdoc />
        public override void Close()
        {
            if (!_leaveOpen)
            {
                Source.Close();
            }
        }
    }
}
