#if UNMANAGED

namespace SevenZip
{
    /// <summary>
    /// 归档压缩模式。
    /// </summary>
    public enum CompressionMode
    {
        /// <summary>
        /// 创建新归档；覆盖现有归档。
        /// </summary>
        Create,
        /// <summary>
        /// 向归档中追加数据。
        /// </summary>
        Append,
    }
}

#endif
