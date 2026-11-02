using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace PittBradford.XR
{
    /// <summary>
    /// Live local weather from Open-Meteo (free, no API key). Used by the outdoor scenarios to set wind and temperature.
    /// If the network is unavailable the scenario falls back to believable simulated values and says so.
    /// </summary>
    public static class Weather
    {
        public struct Reading
        {
            public bool Live;
            public float TemperatureF, WindMph, WindFromDegrees;
            public string Source;
        }

        [Serializable] class Response { public Current current; }
        [Serializable] class Current { public float temperature_2m; public float wind_speed_10m; public float wind_direction_10m; }

        public static Reading Fallback => new Reading { Live = false, TemperatureF = 54f, WindMph = 9f, WindFromDegrees = 250f, Source = "simulated (no network)" };

        // Bradford, Pennsylvania
        public static IEnumerator Fetch(Action<Reading> done, double latitude = 41.9559, double longitude = -78.6439)
        {
            // -pbOfflineWeather on the command line skips the network (repeatable screenshot runs and comparisons)
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-pbOfflineWeather") >= 0)
            {
                done(Fallback);
                yield break;
            }
            string url =$"https://api.open-meteo.com/v1/forecast?latitude={latitude:F4}&longitude={longitude:F4}" +
                         "&current=temperature_2m,wind_speed_10m,wind_direction_10m&temperature_unit=fahrenheit&wind_speed_unit=mph";
            using (var req = UnityWebRequest.Get(url))
            {
                req.timeout = 6;
                yield return req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        var r = JsonUtility.FromJson<Response>(req.downloadHandler.text);
                        done(new Reading { Live = true, TemperatureF = r.current.temperature_2m, WindMph = r.current.wind_speed_10m, WindFromDegrees = r.current.wind_direction_10m, Source = "Open-Meteo, Bradford PA (live)" });
                        yield break;
                    }
                    catch (Exception e) { Debug.LogWarning("Weather parse failed: " + e.Message); }
                }
            }
            done(Fallback);
        }
    }
}
