#if UNMANAGED

namespace SevenZip
{
    using System;
    using System.Globalization;

    /// <summary>
    /// 用于存储 7-zip 归档中文件信息的结构。
    /// </summary>
    public struct ArchiveFileInfo
    {
        /// <summary>
        /// 获取或设置文件在归档文件表中的索引。
        /// </summary>
        [CLSCompliant(false)]
        public int Index { get; set; }

        /// <summary>
        /// 获取或设置文件名。
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// 获取或设置文件的最后写入时间。
        /// </summary>
        public DateTime LastWriteTime { get; set; }

        /// <summary>
        /// 获取或设置文件的创建时间。
        /// </summary>
        public DateTime CreationTime { get; set; }

        /// <summary>
        /// 获取或设置文件的最后访问时间。
        /// </summary>
        public DateTime LastAccessTime { get; set; }

        /// <summary>
        /// 获取或设置文件的大小（解压后）。
        /// </summary>
        [CLSCompliant(false)]
        public ulong Size { get; set; }

        /// <summary>
        /// 获取或设置文件的 CRC 校验和。
        /// </summary>
        [CLSCompliant(false)]
        public uint Crc { get; set; }

        /// <summary>
        /// 获取或设置文件属性。
        /// </summary>
        [CLSCompliant(false)]
        public uint Attributes { get; set; }

        /// <summary>
        /// 获取或设置是否为目录。
        /// </summary>
        public bool IsDirectory { get; set; }

        /// <summary>
        /// 获取或设置是否已加密。
        /// </summary>
        public bool Encrypted { get; set; }

        /// <summary>
        /// 获取或设置文件的注释。
        /// </summary>
        public string Comment { get; set; }

        /// <summary>
        /// 文件的压缩方法。
        /// </summary>
        public string Method { get; set; }

        /// <summary>
        /// 确定指定的 System.Object 是否等于当前 ArchiveFileInfo。
        /// </summary>
        /// <param name="obj">要与当前 ArchiveFileInfo 进行比较的 System.Object。</param>
        /// <returns>如果指定的 System.Object 等于当前 ArchiveFileInfo，则为 true；否则为 false。</returns>
        public override bool Equals(object obj)
        {
            return obj is ArchiveFileInfo info && Equals(info);
        }

        /// <summary>
        /// 确定指定的 ArchiveFileInfo 是否等于当前 ArchiveFileInfo。
        /// </summary>
        /// <param name="afi">要与当前 ArchiveFileInfo 进行比较的 ArchiveFileInfo。</param>
        /// <returns>如果指定的 ArchiveFileInfo 等于当前 ArchiveFileInfo，则为 true；否则为 false。</returns>
        public bool Equals(ArchiveFileInfo afi)
        {
            return afi.Index == Index && afi.FileName == FileName;
        }

        /// <summary>
        /// 用作特定类型的哈希函数。
        /// </summary>
        /// <returns>当前 ArchiveFileInfo 的哈希代码。</returns>
        public override int GetHashCode()
        {
            return FileName.GetHashCode() ^ Index;
        }

        /// <summary>
        /// 返回表示当前 ArchiveFileInfo 的 System.String。
        /// </summary>
        /// <returns>表示当前 ArchiveFileInfo 的 System.String。</returns>
        public override string ToString()
        {
            return "[" + Index.ToString(CultureInfo.CurrentCulture) + "] " + FileName;
        }

        /// <summary>
        /// 确定指定的两个 ArchiveFileInfo 实例是否被视为相等。
        /// </summary>
        /// <param name="afi1">要比较的第一个 ArchiveFileInfo。</param>
        /// <param name="afi2">要比较的第二个 ArchiveFileInfo。</param>
        /// <returns>如果指定的 ArchiveFileInfo 实例被视为相等，则为 true；否则为 false。</returns>
        public static bool operator ==(ArchiveFileInfo afi1, ArchiveFileInfo afi2)
        {
            return afi1.Equals(afi2);
        }

        /// <summary>
        /// 确定指定的两个 ArchiveFileInfo 实例是否被视为不相等。
        /// </summary>
        /// <param name="afi1">要比较的第一个 ArchiveFileInfo。</param>
        /// <param name="afi2">要比较的第二个 ArchiveFileInfo。</param>
        /// <returns>如果指定的 ArchiveFileInfo 实例被视为不相等，则为 true；否则为 false。</returns>
        public static bool operator !=(ArchiveFileInfo afi1, ArchiveFileInfo afi2)
        {
            return !afi1.Equals(afi2);
        }
    }
}

#endif
