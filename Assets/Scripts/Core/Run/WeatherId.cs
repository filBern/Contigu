namespace Contigu.Core
{
    /// <summary>The weather of one round (see RunManager.CurrentWeather). Rolled for the whole run at its start, so the shop can announce the next one.</summary>
    public enum WeatherId
    {
        Clear,
        Rain,
        Sunshine,
        Wind,
        Frost,
        Dew,
        GoldenHour,
        PollenShower
    }
}
