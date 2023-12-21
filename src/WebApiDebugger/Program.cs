var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

throw new Exception("Some exception throw while configuring the application");

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();

app.Run();