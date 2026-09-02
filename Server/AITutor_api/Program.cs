using Npgsql;
using AITutor.Core;
using AITutor.Core.Ollama;
using AITutor.Core.Data;
using AITutor.Core.Ingestion;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddCors();
builder.Services.AddHttpClient<IEmbeddingService, OllamaEmbeddingService>(c => c.Timeout = TimeSpan.FromMinutes(5));
builder.Services.AddHttpClient<IChatService, OllamaChatService>(c => c.Timeout = TimeSpan.FromMinutes(5));
var dataSourceBuilder = new NpgsqlDataSourceBuilder(builder.Configuration.GetConnectionString("Default"));
dataSourceBuilder.UseVector();
builder.Services.AddSingleton(dataSourceBuilder.Build());
builder.Services.AddSingleton<IChunkRepository, PgvectorChunkRepository>();
builder.Services.AddScoped<IngestionService>();

var app = builder.Build();

app.Urls.Add("http://localhost:5000");
app.UseCors(policy => policy.WithOrigins("http://localhost:5173").AllowAnyHeader().AllowAnyMethod());

app.MapControllers();
app.Run();
