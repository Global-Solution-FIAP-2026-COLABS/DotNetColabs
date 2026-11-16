using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Colabs.Models;

[Table("DB_REVIEW")]
public class Review
{
    [Key]
    [Column("ID")]
    public int Id { get; set; }

    [Column("ID_DONATION")]
    public int IdDonation { get; set; }
    public Donation Donation { get; set; } = null!;

    /// <summary>Nota de 1 a 5 (garantida também por CHECK no banco).</summary>
    [Column("RATING")]
    public int Rating { get; set; }

    [MaxLength(500)]
    [Column("DESCRIPTION", TypeName = "VARCHAR2(500)")]
    public string? Description { get; set; }

    [Column("REVIEW_DATE", TypeName = "TIMESTAMP(6)")]
    public DateTime ReviewDate { get; set; }

    public void Update(int rating, string? description)
    {
        Rating = rating;
        Description = description;
    }
}