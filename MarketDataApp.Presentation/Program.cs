using MarketDataApp.Business.Services;
using MarketDataApp.Core.Interfaces;
using MarketDataApp.DataAccess.Clients;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddMemoryCache();

builder.Services.AddHttpClient<ICotService, CotService>();

builder.Services.AddHttpClient<MarketDataApp.Business.Services.SeasonalityService>();

builder.Services.AddScoped<MarketDataApp.Business.Services.SqueezeService>();

builder.Services.AddHttpClient<MarketDataApp.Business.Services.AiMacroService>();

builder.Services.AddScoped<
    IMarketApiClient,
    DerivApiClient>();

builder.Services.AddScoped<
    IMarketService,
    MarketService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "AllowAll",
        policy =>
        {
            policy
                .AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");

app.UseStaticFiles();

app.UseAuthorization();

app.MapControllers();

app.Run();