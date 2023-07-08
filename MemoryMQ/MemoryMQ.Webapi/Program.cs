using MemoryMQ;
using MemoryMQ.Configuration;
using MemoryMQ.Consumer;
using MemoryMQ.Webapi;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddMemoryMQ(it =>
{
    it.EnablePersistent = true;
    it.RetryMode = RetryMode.Incremental;
    it.RetryInterval = TimeSpan.FromSeconds(5);
});

builder.Services.AddScoped<ConsumerA>();
builder.Services.AddScoped<ConsumerB>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();