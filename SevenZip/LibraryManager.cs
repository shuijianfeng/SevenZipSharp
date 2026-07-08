namespace SevenZip
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Diagnostics;

    using System.IO;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Runtime.CompilerServices;
    using System.Threading;
    
    using System.Runtime.InteropServices.Marshalling;
#if UNMANAGED
    /// <summary>
    /// 7-zip 库的低级封装。
    /// </summary>
    internal static class SevenZipLibraryManager
    {
        /// <summary>
        /// 所有锁操作的同步根。
        /// </summary>
        private static readonly Lock SyncRoot = new();

        /// <summary>
        /// 7-zip dll 的路径。
        /// </summary>
        /// <remarks>7zxa.dll 仅支持从 .7z 归档解压。
        /// 7za.dll 的特性：
        ///     - 支持 7z 格式；
        ///     - 内置编码器：LZMA、PPMD、BCJ、BCJ2、COPY、AES-256 加密。
        ///     - 内置解码器：LZMA、PPMD、BCJ、BCJ2、COPY、AES-256 加密、BZip2、Deflate。
        /// 7z.dll（来自 7-zip 发行版）支持所有 InArchiveFormat 的编码和解码。
        /// </remarks>
        private static string _libraryFileName;

        private static string DetermineLibraryFilePath()
        {
            // AppContext.BaseDirectory 对 AOT 友好，且避免了反射开销。
            return Path.Combine(AppContext.BaseDirectory, Environment.Is64BitProcess ? "7z64.dll" : "7z.dll");
        }

        /// <summary>
        /// 7-zip 库句柄。
        /// </summary>
        private static IntPtr _modulePtr;

        /// <summary>
        /// 7-zip 库特性。
        /// </summary>
        private static LibraryFeature? _features;

        private static Dictionary<object, Dictionary<InArchiveFormat, IInArchive>> _inArchives;
        private static Dictionary<object, Dictionary<OutArchiveFormat, IOutArchive>> _outArchives;
        private static int _totalUsers;
        private static bool? _modifyCapable;

        private static void InitUserInFormat(object user, InArchiveFormat format)
        {
            if (!_inArchives.TryGetValue(user, out var formats))
            {
                formats = new Dictionary<InArchiveFormat, IInArchive>();
                _inArchives.Add(user, formats);
            }

            if (formats.TryAdd(format, null))
            {
                _totalUsers++;
            }
        }

        private static void InitUserOutFormat(object user, OutArchiveFormat format)
        {
            if (!_outArchives.TryGetValue(user, out var formats))
            {
                formats = new Dictionary<OutArchiveFormat, IOutArchive>();
                _outArchives.Add(user, formats);
            }

            if (formats.TryAdd(format, null))
            {
                _totalUsers++;
            }
        }

        private static void Init()
        {
            _inArchives = new Dictionary<object, Dictionary<InArchiveFormat, IInArchive>>();
            _outArchives = new Dictionary<object, Dictionary<OutArchiveFormat, IOutArchive>>();
        }

        /// <summary>
        /// 如有必要则加载 7-zip 库，并将用户添加到引用列表中
        /// </summary>
        /// <param name="user">函数的调用者</param>
        /// <param name="format">归档格式</param>
        public static void LoadLibrary(object user, Enum format)
        {
            if (!NativeMethods.isinit)
            {
                NativeMethods.isinit=true;
            }
            lock (SyncRoot)
            {
                if (_inArchives == null || _outArchives == null)
                {
                    Init();
                }

                //if (_modulePtr == IntPtr.Zero)
                //{
                //    if (_libraryFileName == null)
                //    {
                //        _libraryFileName = DetermineLibraryFilePath();
                //    }

                //    if (!File.Exists(_libraryFileName))
                //    {
                //        throw new SevenZipLibraryException("DLL file does not exist.");
                //    }

                //    if ((_modulePtr = NativeMethods.LoadLibrary(_libraryFileName)) == IntPtr.Zero)
                //    {
                //        throw new SevenZipLibraryException($"failed to load library from \"{_libraryFileName}\".");
                //    }

                //    if (NativeMethods.GetProcAddress(_modulePtr, "GetHandlerProperty") == IntPtr.Zero)
                //    {
                //        NativeMethods.FreeLibrary(_modulePtr);
                //        throw new SevenZipLibraryException("library is invalid.");
                //    }
                //}

                if (format is InArchiveFormat archiveFormat)
                {
                    InitUserInFormat(user, archiveFormat);
                    return;
                }

                if (format is OutArchiveFormat outArchiveFormat)
                {
                    InitUserOutFormat(user, outArchiveFormat);
                    return;
                }

                throw new ArgumentException($"Enum {format} is not a valid archive format attribute!");
            }
        }

        /// <summary>
        /// 获取指示库是否支持修改归档的值。
        /// </summary>
        public static bool ModifyCapable
        {
            get
            {
                lock (SyncRoot)
                {
                    if (!_modifyCapable.HasValue)
                    {
                        if (_libraryFileName == null)
                        {
                            _libraryFileName = DetermineLibraryFilePath();
                        }

                        var dllVersionInfo = FileVersionInfo.GetVersionInfo(_libraryFileName);
                        _modifyCapable = dllVersionInfo.FileMajorPart >= 9;
                    }

                    return _modifyCapable.Value;
                }
            }
        }

        static readonly string Namespace = typeof(SevenZipLibraryManager).Assembly.GetName().Name;

        private static string GetResourceString(string str)
        {
            return Namespace + ".arch." + str;
        }

        private static readonly Assembly s_assembly = typeof(SevenZipLibraryManager).Assembly;

        private static bool ExtractionBenchmark(string archiveFileName, Stream outStream, ref LibraryFeature? features, LibraryFeature testedFeature)
        {
            var stream = s_assembly.GetManifestResourceStream(GetResourceString(archiveFileName));
            
            try
            {
                using (var extractor = new SevenZipExtractor(stream))
                {
                    extractor.ExtractFile(0, outStream);
                }
            }
            catch (Exception)
            {
                return false;
            }

            features |= testedFeature;
            return true;
        }

        private static bool CompressionBenchmark(Stream inStream, Stream outStream, OutArchiveFormat format, CompressionMethod method, ref LibraryFeature? features, LibraryFeature testedFeature)
        {
            try
            {
                var compressor = new SevenZipCompressor { ArchiveFormat = format, CompressionMethod = method };
                compressor.CompressStream(inStream, outStream);
            }
            catch (Exception)
            {
                return false;
            }

            features |= testedFeature;
            return true;
        }

        public static LibraryFeature CurrentLibraryFeatures
        {
            get
            {
                lock (SyncRoot)
                {
                    if (_features.HasValue)
                    {
                        return _features.Value;
                    }

                    _features = LibraryFeature.None;

                    #region Benchmark

                    #region Extraction features

                    using (var outStream = new MemoryStream())
                    {
                        ExtractionBenchmark("Test.lzma.7z", outStream, ref _features, LibraryFeature.Extract7z);
                        ExtractionBenchmark("Test.lzma2.7z", outStream, ref _features, LibraryFeature.Extract7zLZMA2);

                        var i = 0;

                        if (ExtractionBenchmark("Test.bzip2.7z", outStream, ref _features, _features.Value))
                        {
                            i++;
                        }

                        if (ExtractionBenchmark("Test.ppmd.7z", outStream, ref _features, _features.Value))
                        {
                            i++;
                            if (i == 2 && (_features & LibraryFeature.Extract7z) != 0 &&
                                (_features & LibraryFeature.Extract7zLZMA2) != 0)
                            {
                                _features |= LibraryFeature.Extract7zAll;
                            }
                        }

                        ExtractionBenchmark("Test.rar", outStream, ref _features, LibraryFeature.ExtractRar);
                        ExtractionBenchmark("Test.tar", outStream, ref _features, LibraryFeature.ExtractTar);
                        ExtractionBenchmark("Test.txt.bz2", outStream, ref _features, LibraryFeature.ExtractBzip2);
                        ExtractionBenchmark("Test.txt.gz", outStream, ref _features, LibraryFeature.ExtractGzip);
                        ExtractionBenchmark("Test.txt.xz", outStream, ref _features, LibraryFeature.ExtractXz);
                        ExtractionBenchmark("Test.zip", outStream, ref _features, LibraryFeature.ExtractZip);
                    }

                    #endregion

                    #region Compression features

                    using (var inStream = new MemoryStream())
                    {
                        inStream.Write(Encoding.UTF8.GetBytes("Test"), 0, 4);

                        using (var outStream = new MemoryStream())
                        {
                            CompressionBenchmark(inStream, outStream,
                                OutArchiveFormat.SevenZip, CompressionMethod.Lzma,
                                ref _features, LibraryFeature.Compress7z);
                            CompressionBenchmark(inStream, outStream,
                                OutArchiveFormat.SevenZip, CompressionMethod.Lzma2,
                                ref _features, LibraryFeature.Compress7zLZMA2);

                            var i = 0;

                            if (_features != null && CompressionBenchmark(inStream, outStream,
                                    OutArchiveFormat.SevenZip, CompressionMethod.BZip2,
                                    ref _features, _features.Value))
                            {
                                i++;
                            }

                            if (_features != null && CompressionBenchmark(inStream, outStream,
                                    OutArchiveFormat.SevenZip, CompressionMethod.Ppmd,
                                    ref _features, _features.Value))
                            {
                                i++;
                                if (i == 2 && (_features & LibraryFeature.Compress7z) != 0 &&
                                (_features & LibraryFeature.Compress7zLZMA2) != 0)
                                {
                                    _features |= LibraryFeature.Compress7zAll;
                                }
                            }

                            CompressionBenchmark(inStream, outStream,
                                OutArchiveFormat.Zip, CompressionMethod.Default,
                                ref _features, LibraryFeature.CompressZip);
                            CompressionBenchmark(inStream, outStream,
                                OutArchiveFormat.BZip2, CompressionMethod.Default,
                                ref _features, LibraryFeature.CompressBzip2);
                            CompressionBenchmark(inStream, outStream,
                                OutArchiveFormat.GZip, CompressionMethod.Default,
                                ref _features, LibraryFeature.CompressGzip);
                            CompressionBenchmark(inStream, outStream,
                                OutArchiveFormat.Tar, CompressionMethod.Default,
                                ref _features, LibraryFeature.CompressTar);
                            CompressionBenchmark(inStream, outStream,
                                OutArchiveFormat.XZ, CompressionMethod.Default,
                                ref _features, LibraryFeature.CompressXz);
                        }
                    }

                    #endregion

                    #endregion

                    if (_features != null && ModifyCapable && (_features.Value & LibraryFeature.Compress7z) != 0)
                    {
                        _features |= LibraryFeature.Modify;
                    }

                    return _features.Value;
                }
            }
        }

        /// <summary>
        /// 从引用列表中移除用户，并在列表为空时释放 7-zip 库
        /// </summary>
        /// <param name="user">函数的调用者</param>
        /// <param name="format">归档格式</param>
        //public static void FreeLibrary(object user, Enum format)
        //{

        //    lock (SyncRoot)
        //    {
        //        if (_modulePtr != IntPtr.Zero)
        //        {
        //            if (format is InArchiveFormat archiveFormat)
        //            {
        //                if (_inArchives != null && _inArchives.ContainsKey(user) &&
        //                    _inArchives[user].ContainsKey(archiveFormat) &&
        //                    _inArchives[user][archiveFormat] != null)
        //                {
        //                    try
        //                    {
        //                        //Marshal.ReleaseComObject(_inArchives[user][archiveFormat]);
        //                    }
        //                    catch (InvalidComObjectException) { }

        //                    _inArchives[user].Remove(archiveFormat);
        //                    _totalUsers--;

        //                    if (_inArchives[user].Count == 0)
        //                    {
        //                        _inArchives.Remove(user);
        //                    }
        //                }
        //            }

        //            if (format is OutArchiveFormat outArchiveFormat)
        //            {
        //                if (_outArchives != null && _outArchives.ContainsKey(user) &&
        //                    _outArchives[user].ContainsKey(outArchiveFormat) &&
        //                    _outArchives[user][outArchiveFormat] != null)
        //                {
        //                    try
        //                    {
        //                        //Marshal.ReleaseComObject(_outArchives[user][outArchiveFormat]);
        //                    }
        //                    catch (InvalidComObjectException) { }

        //                    _outArchives[user].Remove(outArchiveFormat);
        //                    _totalUsers--;

        //                    if (_outArchives[user].Count == 0)
        //                    {
        //                        _outArchives.Remove(user);
        //                    }
        //                }
        //            }

        //            if ((_inArchives == null || _inArchives.Count == 0) && (_outArchives == null || _outArchives.Count == 0))
        //            {
        //                _inArchives = null;
        //                _outArchives = null;

        //                if (_totalUsers == 0)
        //                {
        //                    //NativeMethods.FreeLibrary(_modulePtr);
        //                    //_modulePtr = IntPtr.Zero;
        //                }
        //            }
        //        }
        //    }
        //}

        /// <summary>
        /// 获取用于解压 7-zip 归档的 IInArchive 接口。
        /// </summary>
        /// <param name="format">归档格式。</param>
        /// <param name="user">归档格式的使用者。</param>
        public static IInArchive InArchive(InArchiveFormat format, object user)
        {
            lock (SyncRoot)
            {
                if (!_inArchives.TryGetValue(user, out var userFormats) || userFormats[format] == null)
                {


                    //if (_modulePtr == IntPtr.Zero)
                    //{
                    //    LoadLibrary(user, format);

                    //    if (_modulePtr == IntPtr.Zero)
                    //    {
                    //        throw new SevenZipLibraryException();
                    //    }
                    //}

                    //var createObject = (NativeMethods.CreateObjectDelegate)
                    //    Marshal.GetDelegateForFunctionPointer(
                    //        NativeMethods.GetProcAddress(_modulePtr, "CreateObject"),
                    //        typeof(NativeMethods.CreateObjectDelegate));

                    //if (createObject == null)
                    //{
                    //    throw new SevenZipLibraryException();
                    //}

                    //object result;
                    var interfaceId = typeof(IInArchive).GUID;
                    var classId = Formats.InFormatGuids[format];

                    try
                    {
                        CreateObjectDelegateIInArchive(ref classId, ref interfaceId, out var result);
                        InitUserInFormat(user, format);
                        _inArchives[user][format] = result as IInArchive;
                    }
                    catch (Exception)
                    {
                        throw new SevenZipLibraryException("Your 7-zip library does not support this archive type.");
                    }

                   
                }

                return _inArchives[user][format];
            }
        }
        private static unsafe void CreateObjectDelegateIInArchive(ref Guid classID, ref Guid interfaceID,
                                                        out IInArchive? outObject)
        {
            bool invokeSucceeded = default;
            Unsafe.SkipInit(out outObject);
            void* outObjectNative = default;
            try
            {
                fixed (Guid* interfaceIDNative = &interfaceID)
                {
                    fixed (Guid* classIDNative = &classID)
                    {
                        int result = NativeMethods.CreateObjectDelegate(classIDNative, interfaceIDNative, &outObjectNative);
                        if (result != 0)
                        {
                            Marshal.ThrowExceptionForHR(result);
                        }
                    }
                }

                invokeSucceeded = true;
                outObject = ComInterfaceMarshaller<IInArchive>.ConvertToManaged(outObjectNative);
            }
            finally
            {
                if (invokeSucceeded)
                {
                    ComInterfaceMarshaller<IInArchive>.Free(outObjectNative);
                }
            }
        }
        private static unsafe void CreateObjectDelegateIOutArchive(ref Guid classID, ref Guid interfaceID,
                                                        out IOutArchive? outObject)
        {
            bool invokeSucceeded = default;
            Unsafe.SkipInit(out outObject);
            void* outObjectNative = default;
            try
            {
                fixed (Guid* interfaceIDNative = &interfaceID)
                {
                    fixed (Guid* classIDNative = &classID)
                    {
                        int result = NativeMethods.CreateObjectDelegate(classIDNative, interfaceIDNative, &outObjectNative);
                        if (result != 0)
                        {
                            Marshal.ThrowExceptionForHR(result);
                        }
                    }
                }

                invokeSucceeded = true;
                outObject = ComInterfaceMarshaller<IOutArchive>.ConvertToManaged(outObjectNative);
            }
            finally
            {
                if (invokeSucceeded)
                {
                    ComInterfaceMarshaller<IOutArchive>.Free(outObjectNative);
                }
            }
        }
        /// <summary>
        /// 获取用于打包 7-zip 归档的 IOutArchive 接口。
        /// </summary>
        /// <param name="format">归档格式。</param>  
        /// <param name="user">归档格式的使用者。</param>
        public static IOutArchive OutArchive(OutArchiveFormat format, object user)
        {
            lock (SyncRoot)
            {
                if (_outArchives[user][format] == null)
                {

                    //if (_modulePtr == IntPtr.Zero)
                    //{
                    //    throw new SevenZipLibraryException();
                    //}

                    //var createObject = (NativeMethods.CreateObjectDelegate)
                    //    Marshal.GetDelegateForFunctionPointer(
                    //        NativeMethods.GetProcAddress(_modulePtr, "CreateObject"),
                    //        typeof(NativeMethods.CreateObjectDelegate));
                    var interfaceId = typeof(IOutArchive).GUID;
                    

                    try
                    {
                        var classId = Formats.OutFormatGuids[format];
                        CreateObjectDelegateIOutArchive(ref classId, ref interfaceId, out var result);
                        
                        InitUserOutFormat(user, format);
                        _outArchives[user][format] = result as IOutArchive;
                    }
                    catch (Exception)
                    {
                        throw new SevenZipLibraryException("Your 7-zip library does not support this archive type.");
                    }
                }

                return _outArchives[user][format];
            }
        }

        public static void SetLibraryPath(string libraryPath)
        {
            if (_modulePtr != IntPtr.Zero && !Path.GetFullPath(libraryPath).Equals(Path.GetFullPath(_libraryFileName), StringComparison.OrdinalIgnoreCase))
            {
                throw new SevenZipLibraryException($"can not change the library path while the library \"{_libraryFileName}\" is being used.");
            }
            
            if (!File.Exists(libraryPath))
            {
                throw new SevenZipLibraryException($"can not change the library path because the file \"{libraryPath}\" does not exist.");
            }

            _libraryFileName = libraryPath;
            _features = null;
        }
    }
#endif
}
