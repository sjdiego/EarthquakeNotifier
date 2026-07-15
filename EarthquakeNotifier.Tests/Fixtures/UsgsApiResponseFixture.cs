namespace EarthquakeNotifier.Tests.Fixtures
{
    /// <summary>
    /// Sample USGS GeoJSON API responses for unit testing.
    /// </summary>
    public static class UsgsApiResponseFixture
    {
        /// <summary>
        /// Sample response with multiple earthquakes of varying magnitudes.
        /// </summary>
        public static string GetSampleMultipleEarthquakes() => @"{
  ""type"": ""FeatureCollection"",
  ""features"": [
    {
      ""type"": ""Feature"",
      ""id"": ""us7000kp58"",
      ""properties"": {
        ""mag"": 5.2,
        ""place"": ""20 km WSW of Iquique, Chile"",
        ""time"": 1705325400000,
        ""url"": ""https://earthquake.usgs.gov/earthquakes/eventpage/us7000kp58/executive""
      },
      ""geometry"": {
        ""type"": ""Point"",
        ""coordinates"": [-70.3, -20.3, 35.2]
      }
    },
    {
      ""type"": ""Feature"",
      ""id"": ""us7000kp56"",
      ""properties"": {
        ""mag"": 4.1,
        ""place"": ""Pacific-Antarctic Ridge"",
        ""time"": 1705320000000,
        ""url"": ""https://earthquake.usgs.gov/earthquakes/eventpage/us7000kp56/executive""
      },
      ""geometry"": {
        ""type"": ""Point"",
        ""coordinates"": [-127.5, -56.8, 10.0]
      }
    },
    {
      ""type"": ""Feature"",
      ""id"": ""us7000kp54"",
      ""properties"": {
        ""mag"": 3.8,
        ""place"": ""13 km S of Kailua-Kona, Hawaii"",
        ""time"": 1705315000000,
        ""url"": ""https://earthquake.usgs.gov/earthquakes/eventpage/us7000kp54/executive""
      },
      ""geometry"": {
        ""type"": ""Point"",
        ""coordinates"": [-155.5, 19.4, 5.0]
      }
    }
  ]
}";

        /// <summary>
        /// Sample response with no earthquakes.
        /// </summary>
        public static string GetSampleEmptyResponse() => @"{
  ""type"": ""FeatureCollection"",
  ""features"": []
}";

        /// <summary>
        /// Sample response with a single earthquake.
        /// </summary>
        public static string GetSampleSingleEarthquake() => @"{
  ""type"": ""FeatureCollection"",
  ""features"": [
    {
      ""type"": ""Feature"",
      ""id"": ""us7000kp60"",
      ""properties"": {
        ""mag"": 6.5,
        ""place"": ""39 km ENE of San Francisco, California"",
        ""time"": 1705330000000,
        ""url"": ""https://earthquake.usgs.gov/earthquakes/eventpage/us7000kp60/executive""
      },
      ""geometry"": {
        ""type"": ""Point"",
        ""coordinates"": [-121.8, 37.8, 12.5]
      }
    }
  ]
}";

        /// <summary>
        /// Sample malformed response (invalid JSON).
        /// </summary>
        public static string GetSampleMalformedResponse() => @"{
  ""type"": ""FeatureCollection"",
  ""features"": [
    {
      ""type"": ""Feature"",
      invalid json here
    }
  ]
}";

        /// <summary>
        /// Sample response with missing magnitude field.
        /// </summary>
        public static string GetSampleResponseWithMissingMagnitude() => @"{
  ""type"": ""FeatureCollection"",
  ""features"": [
    {
      ""type"": ""Feature"",
      ""id"": ""us7000kp61"",
      ""properties"": {
        ""place"": ""Unknown location"",
        ""time"": 1705330000000,
        ""url"": ""https://earthquake.usgs.gov/earthquakes/eventpage/us7000kp61/executive""
      },
      ""geometry"": {
        ""type"": ""Point"",
        ""coordinates"": [-122.0, 37.0, 10.0]
      }
    }
  ]
}";
    }
}
