using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using TeckNews.Entities;
using TeckNews.Repositories;
using TeckNews.Utilities;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using User = TeckNews.Entities.User;

namespace TeckNews.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BotManagementController : ControllerBase
    {
        private readonly TelegramBotClient _bot;
        private readonly IBaseRepository<User> _userRepository;
        private readonly IBaseRepository<UserActivity> _userActivityRepository;
        private readonly IBaseRepository<News> _newsRepository;
        private readonly IBaseRepository<NewsKeyWord> _newsKeyWordsRepository;
        private readonly IBaseRepository<NewsUserCollection> _newsUserCollectionRepository;

        public BotManagementController(IBaseRepository<User> userRepository, IBaseRepository<UserActivity> userActivityRepository, IBaseRepository<News> newsRepository, IBaseRepository<NewsKeyWord> newsKeyWordsRepository, IBaseRepository<NewsUserCollection> newsUserCollectionRepository)
        {
            _bot = new TelegramBotClient("6026122963:AAFivrrnCEbaXa8ZacZZR2SwO5KDlM4vfHI");
            _userRepository = userRepository;
            _userActivityRepository = userActivityRepository;
            _newsRepository = newsRepository;
            _newsKeyWordsRepository = newsKeyWordsRepository;
            _newsUserCollectionRepository = newsUserCollectionRepository;
        }

        [HttpGet("[action]")]
        public async Task<ActionResult> SetWebhook()
        {
            var allowedUpdates = new UpdateType[]
            {
                UpdateType.Message,
                UpdateType.CallbackQuery
            };

            await _bot.SetWebhookAsync(url: "https://ame58wevpir8rmqnanc5hw.hooks.webhookrelay.com", allowedUpdates: allowedUpdates);
            return Ok();
        }

        [HttpPost("[action]")]
        public async Task<ActionResult> ReceiveUpdate(object model, CancellationToken cancellationToken)
        {
            var update = JsonConvert.DeserializeObject<Update>(model.ToString());

            if (update == null)
                return Ok();

            long chatId = update.CallbackQuery != null ? update.CallbackQuery.From.Id : update.Message.Chat.Id;

            var user = await _userRepository.Table.FirstOrDefaultAsync(x => x.ChatId == chatId);

            if (user == null)
            {
                user = await _userRepository.AddAsync(new User()
                {
                    ChatId = chatId,
                    FirstName = update.Message.From.FirstName,
                    LastName = update.Message.From.LastName,
                    Username = update.Message.From.Username,
                    UserType = UserType.Guest,
                    ParentId = null,
                    UserActivities = new List<UserActivity>()
                        {
                            new UserActivity() { ActivityType = ActivityType.StartBot }
                        }
                }, cancellationToken);
            }

            if (update.CallbackQuery != null)
            {
                var text = update.CallbackQuery.Data;

                var lastActivity = _userActivityRepository.TableNoTracking.Where(x => x.UserId == user.Id).OrderByDescending(x => x.Id).First();

                if (text == "Canceled")
                {
                    await _bot.SendTextMessageAsync(chatId, DefaultContents.PleaseRetry);
                }
                else if (text.StartsWith("Confirmed_"))
                {
                    var value = text.Split("_")[1].ToString();

                    user.FirstName = value;
                    await _userRepository.UpdateAsync(user, cancellationToken);
                }
                else if (text.StartsWith("Save_"))
                {
                    int newsId = 0;
                    if (int.TryParse(text.Split("_")[1], out newsId))
                    {
                        var news = await _newsRepository.TableNoTracking.FirstOrDefaultAsync(x => x.Id == newsId);
                        if (news == null)
                        {
                            await _bot.SendTextMessageAsync(chatId, DefaultContents.NewsNotFound);
                            return Ok();
                        }

                        await _userActivityRepository.AddAsync(new UserActivity() { UserId = user.Id, ActivityType = ActivityType.SaveNews }, cancellationToken);

                        await _newsUserCollectionRepository.AddAsync(new NewsUserCollection()
                        {
                            NewsId = newsId,
                            UserId = user.Id
                        }, cancellationToken);

                        string message = await GenerateMessageText(news);
                        await _bot.EditMessageTextAsync(chatId, update.CallbackQuery.Message.MessageId, message, ParseMode.Html, replyMarkup: GenerateNewsInlineKeyboard(newsId, false));
                    }
                    else
                        await _bot.SendTextMessageAsync(chatId, DefaultContents.MessageIsNotValid);
                }
                else if (text.StartsWith("UnSave_"))
                {
                    int newsId = 0;
                    if (int.TryParse(text.Split("_")[1], out newsId))
                    {
                        var news = await _newsRepository.TableNoTracking.FirstOrDefaultAsync(x => x.Id == newsId);
                        if (news == null)
                        {
                            await _bot.SendTextMessageAsync(chatId, DefaultContents.NewsNotFound);
                            return Ok();
                        }

                        if (!_newsUserCollectionRepository.TableNoTracking.Any(x => x.UserId == user.Id && x.NewsId == newsId))
                            return Ok();

                        await _userActivityRepository.AddAsync(new UserActivity() { UserId = user.Id, ActivityType = ActivityType.UnSaveNews }, cancellationToken);

                        string message = await GenerateMessageText(news);
                        await _bot.EditMessageTextAsync(chatId, update.CallbackQuery.Message.MessageId, message, ParseMode.Html, replyMarkup: GenerateNewsInlineKeyboard(newsId, true));
                        //await _bot.SendTextMessageAsync(chatId, message, ParseMode.Html, replyMarkup: GenerateNewsInlineKeyboard(newsId, false));
                    }
                    else
                        await _bot.SendTextMessageAsync(chatId, DefaultContents.MessageIsNotValid);
                }
            }

            if (update.Message != null)
            {
                var text = update.Message.Text;

                var lastActivity = _userActivityRepository.TableNoTracking.Where(x => x.UserId == user.Id).OrderByDescending(x => x.Id).First();

                if (text == DefaultContents.Start)
                {
                    await _userActivityRepository.AddAsync(new UserActivity() { UserId = user.Id, ActivityType = ActivityType.StartBot }, cancellationToken);
                    await _bot.SendTextMessageAsync(chatId, DefaultContents.WelcomeToBot, replyMarkup: GenerateMainKeyboard());
                }
                else if (text == DefaultContents.Location)
                {
                    await _userActivityRepository.AddAsync(new UserActivity() { UserId = user.Id, ActivityType = ActivityType.GetLocation }, cancellationToken);
                    await _bot.SendVenueAsync(chatId, 45.87654, 56.76543, "دفتر مرکزی", "خیابان ایکس پلاک 2");
                }
                else if (text == DefaultContents.ContactUs)
                {
                    await _userActivityRepository.AddAsync(new UserActivity() { UserId = user.Id, ActivityType = ActivityType.GetContactUs }, cancellationToken);
                    await _bot.SendTextMessageAsync(chatId, DefaultContents.ContactUsMessage);
                }
                else if (text == DefaultContents.Money)
                {
                    await _userActivityRepository.AddAsync(new UserActivity() { UserId = user.Id, ActivityType = ActivityType.GetMoney }, cancellationToken);
                    await _bot.SendTextMessageAsync(chatId, DefaultContents.MoneyMessage);
                }

                else if (text == DefaultContents.Profile)
                {
                    await _userActivityRepository.AddAsync(new UserActivity() { UserId = user.Id, ActivityType = ActivityType.GetProfile }, cancellationToken);
                    await _bot.SendTextMessageAsync(chatId, string.Format(DefaultContents.ProfileDetail, user.FirstName, user.LastName ?? "___", user.Username ?? "___"), replyMarkup: GenerateProfileKeyboard());
                }
                else if (text == DefaultContents.BackToMainMenu)
                {
                    await _userActivityRepository.AddAsync(new UserActivity() { UserId = user.Id, ActivityType = ActivityType.GetProfile }, cancellationToken);
                    await _bot.SendTextMessageAsync(chatId, DefaultContents.BackToMainMenuMessage, replyMarkup: GenerateMainKeyboard());
                }
                else if (text == DefaultContents.EditFirstName)
                {
                    await _userActivityRepository.AddAsync(new UserActivity() { UserId = user.Id, ActivityType = ActivityType.EditFirstName }, cancellationToken);
                    await _bot.SendTextMessageAsync(chatId, DefaultContents.EditFirstNameMessage);
                }

                else if (text == DefaultContents.HeadOfNews)
                {
                    await _userActivityRepository.AddAsync(new UserActivity() { UserId = user.Id, ActivityType = ActivityType.GetHeadOfNews }, cancellationToken);

                    var today = DateTime.Now.Date;
                    var items = await _newsRepository.TableNoTracking.Where(x => x.CreateDate.Date == today).Select(x => new { x.Id, x.Title }).Take(10).ToListAsync();

                    var message = $"سرتیتر اخبار امروز {today.ToShortDateString()}\n";
                    for (int i = 0; i < items.Count(); i++)
                        message += $"{i + 1} - {items[i].Title}   /News_{items[i].Id}\n";

                    await _bot.SendTextMessageAsync(chatId, message);
                }
                else if (text == DefaultContents.Search)
                {
                    await _userActivityRepository.AddAsync(new UserActivity() { UserId = user.Id, ActivityType = ActivityType.Search }, cancellationToken);
                    await _bot.SendTextMessageAsync(chatId, DefaultContents.PleaseEnterYourText);
                }

                else
                {
                    if (text.StartsWith("/News_"))
                    {
                        int newsId = 0;
                        if (int.TryParse(text.Split("_")[1], out newsId))
                        {
                            var news = await _newsRepository.GetByIdAsync(cancellationToken, newsId);
                            if (news == null)
                            {
                                await _bot.SendTextMessageAsync(chatId, DefaultContents.NewsNotFound);
                                return Ok();
                            }

                            string message = await GenerateMessageText(news);

                            var isExistAsSavedNews = !(await _newsUserCollectionRepository.TableNoTracking.AnyAsync(x => x.NewsId == newsId && x.UserId == user.Id));

                            await _bot.SendTextMessageAsync(chatId, message, null, ParseMode.Html, replyMarkup: GenerateNewsInlineKeyboard(newsId, isExistAsSavedNews));
                        }
                        else
                            await _bot.SendTextMessageAsync(chatId, DefaultContents.MessageIsNotValid);
                    }

                    if (lastActivity.ActivityType is ActivityType.EditFirstName or ActivityType.GetEditFirstNameConfirmation)
                    {
                        await _userActivityRepository.AddAsync(new UserActivity() { UserId = user.Id, ActivityType = ActivityType.GetEditFirstNameConfirmation }, cancellationToken);
                        await _bot.SendTextMessageAsync(chatId, DefaultContents.EditFirstNameAlert, replyMarkup: GenerateConfirmationInlineKeyboard(text));
                    }
                    else if (lastActivity.ActivityType == ActivityType.Search)
                    {
                        await _userActivityRepository.AddAsync(new UserActivity() { UserId = user.Id, ActivityType = ActivityType.ShowSearchResult }, cancellationToken);

                        var items = await _newsRepository.TableNoTracking.Include(x => x.NewsKeyWords).ThenInclude(x => x.KeyWord)
                            .Where(x => x.Title.Contains(text) || x.NewsKeyWords.Any(z => z.KeyWord.Title == text)).ToListAsync();

                        var message = $"نتایج جستجو\n";
                        for (int i = 0; i < items.Count(); i++)
                            message += $"{i + 1} - {items[i].Title}   /News_{items[i].Id}\n";

                        await _bot.SendTextMessageAsync(chatId, message);
                    }
                }
            }

            return Ok();
        }

        private async Task<string> GenerateMessageText(News news)
        {
            var newsKeywords = await _newsKeyWordsRepository.TableNoTracking.Where(x => x.NewsId == news.Id).Include(x => x.KeyWord).ToListAsync();

            string message = $"<b><i>{news.Title}</i></b>\n{news.Desc}\n\n";

            if (newsKeywords != null && newsKeywords.Any())
                foreach (var newsKeyword in newsKeywords)
                    message += $"#{newsKeyword.KeyWord.Title} ";

            return message;
        }

        private InlineKeyboardMarkup GenerateNewsInlineKeyboard(int newsId, bool showSaveText = true)
        {
            var rows = new List<InlineKeyboardButton[]>();

            if (showSaveText)
                rows.Add(new InlineKeyboardButton[] { new InlineKeyboardButton(DefaultContents.SaveNews) { CallbackData = $"Save_{newsId}" } });
            else
                rows.Add(new InlineKeyboardButton[] { new InlineKeyboardButton(DefaultContents.DeleteFromSavedNews) { CallbackData = $"UnSave_{newsId}" } });

            var keyboard = new InlineKeyboardMarkup(rows);
            return keyboard;
        }

        private InlineKeyboardMarkup GenerateConfirmationInlineKeyboard(string text)
        {
            var rows = new List<InlineKeyboardButton[]>();

            rows.Add(new InlineKeyboardButton[]
            {
                new InlineKeyboardButton(DefaultContents.Confirmed) { CallbackData = $"Confirmed_{text}" }
            });

            rows.Add(new InlineKeyboardButton[]
            {
                new InlineKeyboardButton(DefaultContents.Canceled) { CallbackData = "Canceled"}
            });

            var keyboard = new InlineKeyboardMarkup(rows);
            return keyboard;
        }

        private ReplyKeyboardMarkup GenerateProfileKeyboard()
        {
            var rows = new List<KeyboardButton[]>();

            rows.Add(new KeyboardButton[]
            {
                new KeyboardButton(DefaultContents.EditFirstName)
            });

            rows.Add(new KeyboardButton[]
            {
                new KeyboardButton(DefaultContents.EditLastName)
            });

            rows.Add(new KeyboardButton[]
            {
                new KeyboardButton(DefaultContents.SavedNews)
            });

            rows.Add(new KeyboardButton[]
            {
                new KeyboardButton(DefaultContents.BackToMainMenu)
            });

            var keyboard = new ReplyKeyboardMarkup(rows);
            return keyboard;
        }

        private ReplyKeyboardMarkup GenerateMainKeyboard()
        {
            var rows = new List<KeyboardButton[]>();

            rows.Add(new KeyboardButton[]
            {
                new KeyboardButton(DefaultContents.Search)
            });

            rows.Add(new KeyboardButton[]
            {
                new KeyboardButton(DefaultContents.HeadOfNews),
                new KeyboardButton(DefaultContents.Money)
            });

            rows.Add(new KeyboardButton[]
            {
                new KeyboardButton(DefaultContents.Location),
                new KeyboardButton(DefaultContents.ContactUs)
            });

            rows.Add(new KeyboardButton[]
            {
                new KeyboardButton(DefaultContents.Profile)
            });

            var keyboard = new ReplyKeyboardMarkup(rows);
            return keyboard;
        }
    }
}
