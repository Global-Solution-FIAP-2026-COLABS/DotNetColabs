using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Colabs.Models;

[Table("DB_HELP_REQUEST")]
public class HelpRequest
{
    [Key]
    [Column("ID")]
    public int Id { get; set; }

    [Column("ID_CATEGORY")]
    public int IdCategory { get; set; }
    public Category Category { get; set; } = null!;

    /// <summary>Usuário que solicitou a ajuda.</summary>
    [Column("ID_USER")]
    public int IdUser { get; set; }
    public User User { get; set; } = null!;

    /// <summary>Instituição vinculada ao pedido (opcional).</summary>
    [Column("ID_INSTITUTION")]
    public int? IdInstitution { get; set; }
    public Institution? Institution { get; set; }

    [Required(ErrorMessage = "A descrição é obrigatória")]
    [MaxLength(500)]
    [Column("DESCRIPTION", TypeName = "VARCHAR2(500)")]
    public string Description { get; set; } = null!;

    [Column("STATUS", TypeName = "VARCHAR2(20)")]
    public HelpRequestStatus Status { get; set; } = HelpRequestStatus.Open;

    [Column("CREATION_DATE", TypeName = "TIMESTAMP(6)")]
    public DateTime CreationDate { get; set; }

    [Column("COMPLETION_DATE", TypeName = "TIMESTAMP(6)")]
    public DateTime? CompletionDate { get; set; }

    public ICollection<HelpRequestItem> Items { get; set; } = new List<HelpRequestItem>();

    /// <summary>Doações vinculadas (matches) a este pedido.</summary>
    public ICollection<Donation> Donations { get; set; } = new List<Donation>();

    public void Update(int idCategory, int? idInstitution, string description)
    {
        IdCategory = idCategory;
        IdInstitution = idInstitution;
        Description = description;
    }
}