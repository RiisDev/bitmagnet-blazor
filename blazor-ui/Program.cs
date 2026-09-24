global using static Bitmagnet.WebUI.Classes.Services.LogService;
using Bitmagnet.WebUI.Classes.Services;
using Bitmagnet.WebUI.Components;
using Bitmagnet.WebUI.GraphQL;
using Microsoft.AspNetCore.Diagnostics;
using MudBlazor.Services;

AppDomain.CurrentDomain.UnhandledException += (_, exception) => LogError(exception.ExceptionObject.ToString()!);

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

BitmagnetOptions bitmagnetOptions = builder.Configuration.GetSection("Bitmagnet").Get<BitmagnetOptions>() ?? new BitmagnetOptions();
builder.Services.AddSingleton(bitmagnetOptions);
builder.Services.AddHttpClient<GraphQlClient>("BitmagnetGraphQL", client => client.BaseAddress = new Uri(bitmagnetOptions.GraphQLEndpoint));
builder.Services.AddScoped<TorrentFilterState>();
builder.Services.AddScoped<BitmagnetService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<ITorrentApiService, TorrentApiService>();
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddMudServices();

WebApplication app = builder.Build();

if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Error", createScopeForErrors: true);
	app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();
app.MapStaticAssets();
app.UseExceptionHandler(errorHandler => errorHandler.Run(async context => await LogError(context.Features.Get<IExceptionHandlerFeature>()?.Error.ToString())));
app.MapRazorComponents<App>()
	.AddInteractiveServerRenderMode();

app.Run();

/// <summary>
/// Bound from the "Bitmagnet" config section. Points this UI at the Go
/// backend -- keep the backend's host/port/db untouched, this is purely
/// where the Blazor app sends its GraphQL requests.
/// </summary>
public sealed class BitmagnetOptions
{
	public string GraphQLEndpoint { get; set; } = "http://localhost:3333/graphql";
}