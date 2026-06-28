using Serilog;
using Serilog.Events;
using TarimDonusum.FrameWork.Captcha;
using TarimDonusum.FrameWork.Logging;
using TarimDonusum.FrameWork.Menu;
using System.Globalization;
using Microsoft.AspNetCore.Localization;

var builder = WebApplication.CreateBuilder(args);



builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
        .MinimumLevel.Override("System", LogEventLevel.Warning)
        .MinimumLevel.Override("TarimDonusum", LogEventLevel.Information)

        // Tüm loglar dosyaya
        .WriteTo.File(
            "Logs/log-.txt",
            rollingInterval: RollingInterval.Day,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}");
});


builder.Services.AddControllersWithViews().AddViewLocalization().AddDataAnnotationsLocalization(); ;
builder.Services.AddSingleton<CaptchaGenerator>();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddLocalization(options =>
{
    options.ResourcesPath = "Resources";
});



builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(50);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});


var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseSession(); 
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Lifetime.ApplicationStarted.Register(() =>
{
    BMYLog.Log(app.Logger, LogLevel.Information, BMYEventID.UygulamaBasladi, null, "TarimDonusum uygulamasý baþladý.");
});


app.Lifetime.ApplicationStopping.Register(() =>
{
    BMYLog.Log(app.Logger,
        LogLevel.Information,
        BMYEventID.UygulamaSonlandi,
        null,
        "Uygulama sonlanýyor.");

    Log.CloseAndFlush();
});
MenuManager.Initialize(builder.Environment.ContentRootPath);

var supportedCultures = new[]
{
    new CultureInfo("tr"),
    new CultureInfo("en")
};

var localizationOptions = new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("tr"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
};

localizationOptions.RequestCultureProviders.Insert(0, new CookieRequestCultureProvider());
app.UseRequestLocalization(localizationOptions);

app.Run();
