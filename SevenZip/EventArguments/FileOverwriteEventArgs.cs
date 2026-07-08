#if UNMANAGED

namespace SevenZip
{
    using System;

    /// <summary>
    /// FileExists 事件的事件参数，存储文件名并在文件已存在时询问是否覆盖。
    /// </summary>
    public sealed class FileOverwriteEventArgs : EventArgs
    {
        /// <summary>
        /// 初始化 FileOverwriteEventArgs 类的新实例。
        /// </summary>
        /// <param name="fileName">文件名。</param>
        public FileOverwriteEventArgs(string fileName)
        {
            FileName = fileName;
        }

        /// <summary>
        /// 获取或设置指示是否取消解压的值。
        /// </summary>
        public bool Cancel { get; set; }

        /// <summary>
        /// 获取或设置提取到的文件名。值为 null 表示跳过。
        /// </summary>
        public string FileName { get; set; }
    }
}

#endif