using KitchenProject.Models.Enums;

namespace KitchenProject.ViewModels
{
	// حركة مالية واحدة: وارد (تحصيل) أو منصرف (دفعة مورد / مصروف)
	public record FinancialMove(string Kind, DateTime Date, string Reference, string Party, string Link, string Details, PaymentMethod Method, decimal In, decimal Out);
}
