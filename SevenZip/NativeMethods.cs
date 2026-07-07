using System.Runtime.CompilerServices;

namespace SevenZip
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Runtime.InteropServices.Marshalling;

#if UNMANAGED
    internal static partial class NativeMethods
    {
        [DllImport(SevenZipMainDllPath, EntryPoint = "CreateObject")]
        internal static unsafe extern int CreateObjectDelegate(Guid* classIDNative, Guid* interfaceIDNative, void** outObjectNative);

        private const string SevenZipMainDllName = "7z64.dll";

        // 确保你的项目中存在 shareds 文件夹，并且里面有 7z64.dll
        //private const string SevenZipMainDllPath = "shareds\\" + SevenZipMainDllName;
        private const string SevenZipMainDllPath =  SevenZipMainDllName;
        // 【核心修改】：在 ASP.NET Core 中获取真实的物理根目录
        private static readonly string AppRootPath = AppContext.BaseDirectory;

        public static bool isinit = false;

        static NativeMethods()
        {
            // Use custom Dll import resolver
            NativeLibrary.SetDllImportResolver(Assembly.GetExecutingAssembly(), DllImportResolver);
            isinit = true;
        }

        private static IntPtr DllImportResolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
        {
            // If the library to load is not "7z64.dll", then try load it as other library
            if (!libraryName.EndsWith(SevenZipMainDllName, StringComparison.OrdinalIgnoreCase))
            {
                // Load other library
                return LoadDllInternal(libraryName, assembly, searchPath, true);
            }

            IntPtr dllLoadPtr = TryLoadRedirectedDll(SevenZipMainDllName, SevenZipMainDllPath, assembly);

            // If it returns non zero, return pointer.
            if (dllLoadPtr != IntPtr.Zero)
            {
                return dllLoadPtr;
            }

            // Otherwise, throw as it's not found.
            throw new DllNotFoundException("Cannot find or load 7z64.dll from stock \"shareds\" folder or \"Program Files\" on your machine!");
        }

        private static IntPtr TryLoadRedirectedDll(string dllFileName, string dllFilePath, Assembly assembly, bool throwIfFail = true)
        {
            // AppContext.BaseDirectory 本身就是一个目录路径，不需要 Path.GetDirectoryName
            string assemblyParentPath = AppRootPath;
            string sevenZipStockPath = Path.Combine(assemblyParentPath, dllFilePath);

            // Load stock 7-zip dll if exist (从应用的 shareds 文件夹加载)
            if (File.Exists(sevenZipStockPath))
            {
                return LoadDllInternal(sevenZipStockPath, assembly, null, throwIfFail);
            }

            // 如果本地目录没找到，尝试 fallback 到系统安装的 7-Zip 目录

            // Try fallback to the official 7-Zip's .dll
            if (File.Exists(sevenZipStockPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "7-Zip", dllFileName))
            // If not found, try fallback to the ZStandard 7-Zip's .dll
            || File.Exists(sevenZipStockPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "7-Zip-Zstandard", dllFileName))
            // If those two do not exist, then try fallback to the root directory
            || File.Exists(sevenZipStockPath = Path.Combine(assemblyParentPath, dllFileName)))
            {
                return LoadDllInternal(sevenZipStockPath, assembly, null, throwIfFail);
            }

            // If all fails, then return zero as fail
            return IntPtr.Zero;
        }

        private static IntPtr LoadDllInternal(string libraryName, Assembly assembly, DllImportSearchPath? searchPath, bool throwIfFail)
        {
            // Try load the library and if fails, then throw.
            bool isLoadSuccessful = NativeLibrary.TryLoad(libraryName, assembly, searchPath, out IntPtr pResult);
            if ((!isLoadSuccessful || pResult == IntPtr.Zero) && throwIfFail)
                throw new FileLoadException($"Failed while loading library from this path: {libraryName}\r\nMake sure that the library/.dll is a valid Win32 library and not corrupted!");

            // If success, then return the pointer to the library
            return pResult;
        }

        public static T SafeCast<T>(PropVariant var, T def)
        {
            object obj;

            try
            {
                obj = var.Object;
            }
            catch (Exception)
            {
                return def;
            }

            if (obj is T expected)
            {
                return expected;
            }

            return def;
        }
    }
#endif
}