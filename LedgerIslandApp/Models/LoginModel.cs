using System.ComponentModel.DataAnnotations;

namespace LedgerIslandApp.Models
{
    public class LoginModel
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, MinLength(4)]
        public string Password { get; set; } = string.Empty;
    }
}
