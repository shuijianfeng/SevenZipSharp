namespace SevenZip
{
    /// <summary>
    /// 用于精确处理进度的事件参数类。
    /// </summary>
    public sealed class ProgressEventArgs : PercentDoneEventArgs
    {
        /// <summary>
        /// 初始化 ProgressEventArgs 类的新实例。
        /// </summary>
        /// <param name="percentDone">已完成工作的百分比。</param>
        /// <param name="percentDelta">自上一个事件之后完成的工作百分比。</param>
        public ProgressEventArgs(byte percentDone, byte percentDelta)
            : base(percentDone)
        {
            PercentDelta = percentDelta;
        }

        /// <summary>
        /// 获取已完成工作百分比的变化量。
        /// </summary>
        public byte PercentDelta { get; }
    }
}
