// Copyright (c) GexVisor. All rights reserved.

using FluentAssertions;
using GexVisor.Core;
using GexVisor.UI.Models;
using GexVisor.UI.Services;
using Moq;

namespace GexVisor.UI.Tests.Services;

/// <summary>
/// Unit tests for GoalService.
/// Tests goal CRUD, progress calculation, status management, and auto-completion.
/// </summary>
public class GoalServiceTests
{
    private readonly Mock<ILocalStorageService> _mockStorage;
    private readonly Mock<IPersonalAnalyticsService> _mockAnalytics;
    private readonly TradeLogService _tradeLogService;
    private readonly GoalService _service;

    public GoalServiceTests()
    {
        _mockStorage = new Mock<ILocalStorageService>();
        _mockAnalytics = new Mock<IPersonalAnalyticsService>();
        _tradeLogService = new TradeLogService(_mockStorage.Object);
        _service = new GoalService(_mockStorage.Object, _tradeLogService, _mockAnalytics.Object);
    }

    [Fact]
    public async Task LoadAsync_LoadsGoalsFromStorage()
    {
        // Arrange
        var goals = new List<TradingGoal>
        {
            TradingGoal.Create("Goal 1", GoalType.TotalPnL, 1000),
            TradingGoal.Create("Goal 2", GoalType.WinRate, 60),
        };
        _mockStorage.Setup(s => s.GetAsync<List<TradingGoal>>(StorageKeys.TradingGoals))
            .ReturnsAsync(goals);

        // Act
        await _service.LoadAsync();

        // Assert
        _service.AllGoals.Should().HaveCount(2);
    }

    [Fact]
    public async Task LoadAsync_HandlesNullStorage()
    {
        // Arrange
        _mockStorage.Setup(s => s.GetAsync<List<TradingGoal>>(StorageKeys.TradingGoals))
            .ReturnsAsync((List<TradingGoal>?)null);

        // Act
        await _service.LoadAsync();

        // Assert
        _service.AllGoals.Should().BeEmpty();
    }

    [Fact]
    public async Task AddAsync_AddsGoalAndSaves()
    {
        // Arrange
        await _service.LoadAsync();
        var goal = TradingGoal.Create("New Goal", GoalType.TradeCount, 50);

        // Act
        await _service.AddAsync(goal);

        // Assert
        _service.AllGoals.Should().HaveCount(1);
        _service.AllGoals[0].Title.Should().Be("New Goal");
        _mockStorage.Verify(s => s.SetAsync(StorageKeys.TradingGoals, It.IsAny<List<TradingGoal>>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_RemovesGoal()
    {
        // Arrange
        var goal = TradingGoal.Create("Goal to delete", GoalType.TotalPnL, 1000);
        _mockStorage.Setup(s => s.GetAsync<List<TradingGoal>>(StorageKeys.TradingGoals))
            .ReturnsAsync([goal]);
        await _service.LoadAsync();

        // Act
        await _service.DeleteAsync(goal.Id);

        // Assert
        _service.AllGoals.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateAsync_UpdatesExistingGoal()
    {
        // Arrange
        var goal = TradingGoal.Create("Original", GoalType.TotalPnL, 1000);
        _mockStorage.Setup(s => s.GetAsync<List<TradingGoal>>(StorageKeys.TradingGoals))
            .ReturnsAsync([goal]);
        await _service.LoadAsync();

        var updated = goal with { Title = "Updated Title" };

        // Act
        await _service.UpdateAsync(updated);

        // Assert
        _service.AllGoals[0].Title.Should().Be("Updated Title");
        _service.AllGoals[0].UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task GetActiveGoals_FiltersAndOrdersByDueDate()
    {
        // Arrange
        var goals = new List<TradingGoal>
        {
            new() { Title = "No Due", Type = GoalType.TotalPnL, TargetValue = 100, Status = GoalStatus.Active },
            new() { Title = "Due Soon", Type = GoalType.TotalPnL, TargetValue = 100, Status = GoalStatus.Active, DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)) },
            new() { Title = "Due Later", Type = GoalType.TotalPnL, TargetValue = 100, Status = GoalStatus.Active, DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)) },
            new() { Title = "Completed", Type = GoalType.TotalPnL, TargetValue = 100, Status = GoalStatus.Completed },
        };
        _mockStorage.Setup(s => s.GetAsync<List<TradingGoal>>(StorageKeys.TradingGoals))
            .ReturnsAsync(goals);
        await _service.LoadAsync();

        // Act
        var result = _service.GetActiveGoals().ToList();

        // Assert
        result.Should().HaveCount(3);
        result[0].Title.Should().Be("Due Soon");
        result[1].Title.Should().Be("Due Later");
        result[2].Title.Should().Be("No Due");
    }

    [Fact]
    public async Task CompleteGoalAsync_SetsStatusToCompleted()
    {
        // Arrange
        var goal = TradingGoal.Create("Goal", GoalType.TotalPnL, 1000);
        _mockStorage.Setup(s => s.GetAsync<List<TradingGoal>>(StorageKeys.TradingGoals))
            .ReturnsAsync([goal]);
        await _service.LoadAsync();

        // Act
        await _service.CompleteGoalAsync(goal.Id);

        // Assert
        _service.AllGoals[0].Status.Should().Be(GoalStatus.Completed);
        _service.AllGoals[0].CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ArchiveGoalAsync_SetsStatusToArchived()
    {
        // Arrange
        var goal = TradingGoal.Create("Goal", GoalType.TotalPnL, 1000);
        _mockStorage.Setup(s => s.GetAsync<List<TradingGoal>>(StorageKeys.TradingGoals))
            .ReturnsAsync([goal]);
        await _service.LoadAsync();

        // Act
        await _service.ArchiveGoalAsync(goal.Id);

        // Assert
        _service.AllGoals[0].Status.Should().Be(GoalStatus.Archived);
    }

    [Fact]
    public async Task ReactivateGoalAsync_SetsStatusToActive()
    {
        // Arrange
        var goal = new TradingGoal
        {
            Title = "Archived Goal",
            Type = GoalType.TotalPnL,
            TargetValue = 1000,
            Status = GoalStatus.Archived,
        };
        _mockStorage.Setup(s => s.GetAsync<List<TradingGoal>>(StorageKeys.TradingGoals))
            .ReturnsAsync([goal]);
        await _service.LoadAsync();

        // Act
        await _service.ReactivateGoalAsync(goal.Id);

        // Assert
        _service.AllGoals[0].Status.Should().Be(GoalStatus.Active);
    }

    [Fact]
    public async Task RecalculateProgressAsync_UpdatesTotalPnLGoal()
    {
        // Arrange
        await _tradeLogService.LoadAsync();
        await _tradeLogService.AddRangeAsync(
        [
            CreateClosedTrade("SPY", 100m, 110m), // +$1000
            CreateClosedTrade("QQQ", 100m, 120m), // +$2000
        ]);

        var goal = new TradingGoal
        {
            Title = "Earn $5000",
            Type = GoalType.TotalPnL,
            TargetValue = 5000,
            Status = GoalStatus.Active,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30)),
        };
        _mockStorage.Setup(s => s.GetAsync<List<TradingGoal>>(StorageKeys.TradingGoals))
            .ReturnsAsync([goal]);
        await _service.LoadAsync();

        // Act
        await _service.RecalculateProgressAsync();

        // Assert
        _service.AllGoals[0].CurrentValue.Should().Be(3000);
        _service.AllGoals[0].ProgressPercent.Should().Be(60);
    }

    [Fact]
    public async Task RecalculateProgressAsync_UpdatesTradeCountGoal()
    {
        // Arrange
        await _tradeLogService.LoadAsync();
        await _tradeLogService.AddRangeAsync(
        [
            CreateClosedTrade("A", 100m, 110m),
            CreateClosedTrade("B", 100m, 110m),
            CreateClosedTrade("C", 100m, 110m),
        ]);

        var goal = new TradingGoal
        {
            Title = "10 Trades",
            Type = GoalType.TradeCount,
            TargetValue = 10,
            Status = GoalStatus.Active,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30)),
        };
        _mockStorage.Setup(s => s.GetAsync<List<TradingGoal>>(StorageKeys.TradingGoals))
            .ReturnsAsync([goal]);
        await _service.LoadAsync();

        // Act
        await _service.RecalculateProgressAsync();

        // Assert
        _service.AllGoals[0].CurrentValue.Should().Be(3);
        _service.AllGoals[0].ProgressPercent.Should().Be(30);
    }

    [Fact]
    public async Task RecalculateProgressAsync_UpdatesWinRateGoal()
    {
        // Arrange
        await _tradeLogService.LoadAsync();
        await _tradeLogService.AddRangeAsync(
        [
            CreateClosedTrade("A", 100m, 110m), // Win
            CreateClosedTrade("B", 100m, 90m),  // Loss
            CreateClosedTrade("C", 100m, 120m), // Win
            CreateClosedTrade("D", 100m, 115m), // Win
        ]);

        var goal = new TradingGoal
        {
            Title = "75% Win Rate",
            Type = GoalType.WinRate,
            TargetValue = 75,
            Status = GoalStatus.Active,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30)),
        };
        _mockStorage.Setup(s => s.GetAsync<List<TradingGoal>>(StorageKeys.TradingGoals))
            .ReturnsAsync([goal]);
        await _service.LoadAsync();

        // Act
        await _service.RecalculateProgressAsync();

        // Assert
        _service.AllGoals[0].CurrentValue.Should().Be(75); // 3/4 = 75%
    }

    [Fact]
    public async Task RecalculateProgressAsync_AutoCompletesWhenTargetReached()
    {
        // Arrange
        await _tradeLogService.LoadAsync();
        await _tradeLogService.AddRangeAsync(
        [
            CreateClosedTrade("A", 100m, 150m), // +$5000
        ]);

        var goal = new TradingGoal
        {
            Title = "Earn $1000",
            Type = GoalType.TotalPnL,
            TargetValue = 1000,
            Status = GoalStatus.Active,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30)),
        };
        _mockStorage.Setup(s => s.GetAsync<List<TradingGoal>>(StorageKeys.TradingGoals))
            .ReturnsAsync([goal]);
        await _service.LoadAsync();

        // Act
        await _service.RecalculateProgressAsync();

        // Assert
        _service.AllGoals[0].Status.Should().Be(GoalStatus.Completed);
        _service.AllGoals[0].CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task CheckOverdueGoalsAsync_MarksOverdueGoalsAsFailed()
    {
        // Arrange
        var overdueGoal = new TradingGoal
        {
            Title = "Overdue",
            Type = GoalType.TotalPnL,
            TargetValue = 10000,
            Status = GoalStatus.Active,
            DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5)), // Past due
        };
        _mockStorage.Setup(s => s.GetAsync<List<TradingGoal>>(StorageKeys.TradingGoals))
            .ReturnsAsync([overdueGoal]);
        await _service.LoadAsync();

        // Act
        await _service.CheckOverdueGoalsAsync();

        // Assert
        _service.AllGoals[0].Status.Should().Be(GoalStatus.Failed);
    }

    [Fact]
    public async Task OnGoalsChanged_FiresWhenGoalAdded()
    {
        // Arrange
        await _service.LoadAsync();
        var eventFired = false;
        _service.OnGoalsChanged += () => eventFired = true;
        var goal = TradingGoal.Create("Test", GoalType.TotalPnL, 1000);

        // Act
        await _service.AddAsync(goal);

        // Assert
        eventFired.Should().BeTrue();
    }

    [Fact]
    public void TradingGoal_ProgressPercent_CalculatesCorrectly()
    {
        // Arrange
        var goal = new TradingGoal
        {
            Title = "Test",
            Type = GoalType.TotalPnL,
            TargetValue = 1000,
            CurrentValue = 300,
        };

        // Assert
        goal.ProgressPercent.Should().Be(30);
    }

    [Fact]
    public void TradingGoal_IsAchieved_WhenTargetReached()
    {
        // Arrange
        var goal = new TradingGoal
        {
            Title = "Test",
            Type = GoalType.TotalPnL,
            TargetValue = 1000,
            CurrentValue = 1000,
        };

        // Assert
        goal.IsAchieved.Should().BeTrue();
    }

    [Fact]
    public void TradingGoal_IsOverdue_WhenPastDueDate()
    {
        // Arrange
        var goal = new TradingGoal
        {
            Title = "Test",
            Type = GoalType.TotalPnL,
            TargetValue = 1000,
            DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            Status = GoalStatus.Active,
        };

        // Assert
        goal.IsOverdue.Should().BeTrue();
    }

    [Fact]
    public void TradingGoal_TargetDisplay_FormatsCorrectlyForType()
    {
        // Arrange & Assert
        var pnlGoal = new TradingGoal { Title = "PnL", Type = GoalType.TotalPnL, TargetValue = 1000 };
        pnlGoal.TargetDisplay.Should().Be("$1,000");

        var winRateGoal = new TradingGoal { Title = "WR", Type = GoalType.WinRate, TargetValue = 60 };
        winRateGoal.TargetDisplay.Should().Be("60%");

        var tradeCountGoal = new TradingGoal { Title = "TC", Type = GoalType.TradeCount, TargetValue = 50 };
        tradeCountGoal.TargetDisplay.Should().Be("50 trades");
    }

    // === Helper Methods ===
    private static OptionsLog CreateClosedTrade(string ticker, decimal entry, decimal exit)
    {
        return new OptionsLog
        {
            Ticker = ticker,
            EntryPrice = entry,
            ExitPrice = exit,
            Quantity = 1,
            ContractMultiplier = 100,
            OptionTradeType = OptionsLog.TradeType.BTO,
            TradeDirection = TradeLog.Type.Long,
            StrikePrice = entry,
            CreatedDate = DateTime.UtcNow.AddDays(-5),
            ExpirationDate = DateTime.UtcNow.AddDays(30),
        };
    }
}
