namespace SevenZip
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Runtime.InteropServices.Marshalling;
#if UNMANAGED
    /// <summary>
    /// 用于处理文件解包过程的归档解压回调
    /// </summary>
    /// 
    [GeneratedComClass]
    internal sealed partial class ArchiveExtractCallback : CallbackBase, IArchiveExtractCallback, ICryptoGetTextPassword, IDisposable
    {
        private HashSet<uint> _actualIndexes;
        private IInArchive _archive;

        /// <summary>
        /// 用于压缩事件。
        /// </summary>
        private long _bytesCount;

        private long _bytesWritten;
        private long _bytesWrittenOld;
        private string _directory;

        /// <summary>
        /// 已完成工作的比率，范围 [0, 1]。
        /// </summary>
        private float _doneRate;

        private SevenZipExtractor _extractor;
        private FakeOutStreamWrapper _fakeStream;
        private uint? _fileIndex;
        private int _filesCount;
        private OutStreamWrapper _fileStream;
        private bool _directoryStructure;
        private int _currentIndex;
        private const int MemoryPressure = 64 * 1024 * 1024; //64mb 似乎是最大值

        #region Constructors

        /// <summary>
        /// 初始化 ArchiveExtractCallback 类的新实例
        /// </summary>
        /// <param name="archive">归档的 IInArchive 接口</param>
        /// <param name="directory">文件解包到的目录</param>
        /// <param name="filesCount">归档文件数量</param>'
        /// <param name="extractor">回调的所有者</param>
        /// <param name="actualIndexes">实际索引列表（支持固实归档）</param>
        /// <param name="directoryStructure">指示是否保留解压文件目录结构的值。</param>
        public ArchiveExtractCallback(IInArchive archive, string directory, int filesCount, bool directoryStructure,
            List<uint> actualIndexes, SevenZipExtractor extractor)
        {
            Init(archive, directory, filesCount, directoryStructure, actualIndexes, extractor);
        }

        /// <summary>
        /// 初始化 ArchiveExtractCallback 类的新实例
        /// </summary>
        /// <param name="archive">归档的 IInArchive 接口</param>
        /// <param name="directory">文件解包到的目录</param>
        /// <param name="filesCount">归档文件数量</param>
        /// <param name="password">归档的密码</param>
        /// <param name="extractor">回调的所有者</param>
        /// <param name="actualIndexes">实际索引列表（支持固实归档）</param>
        /// <param name="directoryStructure">指示是否保留解压文件目录结构的值。</param>
        public ArchiveExtractCallback(IInArchive archive, string directory, int filesCount, bool directoryStructure,
            List<uint> actualIndexes, string password, SevenZipExtractor extractor)
            : base(password)
        {
            Init(archive, directory, filesCount, directoryStructure, actualIndexes, extractor);
        }

        /// <summary>
        /// 初始化 ArchiveExtractCallback 类的新实例
        /// </summary>
        /// <param name="archive">归档的 IInArchive 接口</param>
        /// <param name="stream">文件解包到的流</param>
        /// <param name="filesCount">归档文件数量</param>
        /// <param name="fileIndex">该流对应的文件索引</param>
        /// <param name="extractor">回调的所有者</param>
        public ArchiveExtractCallback(IInArchive archive, Stream stream, int filesCount, uint fileIndex, SevenZipExtractor extractor)
        {
            Init(archive, stream, filesCount, fileIndex, extractor);
        }

        /// <summary>
        /// 初始化 ArchiveExtractCallback 类的新实例
        /// </summary>
        /// <param name="archive">归档的 IInArchive 接口</param>
        /// <param name="stream">文件解包到的流</param>
        /// <param name="filesCount">归档文件数量</param>
        /// <param name="fileIndex">该流对应的文件索引</param>
        /// <param name="password">归档的密码</param>
        /// <param name="extractor">回调的所有者</param>
        public ArchiveExtractCallback(IInArchive archive, Stream stream, int filesCount, uint fileIndex, string password, SevenZipExtractor extractor)
            : base(password)
        {
            Init(archive, stream, filesCount, fileIndex, extractor);
        }

        private void Init(IInArchive archive, string directory, int filesCount, bool directoryStructure, List<uint> actualIndexes, SevenZipExtractor extractor)
        {
            CommonInit(archive, filesCount, extractor);
            _directory = directory;
            _actualIndexes = actualIndexes != null ? new HashSet<uint>(actualIndexes) : null;
            _directoryStructure = directoryStructure;
            if (!directory.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
            {
                _directory += Path.DirectorySeparatorChar;
            }
        }

        private void Init(IInArchive archive, Stream stream, int filesCount, uint fileIndex, SevenZipExtractor extractor)
        {
            CommonInit(archive, filesCount, extractor);
            _fileStream = new OutStreamWrapper(stream, false);
            _fileStream.BytesWritten += IntEventArgsHandler;
            _fileIndex = fileIndex;
        }

        private void CommonInit(IInArchive archive, int filesCount, SevenZipExtractor extractor)
        {
            _archive = archive;
            _filesCount = filesCount;
            _fakeStream = new FakeOutStreamWrapper();
            _fakeStream.BytesWritten += IntEventArgsHandler;
            _extractor = extractor;
            GC.AddMemoryPressure(MemoryPressure);
        }
        #endregion

        /// <summary>
        /// 当一个新文件即将被解包时发生
        /// </summary>
        /// <remarks>当 7-zip 引擎为新文件请求输出流以进行解包时发生</remarks>
        public event EventHandler<FileInfoEventArgs> FileExtractionStarted;

        /// <summary>
        /// 当一个文件已成功解包时发生
        /// </summary>
        public event EventHandler<FileInfoEventArgs> FileExtractionFinished;

        /// <summary>
        /// 当归档被打开且 7-zip 发送解压数据的大小时发生
        /// </summary>
        public event EventHandler<OpenEventArgs> Open;

        /// <summary>
        /// 当执行解压时发生
        /// </summary>
        public event EventHandler<ProgressEventArgs> Extracting;

        /// <summary>
        /// 在解压过程中当文件已存在时发生
        /// </summary>
        public event EventHandler<FileOverwriteEventArgs> FileExists;

        private void IntEventArgsHandler(object sender, IntEventArgs e)
        {
            // 如果未设置 _bytesCount，则无法更新进度。
            if (_bytesCount == 0)
            {
                return;
            }

            var pold = (int)(_bytesWrittenOld * 100 / _bytesCount);
            _bytesWritten += e.Value;
            var pnow = (int)(_bytesWritten * 100 / _bytesCount);

            if (pnow > pold)
            {
                if (pnow > 100)
                {
                    pold = pnow = 0;
                }

                _bytesWrittenOld = _bytesWritten;
                Extracting?.Invoke(this, new ProgressEventArgs((byte)pnow, (byte)(pnow - pold)));
            }
        }

        #region IArchiveExtractCallback Members

        /// <summary>
        /// 给出解压后归档文件的大小
        /// </summary>
        /// <param name="total">解压后归档文件的大小（以字节为单位）</param>
        public void SetTotal(ulong total)
        {
            _bytesCount = (long)total;
            Open?.Invoke(this, new OpenEventArgs(total));
        }

        public void SetCompleted(in ulong completeValue) { }

        /// <summary>
        /// 设置用于写入解压数据的输出流
        /// </summary>
        /// <param name="index">当前文件索引</param>
        /// <param name="outStream">输出流指针</param>
        /// <param name="askExtractMode">解压模式</param>
        /// <returns>成功则返回 0</returns>
        public int GetStream(uint index, out ISequentialOutStream outStream, AskMode askExtractMode)
        {
            outStream = null;

            if (Canceled)
            {
                return -1;
            }

            _currentIndex = (int)index;

            if (askExtractMode == AskMode.Extract)
            {
                var fileName = _directory;

                if (!_fileIndex.HasValue)
                {
                    // 解压到文件

                    if (_actualIndexes == null || _actualIndexes.Contains(index))
                    {
                        var data = default(PropVariant);
                        _archive.GetProperty(index, ItemPropId.Path, ref data);
                        var entryName = NativeMethods.SafeCast(data, "");

                        #region Get entryName

                        if (string.IsNullOrEmpty(entryName))
                        {
                            if (_filesCount == 1)
                            {
                                var archName = Path.GetFileName(_extractor.FileName);
                                var dotIdx = archName.LastIndexOf('.');
                                if (dotIdx >= 0)
                                {
                                    archName = archName.Substring(0, dotIdx);
                                }
                                if (!archName.EndsWith(".tar", StringComparison.OrdinalIgnoreCase))
                                {
                                    archName += ".tar";
                                }

                                entryName = archName;
                            }
                            else
                            {
                                entryName = "[no name] " + index.ToString(CultureInfo.InvariantCulture);
                            }
                        }

                        #endregion

                        try
                        {
                            fileName = Path.Combine(RemoveIllegalCharacters(_directory, true), RemoveIllegalCharacters(_directoryStructure ? entryName : Path.GetFileName(entryName)));
                            
                            if (string.IsNullOrEmpty(fileName))
                            {
                                throw new SevenZipArchiveException("Some archive name is null or empty.");
                            }
                        }
                        catch (Exception e)
                        {
                            AddException(e);
                            outStream = _fakeStream;

                            return 0;
                        }

                        _archive.GetProperty(index, ItemPropId.IsDirectory, ref data);

                        if (!NativeMethods.SafeCast(data, false))
                        {
                            _archive.GetProperty(index, ItemPropId.LastWriteTime, ref data);
                            var time = NativeMethods.SafeCast(data, DateTime.MinValue);
                            
                            if (File.Exists(fileName))
                            {
                                var fnea = new FileOverwriteEventArgs(fileName);

                                FileExists?.Invoke(this, fnea);
                                
                                if (fnea.Cancel)
                                {
                                    Canceled = true;
                                    return -1;
                                }

                                if (string.IsNullOrEmpty(fnea.FileName))
                                {
                                    outStream = _fakeStream;
                                }
                                else
                                {
                                    fileName = fnea.FileName;
                                }
                            }

                            _doneRate += 1.0f / _filesCount;
                            var iea = new FileInfoEventArgs(_extractor.ArchiveFileData[(int) index], PercentDoneEventArgs.ProducePercentDone(_doneRate));

                            FileExtractionStarted?.Invoke(this, iea);
                            
                            if (iea.Cancel)
                            {
                                Canceled = true;
                                return -1;
                            }

                            if (iea.Skip)
                            {
                                outStream = _fakeStream;
                                return 0;
                            }

                            CreateDirectory(fileName);

                            try
                            {
                                _fileStream = new OutStreamWrapper(new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 65536, FileOptions.SequentialScan), fileName, time, true);
                            }
                            catch (Exception e)
                            {
                                AddException(e is FileNotFoundException
                                    ? new IOException($"The file \"{fileName}\" was not extracted due to the File.Create fail.")
                                    : e);

                                outStream = _fakeStream;

                                return 0;
                            }

                            _fileStream.BytesWritten += IntEventArgsHandler;
                            outStream =  _fileStream;
                        }
                        else
                        {
                            _doneRate += 1.0f / _filesCount;
                            var iea = new FileInfoEventArgs(_extractor.ArchiveFileData[(int)index], PercentDoneEventArgs.ProducePercentDone(_doneRate));
                            FileExtractionStarted?.Invoke(this, iea);
                            
                            if (iea.Cancel)
                            {
                                Canceled = true;
                                return -1;
                            }

                            if (iea.Skip)
                            {
                                outStream = _fakeStream;
                                return 0;
                            }

                            if (!Directory.Exists(fileName))
                            {
                                try
                                {
                                    Directory.CreateDirectory(fileName);
                                }
                                catch (Exception e)
                                {
                                    AddException(e);
                                }

                                outStream = _fakeStream;
                            }
                        }
                    }
                    else
                    {
                        outStream = _fakeStream;
                    }
                }
                else
                {
                    // 解压到流。

                    if (index == _fileIndex)
                    {
                        outStream  = _fileStream;
                        _fileIndex = null;
                    }
                    else
                    {
                        outStream = _fakeStream;
                    }
                }
            }

            return 0;
        }

        /// <inheritdoc />
        public void PrepareOperation(AskMode askExtractMode) { }

        /// <inheritdoc />
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
                        AddException(new ExtractionFailedException("File is corrupted. Data error has occured."));
                        break;
                    case OperationResult.UnsupportedMethod:
                        AddException(new ExtractionFailedException("Unsupported method error has occured."));
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
            else
            {
                if (_fileStream != null && !_fileIndex.HasValue)
                {
                    try
                    {
                        _fileStream.BytesWritten -= IntEventArgsHandler;
                        _fileStream.Dispose();
                    }
                    catch (ObjectDisposedException) { }
                    _fileStream = null;
                }

                var iea = new FileInfoEventArgs(_extractor.ArchiveFileData[_currentIndex], PercentDoneEventArgs.ProducePercentDone(_doneRate));
                FileExtractionFinished?.Invoke(this, iea);
                
                if (iea.Cancel)
                {
                    Canceled = true;
                }
            }
        }

        #endregion

        /// <inheritdoc />
        public int CryptoGetTextPassword(out string password)
        {
            password = Password;
            return 0;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            GC.RemoveMemoryPressure(MemoryPressure);

            if (_fileStream != null)
            {
                try
                {
                    _fileStream.Dispose();
                }
                catch (ObjectDisposedException) { }
                _fileStream = null;
            }

            if (_fakeStream != null)
            {
                try
                {
                    _fakeStream.Dispose();
                }
                catch (ObjectDisposedException) { }
                _fakeStream = null;
            }
        }

        /// <summary>
        /// 确保文件名对应的目录有效，并在必要时创建中间目录
        /// </summary>
        /// <param name="fileName">文件名</param>
        private static void CreateDirectory(string fileName)
        {
            var destinationDirectory = Path.GetDirectoryName(fileName);

            if (!string.IsNullOrEmpty(destinationDirectory))
            {
                Directory.CreateDirectory(destinationDirectory);
            }
        }

        /// <summary>
        /// 移除文件路径中的无效字符。
        /// </summary>
        /// <param name="str"></param>
        /// <param name="isDirectory"></param>
        /// <returns></returns>
        private static string RemoveIllegalCharacters(string str, bool isDirectory = false)
        {
            var splitFileName = new List<string>(str.Split(Path.DirectorySeparatorChar));

            foreach (var chr in Path.GetInvalidFileNameChars())
            {
                for (var i = 0; i < splitFileName.Count; i++)
                {
                    if (isDirectory && chr == ':' && i == 0)
                    {
                        continue;
                    }
                    if (string.IsNullOrEmpty(splitFileName[i]))
                    {
                        continue;
                    }
                    if (splitFileName[i].IndexOf(chr) >= 0)
                    {
                        splitFileName[i] = splitFileName[i].Replace(chr, '_');
                    }
                }
            }

            var twoSeparators = new string(Path.DirectorySeparatorChar, 2);
            if (str.StartsWith(twoSeparators, StringComparison.OrdinalIgnoreCase))
            {
                splitFileName.RemoveAt(0);
                splitFileName.RemoveAt(0);
                splitFileName[0] = twoSeparators + splitFileName[0];
            }

            return string.Join(Path.DirectorySeparatorChar.ToString(), splitFileName);
        }
    }
#endif
}