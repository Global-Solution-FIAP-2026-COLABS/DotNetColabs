using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Colabs.Models;

[Table("DB_HELP_REQUEST_ITEM")]
public class HelpRequestItem
{
    [Key]
    [Column("ID")]
    public int Id { get; set; }

    [Column("ID_HELP_REQUEST")]
    public int IdHelpRequest { get; set; }
    public HelpRequest HelpRequest { get; set; } = null!;

    [Required(ErrorMessage = "O nome do item é obrigatório")]
    [MaxLength(100)]
    [Column("NAME", TypeName = "VARCHAR2(100)")]
    public string Name { get; set; } = null!;

    [Column("QUANTITY")]
    public int Quantity { get; set; }

    public void Update(string name, int quantity)
    {
        Name = name;
        Quantity = quantity;
    }
}