using System.Text.Json;
using static GexVisor.Core.JournalFramework;

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
        public static Guid Id { get; set; } = Guid.NewGuid();
        public bool IsCompleted { get; set; }
        //Desc was a hard choice to leave here, but having it nullable will help for the other logs.
        public string? Description { get; set; }
        public DateTime? TargetCompletionDate { get; set; }
        public DateTime? CreatedDate { get; set; }
        public PriorityLevel? TaskPriority { get; set; }

        //private DateTime GetTargetDate(Guid id)
        //{ null; }

        public void SaveTasks(List<ToDoTask> tasks)
        {
            string directoryPath = "./Data/";
            if (!System.IO.Directory.Exists(directoryPath))
            {
                System.IO.Directory.CreateDirectory(directoryPath);
            }
            string jsonString = JsonSerializer.Serialize(tasks);
            System.IO.File.WriteAllText(directoryPath + "MyTasks.json", jsonString);
        }
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
        public float price;
        public string? ticker, analysis, notes;

        //might end up making a second quantity, fraction quantity,
        //to represent fraction shares, should even make a options only trade log to simply this class.

        //I would consider fee, but they are hardly enough to consider, maybe use a look up table later?

        //Trade result should be a method that is used to keep track of a trade outcome,
        //so in the future context this trade result method would auto calculate when past the
        //expiration and if on close,public
        public static void TradeResult()
        {

        }

        public TradeLog() { }
        //so might need a few overloads of these.
        //market conditions will be a bit harder, this could just be some notes,
        //but would be cool to have 1-2 major catalyst
        //Mental and emotional state, could just add this to notes.
    }
    public class OptionsLog : TradeLog
    {
        public enum TradeType { BTO, BTC, STO, STC };
        public int contractCount;

        public OptionsLog() { }
    }
}
