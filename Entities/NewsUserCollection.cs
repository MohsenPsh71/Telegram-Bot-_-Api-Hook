using System.ComponentModel.DataAnnotations.Schema;

namespace TeckNews.Entities
{
    public class NewsUserCollection
    {
        public int Id { get; set; }

        public int NewsId { get; set; }
        [ForeignKey(nameof(NewsId))]
        public News News { get; set; }

        public int UserId { get; set; }
        [ForeignKey(nameof(UserId))]
        public User User { get; set; }
    }
}
