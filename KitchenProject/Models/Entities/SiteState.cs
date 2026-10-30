using System.ComponentModel.DataAnnotations;

namespace KitchenProject.Models.Entities
{
    public class SiteState:BaseEntity
    {
        [Display(Name = "Site State")]
        public bool State { get; set; }

       
        [Required(ErrorMessage = "رسالة إغلاق الموقع مطلوبة")]
        [Display(Name = "Site closing message")]
        public required string ClosingMessage { get; set; }

    }
}
