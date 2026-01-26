// Copyright (c) GexVisor. All rights reserved.

using FluentAssertions;
using GexVisor.Core;
using GexVisor.UI.Configuration;
using GexVisor.UI.Models;
using GexVisor.UI.Services;
using Moq;

namespace GexVisor.UI.Tests.Services;

/// <summary>
/// Unit tests for AlertService.
/// Tests alert triggering, cooldowns, preferences, and condition detection.
/// </summary>
public class AlertServiceTests : IDisposable
{
    private readonly Mock<IGexStateService> _mockGexState;
    private readonly Mock<ILocalStorageService> _mockStorage;
    private readonly AlertService _service;

    public AlertServiceTests()
    {
        _mockGexState = new Mock<IGexStateService>();
        _mockStorage = new Mock<ILocalStorageService>();
        _service = new AlertService(_mockGexState.Object, _mockStorage.Object);
    }

    public void Dispose()
    {
        _service.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void StartMonitoring_SubscribesToStateChanges()
    {
        // Act
        _service.StartMonitoring();

        // Assert
        _service.IsMonitoring.Should().BeTrue();
    }

    [Fact]
    public void StopMonitoring_UnsubscribesFromStateChanges()
    {
        // Arrange
        _service.StartMonitoring();

        // Act
        _service.StopMonitoring();

        // Assert
        _service.IsMonitoring.Should().BeFalse();
    }

    [Fact]
    public void CheckConditions_DetectsRegimeChange()
    {
        // Arrange
        var initialData = CreateGexData(GexRegime.LongGamma, spotPrice: 450m, totalGex: 3m);
        var changedData = CreateGexData(GexRegime.ShortGamma, spotPrice: 450m, totalGex: 3m);

        _mockGexState.SetupSequence(s => s.LiveGexData)
            .Returns(initialData)
            .Returns(changedData);
        _mockGexState.Setup(s => s.UseLiveData).Returns(true);

        Alert? triggeredAlert = null;
        _service.OnAlertTriggered += a => triggeredAlert = a;

        // First check to establish baseline
        _service.StartMonitoring();
        _service.CheckConditions();

        // Act - check with changed regime
        _service.CheckConditions();

        // Assert
        triggeredAlert.Should().NotBeNull();
        triggeredAlert!.Type.Should().Be(AlertType.RegimeChange);
        triggeredAlert.Severity.Should().Be(AlertSeverity.Critical);
        triggeredAlert.Message.Should().Contain("Short γ");
    }

    [Fact]
    public void CheckConditions_DetectsZeroGammaCross()
    {
        // Arrange
        var belowZero = CreateGexData(GexRegime.LongGamma, spotPrice: 448m, totalGex: 2m, zeroGamma: 450m);
        var aboveZero = CreateGexData(GexRegime.LongGamma, spotPrice: 452m, totalGex: 2m, zeroGamma: 450m);

        _mockGexState.SetupSequence(s => s.LiveGexData)
            .Returns(belowZero)
            .Returns(aboveZero);
        _mockGexState.Setup(s => s.UseLiveData).Returns(true);

        Alert? triggeredAlert = null;
        _service.OnAlertTriggered += a => triggeredAlert = a;

        _service.StartMonitoring();
        _service.CheckConditions();

        // Act
        _service.CheckConditions();

        // Assert
        triggeredAlert.Should().NotBeNull();
        triggeredAlert!.Type.Should().Be(AlertType.ZeroGammaCross);
        triggeredAlert.Message.Should().Contain("above zero-gamma");
    }

    [Fact]
    public void CheckConditions_DetectsGexUpperThresholdBreach()
    {
        // Arrange - Set threshold to 5B
        var belowThreshold = CreateGexData(GexRegime.LongGamma, spotPrice: 450m, totalGex: 4m);
        var aboveThreshold = CreateGexData(GexRegime.LongGamma, spotPrice: 450m, totalGex: 6m);

        _mockGexState.SetupSequence(s => s.LiveGexData)
            .Returns(belowThreshold)
            .Returns(aboveThreshold);
        _mockGexState.Setup(s => s.UseLiveData).Returns(true);

        Alert? triggeredAlert = null;
        _service.OnAlertTriggered += a => triggeredAlert = a;

        _service.StartMonitoring();
        _service.CheckConditions();

        // Act
        _service.CheckConditions();

        // Assert
        triggeredAlert.Should().NotBeNull();
        triggeredAlert!.Type.Should().Be(AlertType.GexThreshold);
        triggeredAlert.Message.Should().Contain("exceeded");
    }

    [Fact]
    public void CheckConditions_DoesNotTriggerDuringCooldown()
    {
        // Arrange
        var initialData = CreateGexData(GexRegime.LongGamma, spotPrice: 450m, totalGex: 3m);
        var changedData = CreateGexData(GexRegime.ShortGamma, spotPrice: 450m, totalGex: 3m);
        var changedBackData = CreateGexData(GexRegime.LongGamma, spotPrice: 450m, totalGex: 3m);

        _mockGexState.SetupSequence(s => s.LiveGexData)
            .Returns(initialData)
            .Returns(changedData)
            .Returns(changedBackData);
        _mockGexState.Setup(s => s.UseLiveData).Returns(true);

        var alertCount = 0;
        _service.OnAlertTriggered += _ => alertCount++;

        _service.StartMonitoring();
        _service.CheckConditions();
        _service.CheckConditions(); // First regime change - triggers

        // Act - Second regime change (back to Long) - should be blocked by cooldown
        _service.CheckConditions();

        // Assert
        alertCount.Should().Be(1);
    }

    [Fact]
    public void DismissAlert_RemovesFromActiveAlerts()
    {
        // Arrange
        var data = CreateGexData(GexRegime.LongGamma, spotPrice: 450m, totalGex: 3m);
        var changedData = CreateGexData(GexRegime.ShortGamma, spotPrice: 450m, totalGex: 3m);

        _mockGexState.SetupSequence(s => s.LiveGexData)
            .Returns(data)
            .Returns(changedData);
        _mockGexState.Setup(s => s.UseLiveData).Returns(true);

        _service.StartMonitoring();
        _service.CheckConditions();
        _service.CheckConditions();

        _service.ActiveAlerts.Should().HaveCount(1);
        var alertId = _service.ActiveAlerts[0].Id;

        // Act
        _service.DismissAlert(alertId);

        // Assert
        _service.ActiveAlerts.Should().BeEmpty();
        _service.AlertHistory.Should().HaveCount(1);
        _service.AlertHistory[0].IsDismissed.Should().BeTrue();
    }

    [Fact]
    public void DismissAllAlerts_ClearsActiveAlerts()
    {
        // Arrange - Create multiple alerts by triggering different conditions
        var data1 = CreateGexData(GexRegime.LongGamma, spotPrice: 448m, totalGex: 4m, zeroGamma: 450m);
        var data2 = CreateGexData(GexRegime.ShortGamma, spotPrice: 452m, totalGex: 6m, zeroGamma: 450m);

        _mockGexState.SetupSequence(s => s.LiveGexData)
            .Returns(data1)
            .Returns(data2);
        _mockGexState.Setup(s => s.UseLiveData).Returns(true);

        _service.StartMonitoring();
        _service.CheckConditions();
        _service.CheckConditions();

        // Act
        _service.DismissAllAlerts();

        // Assert
        _service.ActiveAlerts.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdatePreferencesAsync_SavesAndNotifies()
    {
        // Arrange
        var newPrefs = new AlertPreferences
        {
            AlertsEnabled = false,
            GexUpperThreshold = 10m,
        };

        var prefsChangedCalled = false;
        _service.OnPreferencesChanged += () => prefsChangedCalled = true;

        // Act
        await _service.UpdatePreferencesAsync(newPrefs);

        // Assert
        _service.Preferences.AlertsEnabled.Should().BeFalse();
        _service.Preferences.GexUpperThreshold.Should().Be(10m);
        prefsChangedCalled.Should().BeTrue();
        _mockStorage.Verify(s => s.SetAsync(StorageKeys.AlertPreferences, newPrefs), Times.Once);
    }

    [Fact]
    public async Task LoadPreferencesAsync_LoadsFromStorage()
    {
        // Arrange
        var savedPrefs = new AlertPreferences
        {
            AlertsEnabled = false,
            GexUpperThreshold = 15m,
        };
        _mockStorage.Setup(s => s.GetAsync<AlertPreferences>(StorageKeys.AlertPreferences))
            .ReturnsAsync(savedPrefs);

        // Act
        await _service.LoadPreferencesAsync();

        // Assert
        _service.Preferences.AlertsEnabled.Should().BeFalse();
        _service.Preferences.GexUpperThreshold.Should().Be(15m);
    }

    [Fact]
    public async Task LoadPreferencesAsync_UsesDefaults_WhenNothingSaved()
    {
        // Arrange
        _mockStorage.Setup(s => s.GetAsync<AlertPreferences>(StorageKeys.AlertPreferences))
            .ReturnsAsync((AlertPreferences?)null);

        // Act
        await _service.LoadPreferencesAsync();

        // Assert
        _service.Preferences.AlertsEnabled.Should().BeTrue();
        _service.Preferences.GexUpperThreshold.Should().Be(AppConstants.Alerts.DefaultGexUpperThreshold);
    }

    [Fact]
    public async Task AlertHistory_TrimsToMaxCount()
    {
        // Arrange - Set max history to 3 and trigger more alerts
        var prefs = new AlertPreferences { MaxHistoryCount = 3, AlertCooldownSeconds = 0 };
        await _service.UpdatePreferencesAsync(prefs);

        // Create sequence of regime changes
        var regimes = new[] { GexRegime.LongGamma, GexRegime.ShortGamma, GexRegime.LongGamma, GexRegime.ShortGamma, GexRegime.LongGamma };
        var sequence = _mockGexState.SetupSequence(s => s.LiveGexData);
        foreach (var regime in regimes)
        {
            sequence = sequence.Returns(CreateGexData(regime, 450m, 3m));
        }

        _mockGexState.Setup(s => s.UseLiveData).Returns(true);

        _service.StartMonitoring();

        // Act - Trigger 4 regime changes (more than max history of 3)
        foreach (var _ in regimes)
        {
            _service.CheckConditions();
        }

        // Assert - History should be trimmed to max
        _service.AlertHistory.Count.Should().BeLessThanOrEqualTo(3);
    }

    [Fact]
    public void Dispose_StopsMonitoring()
    {
        // Arrange
        _service.StartMonitoring();

        // Act
        _service.Dispose();

        // Assert
        _service.IsMonitoring.Should().BeFalse();
    }

    private static GexCalculationResult CreateGexData(
        GexRegime regime,
        decimal spotPrice,
        decimal totalGex,
        decimal? zeroGamma = null)
    {
        return new GexCalculationResult
        {
            Symbol = "SPY",
            SpotPrice = spotPrice,
            TotalGex = totalGex,
            Regime = regime,
            ZeroGammaLevel = zeroGamma,
            StrikeGammas = [],
            Timestamp = DateTime.UtcNow,
        };
    }
}
