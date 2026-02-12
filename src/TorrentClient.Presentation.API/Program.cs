using TorrentClient.Application.Managers;
using TorrentClient.Application.Ports;
using TorrentClient.Application.UseCases;
using TorrentClient.Infrastructure.Adapters;
using TorrentClient.Infrastructure.Factories;
using TorrentClient.Presentation.API.Hubs;
using TorrentClient.Presentation.API.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});


builder.Services.AddSingleton<TorrentClient.Application.Common.Interfaces.ILogger, ConsoleLoggerAdapter>();
builder.Services.AddSingleton<ITorrentParser, TorrentParserAdapter>();
builder.Services.AddSingleton<IPeerDiscovery, HttpTrackerAdapter>();
builder.Services.AddSingleton<IPeerConnectionFactory, PeerConnectionFactory>();
builder.Services.AddSingleton<IPieceStoreFactory, PieceStoreFactory>();

builder.Services.AddTransient<DownloadPieceUseCase>();

builder.Services.AddScoped<TorrentManager>();

builder.Services.AddSingleton<TorrentDownloadService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<TorrentDownloadService>());
builder.Services.AddSignalR();
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");

app.UseStaticFiles();
app.MapHub<TorrentHub>("/torrentHub");

app.MapControllers();

app.MapFallbackToFile("index.html");

app.Run();