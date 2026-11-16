    using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Colabs.Models;

[Table("DB_USER")]
public class User
{
    [Key]
    [Column("ID")]
    public int Id { get; set; }

    [Required(ErrorMessage = "O nome do usuário é obrigatório")]
    [MaxLength(100)]
    [Column("NAME", TypeName = "VARCHAR2(100)")]
    public string Name { get; set; } = null!;

    [Required(ErrorMessage = "O CPF é obrigatório")]
    [MaxLength(15)]
    [Column("CPF", TypeName = "VARCHAR2(15)")]
    public string Cpf { get; set; } = null!;

    [Required(ErrorMessage = "O telefone é obrigatório")]
    [MaxLength(15)]
    [Column("PHONE", TypeName = "VARCHAR2(15)")]
    public string Phone { get; set; } = null!;

    [Required(ErrorMessage = "O e-mail é obrigatório")]
    [MaxLength(150)]
    [Column("EMAIL", TypeName = "VARCHAR2(150)")]
    public string Email { get; set; } = null!;

    /// <summary>Hash da senha (nunca a senha em texto puro).</summary>
    [Required]
    [MaxLength(300)]
    [Column("PASSWORD", TypeName = "VARCHAR2(300)")]
    public string Password { get; set; } = null!;

    [Column("ROLE", TypeName = "VARCHAR2(20)")]
    public UserRole Role { get; set; } = UserRole.User;

    [Required(ErrorMessage = "O endereço é obrigatório")]
    [MaxLength(400)]
    [Column("ADDRESS", TypeName = "VARCHAR2(400)")]
    public string Address { get; set; } = null!;

    [Precision(9, 6)]
    [Column("LATITUDE")]
    public decimal? Latitude { get; set; }

    [Precision(9, 6)]
    [Column("LONGITUDE")]
    public decimal? Longitude { get; set; }

    public ICollection<HelpRequest> HelpRequests { get; set; } = new List<HelpRequest>();
    public ICollection<Donation> Donations { get; set; } = new List<Donation>();

    /// <summary>Atualiza os dados cadastrais. CPF, senha e papel não são alterados aqui.</summary>
    public void Update(string name, string phone, string email, string address,
                       decimal? latitude, decimal? longitude)
    {
        Name = name;
        Phone = phone;
        Email = email;
        Address = address;
        Latitude = latitude;
        Longitude = longitude;
    }
}