using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<WebSocketService>(WebSocketService.Create);

builder.Services.AddCors((options) => {
    options.AddDefaultPolicy((builder) => {
        builder.AllowAnyHeader();
        builder.AllowAnyMethod();
        builder.AllowAnyOrigin();
    });
});

var web = builder.Build();
web.UseCors();
web.UseWebSockets();
web.UseStaticFiles();
web.UseDirectoryBrowser();

var webSocketService = web.Services.GetService<WebSocketService>();

webSocketService.Use(web);

web.Run("http://localhost:8080");
