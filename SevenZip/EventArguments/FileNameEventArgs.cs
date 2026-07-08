#if UNMANAGED

namespace SevenZip
{
    using System;

    /// <summary>
    /// 存储文件名的事件参数类。
    /// </summary>
    public sealed class FileNameEventArgs : PercentDoneEventArgs, ICancellable
    {
        /// <summary>
        /// 初始化 FileNameEventArgs 类的新实例。
        /// </summary>
        /// <param name="fileName">文件名。</param>
        /// <param name="percentDone">已完成工作的百分比。</param>
        public FileNameEventArgs(string fileName, byte percentDone) :
            base(percentDone)
        {
            FileName = fileName;
        }

        /// <summary>
        /// 获取或设置是否停止当前的压缩包操作。
        /// </summary>
        public bool Cancel { get; set; }

        /// <summary>
        /// 获取或设置是否停止当前的压缩包操作。
        /// </summary>
        public bool Skip
        {
            get => false;
            set => throw new NotImplementedException();
        }

        /// <summary>
        /// 获取文件名。
        /// </summary>
        public string FileName { get; }
    }
}

#endif