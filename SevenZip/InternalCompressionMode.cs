#if UNMANAGED

namespace SevenZip
{
    internal enum InternalCompressionMode
    {
        /// <summary>
        /// 创建新归档；覆盖现有归档。
        /// </summary>
        Create,
        /// <summary>
        /// 向归档中添加数据。
        /// </summary>
        Append,
        /// <summary>
        /// 修改归档数据。
        /// </summary>
        Modify
    }
}

#endif
