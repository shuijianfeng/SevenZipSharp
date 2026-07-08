namespace SevenZip
{
    using System;

    using SevenZip.Sdk;

    /// <summary>
    /// 用于实现 ICodeProgress 接口的回调
    /// </summary>
    internal sealed class LzmaProgressCallback : ICodeProgress
    {
        private readonly long _inSize;
        private float _oldPercentDone;

        /// <summary>
        /// 初始化 LzmaProgressCallback 类的新实例
        /// </summary>
        /// <param name="inSize">输入大小</param>
        /// <param name="working">进度事件处理程序</param>
        public LzmaProgressCallback(long inSize, EventHandler<ProgressEventArgs> working)
        {
            _inSize = inSize;
            Working += working;
        }

        #region ICodeProgress Members

        /// <summary>
        /// 设置进度
        /// </summary>
        /// <param name="inSize">已处理的输入大小</param>
        /// <param name="outSize">已处理的输出大小</param>
        public void SetProgress(long inSize, long outSize)
        {
            if (Working != null)
            {
                float newPercentDone = (inSize + 0.0f) / _inSize;
                float delta = newPercentDone - _oldPercentDone;
                if (delta * 100 < 1.0)
                {
                    delta = 0;
                }
                else
                {
                    _oldPercentDone = newPercentDone;
                }
                Working(this, new ProgressEventArgs(
                                  PercentDoneEventArgs.ProducePercentDone(newPercentDone),
                                  delta > 0 ? PercentDoneEventArgs.ProducePercentDone(delta) : (byte)0));
            }
        }

        #endregion

        public event EventHandler<ProgressEventArgs> Working;
    }
}
