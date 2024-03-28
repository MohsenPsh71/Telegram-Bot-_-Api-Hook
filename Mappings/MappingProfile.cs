using AutoMapper;
using TeckNews.Entities;
using TeckNews.Dtos;

namespace TeckNews.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<NewsDto, News>().ReverseMap();
        }
    }
}
