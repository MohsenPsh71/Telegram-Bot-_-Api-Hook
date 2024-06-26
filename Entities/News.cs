﻿namespace TeckNews.Entities
{
    public class News
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Desc { get; set; }
        public int MessageId { get; set; }

        public DateTime CreateDate { get; set; }

        public ICollection<NewsKeyWord> NewsKeyWords { get; set; }

    }
}
