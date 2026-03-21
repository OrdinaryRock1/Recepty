namespace Recepty;

class Program
{
    static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

// 1. Tell the app to use Controllers (like your RecipesController)
        builder.Services.AddControllers();

// 2. THE CORS FIX: Create a rule allowing your local frontend to connect
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowMyFrontend", policy =>
            {
                policy.AllowAnyOrigin()  // Allows requests from any HTML file
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        var app = builder.Build();

// 3. Turn on the CORS rule we just made
        app.UseCors("AllowMyFrontend");

// 4. Map the URL routes to your Controllers
        app.MapControllers();

// 5. Start the engine!
        app.Run();
    }
}

