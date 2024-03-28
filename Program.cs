using AutoMapper;
using TeckNews.Data;
using TeckNews.Entities;
using TeckNews.Mappings;
using TeckNews.Repositories;
using TeckNews.Utilities;
using Microsoft.EntityFrameworkCore;
using TeckNews.Entities;
using TeckNews.Utilities;
using AutoMapper;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddDbContext<TeckNewsContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DbConnection"));
});

builder.Services.AddScoped<IBaseRepository<User>, BaseRepository<User>>();
builder.Services.AddScoped<IBaseRepository<News>, BaseRepository<News>>();
builder.Services.AddScoped<IBaseRepository<NewsKeyWord>, BaseRepository<NewsKeyWord>>();
builder.Services.AddScoped<IBaseRepository<KeyWord>, BaseRepository<KeyWord>>();
builder.Services.AddScoped<IBaseRepository<NewsUserCollection>, BaseRepository<NewsUserCollection>>();
builder.Services.AddScoped<IBaseRepository<UserActivity>, BaseRepository<UserActivity>>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Start Registering and Initializing AutoMapper
var mapperConfig = new MapperConfiguration(mc =>
{
    mc.AddProfile(new MappingProfile());
});
IMapper mapper = mapperConfig.CreateMapper();
builder.Services.AddSingleton(mapper);
// End Registering and Initializing AutoMapper

var app = builder.Build();

var context = builder.Services.BuildServiceProvider().GetRequiredService<TeckNewsContext>();
DataInitializer.Initialize(context, app.Configuration);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
