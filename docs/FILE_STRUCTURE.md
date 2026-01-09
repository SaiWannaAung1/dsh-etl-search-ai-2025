

---

#  Backend Architecture Overview

```
backend
├── DshEtlSearch.Api                # API Layer (Presentation / HTTP)
├── DshEtlSearch.Core               # Core Layer (Domain + Business Rules)
├── DshEtlSearch.Infrastructure     # Infrastructure Layer (External & Persistence)
├── DshEtlSearch.UnitTests          # Unit Tests
├── DshEtlSearch.IntegrationTests   # Integration Tests
```

---

## 1️⃣ API Layer — `DshEtlSearch.Api`

**Purpose:**
➡️ Handles **HTTP requests**, **API endpoints**, and **request/response mapping**
➡️ No business logic — only orchestration

```
DshEtlSearch.Api
├── Configuration
│   ├── AppSettings.cs
│   │   # Strongly-typed application configuration
│   └── ServiceCollectionExtensions.cs
│       # Dependency Injection registration for API services
│
├── Controllers
│   ├── ChatController.cs
│   │   # Chat-related HTTP endpoints
│   └── SearchController.cs
│       # Search-related HTTP endpoints
│
├── Middleware
│   ├── ExceptionHandlingMiddleware.cs
│   │   # Global exception handling & error formatting
│   └── ResponseCachingMiddleware.cs
│       # API response caching logic
│
├── Models
│   ├── Requests
│   │   ├── ChatRequest.cs
│   │   │   # Chat request DTO from client
│   │   └── SearchRequest.cs
│   │       # Search request DTO from client
│   │
│   └── Responses
│       ├── ChatResponse.cs
│       │   # Chat API response DTO
│       ├── DatasetDetailResponse.cs
│       │   # Dataset detail response
│       ├── SearchResponse.cs
│       │   # Search results response
│       └── SourceFileResponse.cs
│           # Metadata source file response
│
├── DatasetStorage
│   # API-level dataset file storage (temporary / exposed data)
│
└── Program.cs
    # Application bootstrap (host, middleware, DI, routing)
```

🔹 **Rule:**
API layer depends on **Core interfaces**, never on Infrastructure directly.

---

## 2️⃣ Core Layer — `DshEtlSearch.Core`

**Purpose:**
➡️ Contains **business logic**, **domain models**, and **contracts**
➡️ Completely independent of frameworks and databases

```
DshEtlSearch.Core
├── Common
│   ├── Enums
│   │   ├── FileType.cs
│   │   │   # Supported data file types
│   │   ├── MetadataFormat.cs
│   │   │   # Metadata format definitions
│   │   └── VectorSourceType.cs
│   │       # Vector data source types
│   │
│   ├── BaseSpecification.cs
│   │   # Base implementation for Specification Pattern
│   ├── Result.cs
│   │   # Standard success/failure result wrapper
│   └── VectorSearchResult.cs
│       # Vector search result model
│
├── Domain
│   ├── Dataset.cs
│   │   # Core dataset entity
│   ├── DataFile.cs
│   │   # Dataset file entity
│   ├── EmbeddingVector.cs
│   │   # Vector embedding domain model
│   └── MetadataRecord.cs
│       # Metadata domain entity
│
├── Interfaces
│   ├── Application
│   │   └── IEtlOrchestrator.cs
│   │       # High-level ETL workflow contract
│   │
│   ├── Infrastructure
│   │   ├── IDownloader.cs
│   │   │   # Downloads dataset files
│   │   ├── IExtractionService.cs
│   │   │   # Extracts compressed archives
│   │   ├── IMetadataParser.cs
│   │   │   # Parses metadata files
│   │   ├── IVectorStore.cs
│   │   │   # Vector database abstraction
│   │   └── IMetadataRepository.cs
│   │       # Metadata persistence abstraction
│   │
│   └── Services
│       ├── IEtlService.cs
│       │   # Core ETL business logic
│       └── IEmbeddingService.cs
│           # Embedding generation service
│
└── DshEtlSearch.Core.csproj
```

🔹 **Rule:**
Core **depends on nothing**.
Everything else depends on Core.

---

## 3️⃣ Infrastructure Layer — `DshEtlSearch.Infrastructure`

**Purpose:**
➡️ Implements **Core interfaces**
➡️ Handles **databases, file systems, APIs, AI services**

```
DshEtlSearch.Infrastructure
├── Data
│   ├── SQLite
│   │   ├── AppDbContext.cs
│   │   │   # Entity Framework database context
│   │   └── SqliteMetadataRepository.cs
│   │       # SQLite implementation of IMetadataRepository
│   │
│   ├── VectorStore
│   │   ├── QdrantAdapter.cs
│   │   │   # Low-level Qdrant API adapter
│   │   └── QdrantVectorStore.cs
│   │       # IVectorStore implementation
│   │
│   └── SpecificationEvaluator.cs
│       # Evaluates specification queries
│
├── ExternalServices
│   ├── Ai
│   │   └── GeminiLlmService.cs
│   │       # LLM integration (Google Gemini)
│   │
│   ├── Ceh
│   │   ├── CehCatalogueClient.cs
│   │   │   # CEH external API client
│   │   └── OnnxEmbeddingService.cs
│   │       # ONNX embedding generator
│   │
│   ├── GoogleDrive
│   │   └── GoogleDriveService.cs
│   │       # Google Drive file access
│   │
│   └── Ingestion
│       └── EtlOrchestrator.cs
│           # Concrete ETL orchestration
│
├── FileProcessing
│   ├── Downloader
│   │   └── CehDatasetDownloader.cs
│   │       # Downloads datasets from CEH
│   │
│   ├── Extractor
│   │   └── ZipExtractionService.cs
│   │       # Extracts ZIP archives
│   │
│   └── Parsers
│       ├── Strategies
│       │   ├── Iso19115XmlParser.cs
│       │   │   # ISO 19115 XML metadata parser
│       │   ├── JsonExpandedParser.cs
│       │   │   # Expanded JSON metadata parser
│       │   └── SchemaOrgJsonLdParser.cs
│       │       # Schema.org JSON-LD parser
│       │
│       └── MetadataParserFactory.cs
│           # Parser strategy selector
│
└── DshEtlSearch.Infrastructure.csproj
```

🔹 **Rule:**
Infrastructure **depends on Core**, never the opposite.

---

## 4️⃣ Testing Layer

### 🧪 Unit Tests — `DshEtlSearch.UnitTests`

**Purpose:**
➡️ Test **business logic in isolation**
➡️ No database, no network

```
DshEtlSearch.UnitTests
├── Core
│   ├── Domain
│   │   └── DatasetTests.cs
│   │       # Domain entity behavior tests
│   └── Features
│       # Feature-level business logic tests
│
├── Infrastructure
│   ├── Downloader
│   │   └── CehDatasetDownloaderTests.cs
│   │       # Downloader behavior tests (mocked)
│   └── Parsers
│       └── ParserStrategiesTests.cs
│           # Metadata parsing tests
│
└── DshEtlSearch.UnitTests.csproj
```

---

### 🔗 Integration Tests — `DshEtlSearch.IntegrationTests`

**Purpose:**
➡️ Test **real infrastructure together**
➡️ Databases, APIs, filesystem

```
DshEtlSearch.IntegrationTests
├── Controller
│   └── SearchControllerTests.cs
│       # End-to-end API tests
│
├── ExternalServices
│   ├── EtlOrchestratorTests.cs
│   │   # Full ETL pipeline test
│   └── ZipExtractionServiceTests.cs
│       # Real ZIP extraction tests
│
├── Infrastructure
│   └── Repositories
│       └── SqliteMetadataRepositoryTests.cs
│           # SQLite repository integration tests
│
└── DshEtlSearch.IntegrationTests.csproj
```

---
# Frontend Directory Structure


## ✅ Final Architecture Summary

| Layer                 | Responsibility                     |
| --------------------- | ---------------------------------- |
| **API**               | HTTP, Controllers, DTOs            |
| **Core**              | Domain, Business Rules, Interfaces |
| **Infrastructure**    | DB, Files, External APIs, AI       |
| **Unit Tests**        | Fast, isolated tests               |
| **Integration Tests** | Real system validation             |

---

```text
src/
├── lib/
│   ├── models/            # Pure TypeScript interfaces (The "M" in MVVM)
│   │   ├── Dataset.ts     
│   │   ├── Chat.ts        
│   │   └── Search.ts      
│   │
│   ├── services/          # API Communication (Fetch logic)
│   │   ├── BaseService.ts # Common API logic (Error handling, URLs)
│   │   ├── SearchService.ts
│   │   └── ChatService.ts
│   │
│   ├── viewmodels/        # Svelte 5 logic classes (The "VM" in MVVM)
│   │   ├── SearchVM.svelte.ts # Uses $state for UI logic
│   │   └── ChatVM.svelte.ts   # Uses $state for UI logic
│   │
│   └── components/        # Reusable UI fragments (Cards, Buttons)
│       ├── ChatMessage.svelte
│       └── DatasetCard.svelte
│
├── routes/                # SvelteKit Pages (The "View" in MVVM)
│   ├── +layout.svelte     # Shared Layout (Navbar, CSS)
│   ├── search/
│   │   └── +page.svelte   # Search View
│   ├── chat/
│   │   └── +page.svelte   # Chat View
│   └── +page.svelte       # Home View
│
├── app.css                # Global Tailwind styles
└── app.d.ts               # Global TypeScript declarations

```






