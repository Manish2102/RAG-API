using Azure;
using Azure.AI.OpenAI;
using Azure.AI.TextAnalytics;
using Azure.Search.Documents.Indexes;
using BusinessLogicLayer.AISearchInterfaces;
using BusinessLogicLayer.AISearchServices;
using BusinessLogicLayer.Configurations;
using BusinessLogicLayer.CosmosInterface;
using BusinessLogicLayer.CosmosServices;
using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.Repositories;
using BusinessLogicLayer.Services;
using BusinessLogicLayer.TestInterfaces;
using BusinessLogicLayer.TestServices;
using DataAccessLayer.Interfaces;
using DataAccessLayer.Repositories;
using Microsoft.Azure.Cosmos;
using OpenAI.Embeddings;
using System.ClientModel;
using UglyToad.PdfPig.DocumentLayoutAnalysis.Export;

var builder = WebApplication.CreateBuilder(args);


var searchConfig = builder.Configuration.GetSection("SearchClient");
builder.Services.AddSingleton(searchConfig);

builder.Services.AddSingleton<SearchIndexClient>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    string endpoint = config["SearchClient:endpoint"] ?? "";
    string apiKey = config["SearchClient:apikey"] ?? "";

    return new SearchIndexClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
});


builder.Services.AddSingleton<EmbeddingClient>(provider =>
{
    var config = provider.GetRequiredService<IConfiguration>();
    var openAIConfig = config.GetSection("OpenAI");

    var endPoint = openAIConfig["endpoint"] ?? throw new ArgumentException("OpenAI endpoint not found");
    var apiKey = openAIConfig["apikey"] ?? throw new ArgumentException("OpenAI apikey not found");
    var deploymentName = openAIConfig["deploymentNameEmbedding"] ?? throw new ArgumentException("deploymentNameEmbedding not found");

    var openAiClient = new AzureOpenAIClient(
        new Uri(endPoint),
        new ApiKeyCredential(apiKey)
    );

    return openAiClient.GetEmbeddingClient(deploymentName);
});

builder.Services.AddSingleton(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    return new CosmosClient(config["cosmos:connectionstring"]);
});

builder.Services.AddSingleton<TextAnalyticsClient>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();

    var endpoint = configuration["textanalytics:endpoint"];
    var key = configuration["textanalytics:apikey"];

    if (string.IsNullOrWhiteSpace(endpoint))
        throw new InvalidOperationException("LanguageService--Endpoint is missing");

    if (string.IsNullOrWhiteSpace(key))
        throw new InvalidOperationException("LanguageService--Key is missing");

    return new TextAnalyticsClient(
        new Uri(endpoint),
        new AzureKeyCredential(key)
    );
});
//var client = new EmbeddingClient(deploymentName, new AzureKeyCredential(apiKey), OpenAIOptions);

builder.Services.AddScoped<IDocumentUploadService, DocumentUploadService>();

builder.Services.AddScoped<IDocumentUploadInterface, DocumentUploadRepository>();

builder.Services.AddScoped<IFileTextExtractor, FileTextExtracterService>();

builder.Services.AddScoped<ITextPreprocessService, TextPreprocessService>();

builder.Services.AddScoped<ITextChunkService, TextChunkService>();

builder.Services.AddScoped<IEmbeddingService, EmbeddingService>();

builder.Services.AddScoped<IDocumentProcessingSearchService, DocumentProcessingService>();

builder.Services.AddScoped<IRagSearchService, RagSearchService>();

builder.Services.AddScoped<IDocumentProcessingCosmosService, DocumentProcessingCosmosService>();

builder.Services.AddScoped<ICosmosRagServvice, CosmosRagService>();

builder.Services.AddScoped<ILLmService, LLmService>();


builder.Services.AddHttpClient();

builder.Services.AddScoped<ICosmosVectorService, CosmosVectorService>();


builder.Services.Configure<AzureLanguageOptions>(
    builder.Configuration.GetSection("textanalytics"));
builder.Services.AddScoped<IPiiRedactionService, AzurePiiRedactionService>();
builder.Services.AddScoped<IIpProtectionService, IpProtectionService>();


builder.Services.AddScoped<IAzureSearchIndexService, AzureSearchIndexService>();
builder.Services.AddScoped<ICosmosVectorService, CosmosVectorService>();
builder.Services.AddScoped<IDocumentProcessingCosmosService, DocumentProcessingCosmosService>();


/// Test services
/// 

builder.Services.AddScoped<ITextExtractorTest, TextExtractorService>();

//builder.Services.AddScoped<IPiiRedactionService, TestPiiRedactService>();

builder.Services.AddControllers();

builder.Services.AddSwaggerGen();


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
