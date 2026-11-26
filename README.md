# 📈 MarketData Analytics – Yapay Zeka Destekli Finansal Analiz & Makro Terminali

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-13.0-239120?logo=c-sharp&logoColor=white)](https://learn.microsoft.com/tr-tr/dotnet/csharp/)
[![Mimari](https://img.shields.io/badge/Mimari-N--Tier%20(Çok%20Katmanlı)-blue)](https://learn.microsoft.com/tr-tr/dotnet/architecture/modern-web-apps-azure/common-web-application-architectures)
[![Yapay Zeka](https://img.shields.io/badge/Yapay%20Zeka-Google%20Gemini-4285F4?logo=google&logoColor=white)](https://ai.google.dev/)
[![Lisans](https://img.shields.io/badge/Lisans-MIT-green.svg)](LICENSE)

> Kurumsal düzeyde finansal veri analizi, canlı piyasa akışı ve yapay zeka tabanlı makroekonomik piyasa özetleri sunan; **ASP.NET Core Web API**, **Çok Katmanlı Mimari (N-Tier)**, **WebSockets** ve **Google Gemini LLM** teknolojileriyle geliştirilmiş kapsamlı bir finansal analiz platformudur.

---

## 📑 İçindekiler
- [Proje Hakkında](#-proje-hakkında)
- [Öne Çıkan Özellikler](#-öne-çıkan-özellikler)
- [Mimari Yapı (N-Tier)](#-mimari-yapı-n-tier)
- [Teknoloji Yığını](#-teknoloji-yığını)
- [Kurulum ve Çalıştırma](#-kurulum-ve-çalıştırma)
- [Konfigürasyon ve Güvenlik](#-konfigürasyon-ve-güvenlik)
- [API Uç Noktaları (Endpoints)](#-api-uç-noktaları-endpoints)
- [Geliştirici](#-geliştirici)

---

## 🚀 Proje Hakkında

**MarketData Analytics**, finansal piyasalardaki teknik ve temel verileri tek bir merkezde toplayıp kurumsal düzeyde analiz eden bir platformdur. 

Kurumsal yazılım stajı sırasında temelleri atılan **3/4 Katmanlı Mimari (N-Tier)** prensipleri üzerine inşa edilmiştir. Platform; CFTC kurumsal pozisyonlanma raporlarını (COT), Forex Factory yüksek etkili ekonomik haber beslemelerini ve Deriv üzerinden gerçek zamanlı mum verilerini toplayarak algoritmik analizlerden geçirir ve Google Gemini yapay zekası ile kapsamlı makroekonomik piyasa raporları üretir.

---

## ✨ Öne Çıkan Özellikler

- 🏛️ **Çok Katmanlı Mimari (N-Tier):** `Core`, `Business`, `DataAccess` ve `Presentation` katmanlarıyla gevşek bağlı (loosely-coupled), SOLID prensiplerine ve Dependency Injection (DI) yapısına uygun sürdürülebilir mimari.
- ⚡ **Gerçek Zamanlı Piyasa Akışı (WebSockets):** `System.Net.WebSockets.ClientWebSocket` altyapısıyla Deriv API'ye bağlanarak düşük gecikmeli canlı mum verisi ve geçmiş veri akışı.
- 📊 **Kurumsal Pozisyonlanma Analizi (CFTC COT):**
  - ABD Emtia Vadeli İşlemler Komisyonu (CFTC) büyük spekülatör ve fon net pozisyonlanma verilerinin takibi.
  - 6 aylık ve 1 yıllık normalize edilmiş COT endeks skorları ve kurumsal yön (bias) hesaplaması.
  - Geçmişe dönük COT sinyal simülasyonu ve **Backtesting** motoru.
- 🎯 **Çoklu Zaman Dilimli Volatilite Sıkışma Tarayıcısı (Squeeze Scanner):**
  - Bollinger Bantları ve Keltner Kanalları arasındaki sıkışmayı (Compression) 1 dakikadan 1 güne kadar farklı zaman dilimlerinde (Multi-timeframe) tarayan algoritma.
  - Olası patlama/kırılım noktalarını ve bar bazlı sıkışma sürelerini tespit edebilme.
- 📅 **Haftalık Ekonomik Takvim Entegrasyonu:**
  - Forex Factory XML beslemesi üzerinden haftalık yüksek etkili ekonomik takvim verilerinin otomatik olarak çekilmesi ve filtrelenmesi.
- 🤖 **Yapay Zeka Destekli Makro Analist (Google Gemini Entegrasyonu):**
  - Teknik sıkışma (Squeeze) verilerini, COT kurumsal sentiment'ini ve ekonomik takvim katalizörlerini birleştirerek kurumsal dilde otomatik makro analiz raporu üreten yapay zeka ajanı.
  - Yatırım tavsiyesi içermeyen, risk uyarı kurallarına sadık kalan özel prompt mühendisliği.
- ⚡ **Yüksek Başarımlı Bellek İçi Önbellekleme (IMemoryCache):**
  - Harici veri kaynaklarına yapılan istekleri ve sunucu maliyetlerini düşüren, yanıt sürelerini milisaniyelere indiren önbellek katmanı.

---

## 🏗️ Mimari Yapı (N-Tier)

Proje, kurumsal yazılım standartlarına uygun şekilde 4 ana katmana ayrılmıştır:

```
MarketDataAnalytics/
├── MarketDataApp.Core/              # Varlıklar, DTO'lar ve Servis Arayüzleri
│   ├── Entities/                    # CandleStick, Temel model yapıları
│   ├── DTOs/                        # AiMacroModels, CotAnalysisDto, BacktestResultDto
│   └── Interfaces/                  # ICotService, IMarketService, IMarketApiClient
│
├── MarketDataApp.Business/          # İş mantığı, algoritmik tarama ve yapay zeka servisleri
│   └── Services/
│       ├── AiMacroService.cs        # Gemini LLM sentezi ve ekonomik takvim servisi
│       ├── CotService.cs            # CFTC pozisyonlanma analizi ve backtest motoru
│       ├── SqueezeService.cs        # Çoklu zaman dilimli volatilite sıkışma algoritması
│       ├── SeasonalityService.cs    # Geçmiş mevsimsellik ve döngü analizleri
│       └── MarketService.cs         # Piyasa veri koordinasyon servisi
│
├── MarketDataApp.DataAccess/        # Dış kaynaklar, API istemcileri ve WebSockets
│   └── Clients/
│       └── DerivApiClient.cs        # Asenkron WebSocket canlı piyasa istemcisi
│
└── MarketDataApp.Presentation/      # ASP.NET Core Web API ve Web Kullanıcı Arayüzü
    ├── Controllers/                 # RESTful API Controller sınıfları (Swagger entegreli)
    └── wwwroot/                     # HTML5, CSS3 ve JavaScript dinamik gösterge panelleri
```

---

## 🛠️ Teknoloji Yığını

| Katman / Alan | Teknoloji | Açıklama |
|---|---|---|
| **Platform & Dil** | C# 13, .NET 10 | Modern, asenkron ve yüksek performanslı backend |
| **Web API Çerçevesi** | ASP.NET Core Web API | RESTful mimari, Dependency Injection |
| **Ağ & Canlı İletişim** | `System.Net.WebSockets` | Düşük gecikmeli iki yönlü soket veri akışı |
| **Yapay Zeka (LLM)** | Google Gemini API | Makroekonomik piyasa sentezi ve raporlama |
| **Önbellek & Performans** | In-Memory Caching (`IMemoryCache`) | Harici API sınırlarını koruma ve hızlı yanıt |
| **Veri Ayrıştırma** | `System.Text.Json`, `System.Xml.Linq` | JSON ve XML akışlarının işlenmesi |
| **API Dokümantasyonu** | Swagger / OpenAPI (Swashbuckle) | İnteraktif API test ve dokümantasyon arayüzü |
| **Kullanıcı Arayüzü** | HTML5, CSS3, Modern JavaScript | Responsive finansal gösterge ve analiz panelleri |

---

## 🏁 Kurulum ve Çalıştırma

### Gereksinimler
- [.NET 10 SDK](https://dotnet.microsoft.com/download) (veya .NET 8 / 9)
- [Visual Studio 2022/2025](https://visualstudio.microsoft.com/) veya [VS Code](https://code.visualstudio.com/)
- [Google AI Studio](https://aistudio.google.com/) üzerinden ücretsiz alınabilen Gemini API Anahtarı

### Adım Adım Kurulum

1. **Projeyi Klonlayın:**
   ```bash
   git clone https://github.com/tarikthedev/MarketDataAnalytics.git
   cd MarketDataAnalytics
   ```

2. **API Anahtarını Yapılandırın:**
   `MarketDataApp.Presentation/appsettings.json` dosyasını açıp Gemini API anahtarınızı ekleyin:
   ```json
   {
     "Gemini": {
       "ApiKey": "BURAYA_GEMINI_API_ANAHTARINIZI_YAZIN",
       "Model": "gemini-2.5-flash"
     }
   }
   ```
   *(Alternatif olarak işletim sistemi ortam değişkeni olarak da tanımlayabilirsiniz: `set GEMINI_API_KEY="ANAHTARINIZ"`)*

3. **Bağımlılıkları Yükleyin ve Derleyin:**
   ```bash
   dotnet restore
   dotnet build
   ```

4. **Projeyi Başlatın:**
   ```bash
   dotnet run --project MarketDataApp.Presentation
   ```

5. **Arayüze Erişin:**
   - **Web Terminali:** `http://localhost:5000/index.html` (veya terminalde belirtilen port)
   - **Swagger API Dokümantasyonu:** `http://localhost:5000/swagger`

---

## 📡 API Uç Noktaları (Endpoints)

| Metot | Uç Nokta | Açıklama |
|---|---|---|
| `GET` | `/api/market/candles` | WebSocket üzerinden canlı veya geçmiş mum verilerini getirir |
| `GET` | `/api/cot/overview` | Kurumsal CFTC COT pozisyonlanma ve bias verilerini listeler |
| `GET` | `/api/cot/history/{currency}` | Belirli bir para biriminin geçmiş COT verilerini getirir |
| `GET` | `/api/cot/backtest` | COT stratejisi üzerinde geçmişe dönük simülasyon çalıştırır |
| `GET` | `/api/squeeze` | Çoklu zaman diliminde volatilite sıkışması olan piyasaları tarar |
| `GET` | `/api/seasonality/{symbol}` | Paritenin aylık döngüsel mevsimsellik verilerini döner |
| `GET` | `/api/calendar/all` | Bu haftanın yüksek etkili ekonomik haberlerini listeler |
| `GET` | `/api/aimacro/report/{symbol}` | Yapay zeka destekli makroekonomik analiz raporu oluşturur |

---

## 🔒 Güvenlik & Kurumsal Pratikler

* **Gizlilik:** Özel API anahtarları kaynak koduna asla gömülmez; `IConfiguration` ve `.gitignore` ile korunan yerel dosyalardan okunur.
* **Hata Toleransı:** Harici API çağrıları (Forex Factory, CFTC, Deriv, Gemini) try-catch ve fallback mekanizmalarıyla korunarak servis sürekliliği sağlanmıştır.
* **Kaynak Optimizasyonu:** Dış kaynak istekleri `IMemoryCache` üzerinde tutularak gereksiz ağ trafiği ve kota aşımı engellenmiştir.

---

## 👨‍💻 Geliştirici

**Tarık Kocaçiftçi**  
* Kocaeli Üniversitesi – Yazılım Mühendisliği  
* **E-posta:** [tarikthedev@gmail.com](mailto:tarikthedev@gmail.com)  
* **GitHub:** [github.com/tarikthedev](https://github.com/tarikthedev)
