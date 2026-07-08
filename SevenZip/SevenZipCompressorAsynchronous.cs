namespace SevenZip
{
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;

    partial class SevenZipCompressor
    {
        #region Delegates

        private delegate void CompressFiles1Delegate(string archiveName, string[] fileFullNames);
        private delegate void CompressFiles2Delegate(Stream archiveStream, string[] fileFullNames);
        private delegate void CompressFiles3Delegate(string archiveName, int commonRootLength, string[] fileFullNames);
        private delegate void CompressFiles4Delegate(Stream archiveStream, int commonRootLength, string[] fileFullNames);

        private delegate void CompressFilesEncrypted1Delegate(string archiveName, string password, string[] fileFullNames);
        private delegate void CompressFilesEncrypted2Delegate(Stream archiveStream, string password, string[] fileFullNames);
        private delegate void CompressFilesEncrypted3Delegate(string archiveName, int commonRootLength, string password, string[] fileFullNames);
        private delegate void CompressFilesEncrypted4Delegate(Stream archiveStream, int commonRootLength, string password, string[] fileFullNames);

        private delegate void CompressDirectoryDelegate(string directory, string archiveName, string password, string searchPattern, bool recursion);
        private delegate void CompressDirectory2Delegate(string directory, Stream archiveStream, string password, string searchPattern, bool recursion);

        private delegate void CompressStreamDelegate(Stream inStream, Stream outStream, string password);

        private delegate void ModifyArchiveDelegate(string archiveName, IDictionary<int, string> newFileNames, string password);

        #endregion

        #region BeginCompressFiles overloads
        
        /// <summary>
        /// 异步地将文件打包到压缩包中。
        /// </summary>
        /// <param name="fileFullNames">要打包的文件名数组。</param>
        /// <param name="archiveName">压缩包文件名。</param>
        public void BeginCompressFiles(string archiveName, params string[] fileFullNames)
        {
            SaveContext();
            Task.Run(() => new CompressFiles1Delegate(CompressFiles).Invoke(archiveName, fileFullNames))
                .ContinueWith(_ => ReleaseContext());
        }

        /// <summary>
        /// 异步地将文件打包到压缩包中。
        /// </summary>
        /// <param name="fileFullNames">要打包的文件名数组。</param>
        /// <param name="archiveStream">压缩包输出流。
        /// 如需归档到磁盘，请使用 CompressFiles(string archiveName ... ) 重载。</param>
        public void BeginCompressFiles(Stream archiveStream, params string[] fileFullNames)
        {
            SaveContext();
            Task.Run(() => new CompressFiles2Delegate(CompressFiles).Invoke(archiveStream, fileFullNames))
                .ContinueWith(_ => ReleaseContext());
        }

        /// <summary>
        /// 异步地将文件打包到压缩包中。
        /// </summary>
        /// <param name="fileFullNames">要打包的文件名数组。</param>
        /// <param name="commonRootLength">文件名公共根的长度。</param>
        /// <param name="archiveName">压缩包文件名。</param>
        public void BeginCompressFiles(string archiveName, int commonRootLength, params string[] fileFullNames)
        {
            SaveContext();
            Task.Run(() => new CompressFiles3Delegate(CompressFiles).Invoke(archiveName, commonRootLength, fileFullNames))
                .ContinueWith(_ => ReleaseContext());
        }

        /// <summary>
        /// 异步地将文件打包到压缩包中。
        /// </summary>
        /// <param name="fileFullNames">要打包的文件名数组。</param>
        /// <param name="commonRootLength">文件名公共根的长度。</param>
        /// <param name="archiveStream">压缩包输出流。
        /// 如需归档到磁盘，请使用 CompressFiles(string archiveName, ... ) 重载。</param>
        public void BeginCompressFiles(Stream archiveStream, int commonRootLength, params string[] fileFullNames)
        {
            SaveContext();
            Task.Run(() => new CompressFiles4Delegate(CompressFiles).Invoke(archiveStream, commonRootLength, fileFullNames))
                .ContinueWith(_ => ReleaseContext());
        }

        /// <summary>
        /// 异步地将文件打包到压缩包中。
        /// </summary>
        /// <param name="fileFullNames">要打包的文件名数组。</param>
        /// <param name="archiveName">压缩包文件名</param>
        /// <param name="password">压缩包密码。</param>
        public void BeginCompressFilesEncrypted(string archiveName, string password, params string[] fileFullNames  )
        {
            SaveContext();
            Task.Run(() => new CompressFilesEncrypted1Delegate(CompressFilesEncrypted).Invoke(archiveName, password, fileFullNames))
                .ContinueWith(_ => ReleaseContext());
        }

        /// <summary>
        /// 异步地将文件打包到压缩包中。
        /// </summary>
        /// <param name="fileFullNames">要打包的文件名数组。</param>
        /// <param name="archiveStream">压缩包输出流。
        /// 如需归档到磁盘，请使用 CompressFiles( ... string archiveName ... ) 重载。</param>
        /// <param name="password">压缩包密码。</param>
        public void BeginCompressFilesEncrypted(Stream archiveStream, string password, params string[] fileFullNames)
        {
            SaveContext();
            Task.Run(() => new CompressFilesEncrypted2Delegate(CompressFilesEncrypted).Invoke(archiveStream, password, fileFullNames))
                .ContinueWith(_ => ReleaseContext());
        }

        /// <summary>
        /// 异步地将文件打包到压缩包中。
        /// </summary>
        /// <param name="fileFullNames">要打包的文件名数组。</param>
        /// <param name="archiveName">压缩包文件名</param>
        /// <param name="password">压缩包密码。</param>
        /// <param name="commonRootLength">文件名公共根的长度。</param>
        public void BeginCompressFilesEncrypted(string archiveName, int commonRootLength, string password, params string[] fileFullNames)
        {
            SaveContext();
            Task.Run(() => new CompressFilesEncrypted3Delegate(CompressFilesEncrypted).Invoke(archiveName, commonRootLength, password, fileFullNames))
                .ContinueWith(_ => ReleaseContext());
        }

        /// <summary>
        /// 异步地将文件打包到压缩包中。
        /// </summary>
        /// <param name="fileFullNames">要打包的文件名数组。</param>
        /// <param name="archiveStream">压缩包输出流。
        /// 如需归档到磁盘，请使用 CompressFiles( ... string archiveName ... ) 重载。</param>
        /// <param name="password">压缩包密码。</param>
        /// <param name="commonRootLength">文件名公共根的长度。</param>
        public void BeginCompressFilesEncrypted(Stream archiveStream, int commonRootLength, string password, params string[] fileFullNames)
        {
            SaveContext();
            Task.Run(() => new CompressFilesEncrypted4Delegate(CompressFilesEncrypted).Invoke(archiveStream, commonRootLength, password, fileFullNames))
                .ContinueWith(_ => ReleaseContext());
        }

        #endregion

        #region CompressFilesAsync overloads

        /// <summary>
        /// 异步地将文件打包到压缩包中。
        /// </summary>
        /// <param name="fileFullNames">要打包的文件名数组。</param>
        /// <param name="archiveName">压缩包文件名。</param>
        public async Task CompressFilesAsync(string archiveName, params string[] fileFullNames)
        {
            try
            {
                SaveContext();
                await Task.Run(() => new CompressFiles1Delegate(CompressFiles).Invoke(archiveName, fileFullNames));
            }
            finally
            {
                ReleaseContext();
            }
        }

        /// <summary>
        /// 异步地将文件打包到压缩包中。
        /// </summary>
        /// <param name="fileFullNames">要打包的文件名数组。</param>
        /// <param name="archiveStream">压缩包输出流。
        /// 如需归档到磁盘，请使用 CompressFiles(string archiveName ... ) 重载。</param>
        public async Task CompressFilesAsync(Stream archiveStream, params string[] fileFullNames)
        {
            try
            {
                SaveContext();
                await Task.Run(() => new CompressFiles2Delegate(CompressFiles).Invoke(archiveStream, fileFullNames));
            }
            finally
            {
                ReleaseContext();
            }
        }

        /// <summary>
        /// 异步地将文件打包到压缩包中。
        /// </summary>
        /// <param name="fileFullNames">要打包的文件名数组。</param>
        /// <param name="commonRootLength">文件名公共根的长度。</param>
        /// <param name="archiveName">压缩包文件名。</param>
        public async Task CompressFilesAsync(string archiveName, int commonRootLength, params string[] fileFullNames)
        {
            try
            {
                SaveContext();
                await Task.Run(() => new CompressFiles3Delegate(CompressFiles).Invoke(archiveName, commonRootLength, fileFullNames));
            }
            finally
            {
                ReleaseContext();
            }
        }

        /// <summary>
        /// 异步地将文件打包到压缩包中。
        /// </summary>
        /// <param name="fileFullNames">要打包的文件名数组。</param>
        /// <param name="commonRootLength">文件名公共根的长度。</param>
        /// <param name="archiveStream">压缩包输出流。
        /// 如需归档到磁盘，请使用 CompressFiles(string archiveName, ... ) 重载。</param>
        public async Task CompressFilesAsync(Stream archiveStream, int commonRootLength, params string[] fileFullNames)
        {
            try
            {
                SaveContext();
                await Task.Run(() => new CompressFiles4Delegate(CompressFiles).Invoke(archiveStream, commonRootLength, fileFullNames));
            }
            finally
            {
                ReleaseContext();
            }
        }

        /// <summary>
        /// 异步地将文件打包到压缩包中。
        /// </summary>
        /// <param name="fileFullNames">要打包的文件名数组。</param>
        /// <param name="archiveName">压缩包文件名</param>
        /// <param name="password">压缩包密码。</param>
        public async Task CompressFilesEncryptedAsync(string archiveName, string password, params string[] fileFullNames)
        {
            try
            {
                SaveContext();
                await Task.Run(() => new CompressFilesEncrypted1Delegate(CompressFilesEncrypted).Invoke(archiveName, password, fileFullNames));
            }
            finally
            {
                ReleaseContext();
            }
        }

        /// <summary>
        /// 异步地将文件打包到压缩包中。
        /// </summary>
        /// <param name="fileFullNames">要打包的文件名数组。</param>
        /// <param name="archiveStream">压缩包输出流。
        /// 如需归档到磁盘，请使用 CompressFiles( ... string archiveName ... ) 重载。</param>
        /// <param name="password">压缩包密码。</param>
        public async Task CompressFilesEncryptedAsync(Stream archiveStream, string password, params string[] fileFullNames)
        {
            try
            {
                SaveContext();
                await Task.Run(() => new CompressFilesEncrypted2Delegate(CompressFilesEncrypted).Invoke(archiveStream, password, fileFullNames));
            }
            finally
            {
                ReleaseContext();
            }
        }

        /// <summary>
        /// 异步地将文件打包到压缩包中。
        /// </summary>
        /// <param name="fileFullNames">要打包的文件名数组。</param>
        /// <param name="archiveName">压缩包文件名</param>
        /// <param name="password">压缩包密码。</param>
        /// <param name="commonRootLength">文件名公共根的长度。</param>
        public async Task CompressFilesEncryptedAsync(string archiveName, int commonRootLength, string password, params string[] fileFullNames)
        {
            try
            {
                SaveContext();
                await Task.Run(() => new CompressFilesEncrypted3Delegate(CompressFilesEncrypted).Invoke(archiveName, commonRootLength, password, fileFullNames));
            }
            finally
            {
                ReleaseContext();
            }
        }

        /// <summary>
        /// 异步地将文件打包到压缩包中。
        /// </summary>
        /// <param name="fileFullNames">要打包的文件名数组。</param>
        /// <param name="archiveStream">压缩包输出流。
        /// 如需归档到磁盘，请使用 CompressFiles( ... string archiveName ... ) 重载。</param>
        /// <param name="password">压缩包密码。</param>
        /// <param name="commonRootLength">文件名公共根的长度。</param>
        public async Task CompressFilesEncryptedAsync(Stream archiveStream, int commonRootLength, string password, params string[] fileFullNames)
        {
            try
            {
                SaveContext();
                await Task.Run(() => new CompressFilesEncrypted4Delegate(CompressFilesEncrypted).Invoke(archiveStream, commonRootLength, password, fileFullNames));
            }
            finally
            {
                ReleaseContext();
            }
        }

        #endregion

        #region BeginCompressDirectory overloads

        /// <summary>
        /// 异步地打包指定目录中的所有文件。
        /// </summary>
        /// <param name="directory">要压缩的目录。</param>
        /// <param name="archiveName">压缩包文件名。</param>        
        /// <param name="password">压缩包密码。</param>
        /// <param name="searchPattern">搜索字符串，例如 "*.txt"。</param>
        /// <param name="recursion">如果为 true，则递归搜索文件；否则不递归。</param>
        public void BeginCompressDirectory(string directory, string archiveName, string password = "", string searchPattern = "*", bool recursion = true)
        {
            SaveContext();
            Task.Run(() => new CompressDirectoryDelegate(CompressDirectory).Invoke(directory, archiveName, password, searchPattern, recursion))
                .ContinueWith(_ => ReleaseContext());
        }

        /// <summary>
        /// 异步地打包指定目录中的所有文件。
        /// </summary>
        /// <param name="directory">要压缩的目录。</param>
        /// <param name="archiveStream">压缩包输出流。
        /// 如需归档到磁盘，请使用 CompressDirectory( ... string archiveName ... ) 重载。</param>        
        /// <param name="password">压缩包密码。</param>
        /// <param name="searchPattern">搜索字符串，例如 "*.txt"。</param>
        /// <param name="recursion">如果为 true，则递归搜索文件；否则不递归。</param>
        public void BeginCompressDirectory(string directory, Stream archiveStream, string password , string searchPattern = "*", bool recursion = true)
        {
            SaveContext();
            Task.Run(() => new CompressDirectory2Delegate(CompressDirectory).Invoke(directory, archiveStream, password, searchPattern, recursion))
                .ContinueWith(_ => ReleaseContext());
        }

        #endregion

        #region CompressDirectoryAsync overloads

        /// <summary>
        /// 异步地打包指定目录中的所有文件。
        /// </summary>
        /// <param name="directory">要压缩的目录。</param>
        /// <param name="archiveName">压缩包文件名。</param>        
        /// <param name="password">压缩包密码。</param>
        /// <param name="searchPattern">搜索字符串，例如 "*.txt"。</param>
        /// <param name="recursion">如果为 true，则递归搜索文件；否则不递归。</param>
        public async Task CompressDirectoryAsync(string directory, string archiveName, string password = "", string searchPattern = "*", bool recursion = true)
        {
            try
            {
                SaveContext();
                await Task.Run(() => new CompressDirectoryDelegate(CompressDirectory).Invoke(directory, archiveName, password, searchPattern, recursion));
            }
            finally
            {
                ReleaseContext();
            }
        }

        /// <summary>
        /// 异步地打包指定目录中的所有文件。
        /// </summary>
        /// <param name="directory">要压缩的目录。</param>
        /// <param name="archiveStream">压缩包输出流。
        /// 如需归档到磁盘，请使用 CompressDirectory( ... string archiveName ... ) 重载。</param>        
        /// <param name="password">压缩包密码。</param>
        /// <param name="searchPattern">搜索字符串，例如 "*.txt"。</param>
        /// <param name="recursion">如果为 true，则递归搜索文件；否则不递归。</param>
        public async Task CompressDirectoryAsync(string directory, Stream archiveStream, string password, string searchPattern = "*", bool recursion = true)
        {
            try
            {
                SaveContext();
                await Task.Run(() => new CompressDirectory2Delegate(CompressDirectory).Invoke(directory, archiveStream, password, searchPattern, recursion));
            }
            finally
            {
                ReleaseContext();
            }
        }

        #endregion

        #region BeginCompressStream overloads

        /// <summary>
        /// 压缩指定的流。
        /// </summary>
        /// <param name="inStream">源未压缩流。</param>
        /// <param name="outStream">目标压缩流。</param>
        /// <param name="password">压缩包密码。</param>
        /// <exception cref="System.ArgumentException">ArgumentException：至少一个指定的流无效。</exception>
        public void BeginCompressStream(Stream inStream, Stream outStream, string password = "")
        {
            SaveContext();
            Task.Run(() => new CompressStreamDelegate(CompressStream).Invoke(inStream, outStream, password))
                .ContinueWith(_ => ReleaseContext());

        }
        #endregion

        #region CompressStreamAsync overloads

        /// <summary>
        /// 压缩指定的流。
        /// </summary>
        /// <param name="inStream">源未压缩流。</param>
        /// <param name="outStream">目标压缩流。</param>
        /// <param name="password">压缩包密码。</param>
        /// <exception cref="System.ArgumentException">ArgumentException：至少一个指定的流无效。</exception>
        public async Task CompressStreamAsync(Stream inStream, Stream outStream, string password = "")
        {
            try
            {
                SaveContext();
                await Task.Run(() => new CompressStreamDelegate(CompressStream).Invoke(inStream, outStream, password));
            }
            finally
            {
                ReleaseContext();
            }
        }

        #endregion

        #region BeginModifyArchive overloads

        /// <summary>
        /// 异步修改现有压缩包（重命名或删除文件）。
        /// </summary>
        /// <param name="archiveName">压缩包文件名。</param>
        /// <param name="newFileNames">新文件名。值为 null 表示删除对应的索引。</param>
        /// <param name="password">压缩包密码。</param>
        public void BeginModifyArchive(string archiveName, IDictionary<int, string> newFileNames, string password = "")
        {
            SaveContext();
            Task.Run(() => new ModifyArchiveDelegate(ModifyArchive).Invoke(archiveName, newFileNames, password))
                .ContinueWith(_ => ReleaseContext());
        }

        #endregion

        #region ModifyArchiveAsync overloads

        /// <summary>
        /// 异步修改现有压缩包（重命名或删除文件）。
        /// </summary>
        /// <param name="archiveName">压缩包文件名。</param>
        /// <param name="newFileNames">新文件名。值为 null 表示删除对应的索引。</param>
        /// <param name="password">压缩包密码。</param>
        public async Task ModifyArchiveAsync(string archiveName, IDictionary<int, string> newFileNames, string password = "")
        {
            try
            {
                SaveContext();
                await Task.Run(() => new ModifyArchiveDelegate(ModifyArchive).Invoke(archiveName, newFileNames, password));
            }
            finally
            {
                ReleaseContext();
            }
        }

        #endregion
    }
}
