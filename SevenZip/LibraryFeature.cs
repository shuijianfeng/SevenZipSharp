namespace SevenZip
{
    using System;

    /// <summary>
    /// 库支持的功能集合。
    /// </summary>
    [Flags]
    [CLSCompliant(false)]
    public enum LibraryFeature : uint
    {
        /// <summary>
        /// 默认功能。
        /// </summary>
        None = 0,
        /// <summary>
        /// 库可以解压使用 LZMA 方法压缩的 7zip 归档。
        /// </summary>
        Extract7z = 0x1,
        /// <summary>
        /// 库可以解压使用 LZMA2 方法压缩的 7zip 归档。
        /// </summary>
        Extract7zLZMA2 = 0x2,
        /// <summary>
        /// 库可以解压使用所有已知方法压缩的 7z 归档。
        /// </summary>
        Extract7zAll = Extract7z|Extract7zLZMA2|0x4,
        /// <summary>
        /// 库可以解压 zip 归档。
        /// </summary>
        ExtractZip = 0x8,
        /// <summary>
        /// 库可以解压 rar 归档。
        /// </summary>
        ExtractRar = 0x10,
        /// <summary>
        /// 库可以解压 gzip 归档。
        /// </summary>
        ExtractGzip = 0x20,
        /// <summary>
        /// 库可以解压 bzip2 归档。
        /// </summary>
        ExtractBzip2 = 0x40,
        /// <summary>
        /// 库可以解压 tar 归档。
        /// </summary>
        ExtractTar = 0x80,
        /// <summary>
        /// 库可以解压 xz 归档。
        /// </summary>
        ExtractXz = 0x100,
        /// <summary>
        /// 库可以解压所有支持的归档类型。
        /// </summary>
        ExtractAll = Extract7zAll|ExtractZip|ExtractRar|ExtractGzip|ExtractBzip2|ExtractTar|ExtractXz,
        /// <summary>
        /// 库可以使用 LZMA 方法将数据压缩为 7zip 归档。
        /// </summary>
        Compress7z = 0x200,
        /// <summary>
        /// 库可以使用 LZMA2 方法将数据压缩为 7zip 归档。
        /// </summary>
        Compress7zLZMA2 = 0x400,
        /// <summary>
        /// 库可以使用所有已知方法将数据压缩为 7zip 归档。
        /// </summary>
        Compress7zAll = Compress7z|Compress7zLZMA2|0x800,
        /// <summary>
        /// 库可以将数据压缩为 tar 归档。
        /// </summary>
        CompressTar = 0x1000,
        /// <summary>
        /// 库可以将数据压缩为 gzip 归档。
        /// </summary>
        CompressGzip = 0x2000,
        /// <summary>
        /// 库可以将数据压缩为 bzip2 归档。
        /// </summary>
        CompressBzip2 = 0x4000,
        /// <summary>
        /// 库可以将数据压缩为 xz 归档。
        /// </summary>
        CompressXz = 0x8000,
        /// <summary>
        /// 库可以将数据压缩为 zip 归档。
        /// </summary>
        CompressZip = 0x10000,
        /// <summary>
        /// 库可以将数据压缩为所有支持的归档类型。
        /// </summary>
        CompressAll = Compress7zAll|CompressTar|CompressGzip|CompressBzip2|CompressXz|CompressZip,
        /// <summary>
        /// 库可以修改归档。
        /// </summary>
        Modify = 0x20000
    }
}
