namespace TimeMetroidvania.GameTime
{
    /// <summary>
    /// 接收世界时间状态，由实现对象自行应用局部时间倍率。
    /// </summary>
    public interface ITimeControllable
    {
        /// <summary>为真时世界时间服务始终传入 1 倍速度。</summary>
        bool IsTimeResistant { get; }

        /// <summary>应用当前时间模式及该对象的局部时间倍率。</summary>
        /// <param name="mode">当前世界时间模式。</param>
        /// <param name="timeScale">世界时间服务计算后的倍率。</param>
        void ApplyWorldTime(WorldTimeMode mode, float timeScale);
    }
}
