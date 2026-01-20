namespace GexVisor.UI.Models;

/// <summary>
/// Year-by-year data for Paper 2's "Gradual Adoption" findings.
/// Tracks the evolution from fragmented (2020) to structural regime (2024+).
/// Note: Data through 2025 is based on historical observations.
/// 2026 data pending full-year analysis completion.
/// </summary>
public record YearData(
    int Year,
    double DetectionRate,    // 3.7% - 100%
    double Magnitude,        // 2.8 - 23.0 ($B)
    double Persistence,      // 0.5 - 0.99
    string Volatility,       // High/Med/Low/Spike
    string Description       // Fragmented/Borderline/Adoption/Turbulence/Regime/Sustained
);

public static class Paper2Timeline
{
    /// <summary>
    /// Historical research data from 2020-2025.
    /// Detection rates and GEX magnitude measurements are empirically derived.
    /// </summary>
    public static readonly YearData[] Data =
    [
        new(2020, 12.1, 2.8, 0.6, "High", "Fragmented"),
        new(2021, 3.7, 4.9, 0.5, "Low", "Borderline"),
        new(2022, 32.4, 8.5, 0.75, "Med", "Adoption"),
        new(2023, 20.2, 12.0, 0.65, "Spike", "Turbulence (Dip)"),
        new(2024, 100.0, 20.3, 0.98, "Low", "Structural Regime"),
        new(2025, 100.0, 23.0, 0.99, "Low", "Sustained") // Historical: Full-year 2025 data
    ];

    public static readonly int[] Years = [2020, 2021, 2022, 2023, 2024, 2025];

    public static YearData GetYearData(int year) =>
        Data.FirstOrDefault(d => d.Year == year) ?? Data[0];
}
