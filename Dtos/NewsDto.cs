namespace TeckNews.Dtos
{
    public class NewsDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Desc { get; set; }
        public int MessageId { get; set; }
        public List<int> KeyWords { get; set; }

    }
}
