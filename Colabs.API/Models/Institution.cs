using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Colabs.Models;

[Table("DB_INSTITUTION")]
public class Institution
{
    [Key]
    [Column("ID")]
    public int Id { get; set; }

    [Required(ErrorMessage = "O nome da instituição é obrigatório")]
    [MaxLength(100)]
    [Column("NAME", TypeName = "VARCHAR2(100)")]
    public string Name { get; set; } = null!;

    [Required(ErrorMessage = "O CNPJ é obrigatório")]
    [MaxLength(18)]
    [Column("CNPJ", TypeName = "VARCHAR2(18)")]
    public string Cnpj { get; set; } = null!;

    [Required(ErrorMessage = "O endereço é obrigatório")]
    [MaxLength(400)]
    [Column("ADDRESS", TypeName = "VARCHAR2(400)")]
    public string Address { get; set; } = null!;

    /// <summary>Latitude obrigatória: a instituição é um ponto de apoio no mapa.</summary>
    [Precision(9, 6)]
    [Column("LATITUDE")]
    public decimal Latitude { get; set; }

    [Precision(9, 6)]
    [Column("LONGITUDE")]
    public decimal Longitude { get; set; }

    public ICollection<HelpRequest> HelpRequests { get; set; } = new List<HelpRequest>();
    public ICollection<Donation> Donations { get; set; } = new List<Donation>();

    /// <summary>Atualiza os dados da instituição. O CNPJ não é alterado.</summary>
    public void Update(string name, string address, decimal latitude, decimal longitude)
    {
        Name = name;
        Address = address;
        Latitude = latitude;
        Longitude = longitude;
    }
}