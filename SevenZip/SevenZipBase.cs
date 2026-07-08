#if UNMANAGED

namespace SevenZip
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.Threading;

    /// <summary>
    /// SevenZip 解压器/压缩器基类。实现了密码字符串和报告错误标志。
    /// </summary>
    public abstract class SevenZipBase : MarshalByRefObject
    {
        private readonly bool _reportErrors;
        private readonly int _uniqueId;
        private static int _incrementingUniqueId = int.MinValue;

        /// <summary>
        /// 如果该类的实例需要在新线程上下文中重新创建，则为 true；否则为 false。
        /// </summary>
        protected internal bool NeedsToBeRecreated;

        internal virtual void SaveContext()
        {
            Context = SynchronizationContext.Current;
            NeedsToBeRecreated = true;
        }

        internal virtual void ReleaseContext()
        {
            Context = null;
            NeedsToBeRecreated = true;
            GC.SuppressFinalize(this);
        }

        private delegate void EventHandlerDelegate<T>(EventHandler<T> handler, T e) where T : EventArgs;

        internal void OnEvent<T>(EventHandler<T> handler, T e, bool synchronous) where T : EventArgs
        {
            try
            {
                if (handler == null) return;

                switch (EventSynchronization)
                {
                    case EventSynchronizationStrategy.AlwaysAsynchronous:
                        synchronous = false;
                        break;
                    case EventSynchronizationStrategy.AlwaysSynchronous:
                        synchronous = true;
                        break;
                }

                if (Context == null)
                {
                    // Usual synchronous call - no allocation
                    handler(this, e);
                }
                else
                {
                    // Allocate a closure tuple only when we need to post to a SynchronizationContext.
                    var state = new Tuple<EventHandler<T>, object, T>(handler, this, e);
                    var callback = new SendOrPostCallback(obj =>
                    {
                        var tuple = (Tuple<EventHandler<T>, object, T>)obj;
                        tuple.Item1(tuple.Item2, tuple.Item3);
                    });

                    if (synchronous)
                    {
                        Context.Send(callback, state);
                    }
                    else
                    {
                        Context.Post(callback, state);
                    }
                }
            }
            catch (Exception ex)
            {
                AddException(ex);
            }
        }

        internal SynchronizationContext Context { get; set; }

        /// <summary>
        /// 获取或设置事件同步策略。
        /// </summary>
        public EventSynchronizationStrategy EventSynchronization { get; set; }

        /// <summary>
        /// 获取此 SevenZipBase 实例的唯一标识符。
        /// </summary>
        public int UniqueID => _uniqueId;

        /// <summary>
        /// 在请求的操作过程中（例如在事件中）抛出的用户异常。
        /// </summary>
        private readonly List<Exception> _exceptions = [];

        private static int GetUniqueID()
        {
            var newUniqueId = Interlocked.Increment(ref _incrementingUniqueId);
            return newUniqueId;
        }

        /// <summary>
        /// 初始化 SevenZipBase 类的新实例。
        /// </summary>
        /// <param name="password">归档密码。</param>
        protected SevenZipBase(string password = "")
        {
            Password = password;
            _reportErrors = true;
            _uniqueId = GetUniqueID();
        }

        /// <summary>
        /// 获取或设置归档密码。
        /// </summary>
        public string Password { get; protected set; }

        /// <summary>
        /// 获取或设置在归档错误时抛出异常的标志。
        /// </summary>
        internal bool ReportErrors => _reportErrors;

        /// <summary>
        /// 获取在请求的操作过程中（例如在事件中）抛出的用户异常。
        /// </summary>
        internal ReadOnlyCollection<Exception> Exceptions => new ReadOnlyCollection<Exception>(_exceptions);

        internal void AddException(Exception e)
        {
            _exceptions.Add(e);
        }

        internal void ClearExceptions()
        {
            _exceptions.Clear();
        }

        internal bool HasExceptions => _exceptions.Count > 0;

        /// <summary>
        /// 在条件允许时抛出指定的异常。
        /// </summary>
        /// <param name="e">要抛出的异常。</param>
        /// <param name="handler">负责该异常的回调处理器。</param>
        internal bool ThrowException(CallbackBase handler, params Exception[] e)
        {
            if (_reportErrors && (handler == null || !handler.Canceled))
            {
                throw e[0];
            }

            return false;
        }

        internal void ThrowUserException()
        {
            if (HasExceptions)
            {
                throw new SevenZipException(SevenZipException.USER_EXCEPTION_MESSAGE);
            }
        }

        /// <summary>
        /// 如果 HRESULT != 0 则抛出异常。
        /// </summary>
        /// <param name="hresult">要检查的结果代码。</param>
        /// <param name="message">异常消息。</param>
        /// <param name="handler">负责回调的类。</param>
        internal void CheckedExecute(int hresult, string message, CallbackBase handler)
        {
            if (hresult != (int)OperationResult.Ok || handler.HasExceptions)
            {
                if (!handler.HasExceptions)
                {
                    if (hresult < -2000000000)
                    {
                        SevenZipException exception;

                        switch (hresult)
                        {
                            case -2146233067:
                                exception = new SevenZipException("Operation is not supported. (0x80131515: E_NOTSUPPORTED)");
                                break;
                            case -2147024784:
                                exception = new SevenZipException("There is not enough space on the disk. (0x80070070: ERROR_DISK_FULL)");
                                break;
                            case -2147024864:
                                exception = new SevenZipException("The file is being used by another process. (0x80070020: ERROR_SHARING_VIOLATION)");
                                break;
                            case -2147024882:
                                exception = new SevenZipException("There is not enough memory (RAM). (0x8007000E: E_OUTOFMEMORY)");
                                break;
                            case -2147024809:
                                exception = new SevenZipException("Invalid arguments provided. (0x80070057: E_INVALIDARG)");
                                break;
                            case -2147467263:
                                exception = new SevenZipException("Functionality not implemented. (0x80004001: E_NOTIMPL)");
                                break;
                            case -2147024891:
                                exception = new SevenZipException("Access is denied. (0x80070005: E_ACCESSDENIED)");
                                break;
                            case -2146233086:
                                exception = new SevenZipException("Argument is out of range. (0x80131502: E_ARGUMENTOUTOFRANGE)");
                                break;
                            case -2147024690:
                                exception = new SevenZipException("Filename or extension is too long. (0x800700CE: ERROR_FILENAME_EXCED_RANGE)");
                                break;
                            default:
                                exception = new SevenZipException(
                                    $"Execution has failed due to an internal SevenZipSharp issue (0x{hresult:x} / {hresult}).\n" +
                                    "You might find more info at https://github.com/squid-box/SevenZipSharp/issues/, but this library is no longer actively supported.");
                                break;
                        }

                        ThrowException(handler, exception);
                    }
                    else
                    {
                        ThrowException(handler, new SevenZipException(message + hresult.ToString(CultureInfo.InvariantCulture) + '.'));
                    }
                }
                else
                {
                    ThrowException(handler, handler.Exceptions[0]);
                }
            }
        }

        /// <summary>
        /// 更改 7-zip 原生库的路径。
        /// </summary>
        /// <param name="libraryPath">7-zip 原生库的路径。</param>
        public static void SetLibraryPath(string libraryPath)
        {
            SevenZipLibraryManager.SetLibraryPath(libraryPath);
        }

        /// <summary>
        /// 获取当前库的功能特性。
        /// </summary>
        [CLSCompliant(false)]
        public static LibraryFeature CurrentLibraryFeatures => SevenZipLibraryManager.CurrentLibraryFeatures;

        /// <summary>
        /// 确定指定的 System.Object 是否等于当前的 SevenZipBase。
        /// </summary>
        /// <param name="obj">要与当前 SevenZipBase 进行比较的 System.Object。</param>
        /// <returns>如果指定的 System.Object 等于当前的 SevenZipBase，则为 true；否则为 false。</returns>
        public override bool Equals(object obj)
        {
            if (obj is not SevenZipBase instance)
            {
                return false;
            }

            return _uniqueId == instance._uniqueId;
        }

        /// <summary>
        /// 用作特定类型的哈希函数。
        /// </summary>
        /// <returns>当前 SevenZipBase 的哈希代码。</returns>
        public override int GetHashCode()
        {
            return _uniqueId;
        }

        /// <summary>
        /// 返回表示当前 SevenZipBase 的 System.String。
        /// </summary>
        /// <returns>表示当前 SevenZipBase 的 System.String。</returns>
        public override string ToString()
        {
            var type = this switch
            {
                SevenZipExtractor => "SevenZipExtractor",
                SevenZipCompressor => "SevenZipCompressor",
                _ => "SevenZipBase"
            };

            return $"{type} [{_uniqueId}]";
        }
    }
}

#endif
