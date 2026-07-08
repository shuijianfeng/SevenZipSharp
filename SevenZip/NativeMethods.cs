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

        private static readonly Assembly s_assembly = typeof(NativeMethods).Assembly;

        static NativeMethods()
        {
            // 使用自定义的 Dll 导入解析器
            NativeLibrary.SetDllImportResolver(s_assembly, DllImportResolver);
            isinit = true;
        }

        private static IntPtr DllImportResolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
        {
            // 如果要加载的库不是 "7z64.dll"，则尝试作为其他库加载
            if (!libraryName.EndsWith(SevenZipMainDllName, StringComparison.OrdinalIgnoreCase))
            {
                // 加载其他库
                return LoadDllInternal(libraryName, assembly, searchPath, true);
            }

            IntPtr dllLoadPtr = TryLoadRedirectedDll(SevenZipMainDllName, SevenZipMainDllPath, assembly);

            // 如果返回非零值，则返回该指针。
            if (dllLoadPtr != IntPtr.Zero)
            {
                return dllLoadPtr;
            }

            // 否则，抛出未找到的异常。
            throw new DllNotFoundException("Cannot find or load 7z64.dll from stock \"shareds\" folder or \"Program Files\" on your machine!");
        }

        private static IntPtr TryLoadRedirectedDll(string dllFileName, string dllFilePath, Assembly assembly, bool throwIfFail = true)
        {
            // AppContext.BaseDirectory 本身就是一个目录路径，不需要 Path.GetDirectoryName
            string assemblyParentPath = AppRootPath;
            string sevenZipStockPath = Path.Combine(assemblyParentPath, dllFilePath);

            // 如果存在则加载自带的 7-zip dll（从应用的 shareds 文件夹加载）
            if (File.Exists(sevenZipStockPath))
            {
                return LoadDllInternal(sevenZipStockPath, assembly, null, throwIfFail);
            }

            // 如果本地目录没找到，尝试 fallback 到系统安装的 7-Zip 目录

            // 尝试回退到官方 7-Zip 的 .dll
            if (File.Exists(sevenZipStockPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "7-Zip", dllFileName))
            // 如果未找到，尝试回退到 ZStandard 7-Zip 的 .dll
            || File.Exists(sevenZipStockPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "7-Zip-Zstandard", dllFileName))
            // 如果这两个都不存在，则尝试回退到根目录
            || File.Exists(sevenZipStockPath = Path.Combine(assemblyParentPath, dllFileName)))
            {
                return LoadDllInternal(sevenZipStockPath, assembly, null, throwIfFail);
            }

            // 如果全部失败，则返回零表示失败
            return IntPtr.Zero;
        }

        private static IntPtr LoadDllInternal(string libraryName, Assembly assembly, DllImportSearchPath? searchPath, bool throwIfFail)
        {
            // 尝试加载库，如果失败则抛出异常。
            bool isLoadSuccessful = NativeLibrary.TryLoad(libraryName, assembly, searchPath, out IntPtr pResult);
            if ((!isLoadSuccessful || pResult == IntPtr.Zero) && throwIfFail)
                throw new FileLoadException($"Failed while loading library from this path: {libraryName}\r\nMake sure that the library/.dll is a valid Win32 library and not corrupted!");

            // 如果成功，则返回该库的指针
            return pResult;
        }

        public static T SafeCast<T>(PropVariant var, T def)
        {
            // 常见类型的快速路径，避免通过 var.Object 进行装箱/反射。
            try
            {
                switch (var.VarType)
                {
                    case VarEnum.VT_EMPTY:
                        return def;
                    case VarEnum.VT_BSTR:
                        {
                            var s = Marshal.PtrToStringBSTR(var.Value);
                            if (s is T ts) return ts;
                            return def;
                        }
                    case VarEnum.VT_BOOL:
                        {
                            var b = var.BoolVal != 0;
                            if (b is T tb) return tb;
                            return def;
                        }
                    case VarEnum.VT_FILETIME:
                        {
                            var dt = DateTime.FromFileTime(var.Int64Value);
                            if (dt is T td) return td;
                            return def;
                        }
                    case VarEnum.VT_UI8:
                        {
                            var v = var.UInt64Value;
                            if (v is T tv) return tv;
                            // 尝试通过 object 进行数值类型转换
                            if (typeof(T) == typeof(ulong)) return (T)(object)v;
                            if (typeof(T) == typeof(long)) return (T)(object)(long)v;
                            if (typeof(T) == typeof(uint)) return (T)(object)(uint)v;
                            if (typeof(T) == typeof(int)) return (T)(object)(int)v;
                            return def;
                        }
                    case VarEnum.VT_UI4:
                        {
                            var v = var.UInt32Value;
                            if (v is T tv) return tv;
                            if (typeof(T) == typeof(uint)) return (T)(object)v;
                            if (typeof(T) == typeof(int)) return (T)(object)(int)v;
                            if (typeof(T) == typeof(long)) return (T)(object)(long)v;
                            if (typeof(T) == typeof(ulong)) return (T)(object)(ulong)v;
                            return def;
                        }
                    case VarEnum.VT_I8:
                        {
                            var v = var.Int64Value;
                            if (v is T tv) return tv;
                            if (typeof(T) == typeof(long)) return (T)(object)v;
                            if (typeof(T) == typeof(int)) return (T)(object)(int)v;
                            if (typeof(T) == typeof(ulong)) return (T)(object)(ulong)v;
                            if (typeof(T) == typeof(uint)) return (T)(object)(uint)v;
                            return def;
                        }
                    case VarEnum.VT_I4:
                        {
                            var v = var.Int32Value;
                            if (v is T tv) return tv;
                            if (typeof(T) == typeof(int)) return (T)(object)v;
                            if (typeof(T) == typeof(uint)) return (T)(object)(uint)v;
                            if (typeof(T) == typeof(long)) return (T)(object)(long)v;
                            if (typeof(T) == typeof(ulong)) return (T)(object)(ulong)v;
                            return def;
                        }
                    default:
                        // 罕见类型的回退处理
                        var obj = var.Object;
                        if (obj is T expected) return expected;
                        return def;
                }
            }
            catch (Exception)
            {
                return def;
            }
        }
    }
#endif
}