using System;

namespace PokemonRaidBot.Utils
{
    public interface ITimeService
    {
        string AsShortTime(DateTime utc);
        string AsFullTime(DateTime utc);
        string AsReadableTimespan(TimeSpan ts);
        string AsDutchString(DateTime dt);
        string AsLocalShortTime(DateTime dt);

    }
}
