using Npgsql;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Road_Infrastructure_Asset_Management.Interface;
using Road_Infrastructure_Asset_Management.Service;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

//pgnsql
var connectionString = builder.Configuration.GetConnectionString("PostgreDB");
if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Connection string 'PostgreDB' is missing or empty.");
}

//Jwt
var jwtSecretKey = builder.Configuration["Jwt:Key"];
var key = Encoding.ASCII.GetBytes(jwtSecretKey);
if (string.IsNullOrEmpty(jwtSecretKey))
{
    throw new InvalidOperationException("JWT secret key is missing or empty.");
}
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

//CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("https://localhost:7056", "http://localhost:5156")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Add services to the container.
builder.Services.AddControllers()
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
        options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
    });
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


// Inject the connection string, not the connection
builder.Services.AddSingleton(connectionString);

//DI
builder.Services.AddScoped<IAssetCagetoriesService, AssetCagetoriesService>();
builder.Services.AddScoped<IAssetsService,AssetsService>();
builder.Services.AddScoped<IBudgetsService,BudgetsService>();
builder.Services.AddScoped<ICostsService,CostsService>();
builder.Services.AddScoped<IIncidentHistoryService,IncidentHistoryService>();
builder.Services.AddScoped<IIncidentsService, IncidentService>();
builder.Services.AddScoped<ITasksService, TasksService>();
builder.Services.AddScoped<IUsersService, UsersService>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
