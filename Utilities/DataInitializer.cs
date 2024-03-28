using TeckNews.Data;
using TeckNews.Entities;
using Microsoft.EntityFrameworkCore;

namespace TeckNews.Utilities;

public class DataInitializer
{
    internal static void Initialize(TeckNewsContext context, IConfiguration configuration)
    {
        context.Database.Migrate();
        InitData(context, configuration);
    }

    private static void InitData(TeckNewsContext context, IConfiguration configuration)
    {
        if(!context.Users.Any(x=>x.UserType == Entities.UserType.Admin))
        {
            var user = configuration.GetSection("AdminInfo").Get<User>();
            if (user != null)
            {
                user.UserType = UserType.Admin;
                context.Add(user);

                context.SaveChanges();
            }
        }
    }
}

