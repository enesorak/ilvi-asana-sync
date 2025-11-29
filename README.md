# Asana Sync v2

Asana verilerini SQL Server'a senkronize eden modern, güvenilir bir .NET 9 uygulaması.

## 🚀 Özellikler

- ✅ **Tam Senkronizasyon**: Users, Workspaces, Projects, Tasks, Stories, Attachments
- ✅ **Rate Limiting**: Asana API limitlerini otomatik yönetir (1400 req/min)
- ✅ **Bulk Operations**: EF Core Bulk Extensions ile hızlı veri yazımı
- ✅ **Attachment Download**: Orijinal + Thumbnail oluşturma
- ✅ **Scheduled Jobs**: Hangfire ile zamanlanmış senkronizasyon
- ✅ **Modern UI**: Tailwind CSS + Alpine.js dashboard
- ✅ **API Documentation**: Swagger/OpenAPI

## 📋 Gereksinimler

- .NET 9 SDK
- SQL Server 2019+
- Asana Personal Access Token

## 🛠️ Kurulum

### 1. Repository'yi klonla

```bash
git clone https://github.com/your-repo/Ilvi.Asana.Sync.git
cd Ilvi.Asana.Sync
```

### 2. Asana Token'ı al

1. [Asana Developer Console](https://app.asana.com/0/developer-console) adresine git
2. "Personal Access Tokens" bölümünden yeni token oluştur
3. Token'ı kopyala

### 3. Konfigürasyon

`src/Ilvi.Asana.Web/appsettings.json` dosyasını düzenle:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=AsanaSync;User Id=sa;Password=YOUR_PASSWORD;TrustServerCertificate=True;",
    "HangfireConnection": "Server=localhost;Database=AsanaSync;User Id=sa;Password=YOUR_PASSWORD;TrustServerCertificate=True;"
  },
  "Asana": {
    "PersonalAccessToken": "YOUR_ASANA_PERSONAL_ACCESS_TOKEN"
  }
}
```

### 4. Veritabanı oluştur

```bash
cd src/Ilvi.Asana.Web
dotnet ef database update
```

### 5. Çalıştır

```bash
dotnet run
```

Uygulama şu adreslerde çalışacak:
- **Dashboard**: http://localhost:5000
- **Hangfire**: http://localhost:5000/hangfire
- **Swagger**: http://localhost:5000/swagger

## 📊 Veritabanı Şeması

```
Users
├── Id (Asana GID)
├── Name
├── Email
└── JsonData

Workspaces
├── Id
├── Name
└── IsOrganization

Projects
├── Id
├── WorkspaceId (FK)
├── Name
├── Archived
├── Color
└── JsonData

Tasks
├── Id
├── ProjectId (FK)
├── AssigneeId (FK)
├── Name
├── Notes
├── Completed
├── DueOn
├── CustomFieldsJson
└── JsonData

TaskDependencies
├── TaskId (FK)
└── DependsOnTaskId (FK)

Attachments
├── Id
├── TaskId (FK)
├── Name
├── LocalPath
├── ThumbnailPath
└── IsDownloaded

Stories
├── Id
├── TaskId (FK)
├── Type
├── Text
└── CreatedById (FK)

SyncConfiguration
├── CronExpression
├── IsEnabled
├── DownloadAttachments
└── GenerateThumbnails

SyncLogs
├── StartedAt
├── CompletedAt
├── Status
├── UsersCount, ProjectsCount, TasksCount, etc.
└── ErrorMessage
```

## 🔧 API Endpoints

### Sync

| Method | Endpoint | Açıklama |
|--------|----------|----------|
| POST | `/api/sync/start` | Manuel sync başlatır |
| POST | `/api/sync/cancel` | Çalışan sync'i iptal eder |
| GET | `/api/sync/status` | Mevcut durumu döndürür |
| GET | `/api/sync/stats` | Veritabanı istatistikleri |
| GET | `/api/sync/logs` | Son sync logları |

### Configuration

| Method | Endpoint | Açıklama |
|--------|----------|----------|
| GET | `/api/configuration` | Ayarları getirir |
| PUT | `/api/configuration` | Ayarları günceller |
| GET | `/api/configuration/cron-presets` | Hazır cron seçenekleri |

## ⚙️ Cron Expression Örnekleri

| Expression | Açıklama |
|------------|----------|
| `0 * * * *` | Her saat başı |
| `0 */3 * * *` | Her 3 saatte bir |
| `0 0 * * *` | Her gün gece yarısı |
| `0 6,18 * * *` | Her gün 06:00 ve 18:00 |
| `0 0 * * 1` | Her Pazartesi gece yarısı |

## 🐳 Docker (Opsiyonel)

```yaml
# docker-compose.yml
version: '3.8'
services:
  app:
    build: .
    ports:
      - "5000:8080"
    environment:
      - ConnectionStrings__DefaultConnection=Server=db;Database=AsanaSync;User Id=sa;Password=YourPassword123!;TrustServerCertificate=True;
      - Asana__PersonalAccessToken=${ASANA_TOKEN}
    depends_on:
      - db
    volumes:
      - ./attachments:/app/attachments

  db:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      - ACCEPT_EULA=Y
      - SA_PASSWORD=YourPassword123!
    ports:
      - "1433:1433"
    volumes:
      - sqldata:/var/opt/mssql

volumes:
  sqldata:
```

## 🔒 Güvenlik Notları

- Asana token'ını environment variable olarak saklayın
- Production'da Hangfire dashboard'a authentication ekleyin
- Connection string'leri güvenli bir şekilde yönetin

## 📝 Lisans

MIT

## 🤝 Katkıda Bulunma

Pull request'ler memnuniyetle karşılanır!
