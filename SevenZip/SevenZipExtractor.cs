namespace SevenZip
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;

    using SevenZip.Sdk.Compression.Lzma;

    /// <summary>
    /// 用于从 7-Zip 支持的归档中解压数据的类。
    /// </summary>
    /// <example>
    /// using (var extr = new SevenZipExtractor(@"C:\Test.7z"))
    /// {
    ///     extr.ExtractArchive(@"C:\TestDirectory");
    /// }
    /// </example>
    public sealed partial class SevenZipExtractor
#if UNMANAGED
        : SevenZipBase, IDisposable
#endif
    {
#if UNMANAGED
        private List<ArchiveFileInfo> _archiveFileData;
        private IInArchive _archive;
        private IInStream _archiveStream;
        private int _offset;
        private ArchiveOpenCallback _openCallback;
        private string _fileName;
        private Stream _inStream;
        private long? _packedSize;
        private long? _unpackedSize;
        private uint? _filesCount;
        private bool? _isSolid;
        private bool _opened;
        private bool _disposed;
        private InArchiveFormat _format = (InArchiveFormat)(-1);
        private ReadOnlyCollection<ArchiveFileInfo> _archiveFileInfoCollection;
        private ReadOnlyCollection<ArchiveProperty> _archiveProperties;
        private ReadOnlyCollection<string> _volumeFileNames;
        private bool _leaveOpen;

        /// <summary>
        /// 用于锁定可能的 Dispose() 调用。
        /// </summary>
        private bool _asynchronousDisposeLock;

        #region Constructors

        /// <summary>
        /// 通用初始化函数。
        /// </summary>
        /// <param name="archiveFullName">归档文件名。</param>
        private void Init(string archiveFullName)
        {
            _fileName = archiveFullName;
            var isExecutable = false;

            if ((int)_format == -1)
            {
                _format = FileChecker.CheckSignature(archiveFullName, out _offset, out isExecutable);
            }

            PreserveDirectoryStructure = true;
            SevenZipLibraryManager.LoadLibrary(this, _format);

            try
            {
                _archive = SevenZipLibraryManager.InArchive(_format, this);
            }
            catch (SevenZipLibraryException)
            {
                //SevenZipLibraryManager.FreeLibrary(this, _format);
                throw;
            }

            if (isExecutable && _format != InArchiveFormat.PE)
            {
                if (!Check())
                {
                    CommonDispose();
                    _format = InArchiveFormat.PE;
                    SevenZipLibraryManager.LoadLibrary(this, _format);

                    try
                    {
                        _archive = SevenZipLibraryManager.InArchive(_format, this);
                    }
                    catch (SevenZipLibraryException)
                    {
                        //SevenZipLibraryManager.FreeLibrary(this, _format);
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// 通用初始化函数。
        /// </summary>
        /// <param name="stream">用于读取归档的流。</param>
        private void Init(Stream stream)
        {
            ValidateStream(stream);
            var isExecutable = false;

            if ((int)_format == -1)
            {
                _format = FileChecker.CheckSignature(stream, out _offset, out isExecutable);
            }

            PreserveDirectoryStructure = true;
            SevenZipLibraryManager.LoadLibrary(this, _format);

            try
            {
                _inStream = new ArchiveEmulationStreamProxy(stream, _offset, _leaveOpen);
                _packedSize = stream.Length;
                _archive = SevenZipLibraryManager.InArchive(_format, this);
            }
            catch (SevenZipLibraryException)
            {
                //SevenZipLibraryManager.FreeLibrary(this, _format);
                throw;
            }

            if (isExecutable && _format != InArchiveFormat.PE)
            {
                if (!Check())
                {
                    CommonDispose();
                    _format = InArchiveFormat.PE;

                    try
                    {
                        _inStream = new ArchiveEmulationStreamProxy(stream, _offset, _leaveOpen);
                        _packedSize = stream.Length;
                        _archive = SevenZipLibraryManager.InArchive(_format, this);
                    }
                    catch (SevenZipLibraryException)
                    {
                        //SevenZipLibraryManager.FreeLibrary(this, _format);
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// 初始化 SevenZipExtractor 类的新实例。
        /// </summary>
        /// <param name="archiveStream">用于读取归档的流。
        /// 可以使用 SevenZipExtractor(string) 从磁盘解压，但这并非必需。</param>
        /// <remarks>归档格式通过签名进行推测。</remarks>
        public SevenZipExtractor(Stream archiveStream) : this(archiveStream, false)
        {
        }

        /// <summary>
        /// 初始化 SevenZipExtractor 类的新实例。
        /// </summary>
        /// <param name="archiveStream">用于读取归档的流。
        /// 可以使用 SevenZipExtractor(string) 从磁盘解压，但这并非必需。</param>
        /// <param name="leaveOpen">保持基础流打开。</param>
        /// <remarks>归档格式通过签名进行推测。</remarks>
        public SevenZipExtractor(Stream archiveStream, bool leaveOpen) : this(archiveStream, leaveOpen, (InArchiveFormat)(-1))
        {
        }

        /// <summary>
        /// 初始化 SevenZipExtractor 类的新实例。
        /// </summary>
        /// <param name="archiveStream">用于读取归档的流。
        /// 可以使用 SevenZipExtractor(string) 从磁盘解压，但这并非必需。</param>
        /// <param name="leaveOpen">保持基础流打开。</param>        
        /// <param name="format">手动设置归档格式。通常不应以此方式指定，
        /// 而应使用 SevenZipExtractor(Stream archiveStream)，该构造函数会自动检测归档格式。</param>
        public SevenZipExtractor(Stream archiveStream, bool leaveOpen, InArchiveFormat format)
        {
            _leaveOpen = leaveOpen;
            _format = format;
            Init(archiveStream);
        }

        /// <summary>
        /// 初始化 SevenZipExtractor 类的新实例。
        /// </summary>
        /// <param name="archiveFullName">归档文件的完整名称。</param>
        public SevenZipExtractor(string archiveFullName)
        {
            Init(archiveFullName);
        }

        /// <summary>
        /// 初始化 SevenZipExtractor 类的新实例。
        /// </summary>
        /// <param name="archiveFullName">归档文件的完整名称。</param>
        /// <param name="format">手动设置归档格式。通常不应以此方式指定，
        /// 而应使用 SevenZipExtractor(string archiveFullName)，该构造函数会自动检测归档格式。</param>
        public SevenZipExtractor(string archiveFullName, InArchiveFormat format)
        {
            _format = format;
            Init(archiveFullName);
        }

        /// <summary>
        /// 初始化 SevenZipExtractor 类的新实例。
        /// </summary>
        /// <param name="archiveFullName">归档文件的完整名称。</param>
        /// <param name="password">加密归档的密码。</param>
        public SevenZipExtractor(string archiveFullName, string password)
            : base(password)
        {
            Init(archiveFullName);
        }

        /// <summary>
        /// 初始化 SevenZipExtractor 类的新实例。
        /// </summary>
        /// <param name="archiveFullName">归档文件的完整名称。</param>
        /// <param name="password">加密归档的密码。</param>
        /// <param name="format">手动设置归档格式。通常不应以此方式指定，
        /// 而应使用 SevenZipExtractor(string archiveFullName, string password)，该构造函数会自动检测归档格式。</param>
        public SevenZipExtractor(string archiveFullName, string password, InArchiveFormat format)
            : base(password)
        {
            _format = format;
            Init(archiveFullName);
        }

        /// <summary>
        /// 初始化 SevenZipExtractor 类的新实例。
        /// </summary>
        /// <param name="archiveStream">用于读取归档的流。</param>
        /// <param name="password">加密归档的密码。</param>
        /// <remarks>归档格式通过签名进行推测。</remarks>
        public SevenZipExtractor(Stream archiveStream, string password) : this(archiveStream, password, false)
        {
        }

        /// <summary>
        /// 初始化 SevenZipExtractor 类的新实例。
        /// </summary>
        /// <param name="archiveStream">用于读取归档的流。</param>
        /// <param name="password">加密归档的密码。</param>
        /// <param name="leaveOpen">保持基础流打开。</param>
        /// <remarks>归档格式通过签名进行推测。</remarks>
        public SevenZipExtractor(Stream archiveStream, string password, bool leaveOpen) : this(archiveStream, password, leaveOpen, (InArchiveFormat)(-1))
        {
        }

        /// <summary>
        /// 初始化 SevenZipExtractor 类的新实例。
        /// </summary>
        /// <param name="archiveStream">用于读取归档的流。</param>
        /// <param name="password">加密归档的密码。</param>
        /// <param name="leaveOpen">保持基础流打开。</param>
        /// <param name="format">手动设置归档格式。通常不应以此方式指定，
        /// 而应使用 SevenZipExtractor(Stream archiveStream, string password)，该构造函数会自动检测归档格式。</param>
        public SevenZipExtractor(Stream archiveStream, string password, bool leaveOpen, InArchiveFormat format)
            : base(password)
        {
            _format = format;
            _leaveOpen = leaveOpen;
            Init(archiveStream);
        }

        #endregion

        #region Properties

        /// <summary>
        /// 获取或设置归档文件的完整名称
        /// </summary>
        public string FileName
        {
            get
            {
                DisposedCheck();

                return _fileName;
            }
        }

        /// <summary>
        /// 获取归档文件的大小
        /// </summary>
        public long PackedSize
        {
            get
            {
                DisposedCheck();

                return _packedSize ?? (_fileName != null ?
                           new FileInfo(_fileName).Length :
                           -1);
            }
        }

        /// <summary>
        /// 获取解压后归档数据的大小
        /// </summary>
        public long UnpackedSize
        {
            get
            {
                DisposedCheck();

                if (!_unpackedSize.HasValue)
                {
                    return -1;
                }

                return _unpackedSize.Value;
            }
        }

        /// <summary>
        /// 获取一个值，指示归档是否为固实压缩
        /// </summary>
        public bool IsSolid
        {
            get
            {
                DisposedCheck();

                if (!_isSolid.HasValue)
                {
                    GetArchiveInfo(true);
                }

                Debug.Assert(_isSolid != null);
                return _isSolid.Value;
            }
        }

        /// <summary>
        /// 获取归档中的文件数量
        /// </summary>
        [CLSCompliant(false)]
        public uint FilesCount
        {
            get
            {
                DisposedCheck();

                if (!_filesCount.HasValue)
                {
                    GetArchiveInfo(true);
                }

                Debug.Assert(_filesCount != null);
                return _filesCount.Value;
            }
        }

        /// <summary>
        /// 获取归档格式
        /// </summary>
        public InArchiveFormat Format
        {
            get
            {
                DisposedCheck();

                return _format;
            }
        }

        /// <summary>
        /// 获取或设置一个值，指示是否保留解压文件的目录结构。
        /// </summary>
        public bool PreserveDirectoryStructure { get; set; }

        #endregion

        /// <summary>
        /// 检查类是否已被释放。
        /// </summary>
        /// <exception cref="System.ObjectDisposedException" />
        private void DisposedCheck()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException("SevenZipExtractor");
            }

            RecreateInstanceIfNeeded();
        }

        #region Core private functions

        private ArchiveOpenCallback GetArchiveOpenCallback()
        {
            return _openCallback ?? (_openCallback = string.IsNullOrEmpty(Password)
                                    ? new ArchiveOpenCallback(_fileName)
                                    : new ArchiveOpenCallback(_fileName, Password));
        }

        /// <summary>
        /// 获取归档输入流。
        /// </summary>
        /// <returns>归档输入包装流。</returns>
        private IInStream GetArchiveStream(bool dispose)
        {
            if (_archiveStream != null)
            {
                if (_archiveStream is DisposeVariableWrapper wrapper)
                {
                    wrapper.DisposeStream = dispose;
                }

                return _archiveStream;
            }

            if (_inStream != null)
            {
                _inStream.Seek(0, SeekOrigin.Begin);
                _archiveStream = new InStreamWrapper(_inStream, false);
            }
            else
            {
                if (!_fileName.EndsWith(".001", StringComparison.OrdinalIgnoreCase))
                {
                    _archiveStream = new InStreamWrapper(
                        new ArchiveEmulationStreamProxy(new FileStream(
                            _fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, bufferSize: 65536, FileOptions.SequentialScan),
                            _offset, _leaveOpen),
                        dispose);
                }
                else
                {
                    _archiveStream = new InMultiStreamWrapper(_fileName, dispose);
                    _packedSize = (_archiveStream as InMultiStreamWrapper)?.Length;
                }
            }

            return _archiveStream;
        }

        /// <summary>
        /// 打开归档，若发生任何错误则抛出异常或返回 OperationResult.DataError。
        /// </summary>       
        /// <param name="archiveStream">符合 IInStream 接口的类实例，即输入流。</param>
        /// <param name="openCallback">ArchiveOpenCallback 实例。</param>
        /// <returns>如果 Open() 成功则返回 OperationResult.Ok。</returns>
        private OperationResult OpenArchiveInner(IInStream archiveStream, IArchiveOpenCallback openCallback)
        {
            ulong checkPos = 1 << 23;
            var res = _archive.Open(archiveStream, in checkPos, openCallback);

            return (OperationResult)res;
        }

        /// <summary>
        /// 打开归档，若发生任何错误则抛出异常或返回 OperationResult.DataError。
        /// </summary>
        /// <param name="archiveStream">符合 IInStream 接口的类实例，即输入流。</param>
        /// <param name="openCallback">ArchiveOpenCallback 实例。</param>
        /// <returns>如果 Open() 成功则返回 true；否则返回 false。</returns>
        private bool OpenArchive(IInStream archiveStream, ArchiveOpenCallback openCallback)
        {
            if (!_opened)
            {
                if (OpenArchiveInner(archiveStream, openCallback) != OperationResult.Ok)
                {
                    if (!ThrowException(null, new SevenZipArchiveException()))
                    {
                        return false;
                    }
                }

                _volumeFileNames = new ReadOnlyCollection<string>(openCallback.VolumeFileNames);
                _opened = true;
            }

            return true;
        }

        /// <summary>
        /// 检索归档的所有信息。
        /// </summary>
        /// <exception cref="SevenZip.SevenZipArchiveException"/>
        private void GetArchiveInfo(bool disposeStream)
        {
            if (_archive == null)
            {
                if (!ThrowException(null, new SevenZipArchiveException()))
                {
                    return;
                }
            }
            else
            {
                IInStream archiveStream = GetArchiveStream(disposeStream);

                try
                {
                    var openCallback = GetArchiveOpenCallback();

                    if (!_opened)
                    {
                        if (!OpenArchive(archiveStream, openCallback))
                        {
                            return;
                        }
                        _opened = !disposeStream;
                    }

                    _filesCount = _archive.GetNumberOfItems();
                    var fileCount = _filesCount.Value;
                    _archiveFileData = new List<ArchiveFileInfo>((int)Math.Min(fileCount, (uint)int.MaxValue));

                    if (_filesCount != 0)
                    {
                        var data = default(PropVariant);

                        try
                        {
                            #region 获取归档项数据

                            for (uint i = 0; i < _filesCount; i++)
                            {
                                try
                                {
                                    var fileInfo = new ArchiveFileInfo();
                                    fileInfo.Index = (int)i;
                                    _archive.GetProperty(i, ItemPropId.Path, ref data);
                                    fileInfo.FileName = NativeMethods.SafeCast(data, "[no name]");
                                    _archive.GetProperty(i, ItemPropId.LastWriteTime, ref data);
                                    fileInfo.LastWriteTime = NativeMethods.SafeCast(data, DateTime.Now);
                                    _archive.GetProperty(i, ItemPropId.CreationTime, ref data);
                                    fileInfo.CreationTime = NativeMethods.SafeCast(data, DateTime.Now);
                                    _archive.GetProperty(i, ItemPropId.LastAccessTime, ref data);
                                    fileInfo.LastAccessTime = NativeMethods.SafeCast(data, DateTime.Now);
                                    _archive.GetProperty(i, ItemPropId.Size, ref data);
                                    fileInfo.Size = NativeMethods.SafeCast<ulong>(data, 0);
                                    if (fileInfo.Size == 0)
                                    {
                                        fileInfo.Size = NativeMethods.SafeCast<uint>(data, 0);
                                    }
                                    _archive.GetProperty(i, ItemPropId.Attributes, ref data);
                                    fileInfo.Attributes = NativeMethods.SafeCast<uint>(data, 0);
                                    _archive.GetProperty(i, ItemPropId.IsDirectory, ref data);
                                    fileInfo.IsDirectory = NativeMethods.SafeCast(data, false);
                                    _archive.GetProperty(i, ItemPropId.Encrypted, ref data);
                                    fileInfo.Encrypted = NativeMethods.SafeCast(data, false);
                                    _archive.GetProperty(i, ItemPropId.Crc, ref data);
                                    fileInfo.Crc = NativeMethods.SafeCast<uint>(data, 0);
                                    _archive.GetProperty(i, ItemPropId.Comment, ref data);
                                    fileInfo.Comment = NativeMethods.SafeCast(data, "");
                                    _archive.GetProperty(i, ItemPropId.Method, ref data);
                                    fileInfo.Method = NativeMethods.SafeCast(data, "");
                                    _archiveFileData.Add(fileInfo);
                                }
                                catch (InvalidCastException)
                                {
                                    ThrowException(null, new SevenZipArchiveException("归档可能已损坏。"));
                                }
                            }

                            #endregion

                            #region 获取归档属性

                            var numProps = _archive.GetNumberOfArchiveProperties();
                            var archProps = new List<ArchiveProperty>((int)Math.Min(numProps, (uint)int.MaxValue));

                            for (uint i = 0; i < numProps; i++)
                            {
                                _archive.GetArchivePropertyInfo(i, out _, out var propId, out _);
                                _archive.GetArchiveProperty(propId, ref data);

                                if (propId == ItemPropId.Solid)
                                {
                                    _isSolid = NativeMethods.SafeCast(data, true);
                                }

                                // TODO 添加更多归档属性
                                if (PropIdToName.PropIdNames.TryGetValue(propId, out var propName))
                                {
                                    archProps.Add(new ArchiveProperty
                                    {
                                        Name = propName,
                                        Value = data.Object
                                    });
                                }
                                else
                                {
                                    Debug.WriteLine("遇到未知的归档属性（代码 " + ((int)propId).ToString(CultureInfo.InvariantCulture) + ")");
                                }
                            }

                            _archiveProperties = new ReadOnlyCollection<ArchiveProperty>(archProps);

                            if (!_isSolid.HasValue && _format == InArchiveFormat.Zip)
                            {
                                _isSolid = false;
                            }

                            if (!_isSolid.HasValue)
                            {
                                _isSolid = true;
                            }

                            #endregion
                        }
                        catch (Exception)
                        {
                            if (openCallback.ThrowException())
                            {
                                throw;
                            }
                        }
                    }
                }
                finally
                {
                    (archiveStream as IDisposable)?.Dispose();
                }

                if (disposeStream)
                {
                    _archive.Close();
                    _archiveStream = null;
                }

                _archiveFileInfoCollection = new ReadOnlyCollection<ArchiveFileInfo>(_archiveFileData);
            }
        }

        /// <summary>
        /// 确保 _archiveFileData 已加载。
        /// </summary>
        /// <param name="disposeStream">在此操作后释放归档流。</param>
        private void InitArchiveFileData(bool disposeStream)
        {
            if (_archiveFileData == null)
            {
                GetArchiveInfo(disposeStream);
            }
        }

        /// <summary>
        /// 生成从 0 到指定数组中最大值的索引数组
        /// </summary>
        /// <param name="indexes">源数组</param>
        /// <returns>从 0 到指定数组中最大值的索引数组</returns>
        private static uint[] SolidIndexes(uint[] indexes)
        {
            var max = 0;
            for (var i = 0; i < indexes.Length; i++)
            {
                var v = (int)indexes[i];
                if (v > max) max = v;
            }

            if (max > 0)
            {
                max++;
                var res = new uint[max];

                for (var i = 0; i < max; i++)
                {
                    res[i] = (uint)i;
                }

                return res;
            }

            return indexes;
        }

        /// <summary>
        /// 检查所有索引是否有效。
        /// </summary>
        /// <param name="indexes">要检查的索引。</param>
        /// <returns>有效则返回 true；否则返回 false。</returns>
        private static bool CheckIndexes(params int[] indexes)
        {
            for (var i = 0; i < indexes.Length; i++)
            {
                if (indexes[i] < 0) return false;
            }
            return true;
        }

        private void ArchiveExtractCallbackCommonInit(ArchiveExtractCallback aec)
        {
            aec.Open += ((s, e) => { _unpackedSize = (long)e.TotalSize; });
            aec.FileExtractionStarted += FileExtractionStartedEventProxy;
            aec.FileExtractionFinished += FileExtractionFinishedEventProxy;
            aec.Extracting += ExtractingEventProxy;
            aec.FileExists += FileExistsEventProxy;
        }

        /// <summary>
        /// 获取 IArchiveExtractCallback 回调
        /// </summary>
        /// <param name="directory">解压文件的目标目录</param>
        /// <param name="filesCount">要解压的文件数量</param>
        /// <param name="actualIndexes">实际索引列表（支持固实压缩归档）</param>
        /// <returns>ArchiveExtractCallback 回调</returns>
        private ArchiveExtractCallback GetArchiveExtractCallback(string directory, int filesCount, List<uint> actualIndexes)
        {
            var aec = string.IsNullOrEmpty(Password) ?
                new ArchiveExtractCallback(_archive, directory, filesCount, PreserveDirectoryStructure, actualIndexes, this) :
                new ArchiveExtractCallback(_archive, directory, filesCount, PreserveDirectoryStructure, actualIndexes, Password, this);
            ArchiveExtractCallbackCommonInit(aec);

            return aec;
        }

        /// <summary>
        /// 获取 IArchiveExtractCallback 回调
        /// </summary>
        /// <param name="stream">解压文件的目标流</param>
        /// <param name="index">文件索引</param>
        /// <param name="filesCount">要解压的文件数量</param>
        /// <returns>ArchiveExtractCallback 回调</returns>
        private ArchiveExtractCallback GetArchiveExtractCallback(Stream stream, uint index, int filesCount)
        {
            var aec = string.IsNullOrEmpty(Password)
                      ? new ArchiveExtractCallback(_archive, stream, filesCount, index, this)
                      : new ArchiveExtractCallback(_archive, stream, filesCount, index, Password, this);
            ArchiveExtractCallbackCommonInit(aec);

            return aec;
        }

        private void FreeArchiveExtractCallback(ArchiveExtractCallback callback)
        {
            callback.Open -= ((s, e) => { _unpackedSize = (long)e.TotalSize; });
            callback.FileExtractionStarted -= FileExtractionStartedEventProxy;
            callback.FileExtractionFinished -= FileExtractionFinishedEventProxy;
            callback.Extracting -= ExtractingEventProxy;
            callback.FileExists -= FileExistsEventProxy;
        }

        #endregion        
#endif

        /// <summary>
        /// 检查指定流是否支持解压。
        /// </summary>
        /// <param name="stream">要检查的流。</param>
        private static void ValidateStream(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            if (!stream.CanSeek || !stream.CanRead)
            {
                throw new ArgumentException("指定的流无法查找或读取。", nameof(stream));
            }

            if (stream.Length == 0)
            {
                throw new ArgumentException("指定的流长度为零。", nameof(stream));
            }
        }

#if UNMANAGED

        #region IDisposable Members

        private void CommonDispose()
        {
            if (_opened)
            {
                try
                {
                    _archive?.Close();
                }
                catch (Exception) { }
            }

            _archive = null;
            _archiveFileData = null;
            _archiveProperties = null;
            _archiveFileInfoCollection = null;

            if (_inStream != null && !_leaveOpen)
            {
                _inStream.Dispose();
                _inStream = null;
            }

            if (_openCallback != null)
            {
                try
                {
                    _openCallback.Dispose();
                }
                catch (ObjectDisposedException) { }
                _openCallback = null;
            }

            if (_archiveStream != null && !_leaveOpen)
            {
                if (_archiveStream is IDisposable disposable)
                {
                    try
                    {
                        if (disposable is DisposeVariableWrapper wrapper)
                        {
                            wrapper.DisposeStream = true;
                        }

                        disposable.Dispose();
                    }
                    catch (ObjectDisposedException) { }
                    _archiveStream = null;
                }
            }

            //SevenZipLibraryManager.FreeLibrary(this, _format);
        }

        /// <summary>
        /// 释放 SevenZipExtractor 使用的非托管资源。
        /// </summary>
        public void Dispose()
        {
            if (_asynchronousDisposeLock)
            {
                throw new InvalidOperationException("在进行异步方法调用时，不得释放 SevenZipExtractor 实例。");
            }

            if (!_disposed)
            {
                CommonDispose();
            }

            _disposed = true;
        }

        #endregion

        #region Core public Members

        #region Events

        /// <summary>
        /// 当新文件即将被解压时发生。
        /// </summary>
        /// <remarks>当 7-zip 引擎为新文件请求输出流以进行解压时发生。</remarks>
        public event EventHandler<FileInfoEventArgs> FileExtractionStarted;

        /// <summary>
        /// 当文件已成功解压时发生。
        /// </summary>
        public event EventHandler<FileInfoEventArgs> FileExtractionFinished;

        /// <summary>
        /// 当归档已解压完成时发生。
        /// </summary>
        public event EventHandler<EventArgs> ExtractionFinished;

        /// <summary>
        /// 当数据正在被解压时发生。
        /// </summary>
        /// <remarks>使用此事件进行精确的进度处理以及各种 ProgressBar.StepBy(e.PercentDelta) 例程。</remarks>
        public event EventHandler<ProgressEventArgs> Extracting;

        /// <summary>
        /// 在解压过程中当文件已存在时发生。
        /// </summary>
        public event EventHandler<FileOverwriteEventArgs> FileExists;

        #region Event proxies

        /// <summary>
        /// FileExtractionStarted 的事件代理。
        /// </summary>
        /// <param name="sender">事件的发送者。</param>
        /// <param name="e">事件参数。</param>
        private void FileExtractionStartedEventProxy(object sender, FileInfoEventArgs e)
        {
            OnEvent(FileExtractionStarted, e, true);
        }

        /// <summary>
        /// FileExtractionFinished 的事件代理。
        /// </summary>
        /// <param name="sender">事件的发送者。</param>
        /// <param name="e">事件参数。</param>
        private void FileExtractionFinishedEventProxy(object sender, FileInfoEventArgs e)
        {
            OnEvent(FileExtractionFinished, e, true);
        }

        /// <summary>
        /// Extracting 的事件代理。
        /// </summary>
        /// <param name="sender">事件的发送者。</param>
        /// <param name="e">事件参数。</param>
        private void ExtractingEventProxy(object sender, ProgressEventArgs e)
        {
            OnEvent(Extracting, e, false);
        }

        /// <summary>
        /// FileExists 的事件代理。
        /// </summary>
        /// <param name="sender">事件的发送者。</param>
        /// <param name="e">事件参数。</param>
        private void FileExistsEventProxy(object sender, FileOverwriteEventArgs e)
        {
            OnEvent(FileExists, e, true);
        }

        #endregion

        #endregion

        #region Properties

        /// <summary>
        /// 获取 ArchiveFileInfo 集合，包含归档中所有文件的信息
        /// </summary>
        public ReadOnlyCollection<ArchiveFileInfo> ArchiveFileData
        {
            get
            {
                DisposedCheck();
                InitArchiveFileData(true);

                return _archiveFileInfoCollection;
            }
        }

        /// <summary>
        /// 获取当前归档的属性
        /// </summary>
        public ReadOnlyCollection<ArchiveProperty> ArchiveProperties
        {
            get
            {
                DisposedCheck();
                InitArchiveFileData(true);

                return _archiveProperties;
            }
        }

        /// <summary>
        /// 获取归档中包含的所有文件名的集合。
        /// </summary>
        /// <remarks>
        /// 每次获取都会重新创建集合
        /// </remarks>
        public ReadOnlyCollection<string> ArchiveFileNames
        {
            get
            {
                DisposedCheck();
                InitArchiveFileData(true);
                var fileNames = new List<string>(_archiveFileData.Count);

                for (var i = 0; i < _archiveFileData.Count; i++)
                {
                    fileNames.Add(_archiveFileData[i].FileName);
                }

                return new ReadOnlyCollection<string>(fileNames);
            }
        }

        /// <summary>
        /// 获取归档卷文件名的列表。
        /// </summary>
        public ReadOnlyCollection<string> VolumeFileNames
        {
            get
            {
                DisposedCheck();
                InitArchiveFileData(true);

                return _volumeFileNames;
            }
        }
        #endregion

        /// <summary>
        /// 执行归档完整性测试。
        /// </summary>
        /// <returns>如果归档完好则返回 true；否则返回 false。</returns>
        public bool Check()
        {
            DisposedCheck();

            try
            {
                InitArchiveFileData(false);
                var archiveStream = GetArchiveStream(true);
                var openCallback = GetArchiveOpenCallback();

                if (!OpenArchive(archiveStream, openCallback))
                {
                    return false;
                }

                using (var aec = GetArchiveExtractCallback("", (int)_filesCount, null))
                {
                    try
                    {
                        CheckedExecute(
                            _archive.Extract(null, uint.MaxValue, 1, aec),
                            SevenZipExtractionFailedException.DEFAULT_MESSAGE, aec);
                    }
                    finally
                    {
                        FreeArchiveExtractCallback(aec);
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }
            finally
            {
                _archive?.Close();

                if (_archiveStream is IDisposable disposable)
                {
                    disposable.Dispose();
                }

                _archiveStream = null;
                _opened = false;
            }

            return true;
        }

        #region ExtractFile overloads

        /// <summary>
        /// 按文件名将文件解压到指定流。
        /// </summary>
        /// <param name="fileName">归档文件表中的文件完整名称。</param>
        /// <param name="stream">文件解压到的目标流。</param>
        public void ExtractFile(string fileName, Stream stream)
        {
            DisposedCheck();

            InitArchiveFileData(false);
            var index = -1;

            for (var i = 0; i < _archiveFileData.Count; i++)
            {
                var afi = _archiveFileData[i];
                if (afi.FileName == fileName && !afi.IsDirectory)
                {
                    index = afi.Index;
                    break;
                }
            }

            if (index == -1)
            {
                if (!ThrowException(null, new ArgumentOutOfRangeException(
                                              nameof(fileName),
                                              "在归档文件表中未找到指定的文件名。")))
                {
                    return;
                }
            }
            else
            {
                ExtractFile(index, stream);
            }
        }

        /// <summary>
        /// 按索引将文件解压到指定流。
        /// </summary>
        /// <param name="index">归档文件表中的索引。</param>
        /// <param name="stream">文件解压到的目标流。</param>
        public void ExtractFile(int index, Stream stream)
        {
            DisposedCheck();
            ClearExceptions();

            if (!CheckIndexes(index))
            {
                if (!ThrowException(null, new ArgumentException("索引必须大于或等于零。", nameof(index))))
                {
                    return;
                }
            }

            if (!stream.CanWrite)
            {
                if (!ThrowException(null, new ArgumentException("指定的流无法写入。", nameof(stream))))
                {
                    return;
                }
            }

            InitArchiveFileData(false);

            if (index > _filesCount - 1)
            {
                if (!ThrowException(null, new ArgumentOutOfRangeException(
                                              nameof(index), "指定的索引大于归档文件数量。")))
                {
                    return;
                }
            }

            var archiveStream = GetArchiveStream(false);
            var openCallback = GetArchiveOpenCallback();

            if (!OpenArchive(archiveStream, openCallback))
            {
                return;
            }

            try
            {
                var indexes = new uint[] { (uint)index };
                var entry = _archiveFileData[index];

                if (_isSolid.Value && !entry.Method.Equals("Copy", StringComparison.OrdinalIgnoreCase))
                {
                    indexes = SolidIndexes(indexes);
                }

                using (var aec = GetArchiveExtractCallback(stream, (uint)index, indexes.Length))
                {
                    try
                    {
                        CheckedExecute(
                            _archive.Extract(indexes, (uint)indexes.Length, 0, aec),
                            SevenZipExtractionFailedException.DEFAULT_MESSAGE, aec);
                    }
                    finally
                    {
                        FreeArchiveExtractCallback(aec);
                    }
                }
            }
            catch (Exception)
            {
                if (openCallback.ThrowException())
                {
                    throw;
                }
            }

            OnEvent(ExtractionFinished, EventArgs.Empty, false);
            ThrowUserException();
        }

        #endregion

        #region ExtractFiles overloads

        /// <summary>
        /// 按索引将文件解压到指定目录。
        /// </summary>
        /// <param name="indexes">归档文件表中的文件索引。</param>
        /// <param name="directory">文件解压到的目标目录。</param>
        public void ExtractFiles(string directory, params int[] indexes)
        {
            DisposedCheck();
            ClearExceptions();

            if (!CheckIndexes(indexes))
            {
                if (!ThrowException(null, new ArgumentException("索引必须大于或等于零。", nameof(indexes))))
                {
                    return;
                }
            }

            InitArchiveFileData(false);

            #region 索引处理

            var uIndexes = new uint[indexes.Length];

            for (var i = 0; i < indexes.Length; i++)
            {
                uIndexes[i] = (uint)indexes[i];
            }

            for (var i = 0; i < uIndexes.Length; i++)
            {
                if (uIndexes[i] >= _filesCount)
                {
                    if (!ThrowException(null,
                                         new ArgumentOutOfRangeException(nameof(indexes),
                                                                        $"索引必须小于 {_filesCount.Value.ToString(CultureInfo.InvariantCulture)}！")))
                    {
                        return;
                    }
                }
            }

            var origIndexes = new List<uint>(uIndexes);
            origIndexes.Sort();
            uIndexes = origIndexes.ToArray();

            if (_isSolid.Value)
            {
                uIndexes = SolidIndexes(uIndexes);
            }

            #endregion

            try
            {
                IInStream archiveStream = GetArchiveStream(origIndexes.Count != 1);

                try
                {
                    var openCallback = GetArchiveOpenCallback();

                    if (!OpenArchive(archiveStream, openCallback))
                    {
                        return;
                    }

                    try
                    {
                        using (var aec = GetArchiveExtractCallback(directory, (int)_filesCount, origIndexes))
                        {
                            try
                            {
                                CheckedExecute(
                                    _archive.Extract(uIndexes, (uint)uIndexes.Length, 0, aec),
                                    SevenZipExtractionFailedException.DEFAULT_MESSAGE, aec);
                            }
                            finally
                            {
                                FreeArchiveExtractCallback(aec);
                            }
                        }
                    }
                    catch (Exception)
                    {
                        if (openCallback.ThrowException())
                        {
                            throw;
                        }
                    }
                }
                finally
                {
                    (archiveStream as IDisposable)?.Dispose();
                }

                OnEvent(ExtractionFinished, EventArgs.Empty, false);
            }
            finally
            {
                if (origIndexes.Count > 1)
                {
                    _archive?.Close();
                    _archiveStream = null;
                    _opened = false;
                }
            }

            ThrowUserException();
        }

        /// <summary>
        /// 按完整文件名将文件解压到指定目录。
        /// </summary>
        /// <param name="fileNames">归档文件表中的完整文件名。</param>
        /// <param name="directory">文件解压到的目标目录。</param>
        public void ExtractFiles(string directory, params string[] fileNames)
        {
            DisposedCheck();
            InitArchiveFileData(false);
            var indexes = new List<int>(fileNames.Length);

            // 构建一次 name->index 查找，以避免 O(n*m) 的 Contains 加嵌套循环。
            var nameToIndex = new Dictionary<string, int>(_archiveFileData.Count, StringComparer.Ordinal);
            for (var i = 0; i < _archiveFileData.Count; i++)
            {
                var afi = _archiveFileData[i];
                if (!afi.IsDirectory && !nameToIndex.ContainsKey(afi.FileName))
                {
                    nameToIndex[afi.FileName] = afi.Index;
                }
            }

            foreach (var fn in fileNames)
            {
                if (!nameToIndex.TryGetValue(fn, out var idx))
                {
                    if (!ThrowException(null, new ArgumentOutOfRangeException(nameof(fileNames), $"在归档文件表中未找到文件 \"{fn}\"。")))
                    {
                        return;
                    }
                }
                else
                {
                    indexes.Add(idx);
                }
            }

            ExtractFiles(directory, indexes.ToArray());
        }

        /// <summary>
        /// 从归档中解压文件，通过回调决定如何处理每个文件。文件的顺序由归档决定。
        /// 不支持 7-Zip（及任何其他固实压缩）归档。
        /// </summary>
        /// <param name="extractFileCallback">对归档中每个文件调用的回调。</param>
        /// <exception cref="SevenZipExtractionFailedException">当尝试从固实压缩归档解压时抛出。</exception>
        public void ExtractFiles(ExtractFileCallback extractFileCallback)
        {
            DisposedCheck();
            InitArchiveFileData(false);

            if (IsSolid)
            {
                throw new SevenZipExtractionFailedException("不支持固实压缩归档。");
            }

            for (var i = 0; i < _archiveFileData.Count; i++)
            {
                var archiveFileInfo = _archiveFileData[i];
                var extractFileCallbackArgs = new ExtractFileCallbackArgs(archiveFileInfo);
                extractFileCallback(extractFileCallbackArgs);

                if (extractFileCallbackArgs.CancelExtraction)
                {
                    break;
                }

                if (extractFileCallbackArgs.ExtractToStream != null || extractFileCallbackArgs.ExtractToFile != null)
                {
                    var callDone = false;

                    try
                    {
                        if (extractFileCallbackArgs.ExtractToStream != null)
                        {
                            ExtractFile(archiveFileInfo.Index, extractFileCallbackArgs.ExtractToStream);
                        }
                        else
                        {
                            using (var file = new FileStream(extractFileCallbackArgs.ExtractToFile, FileMode.CreateNew, FileAccess.Write, FileShare.None, 65536, FileOptions.SequentialScan))
                            {
                                ExtractFile(archiveFileInfo.Index, file);
                            }
                        }

                        callDone = true;
                    }
                    catch (Exception ex)
                    {
                        extractFileCallbackArgs.Exception = ex;
                        extractFileCallbackArgs.Reason = ExtractFileCallbackReason.Failure;
                        extractFileCallback(extractFileCallbackArgs);

                        if (!ThrowException(null, ex))
                        {
                            return;
                        }
                    }

                    if (callDone)
                    {
                        extractFileCallbackArgs.Reason = ExtractFileCallbackReason.Done;
                        extractFileCallback(extractFileCallbackArgs);
                    }
                }
            }
        }

        #endregion

        /// <summary>
        /// 将整个归档解压到指定目录。
        /// </summary>
        /// <param name="directory">文件解压到的目标目录。</param>
        public void ExtractArchive(string directory)
        {
            DisposedCheck();
            ClearExceptions();
            InitArchiveFileData(false);

            try
            {
                IInStream archiveStream = GetArchiveStream(true);

                try
                {
                    var openCallback = GetArchiveOpenCallback();

                    if (!OpenArchive(archiveStream, openCallback))
                    {
                        return;
                    }

                    try
                    {
                        using (var aec = GetArchiveExtractCallback(directory, (int)_filesCount, null))
                        {
                            try
                            {
                                CheckedExecute(
                                    _archive.Extract(null, uint.MaxValue, 0, aec),
                                    SevenZipExtractionFailedException.DEFAULT_MESSAGE, aec);
                                OnEvent(ExtractionFinished, EventArgs.Empty, false);
                            }
                            finally
                            {
                                FreeArchiveExtractCallback(aec);
                            }
                        }
                    }
                    catch (Exception)
                    {
                        if (openCallback.ThrowException())
                        {
                            throw;
                        }
                    }
                }
                finally
                {
                    (archiveStream as IDisposable)?.Dispose();
                }
            }
            finally
            {
                _archive?.Close();
                _archiveStream = null;
                _opened = false;
            }

            ThrowUserException();
        }

        #endregion

#endif

        #region LZMA SDK 函数

        internal static byte[] GetLzmaProperties(Stream inStream, out long outSize)
        {
            var lzmAproperties = new byte[5];

            if (inStream.Read(lzmAproperties, 0, 5) != 5)
            {
                throw new LzmaException();
            }

            outSize = 0;

            for (var i = 0; i < 8; i++)
            {
                var b = inStream.ReadByte();

                if (b < 0)
                {
                    throw new LzmaException();
                }

                outSize |= ((long)(byte)b) << (i << 3);
            }

            return lzmAproperties;
        }

        /// <summary>
        /// 解压指定流（C# 内部实现）
        /// </summary>
        /// <param name="inStream">源压缩流</param>
        /// <param name="outStream">目标解压流</param>
        /// <param name="inLength">压缩数据的长度（为 null 时使用 inStream.Length）</param>
        /// <param name="codeProgressEvent">用于处理编码进度的事件</param>
        public static void DecompressStream(Stream inStream, Stream outStream, int? inLength, EventHandler<ProgressEventArgs> codeProgressEvent)
        {
            if (!inStream.CanRead || !outStream.CanWrite)
            {
                throw new ArgumentException("指定的流无效。");
            }

            var decoder = new Decoder();
            var inSize = (inLength ?? inStream.Length) - inStream.Position;
            decoder.SetDecoderProperties(GetLzmaProperties(inStream, out var outSize));
            decoder.Code(inStream, outStream, inSize, outSize, new LzmaProgressCallback(inSize, codeProgressEvent));
        }

        /// <summary>
        /// 解压使用 LZMA 算法压缩的字节数组（C# 内部实现）
        /// </summary>
        /// <param name="data">要解压的字节数组</param>
        /// <returns>解压后的字节数组</returns>
        public static byte[] ExtractBytes(byte[] data)
        {
            using (var inStream = new MemoryStream(data))
            {
                var decoder = new Decoder();
                inStream.Seek(0, 0);

                using (var outStream = new MemoryStream())
                {
                    decoder.SetDecoderProperties(GetLzmaProperties(inStream, out var outSize));
                    decoder.Code(inStream, outStream, inStream.Length - inStream.Position, outSize, null);
                    return outStream.ToArray();
                }
            }
        }

        #endregion
    }
}
