using BoyutAplication.Data;
using Microsoft.EntityFrameworkCore;
using BoyutAplication.Services;



var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// OpenAPI / Swagger dokümantasyonu
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// EF Core DbContext konfigürasyonu
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=invoice_status.db"));


builder.Services.AddMemoryCache();

builder.Services.AddScoped<IMockIntegratorService, MockIntegratorService>();
builder.Services.AddScoped<IInvoiceStatusService, InvoiceStatusService>();


var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
