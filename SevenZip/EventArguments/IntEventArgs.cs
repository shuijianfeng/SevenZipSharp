#if UNMANAGED

namespace SevenZip
{
    using System;

    /// <summary>
    /// 存储一个整数。
    /// </summary>
    public sealed class IntEventArgs : EventArgs
    {
        /// <summary>
        /// 初始化 IntEventArgs 类的新实例。
        /// </summary>
        /// <param name="value">IntEventArgs 类携带的有用数据。</param>
        public IntEventArgs(int value)
        {
            Value = value;
        }

        /// <summary>
        /// 获取 IntEventArgs 类的值。
        /// </summary>
        public int Value { get; }
    }
}

#endif
