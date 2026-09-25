using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace MyDoc.Models
{
    public class Notification
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = null!;

        [Required]
        [StringLength(120)]
        public string Title { get; set; } = null!;

        [Required]
        [StringLength(600)]
        public string Message { get; set; } = null!;

        [StringLength(500)]
        public string? LinkUrl { get; set; }

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public IdentityUser User { get; set; } = null!;
    }
}
