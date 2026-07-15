namespace EarthquakeNotifier.Tests.Fixtures
{
    /// <summary>
    /// Sample SeismicPortal FDSN JSON API responses for unit testing.
    /// Field names match the real API: "mag", "unid", "lastupdate", "time", "magtype".
    /// </summary>
    public static class SeismicPortalApiResponseFixture
    {
        /// <summary>
        /// Sample response with multiple earthquakes of varying magnitudes.
        /// </summary>
        public static string GetSampleMultipleEarthquakes() => @"{
  ""type"": ""FeatureCollection"",
  ""features"": [
    {
      ""type"": ""Feature"",
      ""id"": ""ep2024_003456"",
      ""properties"": {
        ""mag"": 5.3,
        ""flynn_region"": ""40 km NW of Larissa, Greece"",
        ""lastupdate"": ""2024-01-15T10:30:00Z"",
        ""time"": ""2024-01-15T10:29:00Z"",
        ""unid"": ""ep2024_003456"",
        ""magtype"": ""mb"",
        ""lat"": 39.2,
        ""lon"": 22.5,
        ""depth"": 28.5
      },
      ""geometry"": {
        ""type"": ""Point"",
        ""coordinates"": [22.5, 39.2]
      }
    },
    {
      ""type"": ""Feature"",
      ""id"": ""ep2024_003455"",
      ""properties"": {
        ""mag"": 4.2,
        ""flynn_region"": ""Central Mid-Atlantic Ridge"",
        ""lastupdate"": ""2024-01-15T09:45:00Z"",
        ""time"": ""2024-01-15T09:44:00Z"",
        ""unid"": ""ep2024_003455"",
        ""magtype"": ""mb"",
        ""lat"": 0.5,
        ""lon"": -30.1,
        ""depth"": 10.0
      },
      ""geometry"": {
        ""type"": ""Point"",
        ""coordinates"": [-30.1, 0.5]
      }
    },
    {
      ""type"": ""Feature"",
      ""id"": ""ep2024_003454"",
      ""properties"": {
        ""mag"": 3.9,
        ""flynn_region"": ""Southern Italy"",
        ""lastupdate"": ""2024-01-15T08:20:00Z"",
        ""time"": ""2024-01-15T08:19:00Z"",
        ""unid"": ""ep2024_003454"",
        ""magtype"": ""ml"",
        ""lat"": 38.2,
        ""lon"": 16.8,
        ""depth"": 35.0
      },
      ""geometry"": {
        ""type"": ""Point"",
        ""coordinates"": [16.8, 38.2]
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
      ""id"": ""ep2024_003457"",
      ""properties"": {
        ""mag"": 6.1,
        ""flynn_region"": ""50 km E of Istanbul, Turkey"",
        ""lastupdate"": ""2024-01-15T11:15:00Z"",
        ""time"": ""2024-01-15T11:14:00Z"",
        ""unid"": ""ep2024_003457"",
        ""magtype"": ""mb"",
        ""lat"": 41.0,
        ""lon"": 30.5,
        ""depth"": 15.0
      },
      ""geometry"": {
        ""type"": ""Point"",
        ""coordinates"": [30.5, 41.0]
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
      invalid json structure here
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
      ""id"": ""ep2024_003458"",
      ""properties"": {
        ""flynn_region"": ""Unknown location"",
        ""lastupdate"": ""2024-01-15T10:00:00Z"",
        ""time"": ""2024-01-15T10:00:00Z"",
        ""unid"": ""ep2024_003458"",
        ""magtype"": ""mb"",
        ""lat"": 0.0,
        ""lon"": 0.0,
        ""depth"": 20.0
      },
      ""geometry"": {
        ""type"": ""Point"",
        ""coordinates"": [0.0, 0.0]
      }
    }
  ]
}";

        /// <summary>
        /// Sample response with missing coordinates (invalid geometry).
        /// </summary>
        public static string GetSampleResponseWithInvalidGeometry() => @"{
  ""type"": ""FeatureCollection"",
  ""features"": [
    {
      ""type"": ""Feature"",
      ""id"": ""ep2024_003459"",
      ""properties"": {
        ""mag"": 4.5,
        ""flynn_region"": ""Test location"",
        ""lastupdate"": ""2024-01-15T10:00:00Z"",
        ""time"": ""2024-01-15T10:00:00Z"",
        ""unid"": ""ep2024_003459"",
        ""magtype"": ""mb"",
        ""depth"": 20.0
      },
      ""geometry"": {
        ""type"": ""Point"",
        ""coordinates"": []
      }
    }
  ]
}";
    }
}
