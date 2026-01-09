
## System Overview

The system consists of two main components, the **ETL Subsystem** and the **Search & Discovery System**. Together, these components enable efficient ingestion, indexing, and retrieval of metadata and documents.

```
             (ETL Subsystem)
                     |
                     | 1. WRITE DATA
                     v
   +=========================================+
   |          SHARED STORAGE LAYER           |
   |                                         |
   |  +-------------------+   +------------+ |
   |  |  SQLite Database  |   | Vector Store||
   |  | (Metadata Tables) |   | (Embeddings)||
   |  +---------+---------+   +------+-----+ |
   |            ^                    ^       |
   +============|====================|=======+
                |                    |
                | 2. READ DATA       |
                |                    |
             (Search & Discovery System)
```

### 1. ETL Subsystem

The ETL Subsystem is responsible for ingesting external metadata and documents. It detects changes in source data, streams and extracts content based on file format, stores structured metadata in a relational database, and generates vector embeddings for semantic search.

**Data Source:** CEH Catalogue (WAF / Sitemap)  
**Output:** SQLite Database (relational metadata) and Vector Store (embeddings)

> See: *ETL Subsystem Data Flow Diagram*
```
                                 (Start ETL Job)
                                         |
                                         v
+-----------------------+      +---------------------------+
|   CEH Catalogue WAF   |----->|   Change Detection Svc    |
|   (External Source)   |      |  (Check ETag / Headers)   |
+-----------------------+      +-------------+-------------+
                                             |
                               (If New/Modified Resource)
                                             |
                                             v
                               +---------------------------+
                               |     Stream Downloader     |
                               | (No full load into RAM)   |
                               +-------------+-------------+
                                             |
                                  (Stream File Buffers)
                                             |
                                             v
                       +-------------------------------------------+
                       |           Extraction Strategy             |
                       |  (Factory: XML vs JSON vs ZIP Handler)    |
                       +---------------------+---------------------+
                                             |
                      +----------------------+----------------------+
                      |                                             |
                      v                                             v
          +-----------------------+                   +-----------------------+
          |    Metadata Parser    |                   |   Document Chunker    |
          |  (Extract Titles/Abs) |                   | (Split PDFs/Docs for  |
          |                       |                   |      RAG context)     |
          +-----------+-----------+                   +-----------+-----------+
                      |                                           |
                      | (Structured Info)           (Text Chunks) |
                      v                                           v
          +-----------------------+                   +-----------------------+
          |    SQLite Database    |                   |   Embedding Hash Map  |
          | (Relational Metadata) |                   |     (Cache Check)     |
          +-----------------------+                   +-----------+-----------+
                                                                  |
                                                     (If Hash Miss: Compute)
                                                                  |
                                                                  v
                                                      +-----------------------+
                                                      |   Embedding Model     |
                                                      | (Local LLM / ONNX)    |
                                                      +-----------+-----------+
                                                                  |
                                                            (Vector Floats)
                                                                  |
                                                                  v
                                                      +-----------------------+
                                                      |      Vector Store     |
                                                      | (Semantic Indexing)   |
                                                      +-----------------------+
```



### 2. Search & Discovery System

The Search & Discovery System handles user search and discovery requests. It combines response caching, semantic vector search, and relational filtering, with optional LLM-based enrichment to produce ranked search results or conversational responses.

**Data Source:** Svelte Web Application  
**Output:** JSON search results or conversational answers

> See: *Search & Discovery System Data Flow Diagram*

```
      +------------------------+
      |    User / Svelte UI    |
      |   (Search / Chat)      |
      +-----------+------------+
                  |
                  | (1) JSON Request (Query)
                  v
      +------------------------+
      |   ASP.NET Middleware   |
      |    (Response Cache)    |<---(Hit?)---[ Return Cached Result ]
      +-----------+------------+
                  |
         (Miss: Proceed)
                  |
                  v
      +------------------------+
      |   Search Orchestrator  |
      +-----------+------------+
                  |
      +-----------+-----------------------------------------+
      |                                                     |
      | (2) Embed Query                                     | (3) Keyword Filter
      v                                                     v
+---------------------+                           +---------------------+
|   Embedding Model   |                           |   SQLite Database   |
|   (Query -> Vector) |                           |  (Filters: Date,    |
+----------+----------+                           |   File Type, etc.)  |
           |                                      +----------+----------+
           | (Vector)                                        |
           v                                                 |
+---------------------+                                      |
|    Vector Store     |                                      |
| (Nearest Neighbor)  |<-------------------------------------+
+----------+----------+           (Filter IDs)
           |
           | (4) Return Top-K Document IDs
           v
+---------------------+
|   Result Aggregator |
| (Join Vector IDs    |
|  with SQLite Data)  |
+----------+----------+
           |
           | (5) Context + Original Query
           v
+---------------------+
|      LLM Agent      | (Optional/Bonus for RAG)
|   (Conversational)  | 
+----------+----------+
           |
           | (6) Final Response
           v
      +------------------------+
      |    User / Svelte UI    |
      +------------------------+


```
---

## High-Level System Architecture for Backend

```
+-----------------------------------------------------------------------+
|                            1. FRONTEND                                |
|   +---------------------+                                             |
|   |   Svelte Web App    |                                             |
|   +---------------------+                                             |
+--------------|--------------------------------------------------------+
               | HTTP
               v
+--------------|--------------------------------------------------------+
|                            2. BACKEND                                 |
|                                                                       |
|    +-------------------------------------------------------------+    |
|    |  Layer A: Web API (Presentation)                            |    |
|    +--------------------------+----------------------------------+    |
|                               | Uses                                  |
|                               v                                       |
|    +-------------------------------------------------------------+    |
|    |  Layer B: Core (Business Logic)                             |    |
|    |  - EtlOrchestrator (Uses IMetadataParser)                   |    |
|    |  - Domain: Dataset, MetadataRecord                          |    |
|    +--------------------------^----------------------------------+    |
|                               ^                                       |
|                               | Implements                           |
|    +--------------------------+----------------------------------+    |
|    |  Layer C: Infrastructure (Data & Parsers)                   |    |
|    |                                                             |    |
|    |  [Parser Strategy Pattern]                                  |    |
|    |  - Factory: MetadataParserFactory                           |    |
|    |  - Strategies: XmlParser, JsonParser, RdfParser             |    |
|    |  - Container: ZipFileProcessor                              |    |
|    |                                                             |    |
|    |  [Persistence]                                              |    |
|    |  - SQLite Repository      - Vector Store Impl               |    |
|    +-------------------------------------------------------------+    |
+-------------------------------|---------------------------------------+
                                | Reads / Writes
                                v
+-------------------------------|---------------------------------------+
|                         3. DATA STORAGE                               |
|    +----------------+                  +----------------+             |
|    | SQLite DB File | <--------------> |  Vector Store  |             |
|    +----------------+   (Shared IDs)   |  (Embeddings)  |             |
|                                        +----------------+             |
+-----------------------------------------------------------------------+

```

---

## High-Level System Architecture for Frontend

```aiignore
       +-------------------------------------------------------------+
       |                         VIEW (UI)                           |
       |  (Svelte Components: Search.svelte, Chat.svelte, etc.)      |
       |  - Handles User Input (clicks, typing)                      |
       |  - Binds to VM State ($state)                               |
       +------------------------------+------------------------------+
                                      |
                    Event Handling    |    Reactive Data Binding
                    (Search/Send)     |    (Results/Loading)
                                      v
       +-------------------------------------------------------------+
       |                      VIEWMODEL (VM)                         |
       |  (Logic Classes: SearchVM.svelte.ts, ChatVM.svelte.ts)      |
       |  - Holds Reactive State ($state)                            |
       |  - Processes UI Logic (Formatting, Validation)              |
       |  - Orchestrates Service Calls                                |
       +------------------------------+------------------------------+
                                      |
                     Method Calls     |    DTOs / Results
                     (API Request)    |    (JSON Response)
                                      v
       +------------------------------+------------------------------+
       |                      SERVICES (API)                         |
       |  (BaseService.ts, SearchService.ts, ChatService.ts)         |
       |  - Handles HTTP communication (Fetch/Axios)                 |
       |  - Maps API Responses to Model Objects                      |
       +------------------------------+------------------------------+
                                      |
                        Data Objects  |    Data Structures
                        (Typed Interfaces) | (Business Entities)
                                      v
       +-------------------------------------------------------------+
       |                        MODEL (Domain)                       |
       |  (Types: Dataset.ts, ChatResponse.ts, Author.ts)            |
       |  - Pure TypeScript Interfaces/Classes                       |
       |  - Represents the "Source of Truth"                         |
       +-------------------------------------------------------------+
```