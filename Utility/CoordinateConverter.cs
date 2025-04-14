using CoordinateSharp;
namespace Road_Infrastructure_Asset_Management.Utility
{
    public static class CoordinateConverter
    {
        public static object ConvertGeometryToWGS84(object geometry, int utmZone = 48)
        {
            if (geometry is IDictionary<string, object> geoJson && geoJson.TryGetValue("coordinates", out var coordsObj))
            {
                string type = geoJson["type"].ToString();
                switch (type)
                {
                    case "Point":
                        var pointCoords = coordsObj as double[];
                        if (pointCoords != null && pointCoords.Length == 2)
                        {
                            var utm = new UniversalTransverseMercator("N", utmZone, pointCoords[0], pointCoords[1]);
                            var coord = UniversalTransverseMercator.ConvertUTMtoLatLong(utm);
                            geoJson["coordinates"] = new double[] { coord.Longitude.DecimalDegree, coord.Latitude.DecimalDegree };
                        }
                        break;

                    case "LineString":
                        var lineCoords = coordsObj as double[][];
                        if (lineCoords != null)
                        {
                            var convertedLine = new double[lineCoords.Length][];
                            for (int i = 0; i < lineCoords.Length; i++)
                            {
                                var utm = new UniversalTransverseMercator("N", utmZone, lineCoords[i][0], lineCoords[i][1]);
                                var coord = UniversalTransverseMercator.ConvertUTMtoLatLong(utm);
                                convertedLine[i] = new double[] { coord.Longitude.DecimalDegree, coord.Latitude.DecimalDegree };
                            }
                            geoJson["coordinates"] = convertedLine;
                        }
                        break;

                    case "Polygon":
                        var polyCoords = coordsObj as double[][][];
                        if (polyCoords != null)
                        {
                            var convertedPoly = new double[polyCoords.Length][][];
                            for (int i = 0; i < polyCoords.Length; i++)
                            {
                                convertedPoly[i] = new double[polyCoords[i].Length][];
                                for (int j = 0; j < polyCoords[i].Length; j++)
                                {
                                    var utm = new UniversalTransverseMercator("N", utmZone, polyCoords[i][j][0], polyCoords[i][j][1]);
                                    var coord = UniversalTransverseMercator.ConvertUTMtoLatLong(utm);
                                    convertedPoly[i][j] = new double[] { coord.Longitude.DecimalDegree, coord.Latitude.DecimalDegree };
                                }
                            }
                            geoJson["coordinates"] = convertedPoly;
                        }
                        break;
                }
            }
            return geometry;
        }

    }
}
