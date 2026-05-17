using Microsoft.EntityFrameworkCore;
using FiberHelp.Api.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Configure the database connection
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Server=tcp:your-server.database.windows.net,1433;Initial Catalog=FiberHelpDB;Persist Security Info=False;User ID=your_user;Password=your_password;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";

builder.Services.AddDbContext<FeedbackDbContext>(options =>
    options.UseSqlServer(connectionString));

// Configure CORS to allow external websites to submit feedback
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Enable CORS (Must be before HttpsRedirection and MapControllers)
app.UseCors("AllowAll");

app.UseHttpsRedirection();
app.UseDefaultFiles(); // Allow index.html to be served as the default page
app.UseStaticFiles(); // Enable hosting of HTML/CSS files in wwwroot
app.UseAuthorization();

app.MapControllers();

app.Run();
