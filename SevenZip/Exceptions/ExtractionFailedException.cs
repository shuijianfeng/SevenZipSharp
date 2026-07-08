#if UNMANAGED

namespace SevenZip
{
    using System;

    /// <summary>
    /// ArchiveExtractCallback 的异常类。
    /// </summary>
    public class ExtractionFailedException : SevenZipException
    {
        /// <summary>
        /// 未指定额外信息时显示的异常默认消息
        /// </summary>
        public const string DEFAULT_MESSAGE = "Could not extract files!";

        /// <summary>
        /// 初始化 ExtractionFailedException 类的新实例
        /// </summary>
        public ExtractionFailedException() : base(DEFAULT_MESSAGE) { }

        /// <summary>
        /// 初始化 ExtractionFailedException 类的新实例
        /// </summary>
        /// <param name="message">附加的详细消息</param>
        public ExtractionFailedException(string message) : base(DEFAULT_MESSAGE, message) { }

        /// <summary>
        /// 初始化 ExtractionFailedException 类的新实例
        /// </summary>
        /// <param name="message">附加的详细消息</param>
        /// <param name="inner">发生的内部异常</param>
        public ExtractionFailedException(string message, Exception inner) : base(DEFAULT_MESSAGE, message, inner) { }
    }
}

#endif
