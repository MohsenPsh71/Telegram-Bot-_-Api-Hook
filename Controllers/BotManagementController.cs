using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace FarzamNews.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BotManagementController : ControllerBase
    {
        private readonly TelegramBotClient _bot;

        public BotManagementController() 
        {
            _bot = new TelegramBotClient("6026122963:AAFivrrnCEbaXa8ZacZZR2SwO5KDlM4vfHI");
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
        public async Task<ActionResult> ReceiveUpdate(object model)
        {
            var update = JsonConvert.DeserializeObject<Update>(model.ToString());

            if(update.Message != null)
            {

            }

            return Ok();
        }
    }
}
