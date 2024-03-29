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

        public BotManagementController(IBaseRepository<User> userRepository)
        {
            _bot = new TelegramBotClient("6026122963:AAFivrrnCEbaXa8ZacZZR2SwO5KDlM4vfHI");
            _userRepository = userRepository;
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
                        ParentId = null
                    }, cancellationToken);
                }

                if (text == DefaultContents.Start)
                {
                    await _bot.SendTextMessageAsync(chatId, DefaultContents.WelcomeToBot, replyMarkup: GenerateMainKeyboard());
                }
                else if (text == DefaultContents.Location)
                {
                    await _bot.SendVenueAsync(chatId, 45.87654, 56.76543, "دفتر مرکزی", "خیابان ایکس پلاک 2");
                }
                else if (text == DefaultContents.ContactUs)
                {
                    await _bot.SendTextMessageAsync(chatId, DefaultContents.ContactUsMessage);
                }
                else if (text == DefaultContents.Money)
                {
                    await _bot.SendTextMessageAsync(chatId, DefaultContents.MoneyMessage);
                }
            }

            return Ok();
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
