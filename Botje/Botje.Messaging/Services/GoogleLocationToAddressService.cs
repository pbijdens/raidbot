using Botje.Core;
using Botje.Core.Services;
using Ninject;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Botje.Messaging.Services
{
    /// <summary>
    /// This class should of course not be part of the messaging library, but we'll allow it for now...
    /// 
    /// 
    /// </summary>
    public class GoogleAddressService : ILocationToAddressService
    {
        [Inject]
        public ILoggerFactory LoggerFactory { set { _log = value.Create(GetType()); } }

        private string _googleApiKey;
        private ILogger _log;

        /// <summary>
        /// We need a key!
        /// </summary>
        /// <param name="key"></param>
        public void SetApiKey(string key)
        {
            _googleApiKey = key;
        }

        /// <summary>
        /// For a given latitude and longitude, will ask Google for the 
        /// </summary>
        /// <param name="latitude"></param>
        /// <param name="longitude"></param>
        /// <returns></returns>
        public Task<string> GetAddress(float latitude, float longitude)
        {
            _log.Trace($"Lookup address using http://maps.googleapis.com/maps/api/geocode/json?latlng={latitude.ToString(CultureInfo.InvariantCulture)},{longitude.ToString(CultureInfo.InvariantCulture)}...");

            Task<string> t = new Task<string>(() =>
            {
                Semaphore sem = new Semaphore(0, 1);
                string result = null;
                var client = new RestClient("https://maps.googleapis.com");
                var request = new RestRequest("maps/api/geocode/json", Method.GET);
                request.AddQueryParameter("latlng", $"{latitude.ToString(CultureInfo.InvariantCulture)},{longitude.ToString(CultureInfo.InvariantCulture)}");
                request.AddQueryParameter("key", _googleApiKey);
                request.AddQueryParameter("language", "nl");
                request.AddQueryParameter("result_type", "street_address|intersection|premise|park|point_of_interest");
                var asyncHandle = client.ExecuteAsync<MapsGeocodeResponse>(request, response =>
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        result = response.Data?.results.FirstOrDefault()?.formatted_address;
                        if (string.IsNullOrWhiteSpace(result))
                        {
                            _log.Info($"No address for these coordinates. Status: {response.Data?.status}");
                            result = null;
                        }
                    }
                    else
                    {
                        _log.Error($"Error loading address using http://maps.googleapis.com/maps/api/geocode/json?latlng={latitude.ToString(CultureInfo.InvariantCulture)},{longitude.ToString(CultureInfo.InvariantCulture)}: {response.StatusCode} / {response.StatusDescription}");

                        result = null;
                    }
                    _log.Trace($"... lookup \"{latitude.ToString(CultureInfo.InvariantCulture)},{longitude.ToString(CultureInfo.InvariantCulture)}\" => \"{result}\" with status \"{response.Data.status}\"");
                    sem.Release();
                });
                if (!sem.WaitOne(TimeSpan.FromSeconds(30)))
                {
                    _log.Error("Error looking up address, timeout.");
                }
                return result;
            });
            t.Start();
            return t;
        }

        /// <summary>
        /// Helps serialization
        /// </summary>
        public class MapsGeocodeResponse
        {
            public List<MapsAddress> results { get; set; }

            public string status { get; set; }
        }

        /// <summary>
        /// Helps serialization
        /// </summary>
        public class MapsAddress
        {
            public string formatted_address { get; set; }
        }
    }
}
