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

        //[LibraryImport("kernel32.dll", EntryPoint = "LoadLibraryW", StringMarshalling = StringMarshalling.Utf16)]
        //public static partial IntPtr LoadLibrary(string fileName);

        //[LibraryImport("kernel32.dll")]
        //[return: MarshalAs(UnmanagedType.Bool)]
        //public static partial bool FreeLibrary(IntPtr hModule);

        //[LibraryImport("kernel32.dll", EntryPoint = "GetProcAddress" , StringMarshalling = StringMarshalling.Custom,StringMarshallingCustomType = typeof(AnsiStringMarshaller))]
        //public static partial IntPtr GetProcAddress(IntPtr hModule,  string procName);

        private const string SevenZipMainDllName = "7z64.dll";
        private const string SevenZipMainDllPath = SevenZipMainDllName;
        //private const string SevenZipMiniDllName = "\\shareds\\7z64.dll";
        //private const string SevenZipMiniDllPath = SevenZipMiniDllName;
        //private const string SevenZipEMiniDllName = "\\shareds\\7z64.dll";
        //private const string SevenZipEMiniDllPath = SevenZipEMiniDllName;
        private static readonly string CurrentProcessPath = Process.GetCurrentProcess().MainModule!.FileName;
        public static  bool isinit  = false;
        static NativeMethods()
        {
            // Use custom Dll import resolver
            NativeLibrary.SetDllImportResolver(Assembly.GetExecutingAssembly(), DllImportResolver);
            isinit = true;
        }
        private static IntPtr DllImportResolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
        {
            // If the library to load is not "7z.dll", then try load it as other library
            if (!libraryName.EndsWith(SevenZipMainDllName, StringComparison.OrdinalIgnoreCase))
            {
                // Load other library
                return LoadDllInternal(libraryName, assembly, searchPath, true);
            }
            IntPtr dllLoadPtr = TryLoadRedirectedDll(SevenZipMainDllName, SevenZipMainDllPath, assembly);
            // Try load 7zxa.dll first
            //IntPtr dllLoadPtr = TryLoadRedirectedDll(SevenZipEMiniDllName, SevenZipEMiniDllPath, assembly, false);

            //// If 7zxa.dll is not found, then fallback to 7za.dll
            //if (dllLoadPtr == IntPtr.Zero)
            //{
            //    dllLoadPtr = TryLoadRedirectedDll(SevenZipMiniDllName, SevenZipMiniDllPath, assembly, false);
            //}

            //// If 7za.dll is not found, then fallback to 7z.dll
            //if (dllLoadPtr == IntPtr.Zero)
            //{
            //    dllLoadPtr = TryLoadRedirectedDll(SevenZipMainDllName, SevenZipMainDllPath, assembly);
            //}

            // If it returns non zero, return pointer.
            if (dllLoadPtr != IntPtr.Zero)
            {
                return dllLoadPtr;
            }

            // Otherwise, throw as it's not found.
            throw new DllNotFoundException("Cannot find or load either 7zxa.dll or 7za.dll or 7z.dll from stock \"Lib\" folder or \"Program Files\" on your machine!");
        }

        private static IntPtr TryLoadRedirectedDll(string dllFileName, string dllFilePath, Assembly assembly, bool throwIfFail = true)
        {
            // Get the root directory of the module, then try to get the stock .dll path
            string? assemblyParentPath = Path.GetDirectoryName(CurrentProcessPath);
            string sevenZipStockPath = Path.Combine(assemblyParentPath ?? "", dllFilePath);

            // Load stock 7-zip dll if exist
            if (File.Exists(sevenZipStockPath))
            {
                // Load the stock <dllFileName> library
                return LoadDllInternal(sevenZipStockPath, assembly, null, throwIfFail);
            }

            // If the stock .dll is not found in <collapse_install_path>\Lib\<dllFileName>,
            // then try fallback to the one installed in the system (if exist)

            // Try fallback to the official 7-Zip's .dll
            if (File.Exists(sevenZipStockPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "7-Zip", dllFileName))
            // If not found, try fallback to the ZStandard 7-Zip's .dll
            || File.Exists(sevenZipStockPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "7-Zip-Zstandard", dllFileName))
            // If those two do not exist, then try fallback to the .dll in the <collapse_install_path>\<dllFileName>
            || File.Exists(sevenZipStockPath = Path.Combine(assemblyParentPath ?? "", dllFileName)))
                return LoadDllInternal(sevenZipStockPath, assembly, null, throwIfFail);

            // If all fails, then return zero as fail
            return IntPtr.Zero;
        }

        private static IntPtr LoadDllInternal(string libraryName, Assembly assembly, DllImportSearchPath? searchPath, bool throwIfFail)
        {
            //Console.WriteLine($"[7-zip][NativeMethods::DllImportResolver] Loading library from path: {libraryName} | Search path: {(searchPath == null ? "Default" : searchPath)}");
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