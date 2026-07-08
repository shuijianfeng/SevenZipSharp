namespace SevenZip
{
    using System;

    /// <summary>
    /// LZMA 操作的异常类。
    /// </summary>
    public class LzmaException : SevenZipException
    {
        /// <summary>
        /// 未指定额外信息时显示的异常默认消息
        /// </summary>
        public const string DEFAULT_MESSAGE = "Specified stream is not a valid LZMA compressed stream!";

        /// <summary>
        /// 初始化 LzmaException 类的新实例
        /// </summary>
        public LzmaException() : base(DEFAULT_MESSAGE) { }

        /// <summary>
        /// 初始化 LzmaException 类的新实例
        /// </summary>
        /// <param name="message">附加的详细消息</param>
        public LzmaException(string message) : base(DEFAULT_MESSAGE, message) { }

        /// <summary>
        /// 初始化 LzmaException 类的新实例
        /// </summary>
        /// <param name="message">附加的详细消息</param>
        /// <param name="inner">发生的内部异常</param>
        public LzmaException(string message, Exception inner) : base(DEFAULT_MESSAGE, message, inner) { }
    }
}
