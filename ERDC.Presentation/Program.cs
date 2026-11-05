using ERDC.Application.Interfaces;
using ERDC.Application.Services;
using ERDC.Infrastructure.Persistence;
using ERDC.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// 1. Conectar SQL Server con ApplicationDbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IApplicationDbContext>(provider =>
    provider.GetRequiredService<ApplicationDbContext>());

// 2. Registrar los servicios de Clean Architecture (Capa 2 y Capa 3)
builder.Services.AddScoped<IPedidoService, PedidoService>();
builder.Services.AddScoped<ICotizacionService, CotizacionService>();

// 3. Habilitar permiso CORS para que React se conecte sin bloqueos
builder.Services.AddCors(options =>
{
    options.AddPolicy("PermitirReact", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseCors("PermitirReact");
app.UseAuthorization();
app.MapControllers();
app.Run();
