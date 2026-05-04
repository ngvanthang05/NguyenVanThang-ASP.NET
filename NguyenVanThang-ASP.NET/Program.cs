// Program.cs — CẬP NHẬT HOÀN CHỈNH cho 5 Portal
using Microsoft.EntityFrameworkCore;
using NguyenVanThang_ASP.NET.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using FluentValidation.AspNetCore;
using NguyenVanThang_ASP.NET.Services;

var builder = WebApplication.CreateBuilder(args);

// ================= DATABASE =================
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ================= CORS (cho Mobile App + Web) =================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

// ================= JWT =================
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
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.Zero
    };
});

// ================= AUTHORIZATION POLICIES =================
// Hỗ trợ 5 roles: Admin | Operations | Staff | Driver | Customer
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("OperationsOrAdmin", policy => policy.RequireRole("Admin", "Operations"));
    options.AddPolicy("StaffOrAdmin", policy => policy.RequireRole("Admin", "Staff"));
    options.AddPolicy("DriverOnly", policy => policy.RequireRole("Driver"));
    options.AddPolicy("CustomerOnly", policy => policy.RequireRole("Customer"));
    // Portal 3: cả Admin và Staff dùng quầy vé
    options.AddPolicy("CounterStaff", policy => policy.RequireRole("Admin", "Staff"));
});

// ================= SERVICES =================
builder.Services.AddScoped<IBookingService, BookingService>();

// ================= CONTROLLERS =================
builder.Services.AddControllers()
    .AddFluentValidation(fv => fv.RegisterValidatorsFromAssemblyContaining<Program>())
    .AddJsonOptions(x =>
        x.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles);

// ================= SWAGGER =================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Bus Booking API — 5 Portals",
        Version = "v1",
        Description = @"
## 5 Portals:
- **Portal 1 - Admin**: `/api/admin/*` — Quản lý toàn hệ thống
- **Portal 2 - Operations**: `/api/operations/*` — Điều hành chuyến xe
- **Portal 3 - Staff Sales**: `/api/staff-sales/*` — Bán vé tại quầy
- **Portal 4 - Customer**: `/api/bookings/*` — Khách hàng đặt vé online
- **Portal 5 - Driver**: `/api/driver/*` — Tài xế mobile app

## Roles:
- `Admin` → Portal 1 + tất cả
- `Operations` → Portal 2
- `Staff` → Portal 3
- `Customer` → Portal 4
- `Driver` → Portal 5"
    });

    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization. Nhập: Bearer {token}",
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

    // Group theo tag (portal)
    options.TagActionsBy(api =>
    {
        var controller = api.ActionDescriptor.RouteValues["controller"];
        return controller switch
        {
            "Admin" => new[] { "Portal 1 - Admin" },
            "Operations" => new[] { "Portal 2 - Operations" },
            "StaffSales" => new[] { "Portal 3 - Staff Sales (Quầy)" },
            "Booking" => new[] { "Portal 4 - Customer" },
            "Driver" => new[] { "Portal 5 - Driver" },
            _ => new[] { controller ?? "Other" }
        };
    });
});

var app = builder.Build();

// ================= PIPELINE =================
app.UseDeveloperExceptionPage();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Bus Booking API v1 — 5 Portals");
    c.RoutePrefix = "swagger";
    c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List); // mở rộng theo nhóm
});

app.MapGet("/", () => Results.Redirect("/swagger"));

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();