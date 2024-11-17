public class MovieCredit
{
    public int Id { get; set; }

    public int ActorId { get; set; }
    public virtual Actor Actor { get; set; } = null!;

    public int MovieId { get; set; }
    public virtual Movie Movie { get; set; } = null!;

    [MaxLength(200)]
    public string? Character { get; set; }

    [MaxLength(50)]
    public string? CreditType { get; set; } // e.g., "Cast", "Crew"
}