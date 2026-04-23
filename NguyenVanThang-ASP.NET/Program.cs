// Program.cs — hoàn chỉnh
using Microsoft.EntityFrameworkCore;
using NguyenVanThang_ASP.NET.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using FluentValidation.AspNetCore;
using NguyenVanThang_ASP.NET.Services;

var builder = WebApplication.CreateBuilder(args);

// ================= DB =================
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ================= CORS (cho mobile + web) =================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

// ================= JWT — đọc key từ config =================
var jwtKey = builder.Configuration["Jwt:Key"]!;

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

// ================= SERVICE LAYER =================
builder.Services.AddScoped<IBookingService, BookingService>();

// ================= CONTROLLERS + VALIDATION =================
builder.Services.AddControllers()
    .AddFluentValidation(fv => fv.RegisterValidatorsFromAssemblyContaining<Program>())
    .AddJsonOptions(x => x.JsonSerializerOptions.ReferenceHandler =
        System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles); // tránh circular reference

// ================= SWAGGER + JWT =================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Bus Booking API",
        Version = "v1",
        Description = "API đặt vé xe khách"
    });
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "Nhập JWT token (VD: Bearer abc123...)",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {{
        new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            Reference = new Microsoft.OpenApi.Models.OpenApiReference
            {
                Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                Id = "Bearer"
            }
        },
        new string[] {}
    }});
});

var app = builder.Build();
// ================= 2. Cấu hình HTTP Request Pipeline =================

// Gộp Swagger: Bỏ điều kiện IsDevelopment() nếu bạn muốn Swagger luôn hiện trên Somee
// Vì đôi khi Somee chạy ở mode Production, Swagger sẽ bị ẩn nếu để trong if.

// Thêm dòng này để xem lỗi chi tiết trên Somee
app.UseDeveloperExceptionPage();

app.UseSwagger();
app.UseSwaggerUI(c => {
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Bus Booking API v1");
    c.RoutePrefix = "swagger"; // Đảm bảo swagger chạy tại /swagger
});

// Cấu hình để khi vào trang chủ sẽ redirect sang Swagger
app.MapGet("/", () => Results.Redirect("/swagger"));


app.UseHttpsRedirection();
app.UseCors("AllowAll"); // ⚠️ phải trước Authentication
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();