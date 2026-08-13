using Npgsql;
using System.Text;
using Pgvector;using System.Text.Json;

var http = new HttpClient();

// // database link
// Console.WriteLine("Hello, World!");

// var connString = "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=123";

// var dataSourceBuilder = new NpgsqlDataSourceBuilder(connString);
// dataSourceBuilder.UseVector();
// await using var dataSource = dataSourceBuilder.Build();
// await using var conn = await dataSource.OpenConnectionAsync();

// Console.WriteLine("Konekcija uspešna!");


// Test 1: embedding
var embedBody = JsonSerializer.Serialize(new { model = "nomic-embed-text", prompt = "test rečenica" });
var embedResp = await http.PostAsync("http://localhost:11434/api/embeddings",
    new StringContent(embedBody, Encoding.UTF8, "application/json"));
var embedJson = await embedResp.Content.ReadAsStringAsync();
using var embedDoc = JsonDocument.Parse(embedJson);
var vectorLength = embedDoc.RootElement.GetProperty("embedding").GetArrayLength();
Console.WriteLine($"Embedding OK — dužina vektora: {vectorLength}");

// Test 2: generisanje odgovora
var genBody = JsonSerializer.Serialize(new { model = "llama3.1:8b", prompt = "Reci samo 'radi'.", stream = false });
var genResp = await http.PostAsync("http://localhost:11434/api/generate",
    new StringContent(genBody, Encoding.UTF8, "application/json"));
var genJson = await genResp.Content.ReadAsStringAsync();
using var genDoc = JsonDocument.Parse(genJson);
Console.WriteLine($"Generisanje OK — odgovor: {genDoc.RootElement.GetProperty("response").GetString()}");