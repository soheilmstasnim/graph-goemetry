using NetTopologySuite;
using NetTopologySuite.Algorithm;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Distance;

namespace Osm_Tools.Extensions;
public static class Helper {
    public static LineString[] CutLineOnNearestPoint(this LineString lineString, Point point) {

        try {
            var projectedPoint = ProjectPointOntoLineString(lineString, point);

            var myPoints = new List<PointSegmentDistance>();

            var lineCoordinates = lineString.Coordinates;
            for (byte i = 0; i < lineString.Coordinates.Length; i++) {
                if (i + 1 == lineCoordinates.Length) {
                    break;
                }

                var currentLinePoint = lineString.Coordinates[i];
                var nextLinePoint = lineString.Coordinates[i + 1];
                var distance = Convert.ToDecimal(DistanceComputer.PointToLinePerpendicular(projectedPoint.Coordinate, currentLinePoint, nextLinePoint));
                myPoints.Add(new PointSegmentDistance(distance, i));
            }

            var linePerfectIndex = myPoints.OrderBy(_ => _.Distance).First();

            var pointInLineIndex = linePerfectIndex.Index + 1;


            var combinedList = lineString.Coordinates.ToList();
            combinedList.Insert(pointInLineIndex, projectedPoint.Coordinate);


            var line1Coords = combinedList.ToArray()[0..(pointInLineIndex + 1)];
            var line2Coords = combinedList.ToArray()[pointInLineIndex..^0];

            var line1 = new LineString(line1Coords.ToArray().ConvertToCoordinateSequence(), NtsGeometryServices.Instance.CreateGeometryFactory(GeometryStandardConstraints.SridMax4326));
            var line2 = new LineString(line2Coords.ToArray().ConvertToCoordinateSequence(), NtsGeometryServices.Instance.CreateGeometryFactory(GeometryStandardConstraints.SridMax4326));
            var result = new LineString[] { line1, line2 };

            return result;
        }
        catch (Exception ex) {
            throw new Exception("Cut line went wrong", ex);
        }

    }

    public static Point ProjectPointOntoLineString(LineString lineString, Point point) {

        var precisionModel = new PrecisionModel(100000000000000); // Use a non-zero scale factor
        var geometryFactory = new GeometryFactory(precisionModel, 4326);

        // Find the nearest point on the LineString to the given point
        var distanceOp = new DistanceOp(lineString, point);
        var nearestPoints = distanceOp.NearestPoints();
        return geometryFactory.CreatePoint(nearestPoints[0]);
    }

    public static CoordinateSequence ConvertToCoordinateSequence(this Coordinate[] coordinates) {

        if (coordinates is null) {
            throw new Exception("Coordinates is null and can not convert to sequence");
        }

        var factory = new GeometryFactory();
        var sequence = factory.CoordinateSequenceFactory.Create(coordinates);

        return sequence;
    }
    static List<LineString> SplitLineStringAtPoints(LineString line, List<IFeature> points) {
        var segments = new List<LineString>();
        var coordinates = new List<Coordinate>(line.Coordinates);

        // Sort points along the LineString
        points.Sort((f1, f2) => {
            var d1 = line.Project(((Point)f1.Geometry).Coordinate);
            var d2 = line.Project(((Point)f2.Geometry).Coordinate);
            return d1.CompareTo(d2);
        });

        var splitIndices = new HashSet<int>();
        foreach (var feature in points) {
            var point = (Point)feature.Geometry;
            if (line.Contains(point)) {
                int index = FindNearestCoordinateIndex(coordinates, point.Coordinate);
                splitIndices.Add(index);
            }
        }

        // Split the LineString at the specified indices
        int startIndex = 0;
        foreach (int splitIndex in splitIndices) {
            if (splitIndex > startIndex) {
                var segment = CreateSegment(coordinates, startIndex, splitIndex);
                if (segment != null) {
                    segments.Add(segment);
                }

                startIndex = splitIndex;
            }
        }

        // Add the last segment
        var finalSegment = CreateSegment(coordinates, startIndex, coordinates.Count - 1);
        if (finalSegment != null) {
            segments.Add(finalSegment);
        }

        return segments;
    }

    //Helper to create a LineString segment
    static LineString CreateSegment(List<Coordinate> coordinates, int start, int end) {
        if (start >= end || start < 0 || end >= coordinates.Count) {
            return null;
        }

        var segmentCoordinates = coordinates.GetRange(start, end - start + 1).ToArray();
        return new LineString(segmentCoordinates);
    }

    // Helper to find the nearest coordinate index in a list
    static int FindNearestCoordinateIndex(List<Coordinate> coordinates, Coordinate target) {
        int nearestIndex = -1;
        double minDistance = double.MaxValue;

        for (int i = 0; i < coordinates.Count; i++) {
            double distance = coordinates[i].Distance(target);
            if (distance < minDistance) {
                minDistance = distance;
                nearestIndex = i;
            }
        }

        return nearestIndex;
    }

    // Helper to create a feature with properties
    static IFeature CreateFeature(Geometry geometry, IDictionary<string, object> properties) {
        var feature = new Feature(geometry, new AttributesTable());
        foreach (var kvp in properties) {
            feature.Attributes.Add(kvp.Key, kvp.Value);
        }
        return feature;
    }
    public static double CalculateProjection(LineString line, Coordinate point) {
        double cumulativeLength = 0.0;

        for (int i = 0; i < line.Coordinates.Length - 1; i++) {
            var segmentStart = line.Coordinates[i];
            var segmentEnd = line.Coordinates[i + 1];
            var distance = new DistanceOp(line, new Point(point));
            distance.
            if (new LineSegment(segmentStart, segmentEnd)..Contains(point)) {
                cumulativeLength += segmentStart.Distance(point);
                break;
            }

            cumulativeLength += segmentStart.Distance(segmentEnd);
        }

        return cumulativeLength;
    }
}
public static class GeometryStandardConstraints {
    public const int SridMax4326 = 4326;
    public const int SridMax2855 = 2855;
}
record PointSegmentDistance(decimal Distance, byte Index);
