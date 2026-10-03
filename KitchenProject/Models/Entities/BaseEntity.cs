using System.ComponentModel.DataAnnotations;

namespace KitchenProject.Models.Entities
{
    public class BaseEntity
    {
        public Guid Id { get; set; }

        [DisplayFormat(DataFormatString = "{0:dd-MM-yyyy hh:mm:ss tt}")]
        public DateTime Created { get; set; }

        [DisplayFormat(DataFormatString = "{0:dd-MM-yyyy hh:mm:ss tt}")]
        public DateTime? Modified { get; set;}


        //-------------------------For User Profile-------------------------
        public string CreatedDateLocalTime
        {
            get
            {
                return Created.ToLocalTime().ToString("dd-MM-yyyy hh:mm:ss tt");
            }
        }
        public string? ModifiedDateLocalTime
        {
            get
            {
                return Modified?.ToLocalTime().ToString("dd-MM-yyyy hh:mm:ss tt");
            }
        }
        //-------------------------------------------------------------------

    }
}
