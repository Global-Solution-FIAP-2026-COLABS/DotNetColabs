using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Colabs.Models;

[Table("DB_CATEGORY")]
public class Category
{
    [Key]
    [Column("ID")]
    public int Id { get; set; }

    [Required(ErrorMessage = "O nome da categoria é obrigatório")]
    [MaxLength(50)]
    [Column("NAME", TypeName = "VARCHAR2(50)")]
    public string Name { get; set; } = null!;

    public ICollection<HelpRequest> HelpRequests { get; set; } = new List<HelpRequest>();
    public ICollection<Donation> Donations { get; set; } = new List<Donation>();

    public void Update(string name)
    {
        Name = name;
    }
}