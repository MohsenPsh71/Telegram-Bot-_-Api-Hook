using System.ComponentModel.DataAnnotations.Schema;

namespace TeckNews.Entities
{
    public class NewsKeyWord
    {
        public int Id { get; set; }

        public int NewsId { get; set; }
        [ForeignKey(nameof(NewsId))]
        public News News { get; set; }

        public int KeyWordId { get; set; }
        [ForeignKey(nameof(KeyWordId))]
        public KeyWord KeyWord { get; set; }
    }
}
