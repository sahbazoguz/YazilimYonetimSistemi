using System.ComponentModel.DataAnnotations;

namespace YazilimYonetimSistemi.Web.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Mail adresi zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir mail adresi giriniz.")]
    [Display(Name = "Kurumsal Mail Adresi")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şifre zorunludur.")]
    [DataType(DataType.Password)]
    [Display(Name = "Şifre")]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}
