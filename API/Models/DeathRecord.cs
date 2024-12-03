using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DeathflixAPI.Models;

public class DeathRecord
{
    public int Id { get; set; }

    [Required]
    public int ActorId { get; set; }

    [Required]
    [DataType(DataType.Date)]
    public DateOnly DateOfDeath { get; set; }

    [StringLength(1000)]
    public string? AdditionalDetails { get; set; }

    [DataType(DataType.DateTime)]
    public DateTime LastVerified { get; set; }

    [StringLength(500)]
    public string? SourceUrl { get; set; }

    [Required]
    public virtual Actor Actor { get; set; } = null!;
}