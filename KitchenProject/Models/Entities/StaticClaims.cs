using System.Security.Claims;

namespace KitchenProject.Models.Entities
{
	public class StaticClaims
	{
		public static List<Claim> Claims = new List<Claim>()
	 {
         //  new Claim(type,value),
         new Claim("Create","true"),
		 new Claim("Edit","true"),
		 new Claim("Delete","true"),
		 new Claim("EditUser","true"),// مثلا مبرمج لديه الصلاحية واخر لا، وكذلك يمكن منحها لآدمن
         new Claim("SiteState","true"), 
         //new Claim("UserProfile","true"), 
     };
	}
}
