#if UNMANAGED

namespace SevenZip
{
    using System;

    /// <summary>
    /// SevenZipCompressor 中文件名数组的公共根为空时的异常类。
    /// </summary>
    public class SevenZipInvalidFileNamesException : SevenZipException
    {
        /// <summary>
        /// 未指定额外信息时显示的异常默认消息
        /// </summary>
        public const string DEFAULT_MESSAGE = "Invalid file names have been specified: ";

        /// <summary>
        /// 初始化 SevenZipInvalidFileNamesException 类的新实例
        /// </summary>
        public SevenZipInvalidFileNamesException() : base(DEFAULT_MESSAGE) { }

        /// <summary>
        /// 初始化 SevenZipInvalidFileNamesException 类的新实例
        /// </summary>
        /// <param name="message">附加的详细消息</param>
        public SevenZipInvalidFileNamesException(string message) : base(DEFAULT_MESSAGE, message) { }

        /// <summary>
        /// 初始化 SevenZipInvalidFileNamesException 类的新实例
        /// </summary>
        /// <param name="message">附加的详细消息</param>
        /// <param name="inner">发生的内部异常</param>
        public SevenZipInvalidFileNamesException(string message, Exception inner) : base(DEFAULT_MESSAGE, message, inner) { }
    }
}

#endif
