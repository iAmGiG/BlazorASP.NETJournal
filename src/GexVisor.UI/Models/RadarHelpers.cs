namespace GexVisor.UI.Models;

/// <summary>
/// Static helper methods for radar visualization positioning and calculations.
/// </summary>
public static class RadarHelpers
{
    /// <summary>
    /// Calculates the (x, y) SVG coordinate for a research path node on the radar.
    /// </summary>
    /// <param name="path">The research path to position.</param>
    /// <returns>A tuple of (X, Y) coordinates in SVG space.</returns>
    public static (double X, double Y) GetNodePosition(ResearchPath path)
    {
        var angles = RadarConstants.QuadrantAngles.GetValueOrDefault(
            path.Quadrant,
            (Start: 0, End: 90));

        var angleRange = angles.End - angles.Start;
        var normalizedAngle = angles.Start + (path.Angle / 180.0) * angleRange;
        var rad = (normalizedAngle - 90) * Math.PI / 180;

        var innerR = RadarConstants.RingRadii[path.Ring];
        var outerR = path.Ring + 1 < RadarConstants.RingRadii.Length
            ? RadarConstants.RingRadii[path.Ring + 1]
            : RadarConstants.RingRadii[path.Ring] + 50;

        var r = (innerR + outerR) / 2.0;
        return (r * Math.Cos(rad), r * Math.Sin(rad));
    }

    /// <summary>
    /// Formats a double value for SVG attribute usage with invariant culture.
    /// </summary>
    public static string FormatSvg(double value) =>
        value.ToString("F1", System.Globalization.CultureInfo.InvariantCulture);
}
