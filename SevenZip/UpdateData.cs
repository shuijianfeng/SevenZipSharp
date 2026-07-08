#if UNMANAGED

namespace SevenZip
{
    using System.Collections.Generic;

    /// <summary>
    /// 用于 UpdateCallback 的归档更新数据。
    /// </summary>
    internal struct UpdateData
    {
        public uint FilesCount;
        public InternalCompressionMode Mode;

        public IDictionary<int, string> FileNamesToModify { get; set; }

        public List<ArchiveFileInfo> ArchiveFileData { get; set; }
    }
}

#endif
