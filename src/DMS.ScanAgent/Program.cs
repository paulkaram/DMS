using DMS.ScanAgent;

var builder = WebApplication.CreateBuilder(args);

// Configure to listen on port 18181
builder.WebHost.UseUrls("http://localhost:18181");

// CORS - allow DMS frontend origins
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.SetIsOriginAllowed(origin =>
        {
            var uri = new Uri(origin);
            // Allow localhost (any port) and configured origins
            return uri.Host == "localhost" || uri.Host == "127.0.0.1";
        })
        .AllowAnyMethod()
        .AllowAnyHeader()));

// Register scanner service as singleton (owns ScanningContext)
builder.Services.AddSingleton<ScannerService>();

var app = builder.Build();

app.UseCors();

app.MapScannerEndpoints();

Console.WriteLine("DMS Scan Agent running on http://localhost:18181");
Console.WriteLine("Press Ctrl+C to stop");

app.Run();
