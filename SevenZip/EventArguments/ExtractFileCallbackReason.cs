#if UNMANAGED

namespace SevenZip
{
    /// <summary>
    /// 调用 <see cref="ExtractFileCallback"/> 的原因。
    /// </summary>
    public enum ExtractFileCallbackReason
    {
        /// <summary>
        /// <see cref="ExtractFileCallback"/> 首次为某个文件被调用。
        /// </summary>
        Start,

        /// <summary>
        /// 所有数据已写入目标，未发生任何异常。
        /// </summary>
        Done,

        /// <summary>
        /// 解压文件时发生异常。
        /// </summary>
        Failure
    }
}

#endif
