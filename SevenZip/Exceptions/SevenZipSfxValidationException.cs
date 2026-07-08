namespace SevenZip
{
    using System;

    /// <summary>
    /// 7-zip sfx 设置验证的异常类。
    /// </summary>
    public class SevenZipSfxValidationException : SevenZipException
    {
        /// <summary>
        /// 未指定额外信息时显示的异常默认消息
        /// </summary>
        public static readonly string DefaultMessage = "Sfx settings validation failed.";

        /// <summary>
        /// 初始化 SevenZipSfxValidationException 类的新实例
        /// </summary>
        public SevenZipSfxValidationException() : base(DefaultMessage) { }

        /// <summary>
        /// 初始化 SevenZipSfxValidationException 类的新实例
        /// </summary>
        /// <param name="message">附加的详细消息</param>
        public SevenZipSfxValidationException(string message) : base(DefaultMessage, message) { }

        /// <summary>
        /// 初始化 SevenZipSfxValidationException 类的新实例
        /// </summary>
        /// <param name="message">附加的详细消息</param>
        /// <param name="inner">发生的内部异常</param>
        public SevenZipSfxValidationException(string message, Exception inner) : base(DefaultMessage, message, inner) { }
    }
}
