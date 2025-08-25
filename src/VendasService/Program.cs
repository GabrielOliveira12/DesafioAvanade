using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using VendasService.Data;
using VendasService.Services;
using Shared.Messaging;
using DotNetEnv;

Env.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<VendasDbContext>(options =>
    options.UseInMemoryDatabase("VendasDb"));

builder.Services.AddScoped<IVendasService, VendasService.Services.VendasService>();

builder.Services.AddHttpClient<IEstoqueServiceClient, EstoqueServiceClient>(client =>
{
    var estoqueUrl = Environment.GetEnvironmentVariable("ESTOQUE_SERVICE_URL") ?? 
                     builder.Configuration["Services:EstoqueService"] ?? 
                     "https://localhost:7001";
    client.BaseAddress = new Uri(estoqueUrl);
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
{
    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
});

builder.Services.AddSingleton<IMessagePublisher>(provider =>
{
    var connectionString = Environment.GetEnvironmentVariable("RABBITMQ_CONNECTION_STRING") ?? 
                          builder.Configuration.GetConnectionString("RabbitMQ") ?? 
                          "amqp://guest:guest@localhost:5672/";
    return new RabbitMQMessagePublisher(connectionString);
});

var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY") ?? 
             builder.Configuration["Jwt:Key"] ?? 
             "minha-chave-secreta-super-segura-de-256-bits";
var key = Encoding.ASCII.GetBytes(jwtKey);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<VendasDbContext>();
    context.Database.EnsureCreated();
}

app.Run();

public partial class Program { }
