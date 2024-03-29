using System.ComponentModel.DataAnnotations.Schema;

namespace TeckNews.Entities
{
    public class UserActivity
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        [ForeignKey(nameof(UserId))]
        public User User { get; set; }

        public ActivityType ActivityType { get; set; }
    }

    public enum ActivityType
    {
        StartBot,
        GetLocation,
        GetContactUs,
        GetMoney,
        GetProfile,
        BackToMainMenu,
        EditFirstName,
        GetEditFirstNameConfirmation,
        PleaseRetry,
        GetHeadOfNews,
        Search,
        ShowSearchResult,
        SaveNews,
        UnSaveNews
    }
}
