using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Colabs.Models;

[Table("DB_DONATION")]
public class Donation
{
    [Key]
    [Column("ID")]
    public int Id { get; set; }

    [Column("ID_CATEGORY")]
    public int IdCategory { get; set; }
    public Category Category { get; set; } = null!;

    /// <summary>Doador pessoa física. Ao menos um entre IdUser e IdInstitution é obrigatório.</summary>
    [Column("ID_USER")]
    public int? IdUser { get; set; }
    public User? User { get; set; }

    /// <summary>Doador instituição. Ao menos um entre IdUser e IdInstitution é obrigatório.</summary>
    [Column("ID_INSTITUTION")]
    public int? IdInstitution { get; set; }
    public Institution? Institution { get; set; }

    /// <summary>Pedido de ajuda atendido por esta doação (preenchido no match).</summary>
    [Column("ID_HELP_REQUEST")]
    public int? IdHelpRequest { get; set; }
    public HelpRequest? HelpRequest { get; set; }

    [Required(ErrorMessage = "A descrição é obrigatória")]
    [MaxLength(500)]
    [Column("DESCRIPTION", TypeName = "VARCHAR2(500)")]
    public string Description { get; set; } = null!;

    [Column("STATUS", TypeName = "VARCHAR2(20)")]
    public DonationStatus Status { get; set; } = DonationStatus.Available;

    [Column("CREATION_DATE", TypeName = "TIMESTAMP(6)")]
    public DateTime CreationDate { get; set; }

    [Column("COMPLETION_DATE", TypeName = "TIMESTAMP(6)")]
    public DateTime? CompletionDate { get; set; }

    public ICollection<DonationItem> Items { get; set; } = new List<DonationItem>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();

    /// <summary>Atualiza categoria e descrição. O doador e o vínculo com pedido não mudam aqui.</summary>
    public void Update(int idCategory, string description)
    {
        IdCategory = idCategory;
        Description = description;
    }
}