using graphical_console_exporter;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddOpenTelemetryTracing(b =>
{
    b
    .AddSource("Test")
    .AddProcessor(new SimpleActivityExportProcessor(new GraphicalConsoleExporter()))
    .SetResourceBuilder(
        ResourceBuilder.CreateDefault()
            .AddService(serviceName: "graphical-exporter", serviceVersion: "1.0.0"))
    .AddHttpClientInstrumentation()
    .AddAspNetCoreInstrumentation();
});
builder.Services.AddLogging(o => { o.ClearProviders(); });
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
