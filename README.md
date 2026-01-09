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

| Category         | Technology |
|------------------|------------|
| Backend          | C# .NET 8 (ASP.NET Core Web API) |
| Frontend         | Svelte |
| Vector Embeeding | all-MiniLM-L6-v2 |
| Dataset          | SQLite (relational metadata) + Qdrant (vector embeddings) |

---

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

| Category         | Technology |
|------------------|------------|
| Backend          | C# .NET 8 (ASP.NET Core Web API) |
| Frontend         | Svelte |
| Vector Embeeding | all-MiniLM-L6-v2 |
| Dataset          | SQLite (relational metadata) + Qdrant (vector embeddings) |

---


# Project Documentation

Explore the detailed technical documentation for this project:

| Category             | Documentation                                       | Description                             |
|----------------------|-----------------------------------------------------|-----------------------------------------|
| **Setup**            | **[How to Run](docs/HOWTORUN.md)**                  | Step-by-step guide to setting up .NET, Svelte, and API keys. |
| **Architecture**     | **[System Design](docs/SYSTEM_DESIGN.md)**          | High-level overview of the ETL pipeline and AI integration. |
| **Logic**            | **[Design Patterns](docs/DESIGN_PATTERN.md)**       | Deep dive into Strategy, Specification, Singleton, and MVVM. |
| **Codebase**         | **[File Structure](docs/FILE_STRUCTURE.md)**        | Map of the project directories and file responsibilities. |
| **Quality**          | **[Testing Report](docs/TESTING.md)**               | Summary of Unit, Integration, and E2E test results. |
| **DevOps**           | **[CI/CD Pipeline](docs/CICD.md)**                  | Overview of GitHub Actions, build steps, and automated checks. |
| **Evidence**         | **[Results & Demo](docs/RESULTS.md)**               | Performance metrics, screenshots, and search accuracy reports. |
| **LLM Chat History** | **[Chat History](docs/ChatAnswer/chat_history.md)** | Chat history list for gemini LLM        |
---
