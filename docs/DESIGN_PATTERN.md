To document your **DshETL** project professionally, you should structure your `README.md` to highlight your architectural choices. Use clear headings, technical justifications, and diagrams to help recruiters or contributors understand the "why" behind your code.

Below is a tailored **Design Patterns & Architecture** section you can copy directly into your README.

---

## Architecture & Design Patterns

The **DshETL** project follows a clean, modular architecture to ensure scalability, testability, and a strict separation of concerns between the ETL pipeline, the AI services, and the user interface.

###  Backend (C# / .NET)

| Pattern | Component | Purpose |
| --- | --- | --- |
| **Singleton** | `QdrantClient`, `OnnxEmbeddingService` | Ensures that expensive resources (database connections and heavy ML models) are instantiated only once, preventing memory leaks and resource exhaustion. |
| **Strategy** | `ParsingStrategy` | Encapsulates various document parsing algorithms (e.g., PDF, Text, JSON). This allows the system to switch parsing logic at runtime based on file types without modifying the core ETL engine. |
| **Specification** | Search & Filter Logic | Used to encapsulate business rules into reusable components. It allows for complex data filtering by combining multiple small, testable logic blocks (e.g., `DateRangeSpecification` + `ConfidenceScoreSpecification`). |

#### Singleton Implementation Logic

We chose the **Singleton Pattern** for our AI services because loading the **ONNX model** into memory is a heavy operation. By using a single instance, we achieve:

* **Global Access:** Centralized control over the vector database connection.
* **Performance:** Zero overhead for repeated model inference requests.

---

###  Frontend (Svelte 5)

#### **MVVM (Model-View-ViewModel)**

The frontend is built using the **MVVM** pattern, which provides a robust way to handle state and logic in complex user interfaces.

* **Model:** Represents the domain data and business logic (e.g., `Dataset`, `ChatMessage`).
* **ViewModel:** Acts as the "Brain." It handles the state using Svelte 5 **Runes** (`$state`, `$derived`) and manages interactions between the View and the Backend Services.
* **View:** Pure Svelte components that focus only on presentation. They bind directly to the ViewModel's properties.

**Why MVVM?**

1. **Testability:** We can unit test the logic in the `SearchVM` or `ChatVM` without mounting the UI components.
2. **Decoupling:** The UI can be redesigned completely as long as it binds to the same ViewModel properties.
