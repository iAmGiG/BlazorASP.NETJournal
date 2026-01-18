using System.Text.Json;

namespace GexVisor.Core
{
    public class JournalFramework
    {
        /// <summary>
        /// Defines the various levels of task priority.
        /// </summary>
        /// <remarks>
        /// <para>The priority levels include:</para>
        /// <list type="bullet">
        /// <item><description>Bingo: Tasks with loose time requirements, ranging from cleaning the house to reading a book.</description></item>
        /// <item><description>FreeSpace: Similar to Bingo, but these tasks are not near time priority.</description></item>
        /// <item><description>Low: These tasks have some time constraints or importance.</description></item>
        /// <item><description>Medium: These tasks have more present time constraints.</description></item>
        /// <item><description>High: These tasks have high time constraints or importance.</description></item>
        /// <item><description>Critical: These tasks have the highest time constraints or importance.</description></item>
        /// <item><description>Epic: These tasks are treated more like big story components, similar to how Scrum and other software methodologies work.</description></item>
        /// </list>
        /// </remarks>
        public enum PriorityLevel
        {
            Bingo,
            FreeSpace,
            Low,
            Medium,
            High,
            Critical,
            Epic
        }
        public Guid Id { get; set; } = Guid.NewGuid();
        public bool IsCompleted { get; set; }
        //Desc was a hard choice to leave here, but having it nullable will help for the other logs.
        public string? Description { get; set; }
        public DateTime? TargetCompletionDate { get; set; }
        public DateTime? CreatedDate { get; set; }
        public PriorityLevel? TaskPriority { get; set; }
    }
    /// <summary>
    /// Represents a task with a unique identifier, description, completion status, target completion date, creation date, and priority level.
    /// </summary>
    public class ToDoTask : JournalFramework
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ToDoTask"/> class.
        /// </summary>
        /// <param name="id">The unique identifier of the task.</param>
        /// <param name="description">A description of the task.</param>
        /// <param name="completed">A value indicating whether the task is completed.</param>
        /// <param name="completionDate">The target date for completing the task.</param>
        /// <param name="createdDate">The date the task was created.</param>
        /// <param name="priority">The priority level of the task.</param>
        public ToDoTask(Guid id, string description, bool completed, DateTime completionDate,
            DateTime createdDate, PriorityLevel priority)
        {
            Id = id;
            Description = description;
            IsCompleted = completed;
            TargetCompletionDate = completionDate;
            CreatedDate = createdDate;
            TaskPriority = priority;
        }
        public ToDoTask(Guid id, string description, bool completed)
        {
            Id = id;
            Description = description;
            IsCompleted = completed;
        }
        public ToDoTask() { }
    }

    public class TradeLog : JournalFramework
    {
        public enum Type { Long, Short };

        public Type TradeDirection { get; set; }
        public decimal EntryPrice { get; set; }
        public decimal? ExitPrice { get; set; }
        public decimal Quantity { get; set; }
        public string? Ticker { get; set; }
        public string? Analysis { get; set; }
        public string? Notes { get; set; }

        /// <summary>
        /// Calculates profit/loss for this trade.
        /// Returns null if trade is not yet closed (ExitPrice is null).
        /// Virtual to allow derived classes (e.g., OptionsLog) to override with specialized logic.
        /// </summary>
        public virtual decimal? CalculatePnL()
        {
            if (!ExitPrice.HasValue)
                return null;

            var priceDelta = ExitPrice.Value - EntryPrice;
            var multiplier = TradeDirection == Type.Long ? 1 : -1;
            return priceDelta * Quantity * multiplier;
        }

        public TradeLog() { }
    }
    public class OptionsLog : TradeLog
    {
        public enum TradeType { BTO, BTC, STO, STC };

        public TradeType OptionTradeType { get; set; }
        public decimal StrikePrice { get; set; }
        public DateTime? ExpirationDate { get; set; }

        /// <summary>
        /// Calculates profit/loss for options trade.
        /// Returns null if trade is not yet closed (ExitPrice is null).
        /// For options: P&L = (ExitPrice - EntryPrice) * Quantity * 100
        /// Note: Quantity (inherited) represents the number of contracts.
        /// </summary>
        public override decimal? CalculatePnL()
        {
            if (!ExitPrice.HasValue)
                return null;

            var priceDelta = ExitPrice.Value - EntryPrice;
            // Options contracts represent 100 shares each
            var contractMultiplier = 100;
            // BTO/STO determine if we're buying (positive delta) or selling (negative delta)
            var directionMultiplier = (OptionTradeType == TradeType.BTO || OptionTradeType == TradeType.BTC) ? 1 : -1;

            return priceDelta * Quantity * contractMultiplier * directionMultiplier;
        }

        public OptionsLog() { }
    }
}
