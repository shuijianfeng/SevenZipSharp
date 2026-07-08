#if UNMANAGED

namespace SevenZip
{
    using System;

    /// <summary>
    /// 用于报告解压后压缩包数据大小的事件参数。
    /// </summary>
    public sealed class OpenEventArgs : EventArgs
    {
        private readonly ulong _totalSize;

        /// <summary>
        /// 初始化 OpenEventArgs 类的新实例。
        /// </summary>
        /// <param name="totalSize">解压后压缩包数据的大小。</param>
        [CLSCompliant(false)]
        public OpenEventArgs(ulong totalSize)
        {
            _totalSize = totalSize;
        }

        /// <summary>
        /// 获取解压后压缩包数据的大小。
        /// </summary>
        [CLSCompliant(false)]
        public ulong TotalSize => _totalSize;
    }
}

#endif
