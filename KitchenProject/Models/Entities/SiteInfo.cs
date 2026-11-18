using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace KitchenProject.Models.Entities
{
    public class SiteInfo : BaseEntity
    {
        [StringLength(50, ErrorMessage = "الاسم يجب أن يكون بين {2} و {1} حرفاً", MinimumLength = 3)]
        public required string Name { get; set; }

        [StringLength(200)]
        public required string Activity { get; set; }

        [StringLength(2000)]
        public required string About { get; set; }

        [Display(Name = "Site Logo")]
        public string? LogoUrl { get; set; }

        [Display(Name = "Cover Image")]
        public string? CoverImageUrl { get; set; }

        // صورة الورق الرسمي (A4) تُستخدم خلفية لكل المستندات المطبوعة
        [Display(Name = "Letterhead")]
        public string? LetterheadUrl { get; set; }

        // الشروط الافتراضية التي تُعبأ تلقائياً في كل عرض سعر جديد
        [StringLength(4000)]
        public string? DefaultQuoteTerms { get; set; }

        // عدد الصفوف في كل صفحة بجداول النظام (نظام الصفحات)
        [Range(5, 200, ErrorMessage = "عدد الصفوف في الصفحة يجب أن يكون بين {1} و {2}")]
        public int PageSize { get; set; } = 10;

        [NotMapped]
        public IFormFile? Logo { get; set; }

        [NotMapped]
        public IFormFile? CoverImage { get; set; }

    }
}
