using System.Threading.Tasks;

namespace PokemonRaidBot.LocationAPI
{
    public interface ILocationToAddressService
    {
        /// <summary>
        /// Looks up the address.
        /// </summary>
        /// <param name="latitude"></param>
        /// <param name="longitude"></param>
        /// <returns></returns>
        Task<string> GetAddress(double latitude, double longitude);
    }
}
