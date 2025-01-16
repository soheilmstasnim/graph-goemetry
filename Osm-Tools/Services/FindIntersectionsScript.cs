using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using Osm_Tools.Extensions;

namespace Osm_Tools.Services;
internal static class FindIntersectionsScript {

    public static void FindIntersectionsInFile(string inputFile) {


        string outputIntersectionsFile = "intersections.geojson";
        string outputSplitLinesFile = "split_lines.geojson";

        // Read LineStrings from GeoJSON
        var geoJsonReader = new GeoJsonReader();
        var geoJsonWriter = new GeoJsonWriter();

        var features = geoJsonReader.Read<FeatureCollection>(File.ReadAllText(inputFile));
        var lineStrings = new List<LineString>();

        // Collect all LineStrings
        foreach (var geom in features) {
            if (geom.Geometry is LineString lineString) {
                lineStrings.Add(lineString);
            }
        }

        // Find intersection points
        var intersectionPoints = new List<Point>();
        for (int i = 0; i < lineStrings.Count; i++) {
            for (int j = i + 1; j < lineStrings.Count; j++) {
                var intersection = lineStrings[i].Intersection(lineStrings[j]);
                if (intersection is Point point) {
                    intersectionPoints.Add(point);
                }
                else if (intersection is MultiPoint multiPoint) {
                    foreach (var geom in multiPoint.Geometries) {
                        if (geom is Point pt) {
                            intersectionPoints.Add(pt);
                        }
                    }
                }
            }
        }

        // Split LineStrings at intersection points
        var splitLines = new List<LineString>();
        foreach (var line in lineStrings) {
            var currentLine = line;
            foreach (var point in intersectionPoints) {
                if (currentLine.Intersects(point)) {
                    var result = currentLine.CutLineOnNearestPoint(point);
                    if (result.Count() > 1) {
                        splitLines.AddRange(result);
                        break;
                    }
                }
            }
        }

        // Write intersections to GeoJSON
        var intersectionsGeometry = new MultiPoint(intersectionPoints.ToArray());
        var intersectionsGeoJson = geoJsonWriter.Write(intersectionsGeometry);
        File.WriteAllText(outputIntersectionsFile, intersectionsGeoJson);

        // Write split lines to GeoJSON
        var splitLinesGeometry = new MultiLineString(splitLines.ToArray());
        var splitLinesGeoJson = geoJsonWriter.Write(splitLinesGeometry);
        File.WriteAllText(outputSplitLinesFile, splitLinesGeoJson);

        Console.WriteLine("Processing complete. Results saved:");
        Console.WriteLine($"Intersections: {outputIntersectionsFile}");
        Console.WriteLine($"Split lines: {outputSplitLinesFile}");
    }

}


