using System.ComponentModel.DataAnnotations;

namespace KitchenProject.Models.Enums
{
    public enum ContractType
    {
        [Display(Name = "عقد مبدئي")]
        Preliminary = 1,
        
        [Display(Name = "عقد نهائي")]
        Final = 2
    }
}
