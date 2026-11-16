using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Colabs.Models;

[Table("DB_DONATION_ITEM")]
public class DonationItem
{
    [Key]
    [Column("ID")]
    public int Id { get; set; }

    [Column("ID_DONATION")]
    public int IdDonation { get; set; }
    public Donation Donation { get; set; } = null!;

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