using System.ComponentModel.DataAnnotations;

namespace KitchenProject.Models.Entities
{
	// بنود الأصناف الجاهزة (A, B, C) - الأسماء تُعدل من شاشة البنود
	public class ItemGroup : BaseEntity
	{
		[Required(ErrorMessage = "رمز البند مطلوب")]
		[MaxLength(10)]
		public string Code { get; set; } = "";

		[Required(ErrorMessage = "اسم البند مطلوب")]
		[MaxLength(100)]
		public string Name { get; set; } = "";

		public int SortOrder { get; set; }

		[MaxLength(250)]
		public string? Description { get; set; }

		public ICollection<Item> Items { get; set; } = new List<Item>();
	}
}
