namespace SevenZip
{
    /// <summary>
    /// 用于报告即将打包的文件信息的事件参数。
    /// </summary>
    public sealed class FileInfoEventArgs : PercentDoneEventArgs, ICancellable
    {
        /// <summary>
        /// 初始化 FileInfoEventArgs 类的新实例。
        /// </summary>
        /// <param name="fileInfo">当前的 ArchiveFileInfo。</param>
        /// <param name="percentDone">已完成工作的百分比。</param>
        public FileInfoEventArgs(ArchiveFileInfo fileInfo, byte percentDone)
            : base(percentDone)
        {
            FileInfo = fileInfo;
        }

        /// <summary>
        /// 获取或设置是否停止当前的压缩包操作。
        /// </summary>
        public bool Cancel { get; set; }

        /// <summary>
        /// 获取或设置是否跳过当前文件。
        /// </summary>
        public bool Skip { get; set; }

        /// <summary>
        /// 获取与该事件对应的 FileInfo。
        /// </summary>
        public ArchiveFileInfo FileInfo { get; }
    }
}
