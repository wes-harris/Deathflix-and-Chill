using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DeathflixAPI.Models;

public class Actor
{
    public int Id { get; set; }

    [Required]
    public int TmdbId { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Column(TypeName = "text")]
    public string? Biography { get; set; }

    [DataType(DataType.Date)]
    public DateOnly? DateOfBirth { get; set; }

    [DataType(DataType.Date)]
    public DateOnly? DateOfDeath { get; set; }

    [StringLength(200)]
    public string? PlaceOfBirth { get; set; }

    [StringLength(200)]
    public string? ProfileImagePath { get; set; }

    [Column(TypeName = "decimal(10,3)")]
    public double Popularity { get; set; }

    public DateTime LastDetailsCheck { get; set; }
    public DateTime LastDeathCheck { get; set; }

    public bool IsDeceased => DateOfDeath.HasValue;

    public virtual DeathRecord? DeathRecord { get; set; }
    public virtual ICollection<MovieCredit> MovieCredits { get; set; } = new List<MovieCredit>();
}