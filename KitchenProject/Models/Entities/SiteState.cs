using System.ComponentModel.DataAnnotations;

namespace KitchenProject.Models.Entities
{
    public class SiteState:BaseEntity
    {
        [Display(Name = "Site State")]
        public bool State { get; set; }

       
        [Required(ErrorMessage = "Site closing message is required")]
        [Display(Name = "Site closing message")]
        public required string ClosingMessage { get; set; }

    }
}
