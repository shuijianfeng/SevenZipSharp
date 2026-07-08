#if UNMANAGED

namespace SevenZip
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    internal class CallbackBase : MarshalByRefObject
    {
        /// <summary>
        /// 在请求操作期间（例如在事件中）抛出的用户异常。
        /// </summary>
        private readonly List<Exception> _exceptions = [];
        
        /// <summary>
        /// 初始化 CallbackBase 类的新实例。
        /// </summary>
        protected CallbackBase()
        {
            Password = "";
            ReportErrors = true;
        }

        /// <summary>
        /// 初始化 CallbackBase 类的新实例。
        /// </summary>
        /// <param name="password">归档密码。</param>
        protected CallbackBase(string password)
        {
            ArgumentException.ThrowIfNullOrEmpty(password);

            Password = password;
            ReportErrors = true;
        }

        /// <summary>
        /// 获取或设置归档密码。
        /// </summary>
        public string Password { get; }

        /// <summary>
        /// 获取或设置指示当前过程是否已取消的值。
        /// </summary>
        public bool Canceled { get; set; }

        /// <summary>
        /// 获取或设置在归档错误时是否抛出异常的标志。
        /// </summary>
        public bool ReportErrors { get; }

        /// <summary>
        /// 获取在请求操作期间（例如在事件中）抛出的用户异常。
        /// </summary>
        public ReadOnlyCollection<Exception> Exceptions => new ReadOnlyCollection<Exception>(_exceptions);

        public void AddException(Exception e)
        {
            _exceptions.Add(e);
        }

        public void ClearExceptions()
        {
            _exceptions.Clear();
        }

        public bool HasExceptions => _exceptions.Count > 0;

        /// <summary>
        /// 在能够抛出时抛出指定的异常。
        /// </summary>
        /// <param name="e">要抛出的异常。</param>
        /// <param name="handler">负责处理该异常的处理程序。</param>
        public bool ThrowException(CallbackBase handler, params Exception[] e)
        {
            if (ReportErrors && (handler == null || !handler.Canceled))
            {
                throw e[0];
            }

            return false;
        }

        /// <summary>
        /// 如果列表中存在异常，则抛出第一个异常。
        /// </summary>
        /// <returns>返回 True 表示没有异常。</returns>
        public bool ThrowException()
        {
            if (HasExceptions && ReportErrors)
            {
                throw _exceptions[0];
            }

            return true;
        }

        public void ThrowUserException()
        {
            if (HasExceptions)
            {
                throw new SevenZipException(SevenZipException.USER_EXCEPTION_MESSAGE);
            }
        }
    }
}

#endif
