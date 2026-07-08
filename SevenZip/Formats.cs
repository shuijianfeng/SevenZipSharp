namespace SevenZip
{
    using System;
    using System.Collections.Frozen;
    using System.Collections.Generic;
    using System.IO;

#if UNMANAGED
    /// <summary>
    /// 可读取的归档格式枚举。
    /// </summary>
    public enum InArchiveFormat
    {
        /// <summary>
        /// 开放的 7-zip 归档格式。
        /// </summary>  
        /// <remarks><a href="http://en.wikipedia.org/wiki/7-zip">维基百科信息</a></remarks> 
        SevenZip,
        /// <summary>
        /// 专有的 Arj 归档格式。
        /// </summary>
        /// <remarks><a href="http://en.wikipedia.org/wiki/ARJ">维基百科信息</a></remarks>
        Arj,
        /// <summary>
        /// 开放的 Bzip2 归档格式。
        /// </summary>
        /// <remarks><a href="http://en.wikipedia.org/wiki/Bzip2">维基百科信息</a></remarks>
        BZip2,
        /// <summary>
        /// Microsoft Cabinet 归档格式。
        /// </summary>
        /// <remarks><a href="http://en.wikipedia.org/wiki/Cabinet_(file_format)">维基百科信息</a></remarks>
        Cab,
        /// <summary>
        /// Microsoft 编译 HTML 帮助文件格式。
        /// </summary>
        /// <remarks><a href="http://en.wikipedia.org/wiki/Microsoft_Compiled_HTML_Help">维基百科信息</a></remarks>
        Chm,
        /// <summary>
        /// Microsoft 复合文件格式。
        /// </summary>
        /// <remarks><a href="http://en.wikipedia.org/wiki/Compound_File_Binary_Format">维基百科信息</a></remarks>
        Compound,
        /// <summary>
        /// 开放的 Cpio 归档格式。
        /// </summary>
        /// <remarks><a href="http://en.wikipedia.org/wiki/Cpio">维基百科信息</a></remarks>
        Cpio,
        /// <summary>
        /// 开放的 Debian 软件包格式。
        /// </summary>
        /// <remarks><a href="http://en.wikipedia.org/wiki/Deb_(file_format)">维基百科信息</a></remarks>
        Deb,
        /// <summary>
        /// 开放的 Gzip 归档格式。
        /// </summary>
        /// <remarks><a href="http://en.wikipedia.org/wiki/Gzip">维基百科信息</a></remarks>
        GZip,
        /// <summary>
        /// 开放的 ISO 磁盘映像格式。
        /// </summary>
        /// <remarks><a href="http://en.wikipedia.org/wiki/ISO_image">维基百科信息</a></remarks>
        Iso,
        /// <summary>
        /// 开放的 Lzh 归档格式。
        /// </summary>
        /// <remarks><a href="http://en.wikipedia.org/wiki/Lzh">维基百科信息</a></remarks>
        Lzh,
        /// <summary>
        /// 开放的核心 7-zip Lzma 原始归档格式。
        /// </summary>
        /// <remarks><a href="http://en.wikipedia.org/wiki/Lzma">维基百科信息</a></remarks>
        Lzma,
        /// <summary>
        /// Nullsoft 安装包格式。
        /// </summary>
        /// <remarks><a href="http://en.wikipedia.org/wiki/NSIS">维基百科信息</a></remarks>
        Nsis,
        /// <summary>
        /// GUID 分区表。
        /// </summary>
        /// <remarks><a href="https://en.wikipedia.org/wiki/GUID_Partition_Table">维基百科信息</a></remarks>
        Gpt,
        /// <summary>
        /// RarLab Rar 归档格式，版本 5。
        /// </summary>
        /// <remarks><a href="http://en.wikipedia.org/wiki/Rar">维基百科信息</a></remarks>
        Rar,
        /// <summary>
        /// RarLab Rar 归档格式，版本 4 或更早。
        /// </summary>
        /// <remarks><a href="http://en.wikipedia.org/wiki/Rar">维基百科信息</a></remarks>
        Rar4,
        /// <summary>
        /// 开放的 Rpm 软件包格式。
        /// </summary>
        /// <remarks><a href="http://en.wikipedia.org/wiki/RPM_Package_Manager">维基百科信息</a></remarks>
        Rpm,
        /// <summary>
        /// 开放的分割文件格式。
        /// </summary>
        /// <remarks><a href="?">维基百科信息</a></remarks>
        Split,
        /// <summary>
        /// 开放的 Tar 归档格式。
        /// </summary>
        /// <remarks><a href="http://en.wikipedia.org/wiki/Tar_(file_format)">维基百科信息</a></remarks>
        Tar,
        /// <summary>
        /// Microsoft Windows 映像磁盘映像格式。
        /// </summary>
        /// <remarks><a href="http://en.wikipedia.org/wiki/Windows_Imaging_Format">维基百科信息</a></remarks>
        Wim,
        /// <summary>
        /// 开放的 LZW 归档格式；在 "compress" 程序中实现；也称为 "Z" 归档格式。
        /// </summary>
        /// <remarks><a href="http://en.wikipedia.org/wiki/Compress">维基百科信息</a></remarks>
        Lzw,
        /// <summary>
        /// 开放的 Zip 归档格式。
        /// </summary>
        /// <remarks><a href="http://en.wikipedia.org/wiki/ZIP_(file_format)">维基百科信息</a></remarks>
        Zip,
        /// <summary>
        /// 开放的 Udf 磁盘映像格式。
        /// </summary>
        Udf,
        /// <summary>
        /// Xar 开源归档格式。
        /// </summary>
        /// <remarks><a href="http://en.wikipedia.org/wiki/Xar_(archiver)">维基百科信息</a></remarks>
        Xar,
        /// <summary>
        /// Mub
        /// </summary>
        Mub,
        /// <summary>
        /// CD 上的 Macintosh 磁盘映像。
        /// </summary>
        /// <remarks><a href="http://en.wikipedia.org/wiki/HFS_Plus">维基百科信息</a></remarks>
        Hfs,
        /// <summary>
        /// Apple Mac OS X Disk Copy 磁盘映像格式。
        /// </summary>
        Dmg,
        /// <summary>
        /// 开放的 Xz 归档格式。
        /// </summary>
        /// <remarks><a href="http://en.wikipedia.org/wiki/Xz">维基百科信息</a></remarks>        
        XZ,
        /// <summary>
        /// MSLZ 归档格式。
        /// </summary>
        Mslz,
        /// <summary>
        /// Flash 视频格式。
        /// </summary>
        /// <remarks><a href="http://en.wikipedia.org/wiki/Flv">维基百科信息</a></remarks>
        Flv,
        /// <summary>
        /// Shockwave Flash 格式。
        /// </summary>
        /// <remarks><a href="http://en.wikipedia.org/wiki/Swf">维基百科信息</a></remarks>         
        Swf,
        /// <summary>
        /// Windows PE 可执行文件格式。
        /// </summary>
        /// <remarks><a href="http://en.wikipedia.org/wiki/Portable_Executable">维基百科信息</a></remarks>
        PE,
        /// <summary>
        /// Linux 可执行文件 Elf 格式。
        /// </summary>
        /// <remarks><a href="http://en.wikipedia.org/wiki/Executable_and_Linkable_Format">维基百科信息</a></remarks>
        Elf,
        /// <summary>
        /// Windows 安装程序数据库。
        /// </summary>
        /// <remarks><a href="http://en.wikipedia.org/wiki/Windows_Installer">维基百科信息</a></remarks>
        Msi,
        /// <summary>
        /// Microsoft 虚拟硬盘文件格式。
        /// </summary>
        /// <remarks><a href="http://en.wikipedia.org/wiki/VHD_%28file_format%29">维基百科信息</a></remarks>
        Vhd,
        /// <summary>
        /// SquashFS 文件系统格式。
        /// </summary>
        /// <remarks><a href="https://en.wikipedia.org/wiki/SquashFS">维基百科信息</a></remarks>
        SquashFS,
        /// <summary>
        /// Lzma86 文件格式。
        /// </summary>
        Lzma86,
        /// <summary>
        /// Dmitry 的部分匹配预测算法。
        /// </summary>
        /// <remarks><a href="https://en.wikipedia.org/wiki/Prediction_by_partial_matching">维基百科信息</a></remarks>
        Ppmd,
        /// <summary>
        /// TE 格式。
        /// </summary>
        TE,
        /// <summary>
        /// UEFIc 格式。
        /// </summary>
        /// <remarks><a href="https://en.wikipedia.org/wiki/Unified_Extensible_Firmware_Interface">维基百科信息</a></remarks>
        UEFIc,
        /// <summary>
        /// UEFIs 格式。
        /// </summary>
        /// <remarks><a href="https://en.wikipedia.org/wiki/Unified_Extensible_Firmware_Interface">维基百科信息</a></remarks>
        UEFIs,
        /// <summary>
        /// 压缩 ROM 文件系统格式。
        /// </summary>
        /// <remarks><a href="https://en.wikipedia.org/wiki/Cramfs">维基百科信息</a></remarks>
        CramFS,
        /// <summary>
        /// APM 格式。
        /// </summary>
        APM,
        /// <summary>
        /// Swfc 格式。
        /// </summary>
        Swfc,
        /// <summary>
        /// NTFS 文件系统格式。
        /// </summary>
        /// <remarks><a href="https://en.wikipedia.org/wiki/NTFS">维基百科信息</a></remarks>
        Ntfs,
        /// <summary>
        /// FAT 文件系统格式。
        /// </summary>
        /// <remarks><a href="https://en.wikipedia.org/wiki/File_Allocation_Table">维基百科信息</a></remarks>
        Fat,
        /// <summary>
        /// MBR 格式。
        /// </summary>
        /// <remarks><a href="https://en.wikipedia.org/wiki/Master_boot_record">维基百科信息</a></remarks>
        Mbr,
        /// <summary>
        /// Mach-O 文件格式。
        /// </summary>
        /// <remarks><a href="https://en.wikipedia.org/wiki/Mach-O">维基百科信息</a></remarks>
        MachO,
        /// <summary>
        /// Apple 文件系统格式。
        /// </summary>
        /// <remarks><a href="https://en.wikipedia.org/wiki/Apple_File_System">维基百科信息</a></remarks>
        Apfs
    }

    /// <summary>
    /// 可写入的归档格式枚举。
    /// </summary>    
    public enum OutArchiveFormat
    {
        /// <summary>
        /// 开放的 7-zip 归档格式。
        /// </summary>
        /// <remarks><a href="http://en.wikipedia.org/wiki/7-zip">维基百科信息</a></remarks>
        SevenZip,
        /// <summary>
        /// 开放的 Zip 归档格式。
        /// </summary>
        /// <remarks><a href="http://en.wikipedia.org/wiki/ZIP_(file_format)">维基百科信息</a></remarks>
        Zip,
        /// <summary>
        /// 开放的 Gzip 归档格式。
        /// </summary>
        /// <remarks><a href="http://en.wikipedia.org/wiki/Gzip">维基百科信息</a></remarks>
        GZip,
        /// <summary>       
        /// 开放的 Bzip2 归档格式。
        /// </summary>
        /// <remarks><a href="http://en.wikipedia.org/wiki/Bzip2">维基百科信息</a></remarks>
        BZip2,
        /// <summary>
        /// Microsoft Windows 映像磁盘映像格式。
        /// </summary>
        /// <remarks><a href="http://en.wikipedia.org/wiki/Windows_Imaging_Format">维基百科信息</a></remarks>
        Wim,
        /// <summary>
        /// 开放的 Tar 归档格式。
        /// </summary>
        /// <remarks><a href="http://en.wikipedia.org/wiki/Tar_(file_format)">维基百科信息</a></remarks>
        Tar,
        /// <summary>
        /// 开放的 Xz 归档格式。
        /// </summary>
        /// <remarks><a href="http://en.wikipedia.org/wiki/Xz">维基百科信息</a></remarks>        
        XZ
    }

    /// <summary>
    /// 压缩级别枚举
    /// </summary>
    public enum CompressionLevel
    {
        /// <summary>
        /// 无压缩
        /// </summary>
        None,
        /// <summary>
        /// 极低压缩级别
        /// </summary>
        Fast,
        /// <summary>
        /// 低压缩级别
        /// </summary>
        Low,
        /// <summary>
        /// 正常压缩级别（默认）
        /// </summary>
        Normal,
        /// <summary>
        /// 高压缩级别
        /// </summary>
        High,
        /// <summary>
        /// 最佳压缩级别（速度慢）
        /// </summary>
        Ultra
    }

    /// <summary>
    /// 压缩方法枚举。
    /// </summary>
    /// <remarks>某些方法仅适用于 Zip 格式，某些方法仅适用于 7-zip。</remarks>
    public enum CompressionMethod
    {
        /// <summary>
        /// Zip 或 7-zip|无压缩方法。
        /// </summary>
        Copy,
        /// <summary>
        /// Zip|Deflate 方法。
        /// </summary>
        Deflate,
        /// <summary>
        /// Zip|Deflate64 方法。
        /// </summary>
        Deflate64,
        /// <summary>
        /// Zip 或 7-zip|Bzip2 方法。
        /// </summary>
        /// <remarks><a href="http://en.wikipedia.org/wiki/Cabinet_(file_format)">维基百科信息</a></remarks>
        BZip2,
        /// <summary>
        /// Zip 或 7-zip|基于 Lempel-Ziv 算法的 LZMA 方法，是 7-zip 的默认方法。
        /// </summary>
        Lzma,
        /// <summary>
        /// 7-zip|LZMA 版本 2，具有改进的多线程支持，通常可略微减小归档大小。
        /// </summary>
        Lzma2,
        /// <summary>
        /// Zip 或 7-zip|基于 Dmitry Shkarin 的 PPMdH 源代码的 PPMd 方法，对文本压缩非常高效。
        /// </summary>
        /// <remarks><a href="http://en.wikipedia.org/wiki/Prediction_by_Partial_Matching">维基百科信息</a></remarks>
        Ppmd,
        /// <summary>
        /// 不更改方法。
        /// </summary>
        Default
    }

    /// <summary>
    /// 归档格式例程
    /// </summary>
    public static class Formats
    {
        /*/// <summary>
        /// 获取指定枚举类型的最大值。
        /// </summary>
        /// <param name="type">枚举类型</param>
        /// <returns>最大值</returns>
        internal static int GetMaxValue(Type type)
        {
            List<int> enumList = new List<int>((IEnumerable<int>)Enum.GetValues(type));
            enumList.Sort();
            return enumList[enumList.Count - 1];
        }*/

        /// <summary>
        /// 用于 7-zip COM 互操作的可读取归档格式接口 GUID 列表。
        /// </summary>
        internal static readonly FrozenDictionary<InArchiveFormat, Guid> InFormatGuids =
            new Dictionary<InArchiveFormat, Guid>
            #region InFormatGuids initialization

            {
                {InArchiveFormat.SevenZip,  new Guid("23170f69-40c1-278a-1000-000110070000")},
                {InArchiveFormat.Arj,       new Guid("23170f69-40c1-278a-1000-000110040000")},
                {InArchiveFormat.BZip2,     new Guid("23170f69-40c1-278a-1000-000110020000")},
                {InArchiveFormat.Cab,       new Guid("23170f69-40c1-278a-1000-000110080000")},
                {InArchiveFormat.Chm,       new Guid("23170f69-40c1-278a-1000-000110e90000")},
                {InArchiveFormat.Compound,  new Guid("23170f69-40c1-278a-1000-000110e50000")},
                {InArchiveFormat.Cpio,      new Guid("23170f69-40c1-278a-1000-000110ed0000")},
                {InArchiveFormat.Deb,       new Guid("23170f69-40c1-278a-1000-000110ec0000")},
                {InArchiveFormat.GZip,      new Guid("23170f69-40c1-278a-1000-000110ef0000")},
                {InArchiveFormat.Iso,       new Guid("23170f69-40c1-278a-1000-000110e70000")},
                {InArchiveFormat.Lzh,       new Guid("23170f69-40c1-278a-1000-000110060000")},
                {InArchiveFormat.Lzma,      new Guid("23170f69-40c1-278a-1000-0001100a0000")},
                {InArchiveFormat.Nsis,      new Guid("23170f69-40c1-278a-1000-000110090000")},
                {InArchiveFormat.Gpt,       new Guid("23170f69-40c1-278a-1000-000110cb0000")},
                {InArchiveFormat.Rar,       new Guid("23170f69-40c1-278a-1000-000110CC0000")},
                {InArchiveFormat.Rar4,      new Guid("23170f69-40c1-278a-1000-000110030000")},
                {InArchiveFormat.Rpm,       new Guid("23170f69-40c1-278a-1000-000110eb0000")},
                {InArchiveFormat.Split,     new Guid("23170f69-40c1-278a-1000-000110ea0000")},
                {InArchiveFormat.Tar,       new Guid("23170f69-40c1-278a-1000-000110ee0000")},
                {InArchiveFormat.Wim,       new Guid("23170f69-40c1-278a-1000-000110e60000")},
                {InArchiveFormat.Lzw,       new Guid("23170f69-40c1-278a-1000-000110050000")},
                {InArchiveFormat.Zip,       new Guid("23170f69-40c1-278a-1000-000110010000")},
                {InArchiveFormat.Udf,       new Guid("23170f69-40c1-278a-1000-000110E00000")},
                {InArchiveFormat.Xar,       new Guid("23170f69-40c1-278a-1000-000110E10000")},
                {InArchiveFormat.Mub,       new Guid("23170f69-40c1-278a-1000-000110E20000")},
                {InArchiveFormat.Hfs,       new Guid("23170f69-40c1-278a-1000-000110E30000")},
                {InArchiveFormat.Dmg,       new Guid("23170f69-40c1-278a-1000-000110E40000")},
                {InArchiveFormat.XZ,        new Guid("23170f69-40c1-278a-1000-0001100C0000")},
                {InArchiveFormat.Mslz,      new Guid("23170f69-40c1-278a-1000-000110D50000")},
                {InArchiveFormat.PE,        new Guid("23170f69-40c1-278a-1000-000110DD0000")},
                {InArchiveFormat.Elf,       new Guid("23170f69-40c1-278a-1000-000110DE0000")},
                {InArchiveFormat.Swf,       new Guid("23170f69-40c1-278a-1000-000110D70000")},
                {InArchiveFormat.Vhd,       new Guid("23170f69-40c1-278a-1000-000110DC0000")},
                {InArchiveFormat.Flv,       new Guid("23170f69-40c1-278a-1000-000110D60000")},
                {InArchiveFormat.SquashFS,  new Guid("23170f69-40c1-278a-1000-000110D20000")},
                {InArchiveFormat.Lzma86,    new Guid("23170f69-40c1-278a-1000-0001100B0000")},
                {InArchiveFormat.Ppmd,      new Guid("23170f69-40c1-278a-1000-0001100D0000")},
                {InArchiveFormat.TE,        new Guid("23170f69-40c1-278a-1000-000110CF0000")},
                {InArchiveFormat.UEFIc,     new Guid("23170f69-40c1-278a-1000-000110D00000")},
                {InArchiveFormat.UEFIs,     new Guid("23170f69-40c1-278a-1000-000110D10000")},
                {InArchiveFormat.CramFS,    new Guid("23170f69-40c1-278a-1000-000110D30000")},
                {InArchiveFormat.APM,       new Guid("23170f69-40c1-278a-1000-000110D40000")},
                {InArchiveFormat.Swfc,      new Guid("23170f69-40c1-278a-1000-000110D80000")},
                {InArchiveFormat.Ntfs,      new Guid("23170f69-40c1-278a-1000-000110D90000")},
                {InArchiveFormat.Fat,       new Guid("23170f69-40c1-278a-1000-000110DA0000")},
                {InArchiveFormat.Mbr,       new Guid("23170f69-40c1-278a-1000-000110DB0000")},
                {InArchiveFormat.MachO,     new Guid("23170f69-40c1-278a-1000-000110DF0000")},
                {InArchiveFormat.Apfs,      new Guid("23170f69-40c1-278a-1000-000110C30000")}
            }.ToFrozenDictionary();

        #endregion

        /// <summary>
        /// 用于 7-zip COM 互操作的可写入归档格式接口 GUID 列表。
        /// </summary>
        internal static readonly FrozenDictionary<OutArchiveFormat, Guid> OutFormatGuids =
            new Dictionary<OutArchiveFormat, Guid>
            #region OutFormatGuids initialization

            {
                {OutArchiveFormat.SevenZip,     new Guid("23170f69-40c1-278a-1000-000110070000")},
                {OutArchiveFormat.Zip,          new Guid("23170f69-40c1-278a-1000-000110010000")},
                {OutArchiveFormat.BZip2,        new Guid("23170f69-40c1-278a-1000-000110020000")},
                {OutArchiveFormat.Wim,          new Guid("23170f69-40c1-278a-1000-000110E60000")},
                {OutArchiveFormat.GZip,         new Guid("23170f69-40c1-278a-1000-000110ef0000")},
                {OutArchiveFormat.Tar,          new Guid("23170f69-40c1-278a-1000-000110ee0000")},
                {OutArchiveFormat.XZ,           new Guid("23170f69-40c1-278a-1000-0001100C0000")}
            }.ToFrozenDictionary();

        #endregion

        internal static readonly FrozenDictionary<CompressionMethod, string> MethodNames =
            new Dictionary<CompressionMethod, string>
            #region MethodNames initialization

            {
                {CompressionMethod.Copy, "Copy"},
                {CompressionMethod.Deflate, "Deflate"},
                {CompressionMethod.Deflate64, "Deflate64"},
                {CompressionMethod.Lzma, "LZMA"},
                {CompressionMethod.Lzma2, "LZMA2"},
                {CompressionMethod.Ppmd, "PPMd"},
                {CompressionMethod.BZip2, "BZip2"}
            }.ToFrozenDictionary();

        #endregion

        internal static readonly FrozenDictionary<OutArchiveFormat, InArchiveFormat> InForOutFormats =
            new Dictionary<OutArchiveFormat, InArchiveFormat>
            #region InForOutFormats initialization

            {
                {OutArchiveFormat.SevenZip, InArchiveFormat.SevenZip},
                {OutArchiveFormat.GZip, InArchiveFormat.GZip},
                {OutArchiveFormat.BZip2, InArchiveFormat.BZip2},
                {OutArchiveFormat.Wim, InArchiveFormat.Wim},
                {OutArchiveFormat.Tar, InArchiveFormat.Tar},
                {OutArchiveFormat.XZ, InArchiveFormat.XZ},
                {OutArchiveFormat.Zip, InArchiveFormat.Zip}
            }.ToFrozenDictionary();

        #endregion

        /// <summary>
        /// 与特定扩展名对应的归档格式列表
        /// </summary>
        private static readonly FrozenDictionary<string, InArchiveFormat> InExtensionFormats =
            new Dictionary<string, InArchiveFormat>
            #region InExtensionFormats initialization

            {{"7z",     InArchiveFormat.SevenZip},
             {"gz",     InArchiveFormat.GZip},
             {"tar",    InArchiveFormat.Tar},
             {"rar",    InArchiveFormat.Rar},
             {"zip",    InArchiveFormat.Zip},
             {"lzma",   InArchiveFormat.Lzma},
             {"lzh",    InArchiveFormat.Lzh},
             {"arj",    InArchiveFormat.Arj},
             {"bz2",    InArchiveFormat.BZip2},
             {"cab",    InArchiveFormat.Cab},
             {"chm",    InArchiveFormat.Chm},
             {"deb",    InArchiveFormat.Deb},
             {"iso",    InArchiveFormat.Iso},
             {"rpm",    InArchiveFormat.Rpm},
             {"wim",    InArchiveFormat.Wim},
             {"udf",    InArchiveFormat.Udf},
             {"mub",    InArchiveFormat.Mub},
             {"xar",    InArchiveFormat.Xar},
             {"hfs",    InArchiveFormat.Hfs},
             {"dmg",    InArchiveFormat.Dmg},
             {"Z",      InArchiveFormat.Lzw},
             {"xz",     InArchiveFormat.XZ},
             {"flv",    InArchiveFormat.Flv},
             {"swf",    InArchiveFormat.Swf},
             {"exe",    InArchiveFormat.PE},
             {"dll",    InArchiveFormat.PE},
             {"vhd",    InArchiveFormat.Vhd},
             {"gpt",    InArchiveFormat.Gpt },
             {"ntfs",   InArchiveFormat.Ntfs }
        }.ToFrozenDictionary();

        #endregion

        /// <summary>
        /// 与特定签名对应的归档格式列表
        /// </summary>
        /// <remarks>基于<a href="http://www.garykessler.net/library/file_sigs.html">此站点</a>的信息。</remarks>
        internal static readonly FrozenDictionary<string, InArchiveFormat> InSignatureFormats =
            new Dictionary<string, InArchiveFormat>
            #region InSignatureFormats initialization

            {{"37-7A-BC-AF-27-1C",                                              InArchiveFormat.SevenZip},
            {"1F-8B-08",                                                        InArchiveFormat.GZip},
            {"75-73-74-61-72",                                                  InArchiveFormat.Tar},
            //257 byte offset
            {"52-61-72-21-1A-07-00",                                            InArchiveFormat.Rar4},
            {"52-61-72-21-1A-07-01-00",                                         InArchiveFormat.Rar},
            {"50-4B-03-04",                                                     InArchiveFormat.Zip},
            {"5D-00-00-40-00",                                                  InArchiveFormat.Lzma},
            {"2D-6C-68",                                                        InArchiveFormat.Lzh},
            //^ 2 byte offset
            {"1F-9D-90",                                                        InArchiveFormat.Lzw},
            {"60-EA",                                                           InArchiveFormat.Arj},
            {"42-5A-68",                                                        InArchiveFormat.BZip2},
            {"4D-53-43-46",                                                     InArchiveFormat.Cab},
            {"49-54-53-46",                                                     InArchiveFormat.Chm},
            {"21-3C-61-72-63-68-3E-0A-64-65-62-69-61-6E-2D-62-69-6E-61-72-79",  InArchiveFormat.Deb},
            {"30-37-30-37-30",                                                  InArchiveFormat.Cpio},
            {"43-44-30-30-31",                                                  InArchiveFormat.Iso},
            //^ 0x8001, 0x8801 or 0x9001 byte offset
            {"ED-AB-EE-DB",                                                     InArchiveFormat.Rpm},
            {"4D-53-57-49-4D-00-00-00",                                         InArchiveFormat.Wim},
            {"udf",                                                             InArchiveFormat.Udf},
            {"mub",                                                             InArchiveFormat.Mub},
            {"78-61-72-21",                                                     InArchiveFormat.Xar},
            //0x400 byte offset
            {"48-2B",                                                           InArchiveFormat.Hfs},
            {"FD-37-7A-58-5A",                                                  InArchiveFormat.XZ},
            {"46-4C-56",                                                        InArchiveFormat.Flv},
            {"46-57-53",                                                        InArchiveFormat.Swf},
            {"4D-5A",                                                           InArchiveFormat.PE},
            {"7F-45-4C-46",                                                     InArchiveFormat.Elf},
            {"78",                                                              InArchiveFormat.Dmg},
            {"63-6F-6E-65-63-74-69-78",                                         InArchiveFormat.Vhd},
            {"45-46-49-20-50-41-52-54-00-00-01-00",                             InArchiveFormat.Gpt}}.ToFrozenDictionary();
        #endregion

        internal static FrozenDictionary<InArchiveFormat, string> InSignatureFormatsReversed;

        static Formats()
        {
            var reversed = new Dictionary<InArchiveFormat, string>(InSignatureFormats.Count);

            foreach (var pair in InSignatureFormats)
            {
                reversed[pair.Value] = pair.Key;
            }

            InSignatureFormatsReversed = reversed.ToFrozenDictionary();
        }

        /// <summary>
        /// 根据指定的归档文件名获取 InArchiveFormat
        /// </summary>
        /// <param name="fileName">归档文件名</param>
        /// <param name="reportErrors">指示是否抛出异常</param>
        /// <returns>通过文件名扩展名识别的 InArchiveFormat</returns>
        /// <exception cref="System.ArgumentException"/>
        public static InArchiveFormat FormatByFileName(string fileName, bool reportErrors)
        {
            if (string.IsNullOrEmpty(fileName) && reportErrors)
            {
                throw new ArgumentException("File name is null or empty string!");
            }
            string extension = Path.GetExtension(fileName);

            if (extension.Length > 0)
            {
                extension = extension.Substring(1);
            }

            if (!InExtensionFormats.TryGetValue(extension, out var format))
            {
                if (reportErrors)
                {
                    throw new ArgumentException("Extension \"" + extension + "\" is not a supported archive file name extension.");
                }
            }

            return format;
        }
    }
#endif
}