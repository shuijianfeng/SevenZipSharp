#if UNMANAGED

namespace SevenZip
{
    using System;

    /// <summary>
    /// 在 SevenZipCompressor 中创建归档失败时的异常类。
    /// </summary>
    public class SevenZipCompressionFailedException : SevenZipException
    {
        /// <summary>
        /// 未指定额外信息时显示的异常默认消息
        /// </summary>
        public const string DEFAULT_MESSAGE = "The compression has failed for an unknown reason with code ";

        /// <summary>
        /// 初始化 SevenZipCompressionFailedException 类的新实例
        /// </summary>
        public SevenZipCompressionFailedException() : base(DEFAULT_MESSAGE) { }

        /// <summary>
        /// 初始化 SevenZipCompressionFailedException 类的新实例
        /// </summary>
        /// <param name="message">附加的详细消息</param>
        public SevenZipCompressionFailedException(string message) : base(DEFAULT_MESSAGE, message) { }

        /// <summary>
        /// 初始化 SevenZipCompressionFailedException 类的新实例
        /// </summary>
        /// <param name="message">附加的详细消息</param>
        /// <param name="inner">发生的内部异常</param>
        public SevenZipCompressionFailedException(string message, Exception inner)
            : base(DEFAULT_MESSAGE, message, inner) { }
    }
}

#endif
