using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using URMS.Data;
using URMS.Services.Implementations;
using URMS.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Services
builder.Services.AddScoped<IAuthService,           AuthService>();
builder.Services.AddScoped<IExaminerService,       ExaminerService>();
builder.Services.AddScoped<IHODService,            HODService>();
builder.Services.AddScoped<ITeacherService,        TeacherService>();
builder.Services.AddScoped<IGpaCalculationService, GpaCalculationService>();
builder.Services.AddScoped<INotificationService,   NotificationService>();
builder.Services.AddScoped<AnalyticsService>();
builder.Services.AddScoped<ExcelImportService>();
builder.Services.AddScoped<PdfService>();
builder.Services.AddScoped<ResultProcessingService>();      // ← ADD THIS
builder.Services.AddScoped<GradeExtractionService>();
builder.Services.AddScoped<StudentMasterImportService>();
builder.Services.AddScoped<ResultGenerationService>();

// File upload size 20MB
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(o =>
    o.MultipartBodyLengthLimit = 20 * 1024 * 1024);

// JWT
var jwtKey = builder.Configuration["Jwt:Key"]!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt => opt.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer=true, ValidateAudience=true, ValidateLifetime=true,
        ValidateIssuerSigningKey=true,
        ValidIssuer=builder.Configuration["Jwt:Issuer"],
        ValidAudience=builder.Configuration["Jwt:Audience"],
        IssuerSigningKey=new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo{Title="URMS API",Version="v1",Description="KICSIT University Result Management System"});
    c.AddSecurityDefinition("Bearer",new OpenApiSecurityScheme{Name="Authorization",Type=SecuritySchemeType.Http,Scheme="bearer",BearerFormat="JWT",In=ParameterLocation.Header});
    c.AddSecurityRequirement(new OpenApiSecurityRequirement{{new OpenApiSecurityScheme{Reference=new OpenApiReference{Type=ReferenceType.SecurityScheme,Id="Bearer"}},Array.Empty<string>()}});
});

builder.Services.AddCors(opt => opt.AddPolicy("BlazorClient", p =>
    p.WithOrigins("https://localhost:7001","http://localhost:5001","http://localhost:5173")
     .AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

// Auto migrate
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try { db.Database.Migrate(); Console.WriteLine("✓ Database migrated."); }
    catch (Exception ex) { Console.WriteLine($"✗ Migration failed: {ex.Message}"); }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => { c.SwaggerEndpoint("/swagger/v1/swagger.json","URMS API v1"); c.RoutePrefix="swagger"; });
}

app.UseStaticFiles();
app.UseCors("BlazorClient");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
Console.WriteLine("URMS API → http://localhost:5000   Swagger → http://localhost:5000/swagger");
app.Run();
