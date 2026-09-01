# RAG-API

An Azure-native Retrieval-Augmented Generation (RAG) backend built on **.NET 10**. It ingests documents (PDF, DOCX, XLSX, PPTX, TXT, and other plain-text formats), extracts and sanitizes their text, chunks and embeds the content, stores the resulting vectors, and answers natural-language questions by retrieving the most relevant chunks and passing them to an Azure OpenAI chat model.

## What it does

1. **Upload** — a document is uploaded through the API and persisted to Azure Blob Storage.
2. **Extract** — text is pulled out of the file (PDF via PdfPig, DOCX/XLSX/PPTX via DocumentFormat.OpenXml, plain-text formats via a direct stream read).
3. **Sanitize** — extracted text is cleaned up, then run through:
   - **PII redaction** using Azure AI Language (`TextAnalyticsClient`), including India-specific categories (PAN, Aadhaar).
   - **IP/patent redaction** using an LLM prompt that detects and strips proprietary/patent-style content.
4. **Chunk** — sanitized text is split into overlapping fixed-size windows.
5. **Embed** — each chunk is embedded via Azure OpenAI embeddings.
6. **Store** — chunks + vectors are written to a vector store.
7. **Query** — a question is embedded, the vector store is searched for the top-K most similar chunks, and those chunks are passed to an Azure OpenAI chat model with a strict "answer only from the given context" system prompt. The API returns the answer along with the supporting chunks.

Two parallel vector-store pipelines are implemented side by side, selectable by which controller/endpoint you call:

| Pipeline | Vector store | Similarity search |
|---|---|---|
| **AI Search** | Azure AI Search (HNSW vector index, 1536-dim) | `VectorizedQuery` |
| **Cosmos** | Azure Cosmos DB (NoSQL) | `VectorDistance()` SQL function |

## Architecture

Classic 3-layer solution (`API-RAG.slnx`):

```
API-RAG (Web API)
  └── BusinessLogicLayer (pipeline logic)
        └── DataAccessLayer (Azure Blob Storage access)
```

- **API-RAG** — ASP.NET Core Web API, controllers, DI composition root (`Program.cs`), Swagger.
- **BusinessLogicLayer** — upload orchestration, text extraction, preprocessing, chunking, embedding, PII/IP redaction, vector index/search operations, and LLM calls. Also contains the Azure AI Search and Cosmos DB integration code (`Azure-AI-Search/`, `Azure-Cosmos-DB/` folders).
- **DataAccessLayer** — Azure Blob Storage repository for raw document persistence.

Dependency injection is used throughout (all services are registered as interfaces in `Program.cs`), with the options pattern for typed configuration (e.g. `IOptions<AzureLanguageOptions>`) and a repository pattern for blob storage.

## API endpoints

| Verb | Route | Description |
|---|---|---|
| `GET` | `/` | Health check |
| `POST` | `/create-index?indexName=...` | Creates an Azure AI Search vector index (HNSW, 1536-dim) |
| `POST` | `/file-upload-AI-Search?indexName=...` (form file `file`) | Uploads, extracts, chunks, embeds, and indexes a document into Azure AI Search |
| `GET` | `/file-upload-AI-Search/query?indexName=...&q=...&fileUrl=&topK=5` | RAG query against the Azure AI Search index |
| `POST` | `api/DocumentProcessingCosmos/process-and-store` (form file `file`) | Uploads, extracts, chunks, embeds, and stores a document's chunks in Cosmos DB |
| `GET` | `api/DocumentProcessingCosmos/query?q=...&fileUrl=&topK=5` | RAG query against Cosmos DB |
| `POST` | `/analyze-text` | Runs Azure PII redaction on raw text |
| `POST` | `/extract-text` (form file `file`) | Extracts text, preprocesses, redacts PII, flags IP-related content |
| `POST` | `/extract-only-text` (form file `file`) | Extracts, preprocesses, redacts PII, and applies IP/patent redaction, returning the final text |

Swagger UI is available at `/swagger` when running in the `Development` environment.

## Technology stack

- **.NET 10** / ASP.NET Core Web API (minimal hosting model)
- **Azure OpenAI** (`Azure.AI.OpenAI`) — embeddings and chat completions
- **Azure AI Search** (`Azure.Search.Documents`) — vector index and search
- **Azure Cosmos DB** (`Microsoft.Azure.Cosmos`) — alternate vector store using native vector search
- **Azure AI Language** (`Azure.AI.TextAnalytics`) — PII entity recognition/redaction
- **Azure Blob Storage** (`Azure.Storage.Blobs`) — raw document persistence
- **PdfPig** — PDF text extraction
- **DocumentFormat.OpenXml** — DOCX/XLSX/PPTX text extraction
- **Swashbuckle.AspNetCore** — Swagger/OpenAPI

## Configuration

None of the required settings are checked into `appsettings.json`. Supply them via `dotnet user-secrets`, environment variables, or an environment-specific `appsettings` override before running:

| Section | Keys | Used for |
|---|---|---|
| `OpenAI` | `endpoint`, `apikey`, `deploymentNameEmbedding`, `deploymentNameChat` | Azure OpenAI embeddings + chat |
| `SearchClient` | `endpoint`, `apikey` | Azure AI Search |
| `cosmos` | `connectionstring`, `database`, `container` | Azure Cosmos DB |
| `textanalytics` | `endpoint`, `apikey` | Azure AI Language (PII redaction) |
| `AzureBlob` | `url`, `containerName` (defaults to `customgpt-data`) | Blob storage |

## Running locally

Requires the **.NET 10 SDK**.

```bash
dotnet build API-RAG.slnx
dotnet run --project API-RAG/API-RAG.csproj
```

The API listens on `http://localhost:5147` (or `https://localhost:7121`) in the `Development` environment, with Swagger UI at `/swagger`.

## Sample data

`test_patent.pdf` / `test_patent.txt` at the repo root are synthetic sample documents (a fictional patent abstract) for manually exercising the ingestion and IP-redaction pipeline through Swagger or the included `API-RAG.http` file.

## Known limitations

- The two vector-store pipelines (AI Search vs Cosmos) duplicate most of their logic rather than sharing a common abstraction.
- `appsettings.json` has no secrets-exclusion safeguard beyond the default `.gitignore` — take care not to commit real credentials into it.
- No automated tests or CI/CD pipeline are set up yet.
