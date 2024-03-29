using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TeckNews.Entities
{
    public class User
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Username { get; set; }
        public long ChatId { get; set; }
        public UserType UserType { get; set; }

        public int? ParentId { get; set; }
        [ForeignKey(nameof(ParentId))]
        public User Parent { get; set; }

        public ICollection<UserActivity> UserActivities { get; set; }
    }

    public enum UserType
    {
        Guest,
        Admin
    }
}
