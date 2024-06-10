using System.ComponentModel.DataAnnotations;

namespace WebApplicationJwtAuthentication.Dtos
{
    public class UserRequestLoginDto
    {
        [Required]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }
    }
}
