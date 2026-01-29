using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using GexVisor.UI.Configuration;
using Microsoft.AspNetCore.Components;

namespace GexVisor.UI.Models;

/// <summary>
/// Static helper methods for radar visualization positioning and calculations.
/// </summary>
public static partial class RadarHelpers
{
    /// <summary>
    /// Calculates the (x, y) SVG coordinate for a research path node on the radar.
    /// </summary>
    /// <param name="path">The research path to position.</param>
    /// <returns>A tuple of (X, Y) coordinates in SVG space.</returns>
    public static (double X, double Y) GetNodePosition(ResearchPath path)
    {
        var angles = GetQuadrantAngles(path.Quadrant);

        var angleRange = angles.End - angles.Start;

        // Normalize angle: treat path.Angle as 0-90 representing position within quadrant (0-100%)
        // Clamp to prevent overflow and avoid edge collisions
        var normalizedPosition = Math.Clamp(path.Angle / 90.0, 0, 1);
        var normalizedAngle = angles.Start + normalizedPosition * angleRange;
        var rad = (normalizedAngle - 90) * Math.PI / 180;

        var innerR = AppConstants.ResearchRadar.RingRadii[path.Ring];
        var outerR = path.Ring + 1 < AppConstants.ResearchRadar.RingRadii.Length
            ? AppConstants.ResearchRadar.RingRadii[path.Ring + 1]
            : AppConstants.ResearchRadar.RingRadii[path.Ring] + AppConstants.ResearchRadar.DefaultOuterRingWidth;

        var r = (innerR + outerR) / 2.0;
        return (r * Math.Cos(rad), r * Math.Sin(rad));
    }

    /// <summary>
    /// Formats a double value for SVG attribute usage with invariant culture.
    /// </summary>
    public static string FormatSvg(double value) =>
        value.ToString("F1", System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>
    /// Gets the start and end angles for a specific research quadrant.
    /// </summary>
    public static (int Start, int End) GetQuadrantAngles(ResearchQuadrant quadrant) => quadrant switch
    {
        ResearchQuadrant.Data => AppConstants.ResearchRadar.QuadrantData,
        ResearchQuadrant.Knowledge => AppConstants.ResearchRadar.QuadrantKnowledge,
        ResearchQuadrant.Scope => AppConstants.ResearchRadar.QuadrantScope,
        ResearchQuadrant.Methodology => AppConstants.ResearchRadar.QuadrantMethodology,
        ResearchQuadrant.Compute => AppConstants.ResearchRadar.QuadrantCompute,
        _ => (0, 90)
    };

    /// <summary>
    /// Regex for splitting PascalCase enum names into words.
    /// Matches capital letters that are not at the start of the string.
    /// </summary>
    [GeneratedRegex("(?<!^)([A-Z])")]
    private static partial Regex EnumNameSplitter();

    // Cache for formatted enum names to avoid repeated regex operations during rendering
    private static readonly ConcurrentDictionary<Enum, string> _enumNameCache = new();

    /// <summary>
    /// Formats an enum value into a readable string (e.g., "InProgress" -> "In Progress").
    /// </summary>
    public static string FormatEnumName<T>(T enumValue) where T : Enum
    {
        return _enumNameCache.GetOrAdd(enumValue, e =>
        {
            return EnumNameSplitter().Replace(e.ToString(), " $1");
        });
    }

    /// <summary>Shared access to ring radii configuration.</summary>
    public static int[] RingRadii => AppConstants.ResearchRadar.RingRadii;

    /// <summary>
    /// Creates a MarkupString for an SVG text element with proper encoding.
    /// Includes inline attributes as fallbacks for when CSS isolation breaks ::deep selectors.
    /// </summary>
    public static MarkupString CreateSvgText(
        string x, string y, string className, string content,
        string? fill = null, string? transform = null,
        string textAnchor = "middle", string? dominantBaseline = null)
    {
        // Default fill to white for visibility on colored backgrounds
        var fillAttr = fill != null ? $" fill=\"{fill}\"" : " fill=\"#fff\"";
        var transformAttr = transform != null ? $" transform=\"{transform}\"" : "";
        var anchorAttr = $" text-anchor=\"{textAnchor}\"";
        var baselineAttr = dominantBaseline != null ? $" dominant-baseline=\"{dominantBaseline}\"" : "";
        var encodedContent = System.Net.WebUtility.HtmlEncode(content);
        return new MarkupString(
            $"<text x=\"{x}\" y=\"{y}\" class=\"{className}\"{fillAttr}{transformAttr}{anchorAttr}{baselineAttr}>{encodedContent}</text>");
    }
}
