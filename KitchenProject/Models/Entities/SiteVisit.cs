using KitchenProject.Models.Enums;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KitchenProject.Models.Entities
{
	public class SiteVisit:BaseEntity
	{
		[ValidateNever]
		public Project Project { get; set; }
		public Guid ProjectId { get; set; }

		[ValidateNever]
		public ApplicationUser Engineer { get; set; }
		public string EngineerId { get; set; }

		[DisplayFormat(DataFormatString = "{0:dd-MM-yyyy hh:mm:ss tt}")]
		public DateTime VisitDate { get; set; }

		[Column(TypeName = "decimal(10,2)")]
		public decimal? Length { get; set; }

		[Column(TypeName = "decimal(10,2)")]
		public decimal? Width { get; set; }

		[Column(TypeName = "decimal(10,2)")]
		public decimal? Height { get; set; }

		public string? MeasurementsText { get; set; }
		public string? Notes { get; set; }
		public SiteVisitStatus Status { get; set; } = SiteVisitStatus.Scheduled;

		// مقاسات تفصيلية متعددة لكل معاينة
		public ICollection<MeasurementItem> MeasurementItems { get; set; } = new List<MeasurementItem>();
	}
}
