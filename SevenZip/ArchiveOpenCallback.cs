namespace SevenZip
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Runtime.InteropServices.Marshalling;
#if UNMANAGED
    /// <summary>
    /// 用于处理归档文件打开的回调
    /// </summary>
    /// 
    [GeneratedComClass]
    internal sealed partial class ArchiveOpenCallback : CallbackBase, IArchiveOpenCallback, IArchiveOpenVolumeCallback,
                                                ICryptoGetTextPassword, IDisposable
    {
        private FileInfo _fileInfo;
        private Dictionary<string, InStreamWrapper> _wrappers = 
            new Dictionary<string, InStreamWrapper>();
        private readonly List<string> _volumeFileNames = new List<string>();

        /// <summary>
        /// 获取分卷文件名列表。
        /// </summary>
        public IList<string> VolumeFileNames => _volumeFileNames;

        /// <summary>
        /// 执行通用初始化。
        /// </summary>
        /// <param name="fileName">分卷文件名。</param>
        private void Init(string fileName)
        {
            if (!string.IsNullOrEmpty(fileName))
            {
                _fileInfo = new FileInfo(fileName);
                _volumeFileNames.Add(fileName);
                if (fileName.EndsWith("001", StringComparison.Ordinal))
                {
                    int index = 2;
                    var baseName = fileName.Substring(0, fileName.Length - 3);
                    string volName;
                    do
                    {
                        volName = baseName + (index > 99 ? index.ToString() :
                            index > 9 ? "0" + index : "00" + index);
                        if (File.Exists(volName))
                        {
                            _volumeFileNames.Add(volName);
                        }
                        else
                        {
                            break;
                        }
                        index++;
                    } while (true);
                }
            }
        }

        /// <summary>
        /// 初始化 ArchiveOpenCallback 类的新实例。
        /// </summary>
        /// <param name="fileName">归档文件名。</param>
        public ArchiveOpenCallback(string fileName)
        {
            Init(fileName);
        }

        /// <summary>
        /// 初始化 ArchiveOpenCallback 类的新实例。
        /// </summary>
        /// <param name="fileName">归档文件名。</param>
        /// <param name="password">归档文件的密码。</param>
        public ArchiveOpenCallback(string fileName, string password) : base(password)
        {
            Init(fileName);
        }

        #region IArchiveOpenCallback Members

        public void SetTotal(IntPtr files, IntPtr bytes) {}

        public void SetCompleted(IntPtr files, IntPtr bytes) {}

        #endregion

        #region IArchiveOpenVolumeCallback Members

        public int GetProperty(ItemPropId propId, ref PropVariant value)
        {
            if (_fileInfo == null)
            {
                // We are likely opening an archive from a Stream, and no file or _fileInfo exists.
                return 0;
            }

            switch (propId)
            {
                case ItemPropId.Name:
                    value.VarType = VarEnum.VT_BSTR;
                    value.Value = Marshal.StringToBSTR(_fileInfo.FullName);
                    break;
                case ItemPropId.IsDirectory:
                    value.VarType = VarEnum.VT_BOOL;
                    
                    value.BoolVal = (short) (_fileInfo.Attributes & FileAttributes.Directory);
                    break;
                case ItemPropId.Size:
                    value.VarType = VarEnum.VT_UI8;
                    value.UInt64Value = (ulong) _fileInfo.Length;
                    break;
                case ItemPropId.Attributes:
                    value.VarType = VarEnum.VT_UI4;
                    value.UInt32Value = (uint) _fileInfo.Attributes;
                    break;
                case ItemPropId.CreationTime:
                    value.VarType = VarEnum.VT_FILETIME;
                    value.Int64Value = _fileInfo.CreationTime.ToFileTime();
                    break;
                case ItemPropId.LastAccessTime:
                    value.VarType = VarEnum.VT_FILETIME;
                    value.Int64Value = _fileInfo.LastAccessTime.ToFileTime();
                    break;
                case ItemPropId.LastWriteTime:
                    value.VarType = VarEnum.VT_FILETIME;
                    value.Int64Value = _fileInfo.LastWriteTime.ToFileTime();
                    break;
            }

            return 0;
        }

        public int GetStream(string name, out IInStream inStream)
        {
            if (!File.Exists(name))
            {
                name = Path.Combine(Path.GetDirectoryName(_fileInfo.FullName), name);
                if (!File.Exists(name))
                {
                    inStream = null;
                    AddException(new FileNotFoundException("The volume \"" + name + "\" was not found. Extraction can be impossible."));
                    return 1;
                }
            }
            _volumeFileNames.Add(name);
            if (_wrappers.TryGetValue(name, out var existing))
            {
                existing.Seek(0, SeekOrigin.Begin, IntPtr.Zero);
                inStream = existing;
            }
            else
            {
                try
                {
                    var wrapper = new InStreamWrapper(
                        new FileStream(name, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, bufferSize: 65536, FileOptions.SequentialScan), true);
                    _wrappers.Add(name, wrapper);
                    inStream = wrapper;                    
                }
                catch (Exception)
                {
                    AddException(new FileNotFoundException("Failed to open the volume \"" + name + "\". Extraction is impossible."));
                    inStream = null;
                    return 1;
                }
            }
            return 0;
        }

        #endregion

        #region ICryptoGetTextPassword Members

        /// <summary>
        /// 设置归档文件的密码
        /// </summary>
        /// <param name="password">归档文件的密码</param>
        /// <returns>如果一切正常则返回零</returns>
        public int CryptoGetTextPassword(out string password)
        {
            password = Password;
            return 0;
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            if (_wrappers != null)
            {
                foreach (InStreamWrapper wrap in _wrappers.Values)
                {
                    wrap.Dispose();
                }
                _wrappers = null;
            }

            GC.SuppressFinalize(this);
        }

        #endregion        
    }
#endif
}
