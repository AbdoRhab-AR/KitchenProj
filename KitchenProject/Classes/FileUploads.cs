namespace KitchenProject.Classes
{
	public static class FileUploads
	{
		public static readonly string[] Images = { ".jpg", ".jpeg", ".png", ".webp" };
		public static readonly string[] Documents = { ".pdf", ".jpg", ".jpeg", ".png" };

		// يحفظ الملف داخل wwwroot/uploads/{folder} باسم عشوائي ويرجع رابطه، أو رسالة خطأ
		public static async Task<(string? Url, string? Error)> SaveAsync(
			IFormFile file, IWebHostEnvironment env, string folder, string[] allowedExtensions, int maxMb = 10)
		{
			var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
			if (!allowedExtensions.Contains(ext))
				return (null, $"نوع الملف غير مسموح. المسموح: {string.Join("، ", allowedExtensions)}");

			if (file.Length > maxMb * 1024L * 1024L)
				return (null, $"حجم الملف أكبر من {maxMb} ميجابايت");

			var dir = Path.Combine(env.WebRootPath, "uploads", folder);
			Directory.CreateDirectory(dir);

			var name = $"{Guid.NewGuid():N}{ext}";
			using (var stream = new FileStream(Path.Combine(dir, name), FileMode.Create))
			{
				await file.CopyToAsync(stream);
			}

			return ($"/uploads/{folder}/{name}", null);
		}

		// يحذف ملفاً سبق رفعه (يتجاهل الروابط خارج مجلد uploads)
		public static void Delete(string? url, IWebHostEnvironment env)
		{
			if (string.IsNullOrEmpty(url) || !url.StartsWith("/uploads/"))
				return;

			var uploadsRoot = Path.GetFullPath(Path.Combine(env.WebRootPath, "uploads"));
			var path = Path.GetFullPath(Path.Combine(env.WebRootPath, url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)));
			if (path.StartsWith(uploadsRoot, StringComparison.OrdinalIgnoreCase) && File.Exists(path))
				File.Delete(path);
		}
	}
}
