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
    it.EnablePersistence = true;
    it.RetryMode = RetryMode.Fixed;
    it.RetryInterval = TimeSpan.FromSeconds(2);
    it.EnableDeadLetterQueue=true;
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