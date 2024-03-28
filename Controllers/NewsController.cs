using TeckNews.Dtos;
using TeckNews.Entities;
using TeckNews.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace TeckNews.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NewsController : ControllerBase
    {
        private readonly IBaseRepository<News> _newsRepository;
        private readonly IBaseRepository<NewsKeyWord> _newsKeyWordRepository;
        private readonly IBaseRepository<KeyWord> _keyWordRepository;

        public NewsController(IBaseRepository<News> newsRepository, IBaseRepository<NewsKeyWord> newsKeyWordRepository, IBaseRepository<KeyWord> keyWordRepository)
        {
            _newsRepository = newsRepository;
            _newsKeyWordRepository = newsKeyWordRepository;
            _keyWordRepository = keyWordRepository;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<NewsDto>> Get(int id, CancellationToken cancellationToken)
        {
            var obj = await _newsRepository.GetByIdAsync(cancellationToken, id);
            
            if (obj == null)
                return NotFound();

            var model = new NewsDto()
            {
                Id = obj.Id,
                Title = obj.Title,
                Desc = obj.Desc,
                MessageId = obj.MessageId
            };

            return Ok(model);
        }

        [HttpGet()]
        public async Task<ActionResult<IEnumerable<NewsDto>>> Get()
        {
            var model = _newsRepository.TableNoTracking.Select(x => new NewsDto()
            {
                Id = x.Id,
                Title = x.Title,
                Desc = x.Desc,
                MessageId = x.MessageId
            });

            return Ok(model);
        }

        [HttpPost]
        public async Task<ActionResult<NewsDto>> Post(NewsDto model, CancellationToken cancellationToken)
        {
            await _newsRepository.AddAsync(new News()
            {
                Id = 0,
                Title = model.Title,
                Desc = model.Desc,
                MessageId = 0
            }, cancellationToken);

            return model;
        }

        [HttpPut]
        public async Task<ActionResult<NewsDto>> Put(NewsDto model, CancellationToken cancellationToken)
        {
            var obj = await _newsRepository.GetByIdAsync(cancellationToken, model.Id);

            obj.Title = model.Title;
            obj.Desc = model.Desc;

            await _newsRepository.UpdateAsync(obj, cancellationToken);
            return model;
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            var obj = await _newsRepository.GetByIdAsync(cancellationToken, id);
            
            if (obj == null)
                return NotFound();

            await _newsRepository.DeleteAsync(obj, cancellationToken);
            return Ok();
        }
    }
}
