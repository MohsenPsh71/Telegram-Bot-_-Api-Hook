using AutoMapper;
using TeckNews.Dtos;
using TeckNews.Entities;
using TeckNews.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Telegram.Bot;
using static System.Net.Mime.MediaTypeNames;

namespace TeckNews.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NewsController : ControllerBase
    {
        private readonly IBaseRepository<News> _newsRepository;
        private readonly IBaseRepository<NewsKeyWord> _newsKeyWordRepository;
        private readonly IBaseRepository<KeyWord> _keyWordRepository;

        private readonly IMapper _mapper;

        private readonly TelegramBotClient _bot;

        public NewsController(IBaseRepository<News> newsRepository, IBaseRepository<NewsKeyWord> newsKeyWordRepository, IBaseRepository<KeyWord> keyWordRepository, IMapper mapper)
        {
            _newsRepository = newsRepository;
            _newsKeyWordRepository = newsKeyWordRepository;
            _keyWordRepository = keyWordRepository;
            _mapper = mapper;

            _bot = new TelegramBotClient("6026122963:AAFivrrnCEbaXa8ZacZZR2SwO5KDlM4vfHI");
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<NewsDto>> Get(int id, CancellationToken cancellationToken)
        {
            var obj = await _newsRepository.GetByIdAsync(cancellationToken, id);

            if (obj == null)
                return NotFound();

            return Ok(_mapper.Map<NewsDto>(obj));
        }

        [HttpGet()]
        public async Task<ActionResult<IEnumerable<NewsDto>>> Get()
        {
            return Ok(_mapper.Map<List<NewsDto>>(_newsRepository.TableNoTracking));
        }

        [HttpPost]
        public async Task<ActionResult<NewsDto>> Post(NewsDto model, CancellationToken cancellationToken)
        {
            var news = _mapper.Map<News>(model);
            news.CreateDate = DateTime.Now;

            string text = $"<b><i>{model.Title}</i></b>\n{model.Desc}\n\n";

            if (model.KeyWords != null && model.KeyWords.Any())
            {
                news.NewsKeyWords = new List<NewsKeyWord>();

                foreach (var keywordId in model.KeyWords)
                {
                    var keyword = await _keyWordRepository.GetByIdAsync(cancellationToken, keywordId);
                    if (keyword == null)
                        continue;

                    text += $"#{keyword.Title} ";

                    news.NewsKeyWords.Add(new NewsKeyWord()
                    {
                        KeyWordId = keywordId
                    });
                }
            }

            var message = await _bot.SendTextMessageAsync(chatId: "@TeckNews", text: text, null, parseMode: Telegram.Bot.Types.Enums.ParseMode.Html, null, null, null, null, null, null, null, cancellationToken);

            news.MessageId = message.MessageId;

            var obj = await _newsRepository.AddAsync(news, cancellationToken);
            return Ok(_mapper.Map<NewsDto>(obj));
        }

        [HttpPut]
        public async Task<ActionResult<NewsDto>> Put(NewsDto model, CancellationToken cancellationToken)
        {
            var obj = await _newsRepository.GetByIdAsync(cancellationToken, model.Id);

            string text = $"<b><i>{model.Title}</i></b>\n{model.Desc}\n\n";

            if (obj == null)
                return NotFound();

            _mapper.Map(model, obj);

            if (model.KeyWords != null && model.KeyWords.Any())
            {
                // Delete Current Keywords
                var keywords = _newsKeyWordRepository.Table.Where(x => x.NewsId == model.Id).ToList();
                foreach (var keyword in keywords)
                {
                    await _newsKeyWordRepository.DeleteAsync(keyword, cancellationToken);
                }

                obj.NewsKeyWords = new List<NewsKeyWord>();

                foreach (var keywordId in model.KeyWords)
                {
                    var keyword = await _keyWordRepository.GetByIdAsync(cancellationToken, keywordId);
                    if (keyword == null)
                        continue;

                    text += $"#{keyword.Title} ";

                    obj.NewsKeyWords.Add(new NewsKeyWord()
                    {
                        KeyWordId = keywordId
                    });
                }
            }

            await _bot.EditMessageTextAsync("@TeckNews", obj.MessageId, text, Telegram.Bot.Types.Enums.ParseMode.Html);
            await _bot.SendTextMessageAsync("@TeckNews", "این خبر بروزرسانی شد", replyToMessageId: obj.MessageId);

            await _newsRepository.UpdateAsync(obj, cancellationToken);
            return Ok(model);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            var obj = await _newsRepository.GetByIdAsync(cancellationToken, id);

            if (obj == null)
                return NotFound();

            var keywords = _newsKeyWordRepository.Table.Where(x => x.NewsId == id).ToList();

            foreach (var keyword in keywords)
            {
                await _newsKeyWordRepository.DeleteAsync(keyword, cancellationToken);
            }

            await _bot.DeleteMessageAsync("@TeckNews", obj.MessageId);

            await _newsRepository.DeleteAsync(obj, cancellationToken);
            return Ok();
        }
    }
}
