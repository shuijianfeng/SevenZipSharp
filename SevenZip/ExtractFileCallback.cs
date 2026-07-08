#if UNMANAGED

namespace SevenZip
{
    /// <summary>
    /// <see cref="SevenZipExtractor.ExtractFiles(SevenZip.ExtractFileCallback)"/> 的回调委托。
    /// </summary>
    public delegate void ExtractFileCallback(ExtractFileCallbackArgs extractFileCallbackArgs);
}

#endif
