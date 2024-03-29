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

        public BotManagementController(IBaseRepository<User> userRepository, IBaseRepository<UserActivity> userActivityRepository)
        {
            _bot = new TelegramBotClient("6026122963:AAFivrrnCEbaXa8ZacZZR2SwO5KDlM4vfHI");
            _userRepository = userRepository;
            _userActivityRepository = userActivityRepository;
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

            if (update.CallbackQuery != null)
            {
                var text = update.CallbackQuery.Data;
                var chatId = update.CallbackQuery.From.Id;

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
            }

            if (update.Message != null)
            {
                var text = update.Message.Text;
                var chatId = update.Message.Chat.Id;

                var user = await _userRepository.TableNoTracking.FirstOrDefaultAsync(x => x.ChatId == chatId);

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

                else
                {
                    if (lastActivity.ActivityType is ActivityType.EditFirstName or ActivityType.GetEditFirstNameConfirmation)
                    {
                        await _userActivityRepository.AddAsync(new UserActivity() { UserId = user.Id, ActivityType = ActivityType.GetEditFirstNameConfirmation }, cancellationToken);
                        await _bot.SendTextMessageAsync(chatId, DefaultContents.EditFirstNameAlert, replyMarkup: GenerateConfirmationInlineKeyboard(text));
                    }
                }
            }

            return Ok();
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
