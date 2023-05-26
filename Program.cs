using ActiverWebAPI.Context;
using ActiverWebAPI.Interfaces.Service;
using ActiverWebAPI.Interfaces.UnitOfWork;
using ActiverWebAPI.Profile;
using ActiverWebAPI.Services;
using ActiverWebAPI.Services.ActivityServices;
using ActiverWebAPI.Services.Middlewares;
using ActiverWebAPI.Services.TagServices;
using ActiverWebAPI.Services.UnitOfWork;
using ActiverWebAPI.Services.UserServices;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using static ActiverWebAPI.Dev.Swagger.Filter;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
    // �N�Ҧ��D�i���Ū� string �ݩʳ]�m������
    c.SchemaFilter<NonNullStringPropertiesSchemaFilter>();
    c.EnableAnnotations();
});

builder.Services.AddDbContext<ActiverDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DevConnection")));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
// Services
builder.Services.AddScoped(typeof(IGenericService<,>), typeof(GenericService<,>));
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<BranchService>();
builder.Services.AddScoped<LocationService>();
builder.Services.AddScoped<ActivityFilterValidationService>();
builder.Services.AddScoped<AreaService>();
builder.Services.AddScoped<TagService>();
builder.Services.AddScoped<ProfessionService>();
builder.Services.AddScoped<CountyService>();
builder.Services.AddScoped<ActivityService>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IEmailService, EmailService>();
//builder.Services.AddScoped<ErrorHandlingMiddleware>();
builder.Services.AddScoped<ApiResponseMiddleware>();
// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevCorsPolicy", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

// AutoMapper
builder.Services.AddAutoMapper(
    cfg => cfg.AddProfile(new MappingProfile(
        builder.Services.BuildServiceProvider().GetService<IPasswordHasher>(),
        builder.Services.BuildServiceProvider().GetService<IConfiguration>(),
        builder.Services.BuildServiceProvider().GetService<IUnitOfWork>()
        )),
        AppDomain.CurrentDomain.GetAssemblies()
);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"]))
        };
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Middlewares
app.UseMiddleware<ApiResponseMiddleware>();
//app.UseMiddleware<ErrorHandlingMiddleware>();

app.UseCors("DevCorsPolicy");
app.UseStaticFiles();
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();


