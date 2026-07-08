#if UNMANAGED

namespace SevenZip
{
    /// <summary>
    /// 事件同步的方式。
    /// </summary>
    public enum EventSynchronizationStrategy
    {
        /// <summary>
        /// 如果用户可以执行某些操作（例如取消执行过程），则以同步方式调用事件。
        /// </summary>
        Default,
        /// <summary>
        /// 始终以异步方式调用事件。
        /// </summary>
        AlwaysAsynchronous,
        /// <summary>
        /// 始终以同步方式调用事件。
        /// </summary>
        AlwaysSynchronous
    }
}

#endif
