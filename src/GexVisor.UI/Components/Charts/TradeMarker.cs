using System.Globalization;
using ApexCharts;
using GexVisor.Core;
using GexVisor.UI.Configuration;

namespace GexVisor.UI.Components.Charts;

/// <summary>
/// Helper class for building ApexCharts annotations for trade entry/exit markers.
/// </summary>
public static class TradeMarker
{
    // Colors from centralized constants
    private const string LongColor = AppConstants.ChartColors.LongColor;
    private const string ShortColor = AppConstants.ChartColors.ShortColor;

    /// <summary>
    /// Build annotations for a trade showing entry and exit points.
    /// </summary>
    public static Annotations BuildTradeAnnotations(OptionsLog trade)
    {
        var annotations = new Annotations
        {
            Points = new List<AnnotationsPoint>()
        };

        // Entry marker
        if (trade.CreatedDate.HasValue && trade.EntryPrice > 0)
        {
            annotations.Points.Add(BuildEntryMarker(trade));
        }

        // Exit marker (only if trade is closed)
        if (trade.ExitPrice.HasValue && trade.ExitPrice > 0)
        {
            annotations.Points.Add(BuildExitMarker(trade));
        }

        return annotations;
    }

    /// <summary>
    /// Build the entry point annotation.
    /// </summary>
    public static AnnotationsPoint BuildEntryMarker(OptionsLog trade)
    {
        var isLong = trade.TradeDirection == TradeLog.Type.Long;
        var color = isLong ? LongColor : ShortColor;
        var label = GetEntryLabel(trade.OptionTradeType);

        return new AnnotationsPoint
        {
            X = trade.CreatedDate?.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture),
            Y = (double)trade.EntryPrice,
            Marker = new AnnotationMarker
            {
                Size = 8,
                FillColor = color,
                StrokeColor = "#ffffff",
                StrokeWidth = 2,
                Shape = AnnotationMarkerShape.Circle
            },
            Label = new Label
            {
                Text = label,
                Style = new Style
                {
                    Background = color,
                    Color = "#ffffff",
                    FontSize = "11px",
                    Padding = new Padding { Left = 5, Right = 5, Top = 2, Bottom = 2 }
                },
                OffsetY = -15
            }
        };
    }

    /// <summary>
    /// Build the exit point annotation.
    /// </summary>
    public static AnnotationsPoint BuildExitMarker(OptionsLog trade)
    {
        var pnl = trade.CalculatePnL();
        var isProfitable = pnl.HasValue && pnl.Value >= 0;
        var color = isProfitable ? LongColor : ShortColor;
        var label = GetExitLabel(trade.OptionTradeType, pnl);

        // Use TargetCompletionDate as exit date, or fall back to a reasonable default
        var exitDate = trade.TargetCompletionDate ?? trade.CreatedDate?.AddDays(1);

        return new AnnotationsPoint
        {
            X = exitDate?.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture),
            Y = (double)(trade.ExitPrice ?? 0),
            Marker = new AnnotationMarker
            {
                Size = 8,
                FillColor = color,
                StrokeColor = "#ffffff",
                StrokeWidth = 2,
                Shape = AnnotationMarkerShape.Circle
            },
            Label = new Label
            {
                Text = label,
                Style = new Style
                {
                    Background = color,
                    Color = "#ffffff",
                    FontSize = "11px",
                    Padding = new Padding { Left = 5, Right = 5, Top = 2, Bottom = 2 }
                },
                OffsetY = -15
            }
        };
    }

    /// <summary>
    /// Build Y-axis annotation for entry price level.
    /// </summary>
    public static AnnotationsYAxis BuildEntryPriceLine(OptionsLog trade)
    {
        var isLong = trade.TradeDirection == TradeLog.Type.Long;
        var color = isLong ? LongColor : ShortColor;

        return new AnnotationsYAxis
        {
            Y = (double)trade.EntryPrice,
            BorderColor = color,
            BorderWidth = 1,
            StrokeDashArray = 4,
            Label = new Label
            {
                Text = $"Entry ${trade.EntryPrice:F2}",
                Style = new Style
                {
                    Background = color,
                    Color = "#ffffff",
                    FontSize = "10px"
                },
                Position = LabelPosition.Left
            }
        };
    }

    /// <summary>
    /// Build Y-axis annotation for exit price level.
    /// </summary>
    public static AnnotationsYAxis? BuildExitPriceLine(OptionsLog trade)
    {
        if (!trade.ExitPrice.HasValue) return null;

        var pnl = trade.CalculatePnL();
        var isProfitable = pnl.HasValue && pnl.Value >= 0;
        var color = isProfitable ? LongColor : ShortColor;

        return new AnnotationsYAxis
        {
            Y = (double)trade.ExitPrice.Value,
            BorderColor = color,
            BorderWidth = 1,
            StrokeDashArray = 4,
            Label = new Label
            {
                Text = $"Exit ${trade.ExitPrice:F2}",
                Style = new Style
                {
                    Background = color,
                    Color = "#ffffff",
                    FontSize = "10px"
                },
                Position = LabelPosition.Left
            }
        };
    }

    private static string GetEntryLabel(OptionsLog.TradeType tradeType)
    {
        return tradeType switch
        {
            OptionsLog.TradeType.BTO => "BTO",
            OptionsLog.TradeType.STO => "STO",
            OptionsLog.TradeType.BTC => "BTC",
            OptionsLog.TradeType.STC => "STC",
            _ => "Entry"
        };
    }

    private static string GetExitLabel(OptionsLog.TradeType tradeType, decimal? pnl)
    {
        var exitType = tradeType switch
        {
            OptionsLog.TradeType.BTO => "STC", // Close long with sell-to-close
            OptionsLog.TradeType.STO => "BTC", // Close short with buy-to-close
            OptionsLog.TradeType.BTC => "STO", // Reverse
            OptionsLog.TradeType.STC => "BTO", // Reverse
            _ => "Exit"
        };

        if (pnl.HasValue)
        {
            var sign = pnl.Value >= 0 ? "+" : "";
            return $"{exitType} ({sign}${pnl.Value:F0})";
        }

        return exitType;
    }
}
