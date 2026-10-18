using Npgsql;
using AITutor.Core;
using AITutor.Core.Ollama;
using AITutor.Core.Data;
using AITutor.Core.Ingestion;
using AITutor.Core.Rag;
using AITutor.Core.Subjects;
using AITutor.Core.Subjects.OperatingSystems;
using AITutor.Core.Subjects.ProgramskiPrevodioci;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddCors();
var ollamaDefaults = new OllamaOptions();
builder.Services.AddSingleton(new OllamaOptions(
    builder.Configuration["Ollama:ChatModel"] ?? ollamaDefaults.ChatModel,
    builder.Configuration["Ollama:EmbeddingModel"] ?? ollamaDefaults.EmbeddingModel,
    builder.Configuration["Ollama:BaseUrl"] ?? ollamaDefaults.BaseUrl));
builder.Services.AddHttpClient<IEmbeddingService, OllamaEmbeddingService>(c => c.Timeout = TimeSpan.FromMinutes(5));
builder.Services.AddHttpClient<IChatService, OllamaChatService>(c => c.Timeout = TimeSpan.FromMinutes(10));
var dataSourceBuilder = new NpgsqlDataSourceBuilder(builder.Configuration.GetConnectionString("Default"));
dataSourceBuilder.UseVector();
builder.Services.AddSingleton(dataSourceBuilder.Build());
builder.Services.AddSingleton<IChunkRepository, PgvectorChunkRepository>();
builder.Services.AddScoped<IngestionService>();
builder.Services.AddScoped<RagQueryService>();
builder.Services.AddScoped<IIntentClassifier, LlmIntentClassifier>();
// Predmeti: svaki implementira ISubjectProfile; jezgro ne zna ništa o pojedinom predmetu.
builder.Services.AddScoped<ISubjectProfile, ProgramskiPrevodiociProfile>();
builder.Services.AddScoped<ISubjectProfile, OperatingSystemsProfile>();
builder.Services.AddScoped(sp => new SubjectRegistry(sp.GetServices<ISubjectProfile>(), builder.Configuration["Subjects:DefaultSubject"]));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Adresa: ASPNETCORE_URLS ako je zadata (Docker), inače lokalni razvoj na 5000.
if (string.IsNullOrEmpty(builder.Configuration["ASPNETCORE_URLS"]) && string.IsNullOrEmpty(builder.Configuration["urls"]))
    app.Urls.Add("http://localhost:5000");

// Dozvoljeni izvori za CORS (Cors:Origins); podrazumevano Vite dev server.
var corsOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() is { Length: > 0 } configured
    ? configured
    : new[] { "http://localhost:5173" };
app.UseCors(policy => policy.WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod());
app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();
app.Run();
