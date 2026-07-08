#if UNMANAGED

namespace SevenZip
{
    using System;

    /// <summary>
    /// 7-zip 库操作的异常类。
    /// </summary>
    public class SevenZipLibraryException : SevenZipException
    {
        /// <summary>
        /// 未指定额外信息时显示的异常默认消息
        /// </summary>
        public const string DEFAULT_MESSAGE = "Can not load 7-zip library or internal COM error!";

        /// <summary>
        /// 初始化 SevenZipLibraryException 类的新实例
        /// </summary>
        public SevenZipLibraryException() : base(DEFAULT_MESSAGE) { }

        /// <summary>
        /// 初始化 SevenZipLibraryException 类的新实例
        /// </summary>
        /// <param name="message">附加的详细消息</param>
        public SevenZipLibraryException(string message) : base(DEFAULT_MESSAGE, message) { }

        /// <summary>
        /// 初始化 SevenZipLibraryException 类的新实例
        /// </summary>
        /// <param name="message">附加的详细消息</param>
        /// <param name="inner">发生的内部异常</param>
        public SevenZipLibraryException(string message, Exception inner) : base(DEFAULT_MESSAGE, message, inner) { }
    }
}

#endif
