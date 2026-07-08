namespace SevenZip
{
    using System;

    /// <summary>
    /// SevenZip 基础异常类。
    /// </summary>
    public class SevenZipException : Exception
    {
        /// <summary>
        /// 抛出的用户异常的消息。
        /// </summary>
        internal const string USER_EXCEPTION_MESSAGE = "The extraction was successful but" +
            "some exceptions were thrown in your events. Check UserExceptions for details.";

        /// <summary>
        /// 初始化 SevenZipException 类的新实例
        /// </summary>
        public SevenZipException() : base("SevenZip unknown exception.") { }

        /// <summary>
        /// 初始化 SevenZipException 类的新实例
        /// </summary>
        /// <param name="defaultMessage">默认异常消息</param>
        public SevenZipException(string defaultMessage)
            : base(defaultMessage) { }

        /// <summary>
        /// 初始化 SevenZipException 类的新实例
        /// </summary>
        /// <param name="defaultMessage">默认异常消息</param>
        /// <param name="message">附加的详细消息</param>
        public SevenZipException(string defaultMessage, string message)
            : base(defaultMessage + " Message: " + message) { }

        /// <summary>
        /// 初始化 SevenZipException 类的新实例
        /// </summary>
        /// <param name="defaultMessage">默认异常消息</param>
        /// <param name="message">附加的详细消息</param>
        /// <param name="inner">发生的内部异常</param>
        public SevenZipException(string defaultMessage, string message, Exception inner)
            : base(
                defaultMessage + (defaultMessage.EndsWith(" ", StringComparison.CurrentCulture) ? "" : " Message: ") +
                message, inner)
        { }

        /// <summary>
        /// 初始化 SevenZipException 类的新实例
        /// </summary>
        /// <param name="defaultMessage">默认异常消息</param>
        /// <param name="inner">发生的内部异常</param>
        public SevenZipException(string defaultMessage, Exception inner)
            : base(defaultMessage, inner) { }
    }
}
