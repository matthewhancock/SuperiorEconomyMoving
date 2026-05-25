using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using api.Configuration;
using api.Quote;
using api.Redirects;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Bind config sections to typed options
builder.Services.Configure<FeaturesOptions>(builder.Configuration.GetSection(FeaturesOptions.SectionName));
builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection(SmtpOptions.SectionName));

// Quote engine
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<FeaturesOptions>>().Value.Quote);
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<SmtpOptions>>().Value);
builder.Services.AddSingleton<QuoteCalculator>();
builder.Services.AddSingleton<SmtpQuoteEmailer>();

var app = builder.Build();

// 1) Redirect middleware FIRST — legacy domains 301 to the canonical target before anything else runs
app.UseMiddleware<DomainRedirectMiddleware>();

// 2) Health endpoint for the App Service deploy contract (matches Vocab-Static)
app.MapGet("/health", () => Results.Ok("Healthy"));

// 3) Feature-flagged InstaQuote routing for /quote.html
//    - InstaQuote ON  → 302 redirect to /instaquote/ (the live calculator)
//    - InstaQuote OFF → fall through to the static /quote.html lead form
app.MapGet("/quote.html", (HttpContext ctx) => {
    var quote = ctx.RequestServices.GetRequiredService<QuoteOptions>();
    return quote.InstaQuote ? Results.Redirect("/instaquote/", permanent: false) : Results.Empty;
});

// 4) Live calculator API — JSON in, JSON out, used by the InstaQuote page client-side
var jsonOptions = new JsonSerializerOptions {
    PropertyNameCaseInsensitive = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
};
app.MapPost("/api/quote/calculate", async (HttpContext ctx, QuoteCalculator calc) => {
    var req = await JsonSerializer.DeserializeAsync<QuoteRequest>(ctx.Request.Body, jsonOptions, ctx.RequestAborted);
    if (req is null) return Results.BadRequest(new { error = "Invalid quote request body." });
    var result = calc.Calculate(req);
    return Results.Json(result, jsonOptions);
});

// 5) Form submit — calculate, email staff (best-effort), render receipt
app.MapPost("/quote/submit", async (HttpContext ctx, QuoteCalculator calc, SmtpQuoteEmailer emailer) => {
    var quote = ctx.RequestServices.GetRequiredService<QuoteOptions>();
    if (!quote.InstaQuote) return Results.NotFound();

    var form = await ctx.Request.ReadFormAsync(ctx.RequestAborted);
    var req = QuoteFormBinder.Bind(form);

    if (string.IsNullOrWhiteSpace(req.FirstName) || string.IsNullOrWhiteSpace(req.Phone) || string.IsNullOrWhiteSpace(req.Email)) {
        return Results.BadRequest("Name, phone, and email are required.");
    }

    var result = calc.Calculate(req);
    var emailSent = await emailer.SendAsync(req, result, ctx.RequestAborted);
    var html = ReceiptPageRenderer.Render(req, result, emailSent);
    return Results.Content(html, "text/html; charset=utf-8");
});

// 6) Static files — served from wwwroot/superior-moving/ (network-id-prefixed, Vocab-Static compatible)
//    UseDefaultFiles handles index.html for directory requests; the StaticFileProvider serves the rest.
var staticRoot = Path.Combine(app.Environment.ContentRootPath, "wwwroot", "superior-moving");
if (Directory.Exists(staticRoot)) {
    var staticFiles = new PhysicalFileProvider(staticRoot);
    var contentTypes = new FileExtensionContentTypeProvider();
    contentTypes.Mappings[".webmanifest"] = "application/manifest+json";

    app.UseDefaultFiles(new DefaultFilesOptions {
        FileProvider = staticFiles,
        DefaultFileNames = ["index.html"]
    });
    app.UseStaticFiles(new StaticFileOptions {
        FileProvider = staticFiles,
        ContentTypeProvider = contentTypes,
        ServeUnknownFileTypes = false
    });
}

app.Run();
