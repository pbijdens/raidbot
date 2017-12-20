using System.Threading.Tasks;

namespace Botje.Core.Services
{
    /// <summary>
    /// Swervice to asynchronously translate a location to an address.
    /// </summary>
    public interface ILocationToAddressService
    {
        /// <summary>
        /// Looks up the address.
        /// </summary>
        /// <param name="latitude"></param>
        /// <param name="longitude"></param>
        /// <returns></returns>
        Task<string> GetAddress(float latitude, float longitude);
    }
}
