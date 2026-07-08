namespace SevenZip
{
    using System.IO;
    using System.Threading.Tasks;

    partial class SevenZipExtractor
    {
        #region Asynchronous core methods

        /// <summary>
        /// 重新创建 SevenZipExtractor 类的实例。
        /// 用于异步方法。
        /// </summary>
        private void RecreateInstanceIfNeeded()
        {
            if (NeedsToBeRecreated)
            {
                NeedsToBeRecreated = false;
                Stream backupStream = null;
                string backupFileName = null;

                if (string.IsNullOrEmpty(_fileName))
                {
                    backupStream = _inStream;
                }
                else
                {
                    backupFileName = _fileName;
                }

                CommonDispose();

                if (backupStream == null)
                {
                    Init(backupFileName);
                }
                else
                {
                    Init(backupStream);
                }
            }
        }

        internal override void SaveContext()
        {
            DisposedCheck();
            _asynchronousDisposeLock = true;
            base.SaveContext();
        }

        internal override void ReleaseContext()
        {
            base.ReleaseContext();
            _asynchronousDisposeLock = false;
        }

        #endregion

        #region Delegates

        /// <summary>
        /// 用于 BeginExtractArchive 的委托。
        /// </summary>
        /// <param name="directory">文件解包到的目录。</param>
        private delegate void ExtractArchiveDelegate(string directory);

        /// <summary>
        /// 用于 BeginExtractFile（按文件名）的委托。
        /// </summary>
        /// <param name="fileName">归档文件表中的文件全名。</param>
        /// <param name="stream">文件解包到的流。</param>
        private delegate void ExtractFileByFileNameDelegate(string fileName, Stream stream);

        /// <summary>
        /// 用于 BeginExtractFile（按索引）的委托。
        /// </summary>
        /// <param name="index">归档文件表中的索引。</param>
        /// <param name="stream">文件解包到的流。</param>
        private delegate void ExtractFileByIndexDelegate(int index, Stream stream);

        /// <summary>
        /// 用于 BeginExtractFiles(string directory, params int[] indexes) 的委托。
        /// </summary>
        /// <param name="indexes">归档文件表中的文件索引。</param>
        /// <param name="directory">文件解包到的目录。</param>
        private delegate void ExtractFiles1Delegate(string directory, int[] indexes);

        /// <summary>
        /// 用于 BeginExtractFiles(string directory, params string[] fileNames) 的委托。
        /// </summary>
        /// <param name="fileNames">归档文件表中的文件全名。</param>
        /// <param name="directory">文件解包到的目录。</param>
        private delegate void ExtractFiles2Delegate(string directory, string[] fileNames);

        /// <summary>
        /// 用于 BeginExtractFiles(ExtractFileCallback extractFileCallback) 的委托。
        /// </summary>
        /// <param name="extractFileCallback">对归档中每个文件调用的回调。</param>
        private delegate void ExtractFiles3Delegate(ExtractFileCallback extractFileCallback);
        #endregion

        /// <summary>
        /// 异步将整个归档解包到指定的目录名。
        /// </summary>
        /// <param name="directory">文件解包到的目录。</param>
        public void BeginExtractArchive(string directory)
        {
            SaveContext();
            Task.Run(() => new ExtractArchiveDelegate(ExtractArchive).Invoke(directory))
                .ContinueWith(_ => ReleaseContext());
        }

        /// <summary>
        /// 异步将整个归档解包到指定的目录名。
        /// </summary>
        /// <param name="directory">文件解包到的目录。</param>
        public async Task ExtractArchiveAsync(string directory)
        {
            try
            {
                SaveContext();
                await Task.Run(() => new ExtractArchiveDelegate(ExtractArchive).Invoke(directory));
            }
            finally
            {
                ReleaseContext();
            }
        }

        /// <summary>
        /// 按文件名异步将文件解包到指定的流。
        /// </summary>
        /// <param name="fileName">归档文件表中的文件全名。</param>
        /// <param name="stream">文件解包到的流。</param>
        public void BeginExtractFile(string fileName, Stream stream)
        {
            SaveContext();
            Task.Run(() => new ExtractFileByFileNameDelegate(ExtractFile).Invoke(fileName, stream))
                .ContinueWith(_ => ReleaseContext());
        }

        /// <summary>
        /// 按文件名异步将文件解包到指定的流。
        /// </summary>
        /// <param name="fileName">归档文件表中的文件全名。</param>
        /// <param name="stream">文件解包到的流。</param>
        public async Task ExtractFileAsync(string fileName, Stream stream)
        {
            try
            {
                SaveContext();
                await Task.Run(() => new ExtractFileByFileNameDelegate(ExtractFile).Invoke(fileName, stream));
            }
            finally
            {
                ReleaseContext();
            }
        }

        /// <summary>
        /// 按索引异步将文件解包到指定的流。
        /// </summary>
        /// <param name="index">归档文件表中的索引。</param>
        /// <param name="stream">文件解包到的流。</param>
        public void BeginExtractFile(int index, Stream stream)
        {
            SaveContext();
            Task.Run(() => new ExtractFileByIndexDelegate(ExtractFile).Invoke(index, stream))
                .ContinueWith(_ => ReleaseContext());
        }

        /// <summary>
        /// 按索引异步将文件解包到指定的流。
        /// </summary>
        /// <param name="index">归档文件表中的索引。</param>
        /// <param name="stream">文件解包到的流。</param>
        public async Task ExtractFileAsync(int index, Stream stream)
        {
            try
            {
                SaveContext();
                await Task.Run(() => new ExtractFileByIndexDelegate(ExtractFile).Invoke(index, stream));
            }
            finally
            {
                ReleaseContext();
            }
        }

        /// <summary>
        /// 按索引异步将文件解包到指定的目录。
        /// </summary>
        /// <param name="indexes">归档文件表中的文件索引。</param>
        /// <param name="directory">文件解包到的目录。</param>
        public void BeginExtractFiles(string directory, params int[] indexes)
        {
            SaveContext();
            Task.Run(() => new ExtractFiles1Delegate(ExtractFiles).Invoke(directory, indexes))
                .ContinueWith(_ => ReleaseContext());
        }

        /// <summary>
        /// 按索引异步将文件解包到指定的目录。
        /// </summary>
        /// <param name="indexes">归档文件表中的文件索引。</param>
        /// <param name="directory">文件解包到的目录。</param>
        public async Task ExtractFilesAsync(string directory, params int[] indexes)
        {
            try
            {
                SaveContext();
                await Task.Run(() => new ExtractFiles1Delegate(ExtractFiles).Invoke(directory, indexes));
            }
            finally
            {
                ReleaseContext();
            }
        }

        /// <summary>
        /// 按文件全名异步将文件解包到指定的目录。
        /// </summary>
        /// <param name="fileNames">归档文件表中的文件全名。</param>
        /// <param name="directory">文件解包到的目录。</param>
        public void BeginExtractFiles(string directory, params string[] fileNames)
        {
            SaveContext();
            Task.Run(() => new ExtractFiles2Delegate(ExtractFiles).Invoke(directory, fileNames))
                .ContinueWith(_ => ReleaseContext());
        }

        /// <summary>
        /// 按文件全名异步将文件解包到指定的目录。
        /// </summary>
        /// <param name="fileNames">归档文件表中的文件全名。</param>
        /// <param name="directory">文件解包到的目录。</param>
        public async Task ExtractFilesAsync(string directory, params string[] fileNames)
        {
            try
            {
                SaveContext();
                await Task.Run(() => new ExtractFiles2Delegate(ExtractFiles).Invoke(directory, fileNames));
            }
            finally
            {
                ReleaseContext();
            }
        }

        /// <summary>
        /// 异步从归档中提取文件，通过回调决定对每个文件的处理方式。
        /// 文件的顺序由归档决定。
        /// 不支持 7-Zip（及其他固实）归档。
        /// </summary>
        /// <param name="extractFileCallback">对归档中每个文件调用的回调。</param>
        public void BeginExtractFiles(ExtractFileCallback extractFileCallback)
        {
            SaveContext();
            Task.Run(() => new ExtractFiles3Delegate(ExtractFiles).Invoke(extractFileCallback))
                .ContinueWith(_ => ReleaseContext());
        }

        /// <summary>
        /// 异步从归档中提取文件，通过回调决定对每个文件的处理方式。
        /// 文件的顺序由归档决定。
        /// 不支持 7-Zip（及其他固实）归档。
        /// </summary>
        /// <param name="extractFileCallback">对归档中每个文件调用的回调。</param>
        public async Task ExtractFilesAsync(ExtractFileCallback extractFileCallback)
        {
            try
            {
                SaveContext();
                await Task.Run(() => new ExtractFiles3Delegate(ExtractFiles).Invoke(extractFileCallback));
            }
            finally
            {
                ReleaseContext();
            }
        }
    }
}
