namespace Recepty;

class Program
{
    static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        


        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowMyFrontend", policy =>
            {
                policy.WithOrigins("https://euphonious-flan-942081.netlify.app") 
              .AllowAnyHeader()
              .AllowAnyMethod();
            });
        });
        builder.Services.AddControllers();
        var app = builder.Build();

        app.UseRouting();
        app.UseCors("AllowMyFrontend");

        app.MapControllers();

        app.Run();
    }
}

