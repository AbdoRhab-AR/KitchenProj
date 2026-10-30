using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KitchenProject.Models.Entities
{
	public class Contract:BaseEntity
	{
		[ValidateNever]
		public Project Projects { get; set; }
		[Required(ErrorMessage = "يجب اختيار المشروع")]
		[Display(Name = "Projects")]
		public Guid ProjectId { get; set; }

		// عرض السعر المعتمد الذي بُني عليه العقد (اختياري)
		[ValidateNever]
		public Quote? Quote { get; set; }
		public Guid? QuoteId { get; set; }

		[MaxLength(30)]
		public required string ContractNo { get; set; }

		// المواصفات الفنية المتفق عليها (الخامات، الألوان، الإكسسوارات...)
		public string? Specifications { get; set; }
		[DisplayFormat(DataFormatString = "{0:dd-MM-yyyy hh:mm:ss tt}")]
		public DateTime? StartDate { get; set; }
		[DisplayFormat(DataFormatString = "{0:dd-MM-yyyy hh:mm:ss tt}")]
		public DateTime? ExpectedEndDate { get; set; }

		[Column(TypeName = "decimal(18,2)")]
		public decimal TotalAmount { get; set; }

		[Column(TypeName = "decimal(18,2)")]
		public decimal PaidAmount { get; set; }

		[NotMapped]
		public decimal RemainingAmount => TotalAmount - PaidAmount;

		[MaxLength(250)]
		public string? ContractFilePath { get; set; }

		[MaxLength(30)]
		public string Status { get; set; } = "Draft";

		public string? Notes { get; set; }
		public ICollection<CustomerPayment> CustomerPayments { get; set; } = new List<CustomerPayment>();
	}
}
