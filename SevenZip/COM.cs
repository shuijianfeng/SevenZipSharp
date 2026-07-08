namespace SevenZip
{
    using System;
    using System.Collections.Frozen;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Runtime.InteropServices.Marshalling;
    using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;

#if UNMANAGED

    // ReSharper disable file ConvertToAutoProperty - 为了 UWP 兼容性。

    /// <summary>
    /// 用于修复 x64 和 x32 变体大小不匹配的结构。
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct PropArray
    {
        readonly uint _cElems;
        readonly IntPtr _pElems;
    }

    /// <summary>
    /// 带有特殊接口例程的 COM VARIANT 结构。
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    internal struct PropVariant
    {
        [FieldOffset(0)] private ushort _vt;
                /// <summary>
        /// FILETIME 变体值。
        /// </summary>
        [FieldOffset(8)] private readonly FILETIME _fileTime;

        /// <summary>
        /// 用于在 x64 位系统上修复变体大小的 PropArray 实例。
        /// </summary>
        [FieldOffset(8)]
        private readonly PropArray _propArray;
        [FieldOffset(8)] private short _boolVal;  // VT_BOOL 时使用

        [FieldOffset(8)] private IntPtr _value;
        [FieldOffset(8)] private uint _uInt32Value;
        [FieldOffset(8)] private int _int32Value;
        [FieldOffset(8)] private long _int64Value;
        [FieldOffset(8)] private ulong _uInt64Value;

        /// <summary>
        /// 获取或设置变体类型。
        /// </summary>
        public VarEnum VarType
        {
            get
            {
                return (VarEnum)_vt;
            }

            set
            {
                _vt = (ushort)value;
            }
        }

        /// <summary>
        /// 获取或设置 COM 变体的指针值。
        /// </summary>
        public IntPtr Value
        {
            get => _value;
            set => _value = value;
        }

        /// <summary>
        /// 获取或设置 COM 变体的 UInt32 值。
        /// </summary>

        public uint UInt32Value
        {
            get => _uInt32Value;
            set => _uInt32Value = value;
        }

        /// <summary>
        /// 获取或设置 COM 变体的 Int32 值。
        /// </summary>

        public int Int32Value
        {
            get => _int32Value;
            set => _int32Value = value;
        }

        /// <summary>
        /// 获取或设置 COM 变体的 Int64 值。
        /// </summary>

        public long Int64Value
        {
            get => _int64Value;
            set => _int64Value = value;
        }

        /// <summary>
        /// 获取或设置 COM 变体的 UInt64 值。
        /// </summary>

        public ulong UInt64Value
        {
            get => _uInt64Value;
            set => _uInt64Value = value;
        }

        /// <summary>
        /// 获取此 PropVariant 的对象。
        /// </summary>
        /// <returns></returns>
        public object Object
        {
            get
            {
                switch (VarType)
                {
                    case VarEnum.VT_BOOL:
                        return _boolVal != 0;
                    case VarEnum.VT_BSTR:
                        return Marshal.PtrToStringBSTR(Value);
                    case VarEnum.VT_EMPTY:
                        return null;
                    case VarEnum.VT_FILETIME:
                        try
                        {
                            return DateTime.FromFileTime(Int64Value);
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            return DateTime.MinValue;
                        }
                    case VarEnum.VT_UI8:
                        return UInt64Value;
                    case VarEnum.VT_UI4:
                        return UInt32Value;
                    case VarEnum.VT_I8:
                        return Int64Value;
                    case VarEnum.VT_I4:
                        return Int32Value;
                    case VarEnum.VT_I2:
                        return (short)Int32Value;
                    case VarEnum.VT_UI2:
                        return (ushort)Int32Value;
                    case VarEnum.VT_I1:
                        return (sbyte)Int32Value;
                    case VarEnum.VT_UI1:
                        return (byte)Int32Value;
                    default:
                        // 针对罕见/未知变体类型的回退处理：固定并使用
                        // Marshal.GetObjectForNativeVariant（不兼容 AOT，但仅
                        // 在遇到上述未处理的特殊类型时才会执行）。
                        var propHandle = GCHandle.Alloc(this, GCHandleType.Pinned);
                        try
                        {
                            return Marshal.GetObjectForNativeVariant(propHandle.AddrOfPinnedObject());
                        }
                        catch (NotSupportedException)
                        {
                            return 0;
                        }
                        finally
                        {
                            propHandle.Free();
                        }
                }
            }
        }

        public short BoolVal { get => _boolVal; set => _boolVal = value; }

        /// <summary>
        /// 确定指定的 System.Object 是否等于当前 PropVariant。
        /// </summary>
        /// <param name="obj">要与当前 PropVariant 比较的 System.Object。</param>
        /// <returns>如果指定的 System.Object 等于当前 PropVariant，则为 true；否则为 false。</returns>
        public override bool Equals(object obj)
        {
            return obj is PropVariant variant && Equals(variant);
        }

        /// <summary>
        /// 确定指定的 PropVariant 是否等于当前 PropVariant。
        /// </summary>
        /// <param name="afi">要与当前 PropVariant 比较的 PropVariant。</param>
        /// <returns>如果指定的 PropVariant 等于当前 PropVariant，则为 true；否则为 false。</returns>
        private bool Equals(PropVariant afi)
        {
            if (afi.VarType != VarType)
            {
                return false;
            }

            if (VarType != VarEnum.VT_BSTR)
            {
                return afi.Int64Value == Int64Value;
            }

            return afi.Value == Value;
        }

        /// <summary>
        ///  作为特定类型的哈希函数。
        /// </summary>
        /// <returns> 当前 PropVariant 的哈希代码。</returns>
        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        /// <summary>
        /// 返回表示当前 PropVariant 的 System.String。
        /// </summary>
        /// <returns>表示当前 PropVariant 的 System.String。</returns>
        public override string ToString()
        {
            return "[" + Value + "] " + Int64Value.ToString(CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// 确定两个指定的 PropVariant 实例是否被视为相等。
        /// </summary>
        /// <param name="afi1">要比较的第一个 PropVariant。</param>
        /// <param name="afi2">要比较的第二个 PropVariant。</param>
        /// <returns>如果两个指定的 PropVariant 实例被视为相等，则为 true；否则为 false。</returns>
        public static bool operator ==(PropVariant afi1, PropVariant afi2)
        {
            return afi1.Equals(afi2);
        }

        /// <summary>
        /// 确定两个指定的 PropVariant 实例是否被视为不相等。
        /// </summary>
        /// <param name="afi1">要比较的第一个 PropVariant。</param>
        /// <param name="afi2">要比较的第二个 PropVariant。</param>
        /// <returns>如果两个指定的 PropVariant 实例被视为不相等，则为 true；否则为 false。</returns>
        public static bool operator !=(PropVariant afi1, PropVariant afi2)
        {
            return !afi1.Equals(afi2);
        }
    }

    /// <summary>
    /// 存储文件解压模式。
    /// </summary>
    internal enum AskMode
    {
        /// <summary>
        /// 解压模式
        /// </summary>
        Extract = 0,
        /// <summary>
        /// 测试模式
        /// </summary>
        Test,
        /// <summary>
        /// 跳过模式
        /// </summary>
        Skip
    }

    /// <summary>
    /// 存储操作结果值。
    /// </summary>
    public enum OperationResult
    {
        /// <summary>
        /// 成功
        /// </summary>
        Ok = 0,
        /// <summary>
        /// 不支持的方法
        /// </summary>
        UnsupportedMethod,
        /// <summary>
        /// 发生数据错误
        /// </summary>
        DataError,
        /// <summary>
        /// 发生 CRC 错误
        /// </summary>
        CrcError,
        /// <summary>
        /// 文件不可用
        /// </summary>
        Unavailable,
        /// <summary>
        /// 意外的文件结尾
        /// </summary>
        UnexpectedEnd,
        /// <summary>
        /// 归档末尾之后有数据
        /// </summary>
        DataAfterEnd,
        /// <summary>
        /// 文件不是归档
        /// </summary>
        IsNotArc,
        /// <summary>
        /// 归档头错误
        /// </summary>
        HeadersError,
        /// <summary>
        /// 密码错误
        /// </summary>
        WrongPassword
    }

    /// <summary>
    /// 条目属性代码
    /// </summary>
    internal enum ItemPropId : uint
    {
        /// <summary>
        /// 无属性
        /// </summary>
        NoProperty = 0,
        MainSubfile,
        /// <summary>
        /// 处理程序条目索引
        /// </summary>
        HandlerItemIndex,
        /// <summary>
        /// 条目路径
        /// </summary>
        Path,
        /// <summary>
        /// 条目名称
        /// </summary>
        Name,
        /// <summary>
        /// 条目扩展名
        /// </summary>
        Extension,
        /// <summary>
        /// 如果条目是文件夹则为 true；否则为 false
        /// </summary>
        IsDirectory,
        /// <summary>
        /// 条目大小
        /// </summary>
        Size,
        /// <summary>
        /// 条目打包大小；通常不存在
        /// </summary>
        PackedSize,
        /// <summary>
        /// 条目属性；通常不存在
        /// </summary>
        Attributes,
        /// <summary>
        /// 条目创建时间；通常不存在
        /// </summary>
        CreationTime,
        /// <summary>
        /// 条目最后访问时间；通常不存在
        /// </summary>
        LastAccessTime,
        /// <summary>
        /// 条目最后写入时间
        /// </summary>
        LastWriteTime,
        /// <summary>
        /// 如果条目是固实的则为 true；否则为 false
        /// </summary>
        Solid,
        /// <summary>
        /// 如果条目有注释则为 true；否则为 false
        /// </summary>
        Commented,
        /// <summary>
        /// 如果条目已加密则为 true；否则为 false
        /// </summary>
        Encrypted,
        /// <summary>
        /// (?)
        /// </summary>
        SplitBefore,
        /// <summary>
        /// (?)
        /// </summary>
        SplitAfter,
        /// <summary>
        /// 字典大小(?)
        /// </summary>
        DictionarySize,
        /// <summary>
        /// 条目 CRC 校验和
        /// </summary>
        Crc,
        /// <summary>
        /// 条目类型(?)
        /// </summary>
        Type,
        /// <summary>
        /// (?)
        /// </summary>
        IsAnti,
        /// <summary>
        /// 压缩方法
        /// </summary>
        Method,
        /// <summary>
        /// (?)；通常不存在
        /// </summary>
        HostOS,
        /// <summary>
        /// 条目文件系统；通常不存在
        /// </summary>
        FileSystem,
        /// <summary>
        /// 条目用户(?)；通常不存在
        /// </summary>
        User,
        /// <summary>
        /// 条目组(?)；通常不存在
        /// </summary>
        Group,
        /// <summary>
        /// 块大小(?)
        /// </summary>
        Block,
        /// <summary>
        /// 条目注释；通常不存在
        /// </summary>
        Comment,
        /// <summary>
        /// 条目位置
        /// </summary>
        Position,
        /// <summary>
        /// 条目前缀(?)
        /// </summary>
        Prefix,
        /// <summary>
        /// 子目录数量
        /// </summary>
        NumSubDirs,
        /// <summary>
        /// 子文件数量
        /// </summary>
        NumSubFiles,
        /// <summary>
        /// 归档旧版解包器版本
        /// </summary>
        UnpackVersion,
        /// <summary>
        /// 卷(?)
        /// </summary>
        Volume,
        /// <summary>
        /// 是否为卷
        /// </summary>
        IsVolume,
        /// <summary>
        /// 偏移值(?)
        /// </summary>
        Offset,
        /// <summary>
        /// 链接(?)
        /// </summary>
        Links,
        /// <summary>
        /// 块数量
        /// </summary>
        NumBlocks,
        /// <summary>
        /// 卷数量(?)
        /// </summary>
        NumVolumes,
        /// <summary>
        /// 时间类型(?)
        /// </summary>
        TimeType,
        /// <summary>
        /// 64 位(?)
        /// </summary>
        Bit64,
        /// <summary>
        /// 大端序
        /// </summary>
        BigEndian,
        /// <summary>
        /// CPU(?)
        /// </summary>
        Cpu,
        /// <summary>
        /// 物理归档大小
        /// </summary>
        PhysicalSize,
        /// <summary>
        /// 头大小
        /// </summary>
        HeadersSize,
        /// <summary>
        /// 归档校验和
        /// </summary>
        Checksum,
        Characts,
        Va,
        Id,
        ShortName,
        CreatorApp,
        SectorSize,
        PosixAttrib,
        SymLink,
        Error,
        /// <summary>
        /// (?)
        /// </summary>
        TotalSize,
        /// <summary>
        /// (?)
        /// </summary>
        FreeSpace,
        /// <summary>
        /// 簇大小(?)
        /// </summary>
        ClusterSize,
        /// <summary>
        /// 卷名称(?)
        /// </summary>
        VolumeName,
        /// <summary>
        /// 本地条目名称(?)；通常不存在
        /// </summary>
        LocalName,
        /// <summary>
        /// (?)
        /// </summary>
        Provider,
        NtSecure,
        IsAltStream,
        IsAux,
        IsDeleted,
        IsTree,
        Sha1,
        Sha256,
        ErrorType,
        NumErrors,
        ErrorFlags,
        WarningFlags,
        Warning,
        NumStreams,
        NumAltStreams,
        AltStreamsSize,
        VirtualSize,
        UnpackSize,
        TotalPhySize,
        /// <summary>
        /// 卷的索引
        /// </summary>
        VolumeIndex,
        SubType,
        ShortComment,
        CodePage,
        IsNotArcType,
        PhySizeCantBeDetected,
        ZerosTailIsAllowed,
        TailSize,
        EmbeddedStubSize,
        NtReparse,
        HardLink,
        INode,
        StreamId,
        ReadOnly,
        OutName,
        CopyLink,
        NumDefined,
        /// <summary>
        /// 用户定义属性；通常不存在
        /// </summary>
        UserDefined = 0x10000
    }

    /// <summary>
    /// PropId 字符串名称字典包装器。
    /// </summary>
    internal static class PropIdToName
    {
        /// <summary>
        /// PropId 字符串名称
        /// </summary>
        public static readonly FrozenDictionary<ItemPropId, string> PropIdNames =
        #region Initialization
            new Dictionary<ItemPropId, string>(46)
            {
                {ItemPropId.Path, "Path"},
                {ItemPropId.Name, "Name"},
                {ItemPropId.IsDirectory, "Folder"},
                {ItemPropId.Size, "Size"},
                {ItemPropId.PackedSize, "Packed Size"},
                {ItemPropId.Attributes, "Attributes"},
                {ItemPropId.CreationTime, "Created"},
                {ItemPropId.LastAccessTime, "Accessed"},
                {ItemPropId.LastWriteTime, "Modified"},
                {ItemPropId.Solid, "Solid"},
                {ItemPropId.Commented, "Commented"},
                {ItemPropId.Encrypted, "Encrypted"},
                {ItemPropId.SplitBefore, "Split Before"},
                {ItemPropId.SplitAfter, "Split After"},
                {
                    ItemPropId.DictionarySize,
                    "Dictionary Size"
                    },
                {ItemPropId.Crc, "CRC"},
                {ItemPropId.Type, "Type"},
                {ItemPropId.IsAnti, "Anti"},
                {ItemPropId.Method, "Method"},
                {ItemPropId.HostOS, "Host OS"},
                {ItemPropId.FileSystem, "File System"},
                {ItemPropId.User, "User"},
                {ItemPropId.Group, "Group"},
                {ItemPropId.Block, "Block"},
                {ItemPropId.Comment, "Comment"},
                {ItemPropId.Position, "Position"},
                {ItemPropId.Prefix, "Prefix"},
                {
                    ItemPropId.NumSubDirs,
                    "Number of subdirectories"
                    },
                {
                    ItemPropId.NumSubFiles,
                    "Number of subfiles"
                    },
                {
                    ItemPropId.UnpackVersion,
                    "Unpacker version"
                    },
                {ItemPropId.VolumeIndex, "VolumeIndex"},
                {ItemPropId.Volume, "Volume"},
                {ItemPropId.IsVolume, "IsVolume"},
                {ItemPropId.Offset, "Offset"},
                {ItemPropId.Links, "Links"},
                {
                    ItemPropId.NumBlocks,
                    "Number of blocks"
                    },
                {
                    ItemPropId.NumVolumes,
                    "Number of volumes"
                    },
                {ItemPropId.TimeType, "Time type"},
                {ItemPropId.Bit64, "64-bit"},
                {ItemPropId.BigEndian, "Big endian"},
                {ItemPropId.Cpu, "CPU"},
                {
                    ItemPropId.PhysicalSize,
                    "Physical Size"
                    },
                {ItemPropId.HeadersSize, "Headers Size"},
                {ItemPropId.Checksum, "Checksum"},
                {ItemPropId.FreeSpace, "Free Space"},
                {ItemPropId.ClusterSize, "Cluster Size"}
            }.ToFrozenDictionary();
        #endregion
    }

    /// <summary>
    /// 7-zip IArchiveOpenCallback 导入接口，用于处理归档的打开。
    /// </summary>
    [GeneratedComInterface]
    [Guid("23170F69-40C1-278A-0000-000600100000")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal unsafe partial interface IArchiveOpenCallback
    {
        // ref ulong 替换为 IntPtr，因为处理程序通常传递 null 值
        // 使用 Marshal.ReadInt64 读取实际值
        /// <summary>
        /// 设置总数据大小
        /// </summary>
        /// <param name="files">文件指针</param>
        /// <param name="bytes">总大小（字节）</param>
        void SetTotal(
            IntPtr files,
            IntPtr bytes);

        /// <summary>
        /// 设置已完成大小
        /// </summary>
        /// <param name="files">文件指针</param>
        /// <param name="bytes">已完成大小（字节）</param>
        void SetCompleted(
            IntPtr files,
            IntPtr bytes);
    }

    /// <summary>
    /// 7-zip ICryptoGetTextPassword 导入接口，用于获取归档密码。
    /// </summary>
    [GeneratedComInterface]
    [Guid("23170F69-40C1-278A-0000-000500100000")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal unsafe partial interface ICryptoGetTextPassword
    {
        /// <summary>
        /// 获取归档密码
        /// </summary>
        /// <param name="password">归档密码</param>
        /// <returns>如果一切正常则返回零</returns>
        [PreserveSig]
        int CryptoGetTextPassword(
            [MarshalAs(UnmanagedType.BStr)] out string password);
    }

    /// <summary>
    /// 7-zip ICryptoGetTextPassword2 导入接口，用于设置归档密码。
    /// </summary>
    [GeneratedComInterface]
    [Guid("23170F69-40C1-278A-0000-000500110000")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal unsafe partial interface ICryptoGetTextPassword2
    {
        /// <summary>
        /// 设置归档密码
        /// </summary>
        /// <param name="passwordIsDefined">指定归档是否有密码（如果没有则为 0）</param>
        /// <param name="password">归档密码</param>
        /// <returns>如果一切正常则返回零</returns>
        [PreserveSig]
        int CryptoGetTextPassword2(
            ref int passwordIsDefined,
            [MarshalAs(UnmanagedType.BStr)] out string password);
    }

    /// <summary>
    /// 7-zip IArchiveExtractCallback 导入接口。
    /// </summary>
    [GeneratedComInterface]
    [Guid("23170F69-40C1-278A-0000-000600200000")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal unsafe partial interface IArchiveExtractCallback
    {
        /// <summary>
        /// 给出解压后归档文件的大小
        /// </summary>
        /// <param name="total">解压后归档文件的大小（字节）</param>
        void SetTotal(ulong total);

        /// <summary>
        /// SetCompleted 7-zip 函数
        /// </summary>
        /// <param name="completeValue"></param>
        void SetCompleted(in ulong completeValue);

        /// <summary>
        /// 获取用于文件解压的流
        /// </summary>
        /// <param name="index">归档文件表中的文件索引</param>
        /// <param name="outStream">指向流的指针</param>
        /// <param name="askExtractMode">解压模式</param>
        /// <returns>S_OK - 正常，S_FALSE - 跳过此文件</returns>
        [PreserveSig]
        int GetStream(
            uint index,
            [MarshalAs(UnmanagedType.Interface)] out ISequentialOutStream outStream,
            AskMode askExtractMode);

        /// <summary>
        /// PrepareOperation 7-zip 函数
        /// </summary>
        /// <param name="askExtractMode">询问模式</param>
        void PrepareOperation(AskMode askExtractMode);

        /// <summary>
        /// 设置操作结果
        /// </summary>
        /// <param name="operationResult">操作结果</param>
        void SetOperationResult(OperationResult operationResult);
    }

    /// <summary>
    /// 7-zip IArchiveUpdateCallback 导入接口。
    /// </summary>
    [GeneratedComInterface]
    [Guid("23170F69-40C1-278A-0000-000600800000")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal unsafe partial interface IArchiveUpdateCallback
    {
        /// <summary>
        /// 给出解压后归档文件的大小。
        /// </summary>
        /// <param name="total">解压后归档文件的大小（字节）</param>
        void SetTotal(ulong total);

        /// <summary>
        /// SetCompleted 7-zip 内部函数。
        /// </summary>
        /// <param name="completeValue"></param>
        void SetCompleted(in ulong completeValue);

        /// <summary>
        /// 获取归档更新模式。
        /// </summary>
        /// <param name="index">文件索引</param>
        /// <param name="newData">如果是新的则为 1，否则为 0</param>
        /// <param name="newProperties">如果是新的则为 1，否则为 0</param>
        /// <param name="indexInArchive">如果无关则为 -1</param>
        /// <returns></returns>
        [PreserveSig]
        int GetUpdateItemInfo(
            uint index, ref int newData,
            ref int newProperties, ref uint indexInArchive);

        /// <summary>
        /// 获取归档条目属性数据。
        /// </summary>
        /// <param name="index">条目索引</param>
        /// <param name="propId">属性标识符</param>
        /// <param name="value">属性值</param>
        /// <returns>正常则返回零</returns>
        [PreserveSig]



        int GetProperty(uint index, ItemPropId propId, ref PropVariant value);

        /// <summary>
        /// 获取用于读取的流。
        /// </summary>
        /// <param name="index">条目索引。</param>
        /// <param name="inStream">用于读取的 ISequentialInStream 指针。</param>
        /// <returns>正常则返回零</returns>
        [PreserveSig]
        int GetStream(
            uint index,
            [MarshalAs(UnmanagedType.Interface)] out ISequentialInStream inStream);

        /// <summary>
        /// 设置当前执行操作的结果。
        /// </summary>
        /// <param name="operationResult">结果值。</param>
        void SetOperationResult(OperationResult operationResult);

        /// <summary>
        /// EnumProperties 7-zip 内部函数。
        /// </summary>
        /// <param name="enumerator">枚举器指针。</param>
        /// <returns></returns>
        long EnumProperties(IntPtr enumerator);
    }

    /// <summary>
    /// 7-zip IArchiveOpenVolumeCallback 导入接口，用于处理归档卷。
    /// </summary>
    [GeneratedComInterface]
    [Guid("23170F69-40C1-278A-0000-000600300000")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal unsafe partial interface IArchiveOpenVolumeCallback
    {
        /// <summary>
        /// 获取归档属性数据。
        /// </summary>
        /// <param name="propId">属性标识符。</param>
        /// <param name="value">属性值。</param>
        [PreserveSig]
        int GetProperty(
            ItemPropId propId, ref PropVariant value);

        /// <summary>
        /// 获取用于读取卷的流。
        /// </summary>
        /// <param name="name">卷文件名。</param>
        /// <param name="inStream">用于读取的 IInStream 指针。</param>
        /// <returns>正常则返回零</returns>
        [PreserveSig]
        int GetStream(
            [MarshalAs(UnmanagedType.LPWStr)] string name,
            [MarshalAs(UnmanagedType.Interface)] out IInStream inStream);
    }

    /// <summary>
    /// 7-zip ISequentialInStream 导入接口
    /// </summary>
    [GeneratedComInterface]
    [Guid("23170F69-40C1-278A-0000-000300010000")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal unsafe partial interface ISequentialInStream
    {
        /// <summary>
        /// 向 7-zip 打包器写入数据
        /// </summary>
        /// <param name="data">可供写入的字节数组</param>
        /// <param name="size">数组大小</param>
        /// <returns>成功则返回 S_OK</returns>
        /// <remarks>如果 (size > 0) 且流中有字节，
        /// 此函数必须至少读取 1 个字节。
        /// 此函数允许读取少于 "size" 个字节。
        /// 如果需要精确数量的数据，必须在循环中调用 Read 函数。
        /// </remarks>
        int Read(
            [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] byte[] data,
            uint size);
    }

    /// <summary>
    /// 7-zip ISequentialOutStream 导入接口
    /// </summary>
    [GeneratedComInterface]
    [Guid("23170F69-40C1-278A-0000-000300020000")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal unsafe partial interface ISequentialOutStream
    {
        /// <summary>
        /// 向解压后文件流写入数据
        /// </summary>
        /// <param name="data">可供读取的字节数组</param>
        /// <param name="size">数组大小</param>
        /// <param name="processedSize">已处理的数据大小</param>
        /// <returns>成功则返回 S_OK</returns>
        /// <remarks>如果 size != 0，返回值为 S_OK 且 (*processedSize == 0)，
        /// 则流中没有更多字节。
        /// 如果 (size > 0) 且流中有字节，
        /// 此函数必须至少读取 1 个字节。
        /// 此函数允许写入少于 "size" 个字节。
        /// 如果需要精确数量的数据，必须在循环中调用 Write 函数。
        /// </remarks>
        [PreserveSig]
        int Write(
            [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] byte[] data,
            uint size, IntPtr processedSize);
    }

    /// <summary>
    /// 7-zip IInStream 导入接口
    /// </summary>
    [GeneratedComInterface]
    [Guid("23170F69-40C1-278A-0000-000300030000")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal unsafe partial interface IInStream
    {
        /// <summary>
        /// 读取例程
        /// </summary>
        /// <param name="data">要设置的字节数组</param>
        /// <param name="size">数组大小</param>
        /// <returns>正常则返回零</returns>
        int Read(
            [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] byte[] data,
            uint size);

        /// <summary>
        /// 定位例程
        /// </summary>
        /// <param name="offset">偏移值</param>
        /// <param name="seekOrigin">定位起始值</param>
        /// <param name="newPosition">新位置指针</param>
        void Seek(
            long offset, SeekOrigin seekOrigin, IntPtr newPosition);
    }

    /// <summary>
    /// 7-zip IOutStream 导入接口
    /// </summary>
    [GeneratedComInterface]
    [Guid("23170F69-40C1-278A-0000-000300040000")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal unsafe partial interface IOutStream
    {
        /// <summary>
        /// 写入例程
        /// </summary>
        /// <param name="data">要获取的字节数组</param>
        /// <param name="size">数组大小</param>
        /// <param name="processedSize">已处理大小</param>
        /// <returns>正常则返回零</returns>
        [PreserveSig]
        int Write(
            [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] byte[] data,
            uint size,
            IntPtr processedSize);

        /// <summary>
        /// 定位例程
        /// </summary>
        /// <param name="offset">偏移值</param>
        /// <param name="seekOrigin">定位起始值</param>
        /// <param name="newPosition">新位置指针</param>       
        void Seek(
            long offset, SeekOrigin seekOrigin, IntPtr newPosition);

        /// <summary>
        /// 设置大小例程
        /// </summary>
        /// <param name="newSize">新大小值</param>
        /// <returns>正常则返回零</returns>
        [PreserveSig]
        int SetSize(long newSize);
    }

    /// <summary>
    /// 7-zip 核心输入归档接口
    /// </summary>
    [GeneratedComInterface]
    [Guid("23170F69-40C1-278A-0000-000600600000")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal unsafe partial interface IInArchive
    {
        /// <summary>
        /// 打开归档以进行读取。
        /// </summary>
        /// <param name="stream">归档文件流</param>
        /// <param name="maxCheckStartPosition">检查的最大起始位置</param>
        /// <param name="openArchiveCallback">打开归档的回调</param>
        /// <returns></returns>
        [PreserveSig]
        int Open(
            IInStream stream,
            in ulong maxCheckStartPosition,
            [MarshalAs(UnmanagedType.Interface)] IArchiveOpenCallback openArchiveCallback);

        /// <summary>
        /// 关闭归档。
        /// </summary>
        void Close();

        /// <summary>
        /// 获取归档文件表中的文件数量。
        /// </summary>
        /// <returns>归档中的文件数量</returns>
        uint GetNumberOfItems();

        /// <summary>
        /// 检索特定属性数据。
        /// </summary>
        /// <param name="index">归档文件表中的文件索引</param>
        /// <param name="propId">属性代码</param>
        /// <param name="value">属性变体值</param>
        void GetProperty(
            uint index,
            ItemPropId propId,
           ref PropVariant value); // PropVariant

        /// <summary>
        /// 从已打开的归档中解压文件。
        /// </summary>
        /// <param name="indexes">要解压的文件索引（必须已排序）</param>
        /// <param name="numItems">0xFFFFFFFF 表示所有文件</param>
        /// <param name="testMode">testMode != 0 表示"测试文件操作"</param>
        /// <param name="extractCallback">用于操作处理的 IArchiveExtractCallback</param>
        /// <returns>成功则返回 0</returns>
        [PreserveSig]
        int Extract(
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] uint[] indexes,
            uint numItems,
            int testMode,
            [MarshalAs(UnmanagedType.Interface)] IArchiveExtractCallback extractCallback);

        /// <summary>
        /// 获取归档属性数据
        /// </summary>
        /// <param name="propId">归档属性标识符</param>
        /// <param name="value">归档属性值</param>
        void GetArchiveProperty(
            ItemPropId propId, // PROPID
            ref PropVariant value); // PropVariant

        /// <summary>
        /// 获取属性数量
        /// </summary>
        /// <returns>属性数量</returns>
        uint GetNumberOfProperties();

        /// <summary>
        /// 获取属性信息
        /// </summary>
        /// <param name="index">条目索引</param>
        /// <param name="name">名称</param>
        /// <param name="propId">属性标识符</param>
        /// <param name="varType">变体类型</param>
        void GetPropertyInfo(
            uint index,
            [MarshalAs(UnmanagedType.BStr)] out string name,
            out ItemPropId propId, // PROPID
            out ushort varType); //VARTYPE

        /// <summary>
        /// 获取归档属性数量
        /// </summary>
        /// <returns>归档属性数量</returns>
        uint GetNumberOfArchiveProperties();

        /// <summary>
        /// 获取归档属性信息
        /// </summary>
        /// <param name="index">条目索引</param>
        /// <param name="name">名称</param>
        /// <param name="propId">属性标识符</param>
        /// <param name="varType">变体类型</param>
        void GetArchivePropertyInfo(
            uint index,
            [MarshalAs(UnmanagedType.BStr)] out string name,
            out ItemPropId propId, // PROPID
            out ushort varType); //VARTYPE
    }

    /// <summary>
    /// 7-zip 核心输出归档接口
    /// </summary>
    [GeneratedComInterface]
    [Guid("23170F69-40C1-278A-0000-000600A00000")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal unsafe partial interface IOutArchive
    {
        /// <summary>
        /// 更新归档条目
        /// </summary>
        /// <param name="outStream">用于写入归档数据的 ISequentialOutStream 指针</param>
        /// <param name="numItems">归档条目数量</param>
        /// <param name="updateCallback">IArchiveUpdateCallback 指针</param>
        /// <returns>正常则返回零</returns>
        [PreserveSig]
        int UpdateItems(
            [MarshalAs(UnmanagedType.Interface)] ISequentialOutStream outStream,
            uint numItems,
            [MarshalAs(UnmanagedType.Interface)] IArchiveUpdateCallback updateCallback);

        /// <summary>
        /// 获取文件时间类型(?)
        /// </summary>
        /// <param name="type">类型指针</param>
        void GetFileTimeType(IntPtr type);
    }

    /// <summary>
    /// 7-zip ISetProperties 接口，用于设置各种归档属性
    /// </summary>
    [GeneratedComInterface]
    [Guid("23170F69-40C1-278A-0000-000600030000")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal unsafe partial interface ISetProperties
    {
        /// <summary>
        /// 设置归档属性
        /// </summary>
        /// <param name="names">属性名称</param>
        /// <param name="values">属性值</param>
        /// <param name="numProperties">属性数量</param>
        /// <returns></returns>        
        int SetProperties(IntPtr names, IntPtr values, int numProperties);
    }
#endif
}



