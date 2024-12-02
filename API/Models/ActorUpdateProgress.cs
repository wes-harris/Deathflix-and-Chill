namespace DeathflixAPI.Models;

public class ActorUpdateProgress
{
    public int TotalActors { get; set; }
    public int ProcessedCount { get; set; }
    public int UpdatedCount { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? LastUpdateTime { get; set; }
    public string CurrentActor { get; set; } = string.Empty;
}