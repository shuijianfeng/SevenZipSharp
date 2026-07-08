#if UNMANAGED

namespace SevenZip
{
    using System;

    /// <summary>
    /// 7-zip 归档打开或读取操作的异常类。
    /// </summary>
    public class SevenZipArchiveException : SevenZipException
    {
        /// <summary>
        /// 未指定额外信息时显示的异常默认消息
        /// </summary>
        public static string DefaultMessage =
            $"Invalid archive: open/read error! Is it encrypted and a wrong password was provided?{Environment.NewLine}" +
            "If your archive is an exotic one, it is possible that SevenZipSharp has no signature for " +
            "its format and thus decided it is TAR by mistake.";

        /// <summary>
        /// 初始化 SevenZipArchiveException 类的新实例
        /// </summary>
        public SevenZipArchiveException() : base(DefaultMessage) { }

        /// <summary>
        /// 初始化 SevenZipArchiveException 类的新实例
        /// </summary>
        /// <param name="message">附加的详细消息</param>
        public SevenZipArchiveException(string message) : base(DefaultMessage, message) { }

        /// <summary>
        /// 初始化 SevenZipArchiveException 类的新实例
        /// </summary>
        /// <param name="message">附加的详细消息</param>
        /// <param name="inner">发生的内部异常</param>
        public SevenZipArchiveException(string message, Exception inner) : base(DefaultMessage, message, inner) { }
    }
}

#endif
