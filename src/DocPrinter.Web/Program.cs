using DocPrinter.Agent;
using DocPrinter.Agent.Abstractions;
using DocPrinter.Web;
using DocPrinter.Web.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Per-circuit live status: one PipelineStatus per conversation, surfaced through both the read
// interface (the page) and the write interface (pipeline/plugin/flip prompt).
builder.Services.AddScoped<PipelineStatus>();
builder.Services.AddScoped<IPipelineStatus>(sp => sp.GetRequiredService<PipelineStatus>());
builder.Services.AddScoped<IProgress<PipelineActivity>>(sp => sp.GetRequiredService<PipelineStatus>());

// Per-circuit flip prompt, exposed both as the agent contract and concretely to the page.
builder.Services.AddScoped<BlazorFlipPrompt>();
builder.Services.AddScoped<IFlipPrompt>(sp => sp.GetRequiredService<BlazorFlipPrompt>());

// Scope the agent + plugin state per circuit so concurrent users don't share a conversation.
builder.Services.AddAgent(ConfigureAgent, ServiceLifetime.Scoped);

// Bind printer settings from the "Printing" config section (PrinterName, SumatraPath, etc.).
builder.Services.Configure<DocPrinter.Printing.PrintingOptions>(
    builder.Configuration.GetSection("Printing"));

// Bind downloader settings (source URLs) from the "Downloader" section. In Development these come
// from User Secrets, so the URLs are never committed to the repository.
builder.Services.Configure<DocPrinter.Downloader.DownloaderOptions>(
    builder.Configuration.GetSection("Downloader"));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

// Reads the agent settings from the "Agent" config section, with DOC_MODEL / DOC_OLLAMA_ENDPOINT
// environment variables overriding it (mirroring the console harness).
void ConfigureAgent(AgentOptions options)
{
    builder.Configuration.GetSection("Agent").Bind(options);

    string? model = Environment.GetEnvironmentVariable("DOC_MODEL");
    if (!string.IsNullOrWhiteSpace(model))
    {
        options.ModelId = model;
    }

    string? endpoint = Environment.GetEnvironmentVariable("DOC_OLLAMA_ENDPOINT");
    if (!string.IsNullOrWhiteSpace(endpoint))
    {
        options.Endpoint = endpoint;
    }
}
