using KitchenProject.Models.Entities;
using KitchenProject.Models.Enums;

namespace KitchenProject.Models
{
	/// <summary>
	/// بيانات البنود والأصناف الجاهزة المعتمدة للشركة (سوبر مات وهاي غلوس وإكسسوارات بلوم)
	/// </summary>
	internal static class FinishedCatalogSeed
	{
		// الحفاظ على المعرفات الثلاثة الأساسية الموجودة في الترحيل الأول
		internal static readonly Guid GroupSuperMatteId = new("a1000000-0000-4000-8000-00000000000a");
		internal static readonly Guid GroupHighGlossId = new("a2000000-0000-4000-8000-000000000002");
		internal static readonly Guid GroupCladdingId = new("a3000000-0000-4000-8000-000000000003");
		internal static readonly Guid GroupMarbleId = new("b1000000-0000-4000-8000-00000000000b");
		internal static readonly Guid GroupBlumDrawersId = new("c1000000-0000-4000-8000-00000000000c");
		internal static readonly Guid GroupAventosId = new("c2000000-0000-4000-8000-000000000002");
		internal static readonly Guid GroupOrganizersServoId = new("c3000000-0000-4000-8000-000000000003");
		internal static readonly Guid GroupAccessoriesLedId = new("c4000000-0000-4000-8000-000000000004");
		internal static readonly Guid GroupServicesId = new("d1000000-0000-4000-8000-000000000001");

		// المجموعات المخزنية (C1..C4): إكسسوارات وتجهيزات تُشترى وتدخل المخزن وتُباع في عروض الأسعار
		internal static readonly Guid[] StockGroupIds =
		[
			GroupBlumDrawersId,
			GroupAventosId,
			GroupOrganizersServoId,
			GroupAccessoriesLedId
		];

		internal static ItemGroup[] GetItemGroups(DateTime seedDate) =>
		[
			new() { Id = GroupSuperMatteId, Code = "A1", Name = "وحدات مطبخ - سوبر مات تركي MDF", Description = "دواليب المطبخ خشب سوبر مات تركي MDF مع إكسسوارات بلوم نمساوية", SortOrder = 1, Created = seedDate },
			new() { Id = GroupHighGlossId, Code = "A2", Name = "وحدات مطبخ - هاي غلوس تركي MDF", Description = "دواليب المطبخ خشب هاي غلوس تركي MDF مع إكسسوارات بلوم نمساوية", SortOrder = 2, Created = seedDate },
			new() { Id = GroupCladdingId, Code = "A3", Name = "التلبيس والأرفف والأجناب", Description = "تلبيس الحوائط والعلب والأرفف الخشبية وأجناب التلبيس", SortOrder = 3, Created = seedDate },
			new() { Id = GroupMarbleId, Code = "B", Name = "الأسطح والرخام", Description = "أسطح الرخام والجرانيت والكوارتز", SortOrder = 4, Created = seedDate },
			new() { Id = GroupBlumDrawersId, Code = "C1", Name = "أدراج بلوم النمساوية", Description = "أدراج بلوم ليقرا وانتارو وميريفو وموفينتو وتاندم", SortOrder = 5, Created = seedDate },
			new() { Id = GroupAventosId, Code = "C2", Name = "قلابات أفانتوس بلوم", Description = "آليات رفع الأبواب أفانتوس HF و HS و HL و HK بلوم", SortOrder = 6, Created = seedDate },
			new() { Id = GroupOrganizersServoId, Code = "C3", Name = "تقسيمات الأدراج وسيرفودرايف", Description = "تقسيم الملاعق وحاملات الصحون والبهارات ومحركات سيرفودرايف", SortOrder = 7, Created = seedDate },
			new() { Id = GroupAccessoriesLedId, Code = "C4", Name = "إكسسوارات وسلال وإضاءة LED", Description = "أدراج سامت وسلال التوابل والزاوية والسقطار والإضاءة والأجهزة", SortOrder = 8, Created = seedDate },
			new() { Id = GroupServicesId, Code = "D", Name = "النقل والتركيب والتجهيزات", Description = "خدمات النقل والمناولة والتركيب وطاولات العمل", SortOrder = 9, Created = seedDate }
		];

		private readonly record struct ItemSeedRow(Guid GroupId, string Category, string Name, string Unit, decimal SalePrice, string? Notes = null);

		private static readonly ItemSeedRow[] Rows =
		[
			// A1: وحدات مطبخ - سوبر مات تركي MDF (1 - 16)
			new(GroupSuperMatteId, "سوبر مات تركي MDF", "دولاب مطبخ علوي عمق 35 سم وأرتفاع 72 سم (سوبر مات)", "متر طولي", 1250m),
			new(GroupSuperMatteId, "سوبر مات تركي MDF", "دولاب مطبخ سفلي عمق 60 سم وأرتفاع 87.5 سم (سوبر مات)", "متر طولي", 1500m),
			new(GroupSuperMatteId, "سوبر مات تركي MDF", "دولاب مطبخ الجزء العلوي المعلق عمق 60 سم وأرتفاع 60 سم (سوبر مات)", "متر طولي", 1450m),
			new(GroupSuperMatteId, "سوبر مات تركي MDF", "دولاب مطبخ سفلي (بار) عمق 60 سم وأرتفاع 87.5 سم (سوبر مات)", "متر طولي", 1500m),
			new(GroupSuperMatteId, "سوبر مات تركي MDF", "دولاب مطبخ الجزء الكامل عمق 60 سم وأرتفاع 219 سم (سوبر مات)", "متر طولي", 3625m),
			new(GroupSuperMatteId, "سوبر مات تركي MDF", "دولاب مطبخ علوي فوق الثلاجة عمق 60 سم (سوبر مات)", "متر طولي", 1450m, "الارتفاع = (حسب ارتفاع الثلاجة) + 3 سم، العرض = (عرض الثلاجة + 5 سم أو 7 سم)"),
			new(GroupSuperMatteId, "سوبر مات تركي MDF", "دولاب مطبخ الجزء العلوي 35 سم وأرتفاع 72 سم (زجاج محلي) (سوبر مات)", "متر طولي", 1300m),
			new(GroupSuperMatteId, "سوبر مات تركي MDF", "دولاب مطبخ سفلي عمق 60 سم وأرتفاع 87.5 سم (زجاج محلي) (سوبر مات)", "متر طولي", 1500m),
			new(GroupSuperMatteId, "سوبر مات تركي MDF", "دولاب مطبخ الجزء العلوي المعلق عمق 60 سم وأرتفاع 60 سم (زجاج محلي) (سوبر مات)", "متر طولي", 1500m),
			new(GroupSuperMatteId, "سوبر مات تركي MDF", "دولاب مطبخ الجزء الكامل عمق 60 سم وأرتفاع 219 (زجاج محلي) (سوبر مات)", "متر طولي", 3865m),
			new(GroupSuperMatteId, "سوبر مات تركي MDF", "دولاب مطبخ الجزء الكامل زجاج من جهتين عمق 60 سم وأرتفاع 219 (زجاج محلي) (سوبر مات)", "متر طولي", 4600m),
			new(GroupSuperMatteId, "سوبر مات تركي MDF", "دولاب مطبخ الجزء العلوي 35 سم وأرتفاع 72 سم (زجاج بلجيكي) (سوبر مات)", "متر طولي", 1525m),
			new(GroupSuperMatteId, "سوبر مات تركي MDF", "دولاب مطبخ سفلي عمق 60 سم وأرتفاع 87.5 سم (زجاج بلجيكي) (سوبر مات)", "متر طولي", 1750m),
			new(GroupSuperMatteId, "سوبر مات تركي MDF", "دولاب مطبخ الجزء العلوي المعلق عمق 60 سم وأرتفاع 60 سم (زجاج بلجيكي) (سوبر مات)", "متر طولي", 1700m),
			new(GroupSuperMatteId, "سوبر مات تركي MDF", "دولاب مطبخ الجزء الكامل عمق 60 سم وأرتفاع 219 (زجاج بلجيكي) (سوبر مات)", "متر طولي", 4475m),
			new(GroupSuperMatteId, "سوبر مات تركي MDF", "دولاب مطبخ الجزء الكامل زجاج من جهتين عمق 60 سم وأرتفاع 219 (زجاج بلجيكي) (سوبر مات)", "متر طولي", 5100m),

			// A2: وحدات مطبخ - هاي غلوس تركي MDF (17 - 32)
			new(GroupHighGlossId, "هاي غلوس تركي MDF", "دولاب مطبخ علوي عمق 35 سم وأرتفاع 72 سم (هاي غلوس)", "متر طولي", 1000m),
			new(GroupHighGlossId, "هاي غلوس تركي MDF", "دولاب مطبخ سفلي عمق 60 سم وأرتفاع 87.5 سم (هاي غلوس)", "متر طولي", 1200m),
			new(GroupHighGlossId, "هاي غلوس تركي MDF", "دولاب مطبخ الجزء العلوي المعلق عمق 60 سم وأرتفاع 60 سم (هاي غلوس)", "متر طولي", 1000m),
			new(GroupHighGlossId, "هاي غلوس تركي MDF", "دولاب مطبخ سفلي (بار) عمق 60 سم وأرتفاع 87.5 سم (هاي غلوس)", "متر طولي", 1200m),
			new(GroupHighGlossId, "هاي غلوس تركي MDF", "دولاب مطبخ الجزء الكامل عمق 60 سم وأرتفاع 219 سم (هاي غلوس)", "متر طولي", 3100m),
			new(GroupHighGlossId, "هاي غلوس تركي MDF", "دولاب مطبخ علوي فوق الثلاجة عمق 60 سم (هاي غلوس)", "متر طولي", 1000m, "الارتفاع = (حسب ارتفاع الثلاجة) + 3 سم، العرض = (عرض الثلاجة + 5 سم أو 7 سم)"),
			new(GroupHighGlossId, "هاي غلوس تركي MDF", "دولاب مطبخ الجزء العلوي 35 سم وأرتفاع 72 سم (زجاج محلي) (هاي غلوس)", "متر طولي", 1300m),
			new(GroupHighGlossId, "هاي غلوس تركي MDF", "دولاب مطبخ سفلي عمق 60 سم وأرتفاع 87.5 سم (زجاج محلي) (هاي غلوس)", "متر طولي", 0m),
			new(GroupHighGlossId, "هاي غلوس تركي MDF", "دولاب مطبخ الجزء العلوي المعلق عمق 60 سم وأرتفاع 60 سم (زجاج محلي) (هاي غلوس)", "متر طولي", 0m),
			new(GroupHighGlossId, "هاي غلوس تركي MDF", "دولاب مطبخ الجزء الكامل عمق 60 سم وأرتفاع 219 (زجاج محلي) (هاي غلوس)", "متر طولي", 3740m),
			new(GroupHighGlossId, "هاي غلوس تركي MDF", "دولاب مطبخ الجزء الكامل زجاج من جهتين عمق 60 سم وأرتفاع 219 (زجاج محلي) (هاي غلوس)", "متر طولي", 0m),
			new(GroupHighGlossId, "هاي غلوس تركي MDF", "دولاب مطبخ الجزء العلوي 35 سم وأرتفاع 72 سم (زجاج بلجيكي) (هاي غلوس)", "متر طولي", 1300m),
			new(GroupHighGlossId, "هاي غلوس تركي MDF", "دولاب مطبخ سفلي عمق 60 سم وأرتفاع 87.5 سم (زجاج بلجيكي) (هاي غلوس)", "متر طولي", 1400m),
			new(GroupHighGlossId, "هاي غلوس تركي MDF", "دولاب مطبخ الجزء العلوي المعلق عمق 60 سم وأرتفاع 60 سم (زجاج بلجيكي) (هاي غلوس)", "متر طولي", 1200m),
			new(GroupHighGlossId, "هاي غلوس تركي MDF", "دولاب مطبخ الجزء الكامل عمق 60 سم وأرتفاع 219 (زجاج بلجيكي) (هاي غلوس)", "متر طولي", 4475m),
			new(GroupHighGlossId, "هاي غلوس تركي MDF", "دولاب مطبخ الجزء الكامل زجاج من جهتين عمق 60 سم وأرتفاع 219 (زجاج بلجيكي) (هاي غلوس)", "متر طولي", 5100m),

			// A3: التلبيس والأرفف والأجناب (33 - 46)
			new(GroupCladdingId, "التلبيس والأرفف", "تلبيس خلفية شفاط", "متر طولي", 0m),
			new(GroupCladdingId, "التلبيس والأرفف", "تلبيس العلوي من أسفل عمق 35 سم", "متر طولي", 450m),
			new(GroupCladdingId, "التلبيس والأرفف", "تلبيس العلوي المعلق من أسفل عمق 60 سم", "متر طولي", 450m),
			new(GroupCladdingId, "التلبيس والأرفف", "تلبيس قرصة خشبية للجزء السفلي عمق 60 سم", "متر طولي", 450m),
			new(GroupCladdingId, "التلبيس والأرفف", "تلبيس 10 سم", "متر طولي", 90m),
			new(GroupCladdingId, "التلبيس والأرفف", "تلبيس حائط مودرن سمك 18 (المتر المربع)", "متر مربع", 600m),
			new(GroupCladdingId, "التلبيس والأرفف", "تلبيس حائط مودرن سمك 36 (المتر المربع)", "متر مربع", 800m),
			new(GroupCladdingId, "التلبيس والأرفف", "تلبيس حائط مودرن سمك 20 سم (المتر المربع)", "متر مربع", 1250m),
			new(GroupCladdingId, "التلبيس والأرفف", "ارفف خشبية (1.8 سم) عمق 20 سم", "قطعة", 450m),
			new(GroupCladdingId, "التلبيس والأرفف", "ارفف خشبية دبل (3.6 سم) عمق 20 سم", "قطعة", 450m),
			new(GroupCladdingId, "التلبيس والأرفف", "جنب تلبيس علوي عمق 35 سم وأرتفاع 72 سم", "قطعة", 450m),
			new(GroupCladdingId, "التلبيس والأرفف", "جنب تلبيس سفلي عمق 60 سم وأرتفاع 72 سم", "قطعة", 500m),
			new(GroupCladdingId, "التلبيس والأرفف", "جنب تلبيس كامل عمق 60 سم وأرتفاع 204 سم", "قطعة", 800m),
			new(GroupCladdingId, "التلبيس والأرفف", "جنب تلبيس عمق 60 سم وأرتفاع 132 سم", "قطعة", 600m),

			// C1: أدراج بلوم النمساوية (47 - 85)
			new(GroupBlumDrawersId, "أدراج بلوم", "درج ليقرا صغير M بلوم", "قطعة", 520m),
			new(GroupBlumDrawersId, "أدراج بلوم", "درج ليقرا كبير C بلوم", "قطعة", 0m),
			new(GroupBlumDrawersId, "أدراج بلوم", "درج ليقرا كبير F بلوم", "قطعة", 790m),
			new(GroupBlumDrawersId, "أدراج بلوم", "درج ليقرا M صغير TOB بلوم", "قطعة", 830m),
			new(GroupBlumDrawersId, "أدراج بلوم", "درج ليقرا C كبير TOB بلوم", "قطعة", 930m),
			new(GroupBlumDrawersId, "أدراج بلوم", "درج ليقرا F كبير TOB بلوم", "قطعة", 1000m),
			new(GroupBlumDrawersId, "أدراج بلوم", "درج انتارو M صغير بلوم", "قطعة", 440m),
			new(GroupBlumDrawersId, "أدراج بلوم", "درج انتارو D كبير بالماسورة بلوم", "قطعة", 600m),
			new(GroupBlumDrawersId, "أدراج بلوم", "درج انتارو D / U كبير تحت الحوض بالماسورة بلوم", "قطعة", 650m, "درج انتارو D+U"),
			new(GroupBlumDrawersId, "أدراج بلوم", "درج انتارو M صغير TOB بلوم", "قطعة", 745m),
			new(GroupBlumDrawersId, "أدراج بلوم", "درج انتارو D كبير بالماسورة TOB بلوم", "قطعة", 780m),
			new(GroupBlumDrawersId, "أدراج بلوم", "درج ميريفو M صغير بلوم", "قطعة", 480m),
			new(GroupBlumDrawersId, "أدراج بلوم", "درج ميريفو E بالماسورة كبير بلوم", "قطعة", 550m),
			new(GroupBlumDrawersId, "أدراج بلوم", "درج ميريفو كبير E اجناب اضافية بلوم", "قطعة", 620m),
			new(GroupBlumDrawersId, "أدراج بلوم", "درج ميريفو M صغير TOB بلوم", "قطعة", 650m),
			new(GroupBlumDrawersId, "أدراج بلوم", "درج ميريفو E بالماسورة TOB بلوم", "قطعة", 720m),
			new(GroupBlumDrawersId, "أدراج بلوم", "درج ميريفو كبير E اجناب اضافية TOB بلوم", "قطعة", 800m),
			new(GroupBlumDrawersId, "أدراج بلوم", "درج ليقرا M بلوم انجليزي", "قطعة", 750m),
			new(GroupBlumDrawersId, "أدراج بلوم", "درج ليقرا C كبير بلوم انجليزي (سوبر مات)", "قطعة", 1200m),
			new(GroupBlumDrawersId, "أدراج بلوم", "درج ليقرا C كبير بلوم انجليزي (هاي غلوس)", "قطعة", 880m),
			new(GroupBlumDrawersId, "أدراج بلوم", "درج انتارو M صغير بلوم انجليزي", "قطعة", 600m),
			new(GroupBlumDrawersId, "أدراج بلوم", "درج انتارو D بالماسورة بلوم انجليزي (سوبر مات)", "قطعة", 600m),
			new(GroupBlumDrawersId, "أدراج بلوم", "درج انتارو D بالماسورة بلوم انجليزي (هاي غلوس)", "قطعة", 1200m),
			new(GroupBlumDrawersId, "أدراج بلوم", "درج موريفو E وجه ماسورة وجنب مواسير بلوم انجليزي", "قطعة", 800m),
			new(GroupBlumDrawersId, "أدراج بلوم", "درج موريفو E وجه ماسورة بالاجناب الاضافية معدنية بلوم انجليزي", "قطعة", 870m),
			new(GroupBlumDrawersId, "أدراج بلوم", "درج ليقرا M صغير بلوم انجليزي TOB", "قطعة", 930m),
			new(GroupBlumDrawersId, "أدراج بلوم", "درج ليقرا C كبير بلوم انجليزي TOB", "قطعة", 1200m),
			new(GroupBlumDrawersId, "أدراج بلوم", "درج انتارو M صغير بلوم انجليزي TOB", "قطعة", 780m),
			new(GroupBlumDrawersId, "أدراج بلوم", "درج انتارو D بالماسورة بلوم انجليزي TOB", "قطعة", 1200m),
			new(GroupBlumDrawersId, "أدراج بلوم", "درج موريفو E وجه ماسورة وجنب مواسير بلوم انجليزي TOB", "قطعة", 980m),
			new(GroupBlumDrawersId, "أدراج بلوم", "درج موريفو E وجه ماسورة بالاجناب الاضافية معدنية بلوم انجليزي TOB", "قطعة", 1050m),
			new(GroupBlumDrawersId, "أدراج بلوم", "درج موفينتو بلوم", "قطعة", 450m),
			new(GroupBlumDrawersId, "أدراج بلوم", "درج موفينتو TOB بلوم", "قطعة", 630m),
			new(GroupBlumDrawersId, "أدراج بلوم", "درج موفينتو رف متحرك بلوم", "قطعة", 580m),
			new(GroupBlumDrawersId, "أدراج بلوم", "درج موفينتو رف متحرك TOB بلوم", "قطعة", 630m),
			new(GroupBlumDrawersId, "أدراج بلوم", "درج تاندم بلومشن بلوم", "قطعة", 350m),
			new(GroupBlumDrawersId, "أدراج بلوم", "درج تاندم كبير بلوم", "قطعة", 350m),
			new(GroupBlumDrawersId, "أدراج بلوم", "درج تاندم TIP ON بلوم (سوبر مات)", "قطعة", 400m),
			new(GroupBlumDrawersId, "أدراج بلوم", "درج تاندم TIP ON بلوم (هاي غلوس)", "قطعة", 450m),

			// C2: قلابات أفانتوس بلوم (86 - 105)
			new(GroupAventosId, "أفانتوس بلوم", "افانتوس 28 HF بلوم", "قطعة", 1400m),
			new(GroupAventosId, "أفانتوس بلوم", "افانتوس 25 HF بلوم", "قطعة", 1300m),
			new(GroupAventosId, "أفانتوس بلوم", "افانتوس 22 HF بلوم", "قطعة", 1200m),
			new(GroupAventosId, "أفانتوس بلوم", "افانتوس I HS بلوم", "قطعة", 1000m),
			new(GroupAventosId, "أفانتوس بلوم", "افانتوس H HS بلوم", "قطعة", 750m),
			new(GroupAventosId, "أفانتوس بلوم", "افانتوس G HS بلوم", "قطعة", 750m),
			new(GroupAventosId, "أفانتوس بلوم", "افانتوس 29 HL بلوم", "قطعة", 1100m),
			new(GroupAventosId, "أفانتوس بلوم", "افانتوس 27 HL بلوم", "قطعة", 900m),
			new(GroupAventosId, "أفانتوس بلوم", "افانتوس 25 HL بلوم", "قطعة", 835m),
			new(GroupAventosId, "أفانتوس بلوم", "افانتوس HK TOP 25 بلوم", "قطعة", 580m),
			new(GroupAventosId, "أفانتوس بلوم", "افانتوس HK TOP 27 بلوم", "قطعة", 580m),
			new(GroupAventosId, "أفانتوس بلوم", "افانتوس HK TOP 29 بلوم", "قطعة", 640m),
			new(GroupAventosId, "أفانتوس بلوم", "افانتوس HK TOP 25 Tip on بلوم", "قطعة", 635m),
			new(GroupAventosId, "أفانتوس بلوم", "افانتوس HK TOP 27 Tip on بلوم", "قطعة", 660m),
			new(GroupAventosId, "أفانتوس بلوم", "افانتوس HK TOP 29 Tip on بلوم", "قطعة", 720m),
			new(GroupAventosId, "أفانتوس بلوم", "افانتوس E / HK-S بلوم", "قطعة", 350m),
			new(GroupAventosId, "أفانتوس بلوم", "افانتوس HK-XS بلوم", "قطعة", 350m),
			new(GroupAventosId, "أفانتوس بلوم", "افانتوس دبل HK-XS بلوم", "قطعة", 690m),
			new(GroupAventosId, "أفانتوس بلوم", "افانتوس HK-XS Tip on بلوم", "قطعة", 380m),
			new(GroupAventosId, "أفانتوس بلوم", "افانتوس دبل HK-XS Tip on بلوم", "قطعة", 750m),

			// C3: تقسيمات الأدراج وسيرفودرايف (106 - 114)
			new(GroupOrganizersServoId, "تقسيمات وسيرفودرايف", "تقسيم ملاعق 90", "قطعة", 1500m),
			new(GroupOrganizersServoId, "تقسيمات وسيرفودرايف", "تقسيم ملاعق 60", "قطعة", 100m),
			new(GroupOrganizersServoId, "تقسيمات وسيرفودرايف", "حاملة صحون", "قطعة", 400m),
			new(GroupOrganizersServoId, "تقسيمات وسيرفودرايف", "حاملة بهارات", "قطعة", 400m),
			new(GroupOrganizersServoId, "تقسيمات وسيرفودرايف", "قاطعة سلفر و اليمنيوم مزدوجة", "قطعة", 600m),
			new(GroupOrganizersServoId, "تقسيمات وسيرفودرايف", "ملحقات سيرفودرايف HS / HL", "قطعة", 4200m),
			new(GroupOrganizersServoId, "تقسيمات وسيرفودرايف", "ملحقات سيرفودرايف HF", "قطعة", 4100m),
			new(GroupOrganizersServoId, "تقسيمات وسيرفودرايف", "محرك درج أونو", "قطعة", 1600m),
			new(GroupOrganizersServoId, "تقسيمات وسيرفودرايف", "محرك سيرفو فليكس (ثلاجة وغسالة)", "قطعة", 3000m, "محرك سيرفو فليكس (ثلاجة + غسالة)"),

			// C4: إكسسوارات وسلال وإضاءة LED (115 - 133)
			new(GroupAccessoriesLedId, "إكسسوارات تركية وصينية", "درج سامت تركي صغير", "قطعة", 350m),
			new(GroupAccessoriesLedId, "إكسسوارات تركية وصينية", "درج سامت تركي كبير", "قطعة", 400m),
			new(GroupAccessoriesLedId, "إكسسوارات تركية وصينية", "قلاب مزطوري", "قطعة", 80m),
			new(GroupAccessoriesLedId, "إكسسوارات تركية وصينية", "قلاب مزطوري دبل", "قطعة", 160m),
			new(GroupAccessoriesLedId, "إكسسوارات تركية وصينية", "سلة توابل 15سم صيني درجة اولي", "قطعة", 500m),
			new(GroupAccessoriesLedId, "إكسسوارات تركية وصينية", "سلة توابل 24 سم صيني درجة اولي", "قطعة", 550m),
			new(GroupAccessoriesLedId, "إكسسوارات تركية وصينية", "سلة زاوية كروم صيني درجة اولي", "قطعة", 800m),
			new(GroupAccessoriesLedId, "إكسسوارات تركية وصينية", "سلة زاوية فاصوليا صيني درجة اولي", "قطعة", 1100m),
			new(GroupAccessoriesLedId, "إكسسوارات تركية وصينية", "سلة زاوية خبز صيني درجة اولي", "قطعة", 500m),
			new(GroupAccessoriesLedId, "إكسسوارات تركية وصينية", "سقطار متحرك صيني درجة اولي- 90", "قطعة", 1100m),
			new(GroupAccessoriesLedId, "إكسسوارات تركية وصينية", "سقطار متحرك صيني درجة اولي- 80", "قطعة", 1050m),
			new(GroupAccessoriesLedId, "إكسسوارات تركية وصينية", "سقطار متحرك صيني درجة اولي- 70", "قطعة", 1000m),
			new(GroupAccessoriesLedId, "إكسسوارات تركية وصينية", "سقطار ثابت صيني درجة اولي-90", "قطعة", 170m),
			new(GroupAccessoriesLedId, "إكسسوارات تركية وصينية", "سقطار ثابت صيني درجة اولي-80", "قطعة", 170m),
			new(GroupAccessoriesLedId, "إضاءة LED", "توريد وتركيب إضاءة (LED) صيني درجة اولي لفنرينة الزجاج", "متر طولي", 170m),
			new(GroupAccessoriesLedId, "إضاءة LED", "توريد وتركيب إضاءة (LED) صيني درجة اولي العلوي من اسفل", "متر طولي", 170m),
			new(GroupAccessoriesLedId, "إضاءة LED", "توريد وتركيب إضاءة (LED) صيني درجة اولي للسفلي من سكلف", "متر طولي", 170m),
			new(GroupAccessoriesLedId, "أجهزة وإكسسوارات ذكية", "اكسسوار متحرك عمودى (أوامر صوتيه) 90", "قطعة", 8950m),
			new(GroupAccessoriesLedId, "أجهزة وإكسسوارات ذكية", "شفاط خلف الغاز على السطح", "قطعة", 9750m),

			// D: النقل والتركيب والتجهيزات (134 - 136)
			new(GroupServicesId, "الخدمات والتجهيزات", "نقل ومناولة", "خدمة", 400m),
			new(GroupServicesId, "الخدمات والتجهيزات", "طاوله نشاره", "قطعة", 2250m, "1م*"),
			new(GroupServicesId, "الخدمات والتجهيزات", "تركيب معدات", "خدمة", 400m)
		];

		internal static Item[] GetFinishedItems(DateTime seedDate) =>
			Rows.Select((row, index) =>
			{
				var number = index + 1;
				return new Item
				{
					Id = new Guid($"f0000000-0000-4000-8000-{number:d12}"),
					Code = $"FG-{number:D4}",
					Name = row.Name,
					Type = StockGroupIds.Contains(row.GroupId) ? ItemType.Raw : ItemType.Finished,
					ItemGroupId = row.GroupId,
					Category = row.Category,
					Unit = row.Unit,
					PurchasePrice = 0m,
					SalePrice = row.SalePrice,
					MinQuantity = 0m,
					Notes = row.Notes,
					IsActive = true,
					Created = seedDate
				};
			}).ToArray();
	}
}
