To ensure anyone can set up the **DshETL** project, the instructions must be divided into the backend (.NET), the AI models (ONNX/Gemini), and the frontend (Svelte).

Here is a professional **Getting Started** guide in Markdown format for your project.

---

# 🚀 Getting Started: DshETL Installation Guide

Follow these steps to set up the environment and run the full ETL and Search/Chat stack.

## 📋 Prerequisites

* **.NET 8 SDK** or higher
* **Node.js 20+** and **npm**
* **Google Cloud Project** (for Google Drive access)
* **Google AI Studio Account** (for Gemini API)

---

## 🛠️ 1. Backend Setup (.NET)

### AI Model Placement

The system uses a local ONNX model for high-performance text embeddings.

1. Locate your `model.onnx` file.
2. Place it in the backend project under:
   `DshEtlSearch.Infrastructure/Models/embedding_model/model.onnx`

### Google Drive Integration

1. Go to the [Google Cloud Console](https://console.cloud.google.com/).
2. Create a **Service Account** and download the **JSON Key**.
3. Place the JSON file in your backend root and name it `google-drive-key.json`.
4. Ensure the Service Account has "Editor" permissions on the target Google Drive folder.

### Environment Variables

Create an `appsettings.Development.json` or set your environment variables:

```json
{
  "Gemini": {
    "ApiKey": "YOUR_GEMINI_API_KEY_HERE"
  },
  "GoogleDrive": {
    "ServiceAccountKeyPath": "google-drive-key.json",
    "FolderId": "YOUR_TARGET_FOLDER_ID"
  }
}

```

### Run the Backend

```bash
# Navigate to the API folder
dotnet run --project DshEtlSearch.Api

```

---

## 🎨 2. Frontend Setup (Svelte 5)

### Dependency Installation

Navigate to the `frontend` directory and install the required packages:

```bash
cd frontend
npm install

```

### Configure the Proxy

The frontend is pre-configured to talk to the .NET API via the Vite proxy. Ensure your `vite.config.ts` matches your backend port (default is 5007).

### Run the Frontend

```bash
npm run dev

```

The application will be available at `http://localhost:5173`.

---

## 🧪 3. Running Tests

To verify the installation is correct, run the automated test suites:

| Layer | Command |
| --- | --- |
| **Backend (Unit/Integration)** | `dotnet test` |
| **Frontend (Unit/Logic)** | `npm run test:unit` |
| **Frontend (End-to-End)** | `npm run test:e2e` |

---

## 🚦 Troubleshooting

* **ONNX Errors:** Ensure you have the `Microsoft.ML.OnnxRuntime` NuGet package installed.
* **CORS/Proxy Issues:** If the frontend cannot reach the API, check that the backend is running on the port specified in `vite.config.ts`.
* **Gemini API:** Verify that your API key is active and has "Pay-as-you-go" or "Free Tier" enabled in Google AI Studio.

---

**Would you like me to help you create a `docker-compose.yml` file to run the entire project (including the Qdrant database) with a single command?**