using Serilog;
using Serilog.Events;
using TarimDonusum.FrameWork.Captcha;
using TarimDonusum.FrameWork.Logging;
using TarimDonusum.FrameWork.Menu;
using TarimDonusum.IsKurallari;
using TarimDonusum.Servisler;
using System.Globalization;
using Microsoft.AspNetCore.Localization;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);



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
builder.Services.AddScoped<KullaniciIsKurallari>();
builder.Services.AddScoped<IMailServisi, MailServisi>();
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


WebApplication app = builder.Build();

await VTGuncelle.GuncelleAsync(app.Configuration, app.Logger);

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

app.Lifetime.ApplicationStarted.Register(() =>
{
    BMYLog.Log(app.Logger, LogLevel.Information, BMYEventID.UygulamaBasladi, null, "TarimDonusum uygulaması başladı.");
});


app.Lifetime.ApplicationStopping.Register(() =>
{
    BMYLog.Log(app.Logger,
        LogLevel.Information,
        BMYEventID.UygulamaSonlandi,
        null,
        "Uygulama sonlanıyor.");

    Log.CloseAndFlush();
});
MenuManager.Initialize(builder.Environment.ContentRootPath);

CultureInfo[] supportedCultures = new[]
{
    new CultureInfo("tr"),
    new CultureInfo("en")
};

RequestLocalizationOptions localizationOptions = new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("tr"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
};

localizationOptions.RequestCultureProviders.Insert(0, new CookieRequestCultureProvider());
app.UseRequestLocalization(localizationOptions);

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
