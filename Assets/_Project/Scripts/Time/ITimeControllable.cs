namespace TimeMetroidvania.GameTime
{
    public interface ITimeControllable
    {
        bool IsTimeResistant { get; }
        void ApplyWorldTime(WorldTimeMode mode, float timeScale);
    }
}
