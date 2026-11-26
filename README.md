# 📈 MarketData Analytics & AI Macro Terminal

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-13.0-239120?logo=c-sharp&logoColor=white)](https://learn.microsoft.com/dotnet/csharp/)
[![Architecture](https://img.shields.io/badge/Architecture-N--Tier%20%2F%20Clean-blue)](https://learn.microsoft.com/dotnet/architecture/modern-web-apps-azure/common-web-application-architectures)
[![AI](https://img.shields.io/badge/AI-Google%20Gemini-4285F4?logo=google&logoColor=white)](https://ai.google.dev/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

> An enterprise-grade, institutional financial analytics platform and AI-driven macroeconomic terminal built with **ASP.NET Core Web API**, **N-Tier Architecture**, **WebSockets**, and **Google Gemini LLM**.

---

## 📑 Table of Contents
- [Project Overview](#-project-overview)
- [Key Features](#-key-features)
- [Architecture & Design](#-architecture--design)
- [Tech Stack](#-tech-stack)
- [Getting Started](#-getting-started)
- [Configuration](#-configuration)
- [API Endpoints](#-api-endpoints)
- [Screenshots & Web UI](#-screenshots--web-ui)
- [Author](#-author)

---

## 🚀 Project Overview

**MarketData Analytics** bridges institutional financial analysis with modern software engineering. It automates complex data ingestion pipelines—including CFTC Commitment of Traders (COT) reports, high-impact macroeconomic event feeds, and live exchange price action—and synthesizes them using quantitative algorithms and generative AI into actionable market intelligence.

Originally conceptualized during a software engineering internship focused on **3-Tier / N-Tier Enterprise Architecture**, the platform has been developed into an end-to-end financial terminal combining real-time streaming, quantitative screening, and LLM-powered macro synthesis.

---

## ✨ Key Features

- 🏛️ **N-Tier Enterprise Architecture:** Clean separation of concerns between `Core`, `Business`, `DataAccess`, and `Presentation` layers. Built on SOLID principles with full Dependency Injection.
- ⚡ **Real-Time Market Streaming (WebSockets):** Drivable high-throughput client using `System.Net.WebSockets.ClientWebSocket` connected to Deriv API for live tick and historical candlestick feeds.
- 📊 **COT (Commitment of Traders) Analysis:**
  - CFTC institutional net positioning metrics (Non-Commercial / Speculator divergence).
  - 6-month & 12-month normalized index scores and institutional bias calculation.
  - Historical positioning charts and backtesting simulation engine.
- 🎯 **Multi-Timeframe Volatility Squeeze Scanner:**
  - Bollinger Bands & Keltner Channels volatility compression detection across multiple time horizons (M1 to D1).
  - Identifies explosive breakout setups and monitors squeeze duration metrics.
- 📅 **Automated Macroeconomic Calendar:**
  - Real-time XML feed ingestion from Forex Factory with automated currency impact classification (High/Medium).
- 🤖 **AI Macroeconomic Analyst (Google Gemini Integration):**
  - Synthesizes technical volatility compressions, institutional sentiment divergences, and upcoming geopolitical catalysts into structured macro reports.
  - Automatically adheres to institutional risk disclosure and non-advisory formatting constraints.
- ⚡ **Optimized In-Memory Caching:**
  - `IMemoryCache` integration to prevent external API throttling, minimize latency, and optimize server-side compute.

---

## 🏗️ Architecture & Design

```
MarketDataAnalytics/
├── MarketDataApp.Core/              # Domain entities, DTOs, and Service Interfaces
│   ├── Entities/                    # CandleStick, Market models
│   ├── DTOs/                        # AiMacroModels, CotAnalysisDto, BacktestResultDto
│   └── Interfaces/                  # ICotService, IMarketService, IMarketApiClient
│
├── MarketDataApp.Business/          # Business logic, quantitative algorithms, AI services
│   └── Services/
│       ├── AiMacroService.cs        # Gemini LLM synthesis & economic calendar parser
│       ├── CotService.cs            # CFTC institutional positioning & backtest engine
│       ├── SqueezeService.cs        # Multi-timeframe Bollinger/Keltner squeeze scanner
│       ├── SeasonalityService.cs    # Historical seasonality tendencies
│       └── MarketService.cs         # Candle data orchestration
│
├── MarketDataApp.DataAccess/        # Data access, external APIs, and WebSocket streaming
│   └── Clients/
│       └── DerivApiClient.cs        # Asynchronous WebSocket client for market data
│
└── MarketDataApp.Presentation/      # ASP.NET Core Web API & Interactive Web Dashboard
    ├── Controllers/                 # RESTful API Controllers (Swagger/OpenAPI enabled)
    └── wwwroot/                     # Vanilla JS, HTML5, CSS3 interactive dashboard
```

---

## 🛠️ Tech Stack

| Domain | Technology / Library |
|---|---|
| **Framework & Language** | C# 13, .NET 10 (ASP.NET Core Web API) |
| **Architecture** | N-Tier / Clean Architecture, Dependency Injection |
| **Real-Time & Networking** | `System.Net.WebSockets`, `HttpClientFactory` |
| **Artificial Intelligence** | Google Gemini API (REST client) |
| **Caching & Performance** | In-Memory Caching (`IMemoryCache`) |
| **Data Parsing & Feeds** | `System.Text.Json`, `System.Xml.Linq` (XDocument) |
| **Documentation & API** | Swagger / OpenAPI (Swashbuckle) |
| **Frontend Dashboard** | HTML5, CSS3, Modern JavaScript (Fetch API, DOM manipulation) |

---

## 🏁 Getting Started

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download) (or .NET 8 / 9 supported)
- [Visual Studio 2022 / 2025](https://visualstudio.microsoft.com/) or [VS Code](https://code.visualstudio.com/) with C# Dev Kit
- A [Google AI Studio API Key](https://aistudio.google.com/) for Gemini LLM features

### Installation

1. **Clone the repository:**
   ```bash
   git clone https://github.com/tarikthedev/MarketDataAnalytics.git
   cd MarketDataAnalytics
   ```

2. **Configure your API Key:**
   Configure your Gemini API key in `MarketDataApp.Presentation/appsettings.json`:
   ```json
   {
     "Gemini": {
       "ApiKey": "YOUR_ACTUAL_GEMINI_API_KEY",
       "Model": "gemini-2.5-flash"
     }
   }
   ```
   *Alternatively, pass it as an environment variable:*
   ```bash
   export GEMINI_API_KEY="YOUR_ACTUAL_GEMINI_API_KEY"
   ```

3. **Restore & Build:**
   ```bash
   dotnet restore
   dotnet build
   ```

4. **Run the application:**
   ```bash
   dotnet run --project MarketDataApp.Presentation
   ```

5. **Access the Terminal:**
   - **Interactive Web UI:** `http://localhost:5000/index.html` (or your assigned local port)
   - **Swagger UI API Docs:** `http://localhost:5000/swagger`

---

## 📡 API Endpoints

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/market/candles` | Fetch real-time / historical candlesticks via WebSocket |
| `GET` | `/api/cot/overview` | Institutional CFTC COT positioning breakdown & bias |
| `GET` | `/api/cot/history/{currency}` | Historical COT data points for specific currency/commodity |
| `GET` | `/api/cot/backtest` | Run historical backtesting simulation on COT signals |
| `GET` | `/api/squeeze` | Run multi-timeframe volatility squeeze scanner |
| `GET` | `/api/seasonality/{symbol}` | Monthly historical tendencies & seasonality patterns |
| `GET` | `/api/calendar/all` | High-impact macroeconomic calendar events |
| `GET` | `/api/aimacro/report/{symbol}` | Generate AI-powered macroeconomic synthesis report |

---

## 🔒 Security & Best Practices

- **Secret Management:** API keys are injected via `IConfiguration` and environment variables. Secrets are excluded from version control via `.gitignore`.
- **Fault-Tolerant External Calls:** All external data calls (Forex Factory, CFTC, Deriv, Gemini) are wrapped with defensive error handling and fallbacks to ensure uninterrupted operation.
- **Resource Protection:** External rates are shielded using `IMemoryCache` with configurable cache expiration windows.

---

## 👨‍💻 Author

**Tarık Kocaçiftçi**  
- Software Engineering Student @ Kocaeli University  
- Contact: tarikthedev@gmail.com  
- LinkedIn: [linkedin.com/in/tarikkocaciftci](https://linkedin.com) *(Update with your profile)*  

---

## 📄 License
This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
