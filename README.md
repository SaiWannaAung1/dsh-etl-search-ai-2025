# dsh-etl-search-ai-2025
## About The Project

### [Gemini LLM conversations for Project](docs/ChatAnswer/chat_history.md)

The system is designed to ingest metadata and supporting documents from the **CEH Catalogue Service** , process them into vector embeddings, and allow users to discover data using natural language queries.

Key capabilities include:
* **Automated ETL Pipeline**: Extracts ISO 19115 metadata, JSON-LD, RDF and supporting documents from remote archives.
* **Semantic Search**: Uses vector embeddings to find datasets based on meaning rather than just keyword matching.
* **RAG (Retrieval Augmented Generation)**: Enhances search results by retrieving context from supporting documents.
* **Conversational UI**: A Svelte-based frontend allowing users to interact with data via an AI agent.

---

## Technology Stack

| Category | Technology |
|----------|------------|
| Backend  | C# .NET 8 (ASP.NET Core Web API) |
| Frontend | Svelte |
| Dataset  | SQLite (relational metadata) + Qdrant (vector embeddings) |

---

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

## High-Level System Architecture

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
