namespace SevenZip
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Runtime.InteropServices.Marshalling;
#if UNMANAGED
    /// <summary>
    /// 用于处理打包文件过程的归档更新回调
    /// </summary>
    /// 
    [GeneratedComClass]
    internal sealed partial class ArchiveUpdateCallback : CallbackBase, IArchiveUpdateCallback, ICryptoGetTextPassword2,
                                                  IDisposable
    {
        #region Fields
        /// <summary>
        /// 不计入目录时的 _files.Count
        /// </summary>
        private int _actualFilesCount;

        /// <summary>
        /// 用于 Compressing 事件。
        /// </summary>
        private long _bytesCount;

        private long _bytesWritten;
        private long _bytesWrittenOld;
        private SevenZipCompressor _compressor;

        /// <summary>
        /// 不包含目录。
        /// </summary>
        private bool _directoryStructure;

        /// <summary>
        /// 已完成工作的比率，范围 [0, 1]
        /// </summary>
        private float _doneRate;

        /// <summary>
        /// 归档条目的名称
        /// </summary>
        private string[] _entries;

        /// <summary>
        /// 要打包的文件数组
        /// </summary>
        private FileInfo[] _files;

        private InStreamWrapper _fileStream;

        private uint _indexInArchive;
        private uint _indexOffset;

        /// <summary>
        /// 文件名公共根的长度。
        /// </summary>
        private int _rootLength;

        /// <summary>
        /// 待压缩的输入流。
        /// </summary>
        private Stream[] _streams;

        private UpdateData _updateData;
        private List<InStreamWrapper> _wrappersToDispose;

        /// <summary>
        /// 获取或设置在 MemoryStream 压缩中使用的默认条目名称。
        /// </summary>
        public string DefaultItemName { private get; set; }

        /// <summary>
        /// 获取或设置一个值，指示是否尽可能快地压缩，而不调用事件。
        /// </summary>
        public bool FastCompression { private get; set; } 

        private int _memoryPressure;

        #endregion

        #region Constructors

        /// <summary>
        /// 初始化 ArchiveUpdateCallback 类的新实例
        /// </summary>
        /// <param name="files">要打包的文件数组</param>
        /// <param name="rootLength">文件名公共根长度</param>
        /// <param name="compressor">回调的所有者</param>
        /// <param name="updateData">压缩参数。</param>
        /// <param name="directoryStructure">是否保留目录结构。</param>
        public ArchiveUpdateCallback(
            FileInfo[] files, int rootLength,
            SevenZipCompressor compressor, UpdateData updateData, bool directoryStructure)
        {
            Init(files, rootLength, compressor, updateData, directoryStructure);
        }

        /// <summary>
        /// 初始化 ArchiveUpdateCallback 类的新实例
        /// </summary>
        /// <param name="files">要打包的文件数组</param>
        /// <param name="rootLength">文件名公共根长度</param>
        /// <param name="password">归档密码</param>
        /// <param name="compressor">回调的所有者</param>
        /// <param name="updateData">压缩参数。</param>
        /// <param name="directoryStructure">是否保留目录结构。</param>
        public ArchiveUpdateCallback(
            FileInfo[] files, int rootLength, string password,
            SevenZipCompressor compressor, UpdateData updateData, bool directoryStructure)
            : base(password)
        {
            Init(files, rootLength, compressor, updateData, directoryStructure);
        }

        /// <summary>
        /// 初始化 ArchiveUpdateCallback 类的新实例
        /// </summary>
        /// <param name="stream">输入流</param>
        /// <param name="compressor">回调的所有者</param>
        /// <param name="updateData">压缩参数。</param>
        /// <param name="directoryStructure">是否保留目录结构。</param>
        public ArchiveUpdateCallback(
            Stream stream, SevenZipCompressor compressor, UpdateData updateData, bool directoryStructure)
        {
            Init(stream, compressor, updateData, directoryStructure);
        }

        /// <summary>
        /// 初始化 ArchiveUpdateCallback 类的新实例
        /// </summary>
        /// <param name="stream">输入流</param>
        /// <param name="password">归档密码</param>
        /// <param name="compressor">回调的所有者</param>
        /// <param name="updateData">压缩参数。</param>
        /// <param name="directoryStructure">是否保留目录结构。</param>
        public ArchiveUpdateCallback(
            Stream stream, string password, SevenZipCompressor compressor, UpdateData updateData,
            bool directoryStructure)
            : base(password)
        {
            Init(stream, compressor, updateData, directoryStructure);
        }

        /// <summary>
        /// 初始化 ArchiveUpdateCallback 类的新实例
        /// </summary>
        /// <param name="streamDict">文件流与归档条目名称的字典</param>
        /// <param name="compressor">回调的所有者</param>
        /// <param name="updateData">压缩参数。</param>
        /// <param name="directoryStructure">是否保留目录结构。</param>
        public ArchiveUpdateCallback(
            IDictionary<string, Stream> streamDict,
            SevenZipCompressor compressor, UpdateData updateData, bool directoryStructure)
        {
            Init(streamDict, compressor, updateData, directoryStructure);
        }

        /// <summary>
        /// 初始化 ArchiveUpdateCallback 类的新实例
        /// </summary>
        /// <param name="streamDict">文件流与归档条目名称的字典</param>
        /// <param name="password">归档密码</param>
        /// <param name="compressor">回调的所有者</param>
        /// <param name="updateData">压缩参数。</param>
        /// <param name="directoryStructure">是否保留目录结构。</param>
        public ArchiveUpdateCallback(
            IDictionary<string, Stream> streamDict, string password,
            SevenZipCompressor compressor, UpdateData updateData, bool directoryStructure)
            : base(password)
        {
            Init(streamDict, compressor, updateData, directoryStructure);
        }
        //public void SetCompleted(ref ulong completeValue)
        //{
        //    // 此处为实现代码
        //}

        private void CommonInit(SevenZipCompressor compressor, UpdateData updateData, bool directoryStructure)
        {
            _compressor = compressor;
            _indexInArchive = updateData.FilesCount;
            _indexOffset = updateData.Mode != InternalCompressionMode.Append ? 0 : _indexInArchive;
            if (_compressor.ArchiveFormat == OutArchiveFormat.Zip)
            {
                _wrappersToDispose = new List<InStreamWrapper>();
            }
            _updateData = updateData;
            _directoryStructure = directoryStructure;
            DefaultItemName = "default";            
        }

        private void Init(
            FileInfo[] files, int rootLength, SevenZipCompressor compressor,
            UpdateData updateData, bool directoryStructure)
        {
            _files = files;
            _rootLength = rootLength;
            if (files != null)
            {
                foreach (var fi in files)
                {
                    if (fi.Exists)
                    {
                        _bytesCount += fi.Length;
                        if ((fi.Attributes & FileAttributes.Directory) == 0)
                        {
                            _actualFilesCount++;
                        }
                    }
                }
            }
            CommonInit(compressor, updateData, directoryStructure);
        }

        private void Init(
            Stream stream, SevenZipCompressor compressor, UpdateData updateData, bool directoryStructure)
        {
            _fileStream = new InStreamWrapper(stream, false);
            _fileStream.BytesRead += IntEventArgsHandler;
            _actualFilesCount = 1;

            try
            {
                _bytesCount = stream.Length;
            }
            catch (NotSupportedException)
            {
                _bytesCount = -1;
            }
            try
            {
                stream.Seek(0, SeekOrigin.Begin);
            }
            catch (NotSupportedException)
            {
                _bytesCount = -1;
            }
            CommonInit(compressor, updateData, directoryStructure);
        }

        private void Init(
            IDictionary<string, Stream> streamDict,
            SevenZipCompressor compressor, UpdateData updateData, bool directoryStructure)
        {
            _streams = new Stream[streamDict.Count];
            streamDict.Values.CopyTo(_streams, 0);
            _entries = new string[streamDict.Count];
            streamDict.Keys.CopyTo(_entries, 0);
            _actualFilesCount = streamDict.Count;
            foreach (Stream str in _streams)
            {
                if (str != null)
                {
                    _bytesCount += str.Length;
                }
            }
            CommonInit(compressor, updateData, directoryStructure);
        }

        #endregion

        /// <summary>
        /// 获取或设置字典大小。
        /// </summary>
        public float DictionarySize
        {
            set
            {
                _memoryPressure = (int)(value * 1024 * 1024);
                GC.AddMemoryPressure(_memoryPressure);
            }
        }

        /// <summary>
        /// 为 GetStream 方法引发事件。
        /// </summary>
        /// <param name="index">当前条目的索引。</param>
        /// <returns>如果未取消则为 true；否则为 false。</returns>
        private bool EventsForGetStream(uint index)
        {
            if (!FastCompression)
            {
                if (_fileStream != null)
                {
                    _fileStream.BytesRead += IntEventArgsHandler;
                }
                _doneRate += 1.0f / _actualFilesCount;
                var fiea = new FileNameEventArgs(_files != null? _files[index].Name : _entries[index],
                                                 PercentDoneEventArgs.ProducePercentDone(_doneRate));
                OnFileCompression(fiea);
                
                if (fiea.Cancel)
                {
                    Canceled = true;
                    return false;
                }
            }
            return true;
        }

        #region Events

        /// <summary>
        /// 当下一个文件即将被打包时发生。
        /// </summary>
        /// <remarks>当 7-zip 引擎请求下一个文件的输入流以进行打包时发生</remarks>
        public event EventHandler<FileNameEventArgs> FileCompressionStarted;

        /// <summary>
        /// 当数据正在被压缩时发生。
        /// </summary>
        public event EventHandler<ProgressEventArgs> Compressing;

        /// <summary>
        /// 当当前文件已压缩完成时发生。
        /// </summary>
        public event EventHandler FileCompressionFinished;

        private void OnFileCompression(FileNameEventArgs e)
        {
            FileCompressionStarted?.Invoke(this, e);
        }

        private void OnCompressing(ProgressEventArgs e)
        {
            Compressing?.Invoke(this, e);
        }

        private void OnFileCompressionFinished(EventArgs e)
        {
            FileCompressionFinished?.Invoke(this, e);
        }

        #endregion

        #region IArchiveUpdateCallback Members

        public void SetTotal(ulong total) {}

        public void SetCompleted(in ulong completeValue) {}

        public int GetUpdateItemInfo(uint index, ref int newData, ref int newProperties, ref uint indexInArchive)
        {
            switch (_updateData.Mode)
            {
                case InternalCompressionMode.Create:
                    newData = 1;
                    newProperties = 1;
                    indexInArchive = uint.MaxValue;
                    break;
                case InternalCompressionMode.Append:
                    if (index < _indexInArchive)
                    {
                        newData = 0;
                        newProperties = 0;
                        indexInArchive = index;
                    }
                    else
                    {
                        newData = 1;
                        newProperties = 1;
                        indexInArchive = uint.MaxValue;
                    }
                    break;
                case InternalCompressionMode.Modify:
                    newData = 0;
                    if (_updateData.FileNamesToModify.TryGetValue((int)index, out var modName) && modName != null)
                    {
                        newProperties = 1;
                    }
                    else
                    {
                        newProperties = 0;
                    }
                    if (_updateData.FileNamesToModify.TryGetValue((int)index, out var modName2) && modName2 == null)
                    {
                        indexInArchive = (uint)_updateData.ArchiveFileData.Count;

                        foreach (var pairModification in _updateData.FileNamesToModify)
                        {
                            if (pairModification.Key <= index && pairModification.Value == null)
                            {
                                do
                                {
                                    indexInArchive--;
                                } while (indexInArchive > 0
                                         && _updateData.FileNamesToModify.TryGetValue((int)indexInArchive, out var del) && del == null);
                            }
                        }
                    }
                    else
                    {
                        indexInArchive = index;
                    }
                    break;
            }

            return 0;
        }

        public int GetProperty(uint index, ItemPropId propID, ref PropVariant value)
        {
            index -= _indexOffset;
            try
            {
                switch (propID)
                {
                    case ItemPropId.IsAnti:
                        value.VarType = VarEnum.VT_BOOL;
                        value.BoolVal = 0;
                        break;
                    case ItemPropId.Path:
                        #region Path

                        value.VarType = VarEnum.VT_BSTR;
                        string val = DefaultItemName;

                        if (_updateData.Mode != InternalCompressionMode.Modify)
                        {
                            if (_files == null)
                            {
                                if (_entries != null)
                                {
                                    val = _entries[index];
                                }
                            }
                            else
                            {
                                if (_directoryStructure)
                                {
                                    if (_rootLength > 0)
                                    {
                                        val = _files[index].FullName.Substring(_rootLength);
                                    }
                                    else
                                    {
                                        val = _files[index].FullName[0] + _files[index].FullName.Substring(2);
                                    }
                                }
                                else
                                {
                                    val = _files[index].Name;
                                }
                            }
                        }
                        else
                        {
                            val = _updateData.FileNamesToModify[(int) index];
                        }
                        value.Value = Marshal.StringToBSTR(val);
                        #endregion
                        break;
                    case ItemPropId.IsDirectory:
                        value.VarType = VarEnum.VT_BOOL;
                        if (_updateData.Mode != InternalCompressionMode.Modify)
                        {
                            if (_files == null)
                            {
                                if (_streams == null)
                                {
                                    value.BoolVal = 0;
                                }
                                else
                                {
                                    value.BoolVal = (short)(_streams[index] == null ? 1 : 0);
                                }
                            }
                            else
                            {
                                value.BoolVal = (short)(_files[index].Attributes & FileAttributes.Directory);
                            }
                        }
                        else
                        {
                            value.BoolVal = (short)(_updateData.ArchiveFileData[(int) index].IsDirectory ? 1 : 0);
                        }
                        break;
                    case ItemPropId.Size:
                        #region Size

                        value.VarType = VarEnum.VT_UI8;
                        ulong size;
                        if (_updateData.Mode != InternalCompressionMode.Modify)
                        {
                            if (_files == null)
                            {
                                if (_streams == null)
                                {
                                    size = _bytesCount > 0 ? (ulong) _bytesCount : 0;
                                }
                                else
                                {
                                    size = (ulong) (_streams[index] == null? 0 : _streams[index].Length);
                                }
                            }
                            else
                            {
                                size = (_files[index].Attributes & FileAttributes.Directory) == 0
                                           ? (ulong) _files[index].Length
                                           : 0;
                            }
                        }
                        else
                        {
                            size = _updateData.ArchiveFileData[(int) index].Size;
                        }
                        value.UInt64Value = size;

                        #endregion
                        break;
                    case ItemPropId.Attributes:
                        value.VarType = VarEnum.VT_UI4;
                        if (_updateData.Mode != InternalCompressionMode.Modify)
                        {
                            if (_files == null)
                            {
                                if (_streams == null)
                                {
                                    value.UInt32Value = (uint)FileAttributes.Normal;
                                }
                                else
                                {
                                    value.UInt32Value = (uint)(_streams[index] == null ? FileAttributes.Directory : FileAttributes.Normal);
                                }
                            }
                            else
                            {
                                value.UInt32Value = (uint) _files[index].Attributes;
                            }
                        }
                        else
                        {
                            value.UInt32Value = _updateData.ArchiveFileData[(int) index].Attributes;
                        }
                        break;
                    #region Times
                    case ItemPropId.CreationTime:
                        value.VarType = VarEnum.VT_FILETIME;
                        if (_updateData.Mode != InternalCompressionMode.Modify)
                        {
                            value.Int64Value = _files == null
                                               ? DateTime.Now.ToFileTime()
                                               : _files[index].CreationTime.ToFileTime();
                        }
                        else
                        {
                            value.Int64Value = _updateData.ArchiveFileData[(int) index].CreationTime.ToFileTime();
                        }
                        break;
                    case ItemPropId.LastAccessTime:
                        value.VarType = VarEnum.VT_FILETIME;
                        if (_updateData.Mode != InternalCompressionMode.Modify)
                        {
                            value.Int64Value = _files == null
                                               ? DateTime.Now.ToFileTime()
                                               : _files[index].LastAccessTime.ToFileTime();
                        }
                        else
                        {
                            value.Int64Value = _updateData.ArchiveFileData[(int) index].LastAccessTime.ToFileTime();
                        }
                        break;
                    case ItemPropId.LastWriteTime:
                        value.VarType = VarEnum.VT_FILETIME;
                        if (_updateData.Mode != InternalCompressionMode.Modify)
                        {
                            value.Int64Value = _files == null
                                               ? DateTime.Now.ToFileTime()
                                               : _files[index].LastWriteTime.ToFileTime();
                        }
                        else
                        {
                            value.Int64Value = _updateData.ArchiveFileData[(int) index].LastWriteTime.ToFileTime();
                        }
                        break;
                    #endregion
                    case ItemPropId.Extension:
                        #region Extension

                        value.VarType = VarEnum.VT_BSTR;
                        if (_updateData.Mode != InternalCompressionMode.Modify)
                        {
                            try
                            {
                                val = _files != null
                                      ? _files[index].Extension.Substring(1)
                                      : _entries == null
                                          ? ""
                                          : Path.GetExtension(_entries[index]);
                                value.Value = Marshal.StringToBSTR(val);
                            }
                            catch (ArgumentException)
                            {
                                value.Value = Marshal.StringToBSTR("");
                            }
                        }
                        else
                        {
                            val = Path.GetExtension(_updateData.ArchiveFileData[(int) index].FileName);
                            value.Value = Marshal.StringToBSTR(val);
                        }

                        #endregion
                        break;
                }
            }
            catch (Exception e)
            {
                AddException(e);
            }
            return 0;
        }

        /// <summary>
        /// 获取用于 7-zip 库的流。
        /// </summary>
        /// <param name="index">文件索引</param>
        /// <param name="inStream">输入文件流</param>
        /// <returns>成功则为零</returns>
        public int GetStream(uint index, out ISequentialInStream inStream)
        {
            index -= _indexOffset;

            if (_files != null)
            {
                _fileStream = null;

                try
                {
                    if (File.Exists(_files[index].FullName))
                    {
                        _fileStream = new InStreamWrapper(
                            new FileStream(_files[index].FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, bufferSize: 65536, FileOptions.SequentialScan),
                            true);
                    }
                }
                catch (Exception e)
                {
                    AddException(e);
                    inStream = null;
                    return -1;
                }

                inStream = _fileStream;

                if (!EventsForGetStream(index))
                {
                    return -1;
                }
            }
            else
            {
                if (_streams == null)
                {
                    inStream = _fileStream;
                }
                else
                {
                    _fileStream = new InStreamWrapper(_streams[index], true);
                    inStream = _fileStream;
                    if (!EventsForGetStream(index))
                    {
                        return -1;
                    }
                }
            }

            return 0;
        }

        public long EnumProperties(IntPtr enumerator)
        {
            //未实现的 HRESULT
            return 0x80004001L;
        }

        public void SetOperationResult(OperationResult operationResult)
        {
            if (operationResult != OperationResult.Ok && ReportErrors)
            {
                switch (operationResult)
                {
                    case OperationResult.CrcError:
                        AddException(new ExtractionFailedException("File is corrupted. Crc check has failed."));
                        break;
                    case OperationResult.DataError:
                        AddException(new ExtractionFailedException("File is corrupted. Data error has occurred."));
                        break;
                    case OperationResult.UnsupportedMethod:
                        AddException(new ExtractionFailedException("Unsupported method error has occurred."));
                        break;
                    case OperationResult.Unavailable:
                        AddException(new ExtractionFailedException("File is unavailable."));
                        break;
                    case OperationResult.UnexpectedEnd:
                        AddException(new ExtractionFailedException("Unexpected end of file."));
                        break;
                    case OperationResult.DataAfterEnd: 
                        AddException(new ExtractionFailedException("Data after end of archive."));
                        break;
                    case OperationResult.IsNotArc:
                        AddException(new ExtractionFailedException("File is not archive."));
                        break;
                    case OperationResult.HeadersError:
                        AddException(new ExtractionFailedException("Archive headers error."));
                        break;
                    case OperationResult.WrongPassword:
                        AddException(new ExtractionFailedException("Wrong password."));
                        break;
                    default:
                        AddException(new ExtractionFailedException($"Unexpected operation result: {operationResult}"));
                        break;
                }
            }
            if (_fileStream != null)
            {
                _fileStream.BytesRead -= IntEventArgsHandler;

                //Zip 的特定实现 - 不能对 Zip 的文件调用 Dispose。
                if (_compressor.ArchiveFormat != OutArchiveFormat.Zip)
                {
                    try
                    {
                        _fileStream.Dispose();                            
                    }
                    catch (ObjectDisposedException) {}
                }
                else
                {
                    _wrappersToDispose.Add(_fileStream);
                }                                
                
                _fileStream = null;
            }
            
            OnFileCompressionFinished(EventArgs.Empty);
        }

        #endregion

        #region ICryptoGetTextPassword2 Members

        public int CryptoGetTextPassword2(ref int passwordIsDefined, out string password)
        {
            passwordIsDefined = string.IsNullOrEmpty(Password) ? 0 : 1;
            password = Password;

            return 0;
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            GC.RemoveMemoryPressure(_memoryPressure);

            if (_fileStream != null)
            {
                try
                {
                    _fileStream.Dispose();
                }
                catch (ObjectDisposedException) {}
            }

            if (_wrappersToDispose == null)
            {
                return;
            }

            foreach (var wrapper in _wrappersToDispose)
            {
                try
                {
                    wrapper.Dispose();
                }
                catch (ObjectDisposedException) {}
            }
        }

        #endregion

        private void IntEventArgsHandler(object sender, IntEventArgs e)
        {
            var lockObject = ((object) _files ?? _streams) ?? _fileStream;

            lock (lockObject)
            {
                var pOld = (byte) (_bytesWrittenOld*100/_bytesCount);
                _bytesWritten += e.Value;
                byte pNow;

                if (_bytesCount < _bytesWritten) //见鬼，这个对 ZIP 的检查简直是金子
                {
                    pNow = 100;
                }
                else
                {
                    pNow = (byte)((_bytesWritten * 100) / _bytesCount);
                }

                if (pNow > pOld)
                {
                    _bytesWrittenOld = _bytesWritten;
                    OnCompressing(new ProgressEventArgs(pNow, (byte) (pNow - pOld)));
                }
            }
        }
    }
#endif
}
