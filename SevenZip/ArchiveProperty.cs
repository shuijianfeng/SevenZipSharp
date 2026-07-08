#if UNMANAGED

namespace SevenZip
{
    /// <summary>
    /// 归档属性结构体。
    /// </summary>
    public struct ArchiveProperty
    {
        /// <summary>
        /// 获取归档属性的名称。
        /// </summary>
        public string Name { get; internal set; }

        /// <summary>
        /// 获取归档属性的值。
        /// </summary>
        public object Value { get; internal set; }

        /// <summary>
        /// 确定指定的 System.Object 是否等于当前 ArchiveProperty。
        /// </summary>
        /// <param name="obj">要与当前 ArchiveProperty 进行比较的 System.Object。</param>
        /// <returns>如果指定的 System.Object 等于当前 ArchiveProperty，则为 true；否则为 false。</returns>
        public override bool Equals(object obj)
        {
            return (obj is ArchiveProperty property) && Equals(property);
        }

        /// <summary>
        /// 确定指定的 ArchiveProperty 是否等于当前 ArchiveProperty。
        /// </summary>
        /// <param name="afi">要与当前 ArchiveProperty 进行比较的 ArchiveProperty。</param>
        /// <returns>如果指定的 ArchiveProperty 等于当前 ArchiveProperty，则为 true；否则为 false。</returns>
        public bool Equals(ArchiveProperty afi)
        {
            return afi.Name == Name && afi.Value == Value;
        }

        /// <summary>
        ///  作为特定类型的哈希函数。
        /// </summary>
        /// <returns> 当前 ArchiveProperty 的哈希码。</returns>
        public override int GetHashCode()
        {
            return Name.GetHashCode() ^ Value.GetHashCode();
        }

        /// <summary>
        /// 返回表示当前 ArchiveProperty 的 System.String。
        /// </summary>
        /// <returns>表示当前 ArchiveProperty 的 System.String。</returns>
        public override string ToString()
        {
            return Name + " = " + Value;
        }

        /// <summary>
        /// 确定指定的 ArchiveProperty 实例是否被视为相等。
        /// </summary>
        /// <param name="afi1">要比较的第一个 ArchiveProperty。</param>
        /// <param name="afi2">要比较的第二个 ArchiveProperty。</param>
        /// <returns>如果指定的 ArchiveProperty 实例被视为相等，则为 true；否则为 false。</returns>
        public static bool operator ==(ArchiveProperty afi1, ArchiveProperty afi2)
        {
            return afi1.Equals(afi2);
        }

        /// <summary>
        /// 确定指定的 ArchiveProperty 实例是否被视为不相等。
        /// </summary>
        /// <param name="afi1">要比较的第一个 ArchiveProperty。</param>
        /// <param name="afi2">要比较的第二个 ArchiveProperty。</param>
        /// <returns>如果指定的 ArchiveProperty 实例被视为不相等，则为 true；否则为 false。</returns>
        public static bool operator !=(ArchiveProperty afi1, ArchiveProperty afi2)
        {
            return !afi1.Equals(afi2);
        }
    }
}

#endif
