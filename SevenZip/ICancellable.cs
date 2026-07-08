namespace SevenZip
{
    /// <summary>
    /// 定义支持取消进程的接口。
    /// </summary>
    public interface ICancellable
    {
        /// <summary>
        /// 获取或设置是否停止当前的归档操作。
        /// </summary>
        bool Cancel { get; set; }

        /// <summary>
        /// 获取或设置是否跳过当前文件。
        /// </summary>
        bool Skip { get; set; }
    }
}
