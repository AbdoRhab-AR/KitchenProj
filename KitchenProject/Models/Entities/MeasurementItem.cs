using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KitchenProject.Models.Entities
{
    public class MeasurementItem : BaseEntity
    {
        [ValidateNever]
        public SiteVisit SiteVisit { get; set; }
        public Guid SiteVisitId { get; set; }

        [Required(ErrorMessage = "اسم المنطقة مطلوب")]
        [MaxLength(100)]
        [Display(Name = "اسم المنطقة / المطبخ")]
        public string AreaName { get; set; } = string.Empty;

        [Column(TypeName = "decimal(10,2)")]
        [Display(Name = "الطول (م)")]
        public decimal? Length { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        [Display(Name = "العرض (م)")]
        public decimal? Width { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        [Display(Name = "الارتفاع (م)")]
        public decimal? Height { get; set; }

        /// <summary>المساحة الأفقية = الطول × العرض (محسوبة تلقائياً)</summary>
        [NotMapped]
        public decimal? Area => (Length.HasValue && Width.HasValue)
            ? Math.Round(Length.Value * Width.Value, 2)
            : null;

        [MaxLength(500)]
        [Display(Name = "ملاحظات")]
        public string? Notes { get; set; }
    }
}
