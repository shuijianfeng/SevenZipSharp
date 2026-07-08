namespace SevenZip
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;

    using SevenZip.Sdk;
    using SevenZip.Sdk.Compression.Lzma;

    /// <summary>
    /// 将数据打包为 7-Zip 支持的归档格式的类。
    /// </summary>
    /// <example>
    /// var compr = new SevenZipCompressor();
    /// compr.CompressDirectory(@"C:\Dir", @"C:\Archive.7z");
    /// </example>
    public sealed partial class SevenZipCompressor
#if UNMANAGED
        : SevenZipBase
#endif
    {
#if UNMANAGED

#region 字段

        private bool _compressingFilesOnDisk;

        /// <summary>
        /// 获取或设置归档压缩级别。
        /// </summary>
        public CompressionLevel CompressionLevel { get; set; }

        private OutArchiveFormat _archiveFormat = OutArchiveFormat.SevenZip;
        private CompressionMethod _compressionMethod = CompressionMethod.Default;

        /// <summary>
        /// 获取自定义压缩参数 - 仅限高级用户使用。
        /// </summary>
        public Dictionary<string, string> CustomParameters { get; private set; }

        private long _volumeSize;
        private string _archiveName;

        /// <summary>
        /// 获取或设置一个值，指示是否将空目录包含到归档中。默认为 true。
        /// </summary>
        public bool IncludeEmptyDirectories { get; set; }

        /// <summary>
        /// 获取或设置一个值，指示是否为 CompressDirectory 保留目录根。
        /// </summary>
        public bool PreserveDirectoryRoot { get; set; }

        /// <summary>
        /// 获取或设置一个值，指示是否保留目录结构。
        /// </summary>
        public bool DirectoryStructure { get; set; }

        private bool _directoryCompress;

        /// <summary>
        /// 获取或设置压缩模式。
        /// </summary>
        public CompressionMode CompressionMode { get; set; }

        private UpdateData _updateData;
        private uint _oldFilesCount;

        /// <summary>
        /// 获取或设置一个值，指示是否加密 7-Zip 归档头。
        /// </summary>
        public bool EncryptHeaders { get; set; }

        /// <summary>
        /// 获取或设置一个值，指示是否仅压缩以写入方式打开的文件。
        /// </summary>
        public bool ScanOnlyWritable { get; set; }

        /// <summary>
        /// 获取或设置 zip 归档的加密方法。
        /// </summary>
        public ZipEncryptionMethod ZipEncryptionMethod { get; set; }

        /// <summary>
        /// 获取或设置临时文件夹路径。
        /// </summary>
        public string TempFolderPath { get; set; }

        /// <summary>
        /// 获取或设置默认归档项名称，当待压缩项没有名称时使用，
        /// 例如，当压缩 MemoryStream 实例时。
        /// </summary>
        public string DefaultItemName { get; set; }

        /// <summary>
        /// 获取或设置一个值，指示是否尽可能快地压缩，不触发事件。
        /// </summary>
        public bool FastCompression { get; set; }

#endregion

#endif
        private static volatile int _lzmaDictionarySize = 1 << 22;

#if UNMANAGED

        private void CommonInit()
        {
            DirectoryStructure = true;
            IncludeEmptyDirectories = true;
            CompressionLevel = CompressionLevel.Normal;
            CompressionMode = CompressionMode.Create;
            ZipEncryptionMethod = ZipEncryptionMethod.ZipCrypto;
            CustomParameters = new Dictionary<string, string>();
            _updateData = new UpdateData();
            DefaultItemName = "default";
        }

        /// <summary>
        /// 初始化 SevenZipCompressor 类的新实例。 
        /// </summary>
        public SevenZipCompressor()
        {
            try
            {
                TempFolderPath = Path.GetTempPath();
            }
            catch (System.Security.SecurityException) // 不允许访问注册表等。
            {
                throw new SevenZipCompressionFailedException("Path.GetTempPath() threw a System.Security.SecurityException. You must call SevenZipCompressor constructor overload with your own temporary path.");
            }

            CommonInit();
        }

        /// <summary>
        /// 初始化 SevenZipCompressor 类的新实例。 
        /// </summary>
        /// <param name="temporaryPath">自定义临时路径（默认值在无参构造函数重载中设置。）</param>
        public SevenZipCompressor(string temporaryPath)
        {
            TempFolderPath = temporaryPath;

            if (!Directory.Exists(TempFolderPath))
            {
                try
                {
                    Directory.CreateDirectory(TempFolderPath);
                }
                catch (Exception)
                {
                    throw new SevenZipCompressionFailedException("The specified temporary path is invalid.");
                }
            }

            CommonInit();
        }
#endif

        /// <summary>
        /// 检查指定流是否支持压缩。
        /// </summary>
        /// <param name="stream">要检查的流。</param>
        private static void ValidateStream(Stream stream)
        {
            if (!stream.CanWrite || !stream.CanSeek)
            {
                throw new ArgumentException("The specified stream can not seek or is not writable.", nameof(stream));
            }
        }

#if UNMANAGED

#region 私有函数

        private IOutArchive MakeOutArchive(IInStream inArchiveStream)
        {
            var inArchive = SevenZipLibraryManager.InArchive(Formats.InForOutFormats[_archiveFormat], this);

            using (var openCallback = GetArchiveOpenCallback())
            {
                ulong checkPos = 1 << 15;

                if (inArchive.Open(inArchiveStream, in checkPos, openCallback) != (int) OperationResult.Ok)
                {
                    if (!ThrowException(null, new SevenZipArchiveException("Can not update the archive: Open() failed.")))
                    {
                        return null;
                    }
                }

                _oldFilesCount = inArchive.GetNumberOfItems();
            }

            return (IOutArchive) inArchive;
        }

        /// <summary>
        /// 保证 SetCompressionProperties 函数的正确执行
        /// </summary>
        /// <param name="method">要检查的压缩方法</param>
        /// <returns>指示指定方法对当前 ArchiveFormat 是否有效的值</returns>
        private bool MethodIsValid(CompressionMethod method)
        {
            if (method == CompressionMethod.Default)
            {
                return true;
            }

            return _archiveFormat switch
            {
                OutArchiveFormat.GZip => method == CompressionMethod.Deflate,
                OutArchiveFormat.BZip2 => method == CompressionMethod.BZip2,
                OutArchiveFormat.SevenZip => method != CompressionMethod.Deflate && method != CompressionMethod.Deflate64,
                OutArchiveFormat.Tar => method == CompressionMethod.Copy,
                OutArchiveFormat.Zip => method != CompressionMethod.Lzma2,
                _ => true
            };
        }

        private bool SwitchIsInCustomParameters(string name)
        {
            return CustomParameters.ContainsKey(name);
        }

        /// <summary>
        /// 设置压缩属性
        /// </summary>
        private void SetCompressionProperties()
        {
            switch (_archiveFormat)
            {
                case OutArchiveFormat.Tar:
                {
                    break;
                }
                default:
                {
                    var setter =
                        CompressionMode == CompressionMode.Create && _updateData.FileNamesToModify == null ? 
                            (ISetProperties) SevenZipLibraryManager.OutArchive(_archiveFormat, this) : 
                            (ISetProperties) SevenZipLibraryManager.InArchive(Formats.InForOutFormats[_archiveFormat], this);
                    
                    if (setter == null)
                    {
                        if (!ThrowException(null, new CompressionFailedException("The specified archive format is unsupported.")))
                        {
                            return;
                        }
                    }

                    if (_volumeSize > 0 && ArchiveFormat != OutArchiveFormat.SevenZip)
                    {
                        throw new CompressionFailedException("Unfortunately, the creation of multi-volume non-7Zip archives is not implemented.");
                    }

#region 检查“禁止”参数

                    if (CustomParameters.ContainsKey("x"))
                    {
                        if (!ThrowException(null, new CompressionFailedException("Use the \"CompressionLevel\" property instead of the \"x\" parameter.")))
                        {
                            return;
                        }
                    }

                    if (CustomParameters.ContainsKey("em"))
                    {
                        if (!ThrowException(null, new CompressionFailedException("Use the \"ZipEncryptionMethod\" property instead of the \"em\" parameter.")))
                        {
                            return;
                        }
                    }

                    if (CustomParameters.ContainsKey("m"))
                    {
                        if (!ThrowException(null, new CompressionFailedException("Use the \"CompressionMethod\" property instead of the \"m\" parameter.")))
                        {
                            return;
                        }
                    }

#endregion

                    var names = new List<IntPtr>(2 + CustomParameters.Count);
                    var values = new List<PropVariant>(2 + CustomParameters.Count);



#region 初始化压缩属性

                        names.Add(Marshal.StringToBSTR("x"));
                    values.Add(default(PropVariant));

                    if (_compressionMethod != CompressionMethod.Default)
                    {
                        names.Add(_archiveFormat == OutArchiveFormat.Zip ? 
                            Marshal.StringToBSTR("m") : 
                            Marshal.StringToBSTR("0"));

                        var pv = default(PropVariant);
                        pv.VarType = VarEnum.VT_BSTR;
                        pv.Value = Marshal.StringToBSTR(Formats.MethodNames[_compressionMethod]);

                        values.Add(pv);
                    }

                    foreach (var pair in CustomParameters)
                    {
                        #region 根据压缩方法验证参数。

                        if (_compressionMethod != CompressionMethod.Ppmd && (pair.Key.Equals("mem", StringComparison.Ordinal) || pair.Key.Equals("o", StringComparison.Ordinal)))
                        {
                            ThrowException(null, new CompressionFailedException($"Parameter \"{pair.Key}\" is only valid with the PPMd compression method."));
                        }

                        #endregion

                        names.Add(Marshal.StringToBSTR(pair.Key));
                        var pv = default(PropVariant);

                        var allDigits = pair.Value.Length > 0;
                        for (var ci = 0; ci < pair.Value.Length; ci++)
                        {
                            if (!char.IsDigit(pair.Value[ci]))
                            {
                                allDigits = false;
                                break;
                            }
                        }

                        if (allDigits)
                        {
                            pv.VarType = VarEnum.VT_UI4;
                            pv.UInt32Value = Convert.ToUInt32(pair.Value, CultureInfo.InvariantCulture);
                        }
                        else
                        {
                            pv.VarType = VarEnum.VT_BSTR;
                            pv.Value = Marshal.StringToBSTR(pair.Value);
                        }

                        values.Add(pv);
                    }

#endregion

#region 设置压缩级别

                    var clpv = values[0];
                    clpv.VarType = VarEnum.VT_UI4;

                    switch (CompressionLevel)
                    {
                        case CompressionLevel.None:
                        {
                            clpv.UInt32Value = 0;
                            break;
                        }
                        case CompressionLevel.Fast:
                        {
                            clpv.UInt32Value = 1;
                            break;
                        }
                        case CompressionLevel.Low:
                        {
                            clpv.UInt32Value = 3;
                            break;
                        }
                        case CompressionLevel.Normal:
                        {
                            clpv.UInt32Value = 5;
                            break;
                        }
                        case CompressionLevel.High:
                        {
                            clpv.UInt32Value = 7;
                            break;
                        }
                        case CompressionLevel.Ultra:
                        {
                            clpv.UInt32Value = 9;
                            break;
                        }
                    }

                    values[0] = clpv;

#endregion

#region 加密头

                    if (EncryptHeaders && _archiveFormat == OutArchiveFormat.SevenZip && !SwitchIsInCustomParameters("he"))
                    {
                        names.Add(Marshal.StringToBSTR("he"));
                        var tmp = default(PropVariant);
                        tmp.VarType = VarEnum.VT_BSTR;
                        tmp.Value = Marshal.StringToBSTR("on");
                        values.Add(tmp);
                    }

#endregion

#region Zip 加密

                    if (_archiveFormat == OutArchiveFormat.Zip &&
                        ZipEncryptionMethod != ZipEncryptionMethod.ZipCrypto &&
                        !SwitchIsInCustomParameters("em"))
                    {
                        names.Add(Marshal.StringToBSTR("em"));

                        var tmp = default(PropVariant);
                        tmp.VarType = VarEnum.VT_BSTR;
                        tmp.Value = Marshal.StringToBSTR(ZipEncryptionMethod.ToString("G"));

                        values.Add(tmp);
                    }

#endregion

                    var namesHandle = GCHandle.Alloc(names.ToArray(), GCHandleType.Pinned);
                    var valuesHandle = GCHandle.Alloc(values.ToArray(), GCHandleType.Pinned);

                    try
                    {
                        setter?.SetProperties(namesHandle.AddrOfPinnedObject(), valuesHandle.AddrOfPinnedObject(), names.Count);
                    }
                    finally
                    {
                        namesHandle.Free();
                        valuesHandle.Free();
                    }

                    break;
                }
            }
        }

        /// <summary>
        /// 查找文件名的公共根
        /// </summary>
        /// <param name="files">文件名数组</param>
        /// <returns>公共根</returns>
        private static int CommonRoot(ICollection<string> files)
        {
            var splitFileNames = new List<string[]>(files.Count);

            foreach (var fn in files)
            {
                splitFileNames.Add(fn.Split(Path.DirectorySeparatorChar));
            }
            var minSplitLength = splitFileNames[0].Length - 1;

            if (files.Count > 1)
            {
                for (var i = 1; i < files.Count; i++)
                {
                    if (minSplitLength > splitFileNames[i].Length)
                    {
                        minSplitLength = splitFileNames[i].Length;
                    }
                }
            }

            // 以字符为单位计算公共前缀长度，不构建字符串。
            var commonLen = 0;

            for (var i = 0; i < minSplitLength; i++)
            {
                var common = true;

                for (var j = 1; j < files.Count; j++)
                {
                    if (splitFileNames[j - 1][i] != splitFileNames[j][i])
                    {
                        common = false;
                        break;
                    }
                }

                if (common)
                {
                    commonLen += splitFileNames[0][i].Length + 1; // +1 为分隔符
                }
                else
                {
                    break;
                }
            }

            return commonLen;
        }

        /// <summary>
        /// 验证公共根
        /// </summary>
        /// <param name="commonRootLength">文件名公共根的长度。</param>
        /// <param name="files">文件名数组</param>
        private static void CheckCommonRoot(IReadOnlyList<string> files, ref int commonRootLength)
        {
            string commonRoot;

            try
            {
                commonRoot = files[0].Substring(0, commonRootLength);
            }
            catch (ArgumentOutOfRangeException)
            {
                throw new SevenZipInvalidFileNamesException("invalid common root.");
            }

            if (commonRoot.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
            {
                commonRoot = commonRoot.Substring(0, commonRootLength - 1);
                commonRootLength--;
            }

            for (var i = 0; i < files.Count; i++)
            {
                if (!files[i].StartsWith(commonRoot, StringComparison.Ordinal))
                {
                    throw new SevenZipInvalidFileNamesException("invalid common root.");
                }
            }
        }

        /// <summary>
        /// 确保目录不为空
        /// </summary>
        /// <param name="directory">目录名</param>
        /// <returns>如果不为空则返回 False</returns>
        private static bool RecursiveDirectoryEmptyCheck(string directory)
        {
            var di = new DirectoryInfo(directory);

            if (di.GetFiles().Length > 0)
            {
                return false;
            }

            var empty = true;

            foreach (var cdi in di.GetDirectories())
            {
                empty &= RecursiveDirectoryEmptyCheck(cdi.FullName);
                if (!empty)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 为归档文件表生成特殊的 FileInfo 数组。
        /// </summary>
        /// <param name="files">要打包的文件数组。</param>
        /// <param name="commonRootLength">文件名公共根的长度</param>
        /// <param name="directoryCompress">指示是为特定目录中的文件生成数组还是仅为文件数组生成的值。</param>
        /// <param name="directoryStructure">保留目录结构。</param>
        /// <returns>归档文件表的特殊 FileInfo 数组。</returns>
        private static FileInfo[] ProduceFileInfoArray(
            IReadOnlyList<string> files, int commonRootLength,
            bool directoryCompress, bool directoryStructure)
        {
            var fis = new List<FileInfo>(files.Count);
            var commonRoot = files[0].Substring(0, commonRootLength);

            if (directoryCompress)
            {
                for (var i = 0; i < files.Count; i++)
                {
                    fis.Add(new FileInfo(files[i]));
                }
            }
            else
            {
                if (!directoryStructure)
                {
                    for (var i = 0; i < files.Count; i++)
                    {
                        var fn = files[i];
                        if (!Directory.Exists(fn))
                        {
                            fis.Add(new FileInfo(fn));
                        }
                    }
                }
                else
                {
                    var fns = new HashSet<string>(files.Count, StringComparer.Ordinal);
                    CheckCommonRoot(files, ref commonRootLength);

                    if (commonRootLength > 0)
                    {
                        commonRootLength++;

                        foreach (var f in files)
                        {
                            var splitAfn = f.Substring(commonRootLength).Split(Path.DirectorySeparatorChar);
                            var cfn = commonRoot;

                            foreach (var t in splitAfn)
                            {
                                cfn += Path.DirectorySeparatorChar + t;

                                if (fns.Add(cfn))
                                {
                                    fis.Add(new FileInfo(cfn));
                                }
                            }
                        }
                    }
                    else
                    {
                        foreach (var f in files)
                        {
                            var splitAfn = f.Substring(commonRootLength).Split(Path.DirectorySeparatorChar);
                            var cfn = splitAfn[0];

                            for (var i = 1; i < splitAfn.Length; i++)
                            {
                                cfn += Path.DirectorySeparatorChar + splitAfn[i];

                                if (fns.Add(cfn))
                                {
                                    fis.Add(new FileInfo(cfn));
                                }
                            }
                        }
                    }
                }
            }

            return fis.ToArray();
        }

        /// <summary>
        /// 用于在目录中添加文件的递归函数
        /// </summary>
        /// <param name="directory">目录</param>
        /// <param name="files">文件列表</param>
        /// <param name="searchPattern">搜索字符串，例如 "*.txt"</param>
        private void AddFilesFromDirectory(string directory, ICollection<string> files, string searchPattern)
        {
            var di = new DirectoryInfo(directory);

            foreach (var fi in di.GetFiles(searchPattern))
            {
                if (!ScanOnlyWritable)
                {
                    files.Add(fi.FullName);
                }
                else
                {
                    try
                    {
                        using (fi.OpenWrite())
                        {
                        }

                        files.Add(fi.FullName);
                    }
                    catch (IOException)
                    {
                    }
                }
            }

            foreach (var cdi in di.GetDirectories())
            {
                if (IncludeEmptyDirectories)
                {
                    files.Add(cdi.FullName);
                }

                AddFilesFromDirectory(cdi.FullName, files, searchPattern);
            }
        }

#endregion

#region GetArchiveUpdateCallback 重载

        /// <summary>
        /// 执行通用的 ArchiveUpdateCallback 初始化。
        /// </summary>
        /// <param name="auc">要初始化的 ArchiveUpdateCallback 实例。</param>
        private void CommonUpdateCallbackInit(ArchiveUpdateCallback auc)
        {
            auc.FileCompressionStarted += FileCompressionStartedEventProxy;
            auc.Compressing += CompressingEventProxy;
            auc.FileCompressionFinished += FileCompressionFinishedEventProxy;
            auc.DefaultItemName = DefaultItemName;
            auc.FastCompression = FastCompression;
        }

        private float GetDictionarySize()
        {
            var dictionarySize = 0.001f;

            switch (_compressionMethod)
            {
                case CompressionMethod.Default:
                case CompressionMethod.Lzma:
                case CompressionMethod.Lzma2:
                {
                    switch (CompressionLevel)
                    {
                        case CompressionLevel.None:
                        {
                            dictionarySize = 0.001f;
                            break;
                        }
                        case CompressionLevel.Fast:
                        {
                            dictionarySize = 1.0f / 16 * 7.5f + 4;
                            break;
                        }
                        case CompressionLevel.Low:
                        {
                            dictionarySize = 7.5f * 11.5f + 4;
                            break;
                        }
                        case CompressionLevel.Normal:
                        {
                            dictionarySize = 16 * 11.5f + 4;
                            break;
                        }
                        case CompressionLevel.High:
                        {
                            dictionarySize = 32 * 11.5f + 4;
                            break;
                        }
                        case CompressionLevel.Ultra:
                        {
                            dictionarySize = 64 * 11.5f + 4;
                            break;
                        }
                    }

                    break;
                }
                case CompressionMethod.BZip2:
                {
                    switch (CompressionLevel)
                    {
                        case CompressionLevel.None:
                        {
                            dictionarySize = 0;
                            break;
                        }
                        case CompressionLevel.Fast:
                        {
                            dictionarySize = 0.095f;
                            break;
                        }
                        case CompressionLevel.Low:
                        {
                            dictionarySize = 0.477f;
                            break;
                        }
                        case CompressionLevel.Normal:
                        case CompressionLevel.High:
                        case CompressionLevel.Ultra:
                        {
                            dictionarySize = 0.858f;
                            break;
                        }
                    }

                    break;
                }
                case CompressionMethod.Deflate:
                case CompressionMethod.Deflate64:
                {
                    dictionarySize = 32;
                    break;
                }
                case CompressionMethod.Ppmd:
                {
                    dictionarySize = 16;
                    break;
                }
            }

            return dictionarySize;
        }

        /// <summary>
        /// 生成 ArchiveUpdateCallback 类的新实例。
        /// </summary>
        /// <param name="files">FileInfo 数组 - 要打包的文件</param>
        /// <param name="rootLength">文件名公共根的长度</param>
        /// <param name="password">归档密码</param>
        /// <returns></returns>
        private ArchiveUpdateCallback GetArchiveUpdateCallback(
            FileInfo[] files, int rootLength, string password)
        {
            SetCompressionProperties();
            var auc = (string.IsNullOrEmpty(password))
                ? new ArchiveUpdateCallback(files, rootLength, this, GetUpdateData(), DirectoryStructure)
                    {DictionarySize = GetDictionarySize()}
                : new ArchiveUpdateCallback(files, rootLength, password, this, GetUpdateData(), DirectoryStructure)
                    {DictionarySize = GetDictionarySize()};
            CommonUpdateCallbackInit(auc);

            return auc;
        }

        /// <summary>
        /// 生成 ArchiveUpdateCallback 类的新实例。
        /// </summary>
        /// <param name="inStream">归档输入流。</param>
        /// <param name="password">归档密码。</param>
        /// <returns></returns>
        private ArchiveUpdateCallback GetArchiveUpdateCallback(Stream inStream, string password)
        {
            SetCompressionProperties();
            var auc = (string.IsNullOrEmpty(password))
                ? new ArchiveUpdateCallback(inStream, this, GetUpdateData(), DirectoryStructure)
                    {DictionarySize = GetDictionarySize()}
                : new ArchiveUpdateCallback(inStream, password, this, GetUpdateData(), DirectoryStructure)
                    {DictionarySize = GetDictionarySize()};
            CommonUpdateCallbackInit(auc);

            return auc;
        }

        /// <summary>
        /// 生成 ArchiveUpdateCallback 类的新实例。
        /// </summary>
        /// <param name="streamDict">Dictionary&lt;归档项名称, 流&gt;。</param>
        /// <param name="password">归档密码</param>
        /// <returns></returns>
        private ArchiveUpdateCallback GetArchiveUpdateCallback(
            IDictionary<string, Stream> streamDict, string password)
        {
            SetCompressionProperties();
            var auc = (string.IsNullOrEmpty(password))
                ? new ArchiveUpdateCallback(streamDict, this, GetUpdateData(), DirectoryStructure)
                    {DictionarySize = GetDictionarySize()}
                : new ArchiveUpdateCallback(streamDict, password, this, GetUpdateData(), DirectoryStructure)
                    {DictionarySize = GetDictionarySize()};
            CommonUpdateCallbackInit(auc);

            return auc;
        }

#endregion

#region 服务“Get”函数

        private void FreeCompressionCallback(ArchiveUpdateCallback callback)
        {
            callback.FileCompressionStarted -= FileCompressionStartedEventProxy;
            callback.Compressing -= CompressingEventProxy;
            callback.FileCompressionFinished -= FileCompressionFinishedEventProxy;
        }

        private string GetTempArchiveFileName(string archiveName)
        {
            return Path.Combine(TempFolderPath, Path.GetFileName(archiveName) + ".~");
        }

        private FileStream GetArchiveFileStream(string archiveName)
        {
            if ((CompressionMode != CompressionMode.Create || _updateData.FileNamesToModify != null) && !File.Exists(archiveName))
            {
                if (
                    !ThrowException(null,
                        new CompressionFailedException("file \"" + archiveName + "\" does not exist.")))
                {
                    return null;
                }
            }

            return _volumeSize == 0
                ? CompressionMode == CompressionMode.Create && _updateData.FileNamesToModify == null
                    ? new FileStream(archiveName, FileMode.Create, FileAccess.ReadWrite, FileShare.None, bufferSize: 65536, FileOptions.SequentialScan)
                    : new FileStream(GetTempArchiveFileName(archiveName), FileMode.Create, FileAccess.ReadWrite, FileShare.None, bufferSize: 65536, FileOptions.SequentialScan)
                : null;
        }

        private void FinalizeUpdate()
        {
            if (_volumeSize == 0 && (CompressionMode != CompressionMode.Create || _updateData.FileNamesToModify != null))
            {
                File.Move(GetTempArchiveFileName(_archiveName), _archiveName);
            }
        }

        private UpdateData GetUpdateData()
        {
            if (_updateData.FileNamesToModify == null)
            {
                var updateData = new UpdateData {Mode = (InternalCompressionMode) ((int) CompressionMode)};
                switch (CompressionMode)
                {
                    case CompressionMode.Create:
                    {
                        updateData.FilesCount = uint.MaxValue;
                        break;
                    }
                    case CompressionMode.Append:
                    {
                        updateData.FilesCount = _oldFilesCount;
                        break;
                    }
                }

                return updateData;
            }

            return _updateData;
        }

        private ISequentialOutStream GetOutStream(Stream outStream)
        {
            if (!_compressingFilesOnDisk)
            {
                return new OutStreamWrapper(outStream, false);
            }

            if (_volumeSize == 0 || CompressionMode != CompressionMode.Create || _updateData.FileNamesToModify != null)
            {
                return new OutStreamWrapper(outStream, true);
            }

            return new OutMultiStreamWrapper(_archiveName, _volumeSize);
        }

        private IInStream GetInStream()
        {
            return File.Exists(_archiveName) &&
                   (CompressionMode != CompressionMode.Create && _compressingFilesOnDisk ||
                    _updateData.FileNamesToModify != null)
                ? new InStreamWrapper(
                    new FileStream(_archiveName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, bufferSize: 65536, FileOptions.SequentialScan),
                    true)
                : null;
        }

        private ArchiveOpenCallback GetArchiveOpenCallback()
        {
            return string.IsNullOrEmpty(Password)
                ? new ArchiveOpenCallback(_archiveName)
                : new ArchiveOpenCallback(_archiveName, Password);
        }

#endregion

#region 核心公共成员

#region 事件

        /// <summary>
        /// 当下一个文件即将被打包时发生。
        /// </summary>
        /// <remarks>当 7-zip 引擎请求下一个文件的输入流以进行打包时发生</remarks>
        public event EventHandler<FileNameEventArgs> FileCompressionStarted;

        /// <summary>
        /// 当当前文件压缩完成时发生。
        /// </summary>
        public event EventHandler<EventArgs> FileCompressionFinished;

        /// <summary>
        /// 当数据正在被压缩时发生
        /// </summary>
        /// <remarks>使用此事件进行精确的进度处理和各种 ProgressBar.StepBy(e.PercentDelta) 例程</remarks>
        public event EventHandler<ProgressEventArgs> Compressing;

        /// <summary>
        /// 当所有文件信息已确定且 SevenZipCompressor 即将开始压缩它们时发生。
        /// </summary>
        /// <remarks>传入的 int 值指示已扫描的文件数。</remarks>
        public event EventHandler<IntEventArgs> FilesFound;

        /// <summary>
        /// 当压缩过程完成时发生
        /// </summary>
        public event EventHandler<EventArgs> CompressionFinished;

#region 事件代理

        /// <summary>
        /// FileCompressionStarted 的事件代理。
        /// </summary>
        /// <param name="sender">事件的发送者。</param>
        /// <param name="e">事件参数。</param>
        private void FileCompressionStartedEventProxy(object sender, FileNameEventArgs e)
        {
            OnEvent(FileCompressionStarted, e, false);
        }

        /// <summary>
        /// FileCompressionFinished 的事件代理。
        /// </summary>
        /// <param name="sender">事件的发送者。</param>
        /// <param name="e">事件参数。</param>
        private void FileCompressionFinishedEventProxy(object sender, EventArgs e)
        {
            OnEvent(FileCompressionFinished, e, false);
        }

        /// <summary>
        /// Compressing 的事件代理。
        /// </summary>
        /// <param name="sender">事件的发送者。</param>
        /// <param name="e">事件参数。</param>
        private void CompressingEventProxy(object sender, ProgressEventArgs e)
        {
            OnEvent(Compressing, e, false);
        }

        /// <summary>
        /// FilesFound 的事件代理。
        /// </summary>
        /// <param name="sender">事件的发送者。</param>
        /// <param name="e">事件参数。</param>
        private void FilesFoundEventProxy(object sender, IntEventArgs e)
        {
            OnEvent(FilesFound, e, false);
        }

#endregion

#endregion

#region 属性

        /// <summary>
        /// 获取或设置归档格式
        /// </summary>
        public OutArchiveFormat ArchiveFormat
        {
            get => _archiveFormat;

            set
            {
                _archiveFormat = value;

                if (!MethodIsValid(_compressionMethod))
                {
                    _compressionMethod = CompressionMethod.Default;
                }
            }
        }

        /// <summary>
        /// 获取或设置压缩方法
        /// </summary>
        public CompressionMethod CompressionMethod
        {
            get => _compressionMethod;

            set => _compressionMethod = !MethodIsValid(value) ? CompressionMethod.Default : value;
        }

        /// <summary>
        /// 获取或设置归档卷的字节大小（0 表示不分卷）。
        /// </summary>
        public long VolumeSize
        {
            get => _volumeSize;

            set => _volumeSize = value > 0 ? value : 0;
        }

#endregion

#region CompressFiles 重载

        /// <summary>
        /// 将文件打包到归档中。
        /// </summary>
        /// <param name="fileFullNames">要打包的文件名数组。</param>
        /// <param name="archiveName">归档文件名。</param>
        public void CompressFiles(
            string archiveName, params string[] fileFullNames)
        {
            CompressFilesEncrypted(archiveName, string.Empty, fileFullNames);
        }

        /// <summary>
        /// 将文件打包到归档中。
        /// </summary>
        /// <param name="fileFullNames">要打包的文件名数组。</param>
        /// <param name="archiveStream">归档输出流。 
        /// 使用 CompressFiles(string archiveName ... ) 重载以归档到磁盘。</param>       
        public void CompressFiles(
            Stream archiveStream, params string[] fileFullNames)
        {
            CompressFilesEncrypted(archiveStream, string.Empty, fileFullNames);
        }

        /// <summary>
        /// 将文件打包到归档中。
        /// </summary>
        /// <param name="fileFullNames">要打包的文件名数组。</param>
        /// <param name="commonRootLength">文件名公共根的长度。</param>
        /// <param name="archiveName">归档文件名。</param>
        public void CompressFiles(
            string archiveName, int commonRootLength, params string[] fileFullNames)
        {
            CompressFilesEncrypted(archiveName, commonRootLength, string.Empty, fileFullNames);
        }

        /// <summary>
        /// 将文件打包到归档中。
        /// </summary>
        /// <param name="fileFullNames">要打包的文件名数组。</param>
        /// <param name="commonRootLength">文件名公共根的长度。</param>
        /// <param name="archiveStream">归档输出流。
        /// 使用 CompressFiles(string archiveName, ... ) 重载以归档到磁盘。</param>
        public void CompressFiles(
            Stream archiveStream, int commonRootLength, params string[] fileFullNames)
        {
            fileFullNames = GetFullFilePaths(fileFullNames);
            CompressFilesEncrypted(archiveStream, commonRootLength, string.Empty, fileFullNames);
        }

        /// <summary>
        /// 将文件打包到归档中。
        /// </summary>
        /// <param name="fileFullNames">要打包的文件名数组。</param>
        /// <param name="archiveName">归档文件名。</param>
        /// <param name="password">归档密码。</param>
        public void CompressFilesEncrypted(
            string archiveName, string password, params string[] fileFullNames)
        {
            fileFullNames = GetFullFilePaths(fileFullNames);
            CompressFilesEncrypted(archiveName, CommonRoot(fileFullNames), password, fileFullNames);
        }

        /// <summary>
        /// 将文件打包到归档中。
        /// </summary>
        /// <param name="fileFullNames">要打包的文件名数组。</param>
        /// <param name="archiveStream">归档输出流。
        /// 使用 CompressFiles( ... string archiveName ... ) 重载以归档到磁盘。</param>
        /// <param name="password">归档密码。</param>
        public void CompressFilesEncrypted(
            Stream archiveStream, string password, params string[] fileFullNames)
        {
            fileFullNames = GetFullFilePaths(fileFullNames);
            CompressFilesEncrypted(archiveStream, CommonRoot(fileFullNames), password, fileFullNames);
        }

        /// <summary>
        /// 将文件打包到归档中。
        /// </summary>
        /// <param name="fileFullNames">要打包的文件名数组。</param>
        /// <param name="commonRootLength">文件名公共根的长度。</param>
        /// <param name="archiveName">归档文件名。</param>
        /// <param name="password">归档密码。</param>
        public void CompressFilesEncrypted(string archiveName, int commonRootLength, string password, params string[] fileFullNames)
        {
            _compressingFilesOnDisk = true;
            _archiveName = archiveName;

            using (var fs = GetArchiveFileStream(archiveName))
            {
                if (fs == null && _volumeSize == 0)
                {
                    return;
                }

                CompressFilesEncrypted(fs, commonRootLength, password, fileFullNames);
            }

            FinalizeUpdate();
        }

        /// <summary>
        /// 将文件打包到归档中。
        /// </summary>
        /// <param name="fileFullNames">要打包的文件名数组。</param>
        /// <param name="commonRootLength">文件名公共根的长度。</param>
        /// <param name="archiveStream">归档输出流。
        /// 使用 CompressFiles( ... string archiveName ... ) 重载以归档到磁盘。</param>
        /// <param name="password">归档密码。</param>
        public void CompressFilesEncrypted(
            Stream archiveStream, int commonRootLength, string password, params string[] fileFullNames)
        {
            ClearExceptions();

            if (fileFullNames.Length > 1 &&
                (_archiveFormat == OutArchiveFormat.BZip2 || _archiveFormat == OutArchiveFormat.GZip ||
                 _archiveFormat == OutArchiveFormat.XZ))
            {
                if (!ThrowException(null,
                    new CompressionFailedException("Can not compress more than one file in this format.")))
                {
                    return;
                }
            }
            
            UpdateCompressorPassword(password);

            if (_volumeSize == 0 || !_compressingFilesOnDisk)
            {
                ValidateStream(archiveStream);
            }

            FileInfo[] files = null;

            try
            {
                files = ProduceFileInfoArray(fileFullNames, commonRootLength, _directoryCompress, DirectoryStructure);
            }
            catch (Exception e)
            {
                if (!ThrowException(null, e))
                {
                    return;
                }
            }

            _directoryCompress = false;

            FilesFound?.Invoke(this, new IntEventArgs(fileFullNames.Length));

            try
            {
                ISequentialOutStream sequentialArchiveStream;

                using ((sequentialArchiveStream = GetOutStream(archiveStream)) as IDisposable)
                {
                    IInStream inArchiveStream;

                    using ((inArchiveStream = GetInStream()) as IDisposable)
                    {
                        IOutArchive outArchive;

                        if (CompressionMode == CompressionMode.Create || !_compressingFilesOnDisk)
                        {
                            SevenZipLibraryManager.LoadLibrary(this, _archiveFormat);
                            outArchive = SevenZipLibraryManager.OutArchive(_archiveFormat, this);
                        }
                        else
                        {
                            // 创建 IInArchive，读取它并转换为 IOutArchive
                            SevenZipLibraryManager.LoadLibrary(this, Formats.InForOutFormats[_archiveFormat]);

                            if ((outArchive = MakeOutArchive(inArchiveStream)) == null)
                            {
                                return;
                            }
                        }

                        using (var auc = GetArchiveUpdateCallback(files, commonRootLength, password))
                        {
                            try
                            {
                                if (files != null)
                                    CheckedExecute(
                                        outArchive.UpdateItems(
                                            sequentialArchiveStream, (uint) files.Length + _oldFilesCount, auc),
                                        SevenZipCompressionFailedException.DEFAULT_MESSAGE, auc);
                            }
                            finally
                            {
                                FreeCompressionCallback(auc);
                            }
                        }
                    }
                }
            }
            finally
            {
                if (CompressionMode == CompressionMode.Create || !_compressingFilesOnDisk)
                {
                    //SevenZipLibraryManager.FreeLibrary(this, _archiveFormat);
                }
                else
                {
                    //SevenZipLibraryManager.FreeLibrary(this, Formats.InForOutFormats[_archiveFormat]);
                    File.Delete(_archiveName);
                }

                _compressingFilesOnDisk = false;
                OnEvent(CompressionFinished, EventArgs.Empty, false);
            }

            ThrowUserException();
        }

#endregion

#region CompressDirectory 重载

        /// <summary>
        /// 打包指定目录中的所有文件。
        /// </summary>
        /// <param name="directory">要压缩的目录。</param>
        /// <param name="archiveName">归档文件名。</param>
        /// <param name="password">归档密码。</param>
        /// <param name="searchPattern">搜索字符串，例如 "*.txt"。</param>
        /// <param name="recursion">如果为 true，将递归搜索文件；否则不递归。</param>
        public void CompressDirectory(string directory, string archiveName, string password = "", string searchPattern = "*", bool recursion = true)
        {
            _compressingFilesOnDisk = true;
            _archiveName = archiveName;

            using (var fs = GetArchiveFileStream(archiveName))
            {
                if (fs == null && _volumeSize == 0)
                {
                    return;
                }

                CompressDirectory(directory, fs, password, searchPattern, recursion);
            }

            FinalizeUpdate();
        }

        /// <summary>
        /// 打包指定目录中的所有文件。
        /// </summary>
        /// <param name="directory">要压缩的目录。</param>
        /// <param name="archiveStream">归档输出流。
        /// 使用 CompressDirectory( ... string archiveName ... ) 重载以归档到磁盘。</param>        
        /// <param name="password">归档密码。</param>
        /// <param name="searchPattern">搜索字符串，例如 "*.txt"。</param>
        /// <param name="recursion">如果为 true，将递归搜索文件；否则不递归。</param>
        public void CompressDirectory(string directory, Stream archiveStream, string password = "", string searchPattern = "*", bool recursion = true)
        {
            var files = new List<string>();

            if (!Directory.Exists(directory))
            {
                throw new ArgumentException("Directory \"" + directory + "\" does not exist!");
            }

            // 获取完整路径，以防这是例如 SFN 路径。
            directory = Path.GetFullPath(directory);

            if (RecursiveDirectoryEmptyCheck(directory))
            {
                throw new SevenZipInvalidFileNamesException("the specified directory is empty!");
            }

            if (recursion)
            {
                AddFilesFromDirectory(directory, files, searchPattern);
            }
            else
            {
                var dirFiles = (new DirectoryInfo(directory)).GetFiles(searchPattern);
                for (var i = 0; i < dirFiles.Length; i++)
                {
                    files.Add(dirFiles[i].FullName);
                }
            }

            var commonRootLength = directory.Length;

            if (directory.EndsWith("\\", StringComparison.Ordinal))
            {
                directory = directory.Substring(0, directory.Length - 1);
            }
            else
            {
                commonRootLength++;
            }

            if (PreserveDirectoryRoot)
            {
                var upperRoot = Path.GetDirectoryName(directory);

                if (upperRoot != null)
                {
                    commonRootLength = upperRoot.Length + (upperRoot.EndsWith("\\", StringComparison.Ordinal) ? 0 : 1);
                }
            }

            _directoryCompress = true;
            CompressFilesEncrypted(archiveStream, commonRootLength, password, files.ToArray());
        }

#endregion

#region CompressFileDictionary 重载

        /// <summary>
        /// 打包指定的文件字典。
        /// </summary>
        /// <param name="fileDictionary">Dictionary&lt;归档项名称, 文件名&gt;。
        /// 如果文件名为 null，则对应的归档项成为目录。</param>
        /// <param name="archiveName">归档文件名。</param>
        /// <param name="password">归档密码。</param>
        public void CompressFileDictionary(
            IDictionary<string, string> fileDictionary, string archiveName, string password = "")
        {
            _compressingFilesOnDisk = true;
            _archiveName = archiveName;

            using (var fs = GetArchiveFileStream(archiveName))
            {
                if (fs == null && _volumeSize == 0)
                {
                    return;
                }

                CompressFileDictionary(fileDictionary, fs, password);
            }

            FinalizeUpdate();
        }

        /// <summary>
        /// 打包指定的文件字典。
        /// </summary>
        /// <param name="fileDictionary">Dictionary&lt;归档项名称, 文件名&gt;。
        /// 如果文件名为 null，则对应的归档项成为目录。</param>
        /// <param name="archiveStream">归档输出流。
        /// 使用 CompressStreamDictionary( ... string archiveName ... ) 重载以归档到磁盘。</param>
        /// <param name="password">归档密码。</param>
        public void CompressFileDictionary(IDictionary<string, string> fileDictionary, Stream archiveStream, string password = "")
        {
            var streamDict = new Dictionary<string, Stream>(fileDictionary.Count);

            foreach (var pair in fileDictionary)
            {
                if (pair.Value == null)
                {
                    streamDict.Add(pair.Key, null);
                }
                else
                {
                    if (!File.Exists(pair.Value))
                    {
                        throw new CompressionFailedException(
                            "The file corresponding to the archive entry \"" + pair.Key + "\" does not exist.");
                    }

                    streamDict.Add(
                        pair.Key,
                        new FileStream(pair.Value, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, bufferSize: 65536, FileOptions.SequentialScan));
                }
            }

            //创建的流将在内部自动释放。
            CompressStreamDictionary(streamDict, archiveStream, password);
        }

#endregion

#region CompressStreamDictionary 重载

        /// <summary>
        /// 打包指定的流字典。
        /// </summary>
        /// <param name="streamDictionary">Dictionary&lt;归档项名称, 流&gt;。
        /// 如果流为 null，则对应的字符串成为目录名。</param>
        /// <param name="archiveName">归档文件名。</param>
        /// <param name="password">归档密码。</param>
        public void CompressStreamDictionary(IDictionary<string, Stream> streamDictionary, string archiveName, string password = "")
        {
            _compressingFilesOnDisk = true;
            _archiveName = archiveName;

            using (var fs = GetArchiveFileStream(archiveName))
            {
                if (fs == null && _volumeSize == 0)
                {
                    return;
                }

                CompressStreamDictionary(streamDictionary, fs, password);
            }

            FinalizeUpdate();
        }

        /// <summary>
        /// 打包指定的流字典。
        /// </summary>
        /// <param name="streamDictionary">Dictionary&lt;归档项名称, 流&gt;。
        /// 如果流为 null，则对应的字符串成为目录名。</param>
        /// <param name="archiveStream">归档输出流。
        /// 使用 CompressStreamDictionary( ... string archiveName ... ) 重载以归档到磁盘。</param>
        /// <param name="password">归档密码。</param>
        public void CompressStreamDictionary(IDictionary<string, Stream> streamDictionary, Stream archiveStream, string password = "")
        {
            ClearExceptions();

            if (streamDictionary.Count > 1 &&
                (_archiveFormat == OutArchiveFormat.BZip2 || _archiveFormat == OutArchiveFormat.GZip ||
                 _archiveFormat == OutArchiveFormat.XZ))
            {
                if (!ThrowException(null,
                    new CompressionFailedException("Can not compress more than one file/stream in this format.")))
                {
                    return;
                }
            }

            if (_volumeSize == 0 || !_compressingFilesOnDisk)
            {
                ValidateStream(archiveStream);
            }
            
            UpdateCompressorPassword(password);
            
            foreach (var pair in streamDictionary)
            {
                if (pair.Value != null && (!pair.Value.CanSeek || !pair.Value.CanRead))
                {
                    if (!ThrowException(null,
                        new ArgumentException(
                            $"The specified stream dictionary contains an invalid stream corresponding to the archive entry \"{pair.Key}\".",
                            nameof(streamDictionary))))
                    {
                        return;
                    }
                }
            }

            try
            {
                ISequentialOutStream sequentialArchiveStream;

                using ((sequentialArchiveStream = GetOutStream(archiveStream)) as IDisposable)
                {
                    IInStream inArchiveStream;

                    using ((inArchiveStream = GetInStream()) as IDisposable)
                    {
                        IOutArchive outArchive;

                        if (CompressionMode == CompressionMode.Create || !_compressingFilesOnDisk)
                        {
                            SevenZipLibraryManager.LoadLibrary(this, _archiveFormat);
                            outArchive = SevenZipLibraryManager.OutArchive(_archiveFormat, this);
                        }
                        else
                        {
                            // 创建 IInArchive，读取它并转换为 IOutArchive
                            SevenZipLibraryManager.LoadLibrary(
                                this, Formats.InForOutFormats[_archiveFormat]);
                            if ((outArchive = MakeOutArchive(inArchiveStream)) == null)
                            {
                                return;
                            }
                        }

                        using (var auc = GetArchiveUpdateCallback(streamDictionary, password))
                        {
                            try
                            {
                                CheckedExecute(outArchive.UpdateItems(sequentialArchiveStream,
                                        (uint) streamDictionary.Count + _oldFilesCount, auc),
                                    SevenZipCompressionFailedException.DEFAULT_MESSAGE, auc);
                            }
                            finally
                            {
                                FreeCompressionCallback(auc);
                            }
                        }
                    }
                }
            }
            finally
            {
                if (CompressionMode == CompressionMode.Create || !_compressingFilesOnDisk)
                {
                    //SevenZipLibraryManager.FreeLibrary(this, _archiveFormat);
                }
                else
                {
                    //SevenZipLibraryManager.FreeLibrary(this, Formats.InForOutFormats[_archiveFormat]);
                    File.Delete(_archiveName);
                }

                _compressingFilesOnDisk = false;
                OnEvent(CompressionFinished, EventArgs.Empty, false);
            }

            ThrowUserException();
        }

#endregion

#region CompressStream 重载

        /// <summary>
        /// 压缩指定流。
        /// </summary>
        /// <param name="inStream">源未压缩流。</param>
        /// <param name="outStream">目标压缩流。</param>
        /// <param name="password">归档密码。</param>
        /// <exception cref="ArgumentException">ArgumentException: 至少一个指定的流无效。</exception>
        public void CompressStream(Stream inStream, Stream outStream, string password = "")
        {
            ClearExceptions();

            if (!inStream.CanSeek || !inStream.CanRead || !outStream.CanWrite)
            {
                if (!ThrowException(null, new ArgumentException("The specified streams are invalid.")))
                {
                    return;
                }
            }

            try
            {
                SevenZipLibraryManager.LoadLibrary(this, _archiveFormat);
                ISequentialOutStream sequentialArchiveStream;

                using ((sequentialArchiveStream = GetOutStream(outStream)) as IDisposable)
                {
                    using (var auc = GetArchiveUpdateCallback(inStream, password))
                    {
                        try
                        {
                            CheckedExecute(
                                SevenZipLibraryManager.OutArchive(_archiveFormat, this).UpdateItems(
                                    sequentialArchiveStream, 1, auc),
                                SevenZipCompressionFailedException.DEFAULT_MESSAGE, auc);
                        }
                        finally
                        {
                            FreeCompressionCallback(auc);
                        }
                    }
                }
            }
            finally
            {
                //SevenZipLibraryManager.FreeLibrary(this, _archiveFormat);
                OnEvent(CompressionFinished, EventArgs.Empty, false);
            }

            ThrowUserException();
        }

#endregion

#region ModifyArchive 重载

        /// <summary>
        /// 修改现有归档（重命名文件或删除文件）。
        /// </summary>
        /// <param name="archiveName">归档文件名。</param>
        /// <param name="newFileNames">新文件名。值为 null 表示删除对应索引。</param>
        /// <param name="password">归档密码。</param>
        public void ModifyArchive(string archiveName, IDictionary<int, string> newFileNames, string password = "")
        {
            ClearExceptions();

            if (!SevenZipLibraryManager.ModifyCapable)
            {
                throw new SevenZipLibraryException("The specified 7zip native library does not support this method.");
            }

            if (!File.Exists(archiveName))
            {
                if (!ThrowException(null,
                    new ArgumentException("The specified archive does not exist.", nameof(archiveName))))
                {
                    return;
                }
            }

            if (newFileNames == null || newFileNames.Count == 0)
            {
                if (!ThrowException(null, new ArgumentException("Invalid new file names.", nameof(newFileNames))))
                {
                    return;
                }
            }

            UpdateCompressorPassword(password);

            try
            {
                using (var extractor = new SevenZipExtractor(archiveName, password))
                {
                    _updateData = new UpdateData();
                    var archiveData = new ArchiveFileInfo[extractor.ArchiveFileData.Count];
                    extractor.ArchiveFileData.CopyTo(archiveData, 0);
                    _updateData.ArchiveFileData = new List<ArchiveFileInfo>(archiveData);
                }

                _updateData.FileNamesToModify = newFileNames;
                _updateData.Mode = InternalCompressionMode.Modify;
            }
            catch (SevenZipException e)
            {
                if (!ThrowException(null, e))
                {
                    return;
                }
            }

            try
            {
                ISequentialOutStream sequentialArchiveStream;
                _compressingFilesOnDisk = true;

                using ((sequentialArchiveStream = GetOutStream(GetArchiveFileStream(archiveName))) as IDisposable)
                {
                    IInStream inArchiveStream;
                    _archiveName = archiveName;

                    using ((inArchiveStream = GetInStream()) as IDisposable)
                    {
                        IOutArchive outArchive;
                        // 创建 IInArchive，读取它并转换为 IOutArchive
                        SevenZipLibraryManager.LoadLibrary(
                            this, Formats.InForOutFormats[_archiveFormat]);
                        if ((outArchive = MakeOutArchive(inArchiveStream)) == null)
                        {
                            return;
                        }

                        using (var auc = GetArchiveUpdateCallback(null, 0, password))
                        {
                            uint deleteCount = 0;

                            if (_updateData.FileNamesToModify != null)
                            {
                                foreach (var pairDeleted in _updateData.FileNamesToModify)
                                {
                                    if (pairDeleted.Value == null) deleteCount++;
                                }
                            }

                            try
                            {
                                CheckedExecute(
                                    outArchive.UpdateItems(
                                        sequentialArchiveStream, _oldFilesCount - deleteCount, auc),
                                    SevenZipCompressionFailedException.DEFAULT_MESSAGE, auc);
                            }
                            finally
                            {
                                FreeCompressionCallback(auc);
                            }
                        }
                    }
                }
            }
            finally
            {
                //SevenZipLibraryManager.FreeLibrary(this, Formats.InForOutFormats[_archiveFormat]);
                File.Delete(archiveName);
                FinalizeUpdate();
                _compressingFilesOnDisk = false;
                _updateData.FileNamesToModify = null;
                _updateData.ArchiveFileData = null;
                OnEvent(CompressionFinished, EventArgs.Empty, false);
            }

            ThrowUserException();
        }

#endregion

#endregion

#endif

        /// <summary>
        /// 获取或设置托管 LZMA 算法的字典大小。
        /// </summary>
        public static int LzmaDictionarySize
        {
            get => _lzmaDictionarySize;
            set => _lzmaDictionarySize = value;
        }

        internal static void WriteLzmaProperties(Encoder encoder)
        {
#region LZMA 属性定义

            CoderPropId[] propIDs =
            {
                CoderPropId.DictionarySize,
                CoderPropId.PosStateBits,
                CoderPropId.LitContextBits,
                CoderPropId.LitPosBits,
                CoderPropId.Algorithm,
                CoderPropId.NumFastBytes,
                CoderPropId.MatchFinder,
                CoderPropId.EndMarker
            };
            object[] properties =
            {
                _lzmaDictionarySize,
                2,
                3,
                0,
                2,
                256,
                "bt4",
                false
            };

#endregion

            encoder.SetCoderProperties(propIDs, properties);
        }

        /// <summary>
        /// 使用 LZMA 算法压缩指定流（内部为 C# 实现）
        /// </summary>
        /// <param name="inStream">源未压缩流</param>
        /// <param name="outStream">目标压缩流</param>
        /// <param name="inLength">未压缩数据的长度（null 表示 inStream.Length）</param>
        /// <param name="codeProgressEvent">用于处理编码进度的事件</param>
        public static void CompressStream(Stream inStream, Stream outStream, int? inLength,
            EventHandler<ProgressEventArgs> codeProgressEvent)
        {
            if (!inStream.CanRead || !outStream.CanWrite)
            {
                throw new ArgumentException("The specified streams are invalid.");
            }

            var encoder = new Encoder();
            WriteLzmaProperties(encoder);
            encoder.WriteCoderProperties(outStream);
            var streamSize = inLength ?? inStream.Length;

            for (var i = 0; i < 8; i++)
            {
                outStream.WriteByte((byte) (streamSize >> (8 * i)));
            }

            encoder.Code(inStream, outStream, -1, -1, new LzmaProgressCallback(streamSize, codeProgressEvent));
        }

        /// <summary>
        /// 使用 LZMA 算法压缩字节数组（内部为 C# 实现）
        /// </summary>
        /// <param name="data">要压缩的字节数组</param>
        /// <returns>压缩后的字节数组</returns>
        public static byte[] CompressBytes(byte[] data)
        {
            using (var inStream = new MemoryStream(data))
            {
                using (var outStream = new MemoryStream())
                {
                    var encoder = new Encoder();
                    WriteLzmaProperties(encoder);
                    encoder.WriteCoderProperties(outStream);
                    var streamSize = inStream.Length;

                    for (var i = 0; i < 8; i++)
                    {
                        outStream.WriteByte((byte) (streamSize >> (8 * i)));
                    }

                    encoder.Code(inStream, outStream, -1, -1, null);
                    return outStream.ToArray();
                }
            }
        }

        /// <summary>
        /// 确保文件名数组为该文件的完整路径。
        /// </summary>
        /// <param name="fileFullNames">文件名数组。</param>
        /// <returns>包含完整路径的文件名数组。</returns>
        private static string[] GetFullFilePaths(IEnumerable<string> fileFullNames)
        {
            if (fileFullNames is string[] arr)
            {
                var result = new string[arr.Length];
                for (var i = 0; i < arr.Length; i++)
                {
                    result[i] = Path.GetFullPath(arr[i]);
                }
                return result;
            }
            return fileFullNames.Select(Path.GetFullPath).ToArray();
        }
        
        /// <summary>
        /// 检查并更新 SevenZipCompressor 中的密码
        /// </summary>
        /// <param name="password">要使用的密码。</param>
        private void UpdateCompressorPassword(string password)
        {
            if (!string.IsNullOrEmpty(password) && string.IsNullOrEmpty(Password))
            {
                // 修改加密归档时，SevenZipCompressor 中未设置 Password。
                Password = password;
            }
        }
    }
}
