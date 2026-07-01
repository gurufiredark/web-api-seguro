using Microsoft.EntityFrameworkCore;
using Segfy.Policies.Api.Data;
using Segfy.Policies.Api.Services;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=policies.db";

builder.Services.AddDbContext<PoliciesDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddScoped<IDateTimeProvider, SystemDateTimeProvider>();
builder.Services.AddScoped<IPolicyNumberGenerator, PolicyNumberGenerator>();
builder.Services.AddScoped<IInsurancePolicyService, InsurancePolicyService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PoliciesDbContext>();
    db.Database.EnsureCreated();
}

app.Run();
