using Microsoft.EntityFrameworkCore;

namespace KitchenProject.Classes
{
	public static class DocumentNumbers
	{
		// أعلى رقم موجود + 1 (وليس العدد + 1) حتى لا يتكرر الرقم بعد الحذف
		// مثال: NextAsync(customers.Select(c => c.CustomerNo), "CUST-") => "CUST-0007"
		public static async Task<string> NextAsync(IQueryable<string> existingNumbers, string prefix)
		{
			var suffixes = await existingNumbers
				.Where(n => n.StartsWith(prefix))
				.Select(n => n.Substring(prefix.Length))
				.ToListAsync();

			var max = suffixes.Select(s => int.TryParse(s, out var x) ? x : 0)
							  .DefaultIfEmpty(0)
							  .Max();

			return $"{prefix}{(max + 1):D4}";
		}
	}
}
