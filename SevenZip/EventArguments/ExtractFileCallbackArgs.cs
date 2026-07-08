#if UNMANAGED

namespace SevenZip
{
    using System;
    using System.IO;

    /// <summary>
    /// 传递给 <see cref="ExtractFileCallback"/> 的参数。
    /// </summary>
    /// <remarks>
    /// 对于每个文件，<see cref="ExtractFileCallback"/> 首次调用时 <see cref="Reason"/>
    /// 设置为 <see cref="ExtractFileCallbackReason.Start"/>。如果回调通过设置 <see cref="ExtractToFile"/>
    /// 或 <see cref="ExtractToStream"/> 选择提取文件数据，则回调将被第二次调用，此时 <see cref="Reason"/>
    /// 设置为 <see cref="ExtractFileCallbackReason.Done"/> 或 <see cref="ExtractFileCallbackReason.Failure"/>，
    /// 以便执行诸如关闭流之类的清理任务。
    /// </remarks>
    public class ExtractFileCallbackArgs : EventArgs
    {
        private readonly ArchiveFileInfo _archiveFileInfo;
        private Stream _extractToStream;

        /// <summary>
        /// 初始化 <see cref="ExtractFileCallbackArgs"/> 类的新实例。
        /// </summary>
        /// <param name="archiveFileInfo">压缩包中文件的相关信息。</param>
        public ExtractFileCallbackArgs(ArchiveFileInfo archiveFileInfo)
        {
            Reason = ExtractFileCallbackReason.Start;
            _archiveFileInfo = archiveFileInfo;
        }

        /// <summary>
        /// 压缩包中文件的相关信息。
        /// </summary>
        /// <value>压缩包中文件的相关信息。</value>
        public ArchiveFileInfo ArchiveFileInfo => _archiveFileInfo;

        /// <summary>
        /// 调用 <see cref="ExtractFileCallback"/> 的原因。
        /// </summary>
        /// <remarks>
        /// 如果既未设置 <see cref="ExtractToFile"/> 也未设置 <see cref="ExtractToStream"/>，
        /// 则在 <see cref="ExtractFileCallbackReason.Start"/> 之后不会再调用 <see cref="ExtractFileCallback"/>。
        /// </remarks>
        /// <value>调用原因。</value>
        public ExtractFileCallbackReason Reason { get; internal set; }

        /// <summary>
        /// 解压过程中发生的异常。
        /// </summary>
        /// <value>异常对象。</value>
        /// <remarks>
        /// 如果回调被调用时 <see cref="Reason"/> 设置为 <see cref="ExtractFileCallbackReason.Failure"/>，
        /// 此成员包含所发生的异常。
        /// 默认行为是在回调返回后重新抛出该异常。
        /// 但是，回调可以将 <see cref="Exception"/> 设置为 <c>null</c> 以吞没异常，
        /// 并继续解压下一个文件。
        /// </remarks>
        public Exception Exception { get; set; }

        /// <summary>
        /// 获取或设置一个值，指示是否取消解压。
        /// </summary>
        /// <value><c>true</c> 表示取消解压；<c>false</c> 表示继续。默认值为 <c>false</c>。</value>
        public bool CancelExtraction { get; set; }

        /// <summary>
        /// 获取或设置是否提取文件以及提取到的位置。
        /// </summary>
        /// <value>文件提取到的路径。</value>
        /// <remarks>
        /// 如果设置了 <see cref="ExtractToStream"/>，此成员将被忽略。
        /// </remarks>
        public string ExtractToFile { get; set; }

        /// <summary>
        /// 获取或设置是否提取文件以及提取到的位置。
        /// </summary>
        /// <value>提取数据写入的目标流。</value>
        /// <remarks>
        /// 如果此成员和 <see cref="ExtractToFile"/> 均为 <c>null</c>（默认值），则文件不会被提取，
        /// 回调将被第二次执行，此时 <see cref="Reason"/> 设置为 <see cref="ExtractFileCallbackReason.Done"/>
        /// 或 <see cref="ExtractFileCallbackReason.Failure"/>。
        /// </remarks>
        public Stream ExtractToStream
        {
            get => _extractToStream;
            set
            {
                if (_extractToStream != null && !_extractToStream.CanWrite)
                {
                    throw new ExtractionFailedException("The specified stream is not writable!");
                }

                _extractToStream = value;
            }
        }

        /// <summary>
        /// 获取或设置在 <see cref="ExtractFileCallbackReason.Start"/> 回调调用与
        /// <see cref="ExtractFileCallbackReason.Done"/> 或 <see cref="ExtractFileCallbackReason.Failure"/> 调用之间保留的任意数据。
        /// </summary>
        /// <value>数据对象。</value>
        public object ObjectData { get; set; }
    }
}

#endif
