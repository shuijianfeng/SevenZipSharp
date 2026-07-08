namespace SevenZip
{
    using System;

    /// <summary>
    /// 用于存储 PercentDone 属性的事件参数。
    /// </summary>
    public class PercentDoneEventArgs : EventArgs
    {
        /// <summary>
        /// 初始化 PercentDoneEventArgs 类的新实例。
        /// </summary>
        /// <param name="percentDone">已完成工作的百分比。</param>
        /// <exception cref="System.ArgumentOutOfRangeException"/>
        public PercentDoneEventArgs(byte percentDone)
        {
            if (percentDone > 100 || percentDone < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(percentDone),
                    "The percent of finished work must be between 0 and 100.");
            }

            PercentDone = percentDone;
        }

        /// <summary>
        /// 获取已完成工作的百分比。
        /// </summary>
        public byte PercentDone { get; }

        /// <summary>
        /// 将 [0, 1] 范围内的比率转换为其等效的百分比值。
        /// </summary>
        /// <param name="doneRate">已完成工作的比率。</param>
        /// <returns>等效的整数百分比值。</returns>
        /// <exception cref="System.ArgumentException"/>
        internal static byte ProducePercentDone(float doneRate)
        {
            return (byte)Math.Round(Math.Min(100 * doneRate, 100), MidpointRounding.AwayFromZero);
        }
    }
}
