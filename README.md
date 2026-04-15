<div align="center">

# 🗺️ VinhKhanh Audio Tour

### Hệ thống hướng dẫn âm thanh thông minh cho phố ẩm thực Vĩnh Khánh

[![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core-8.0-512BD4?style=for-the-badge&logo=dotnet)](https://dotnet.microsoft.com/)
[![.NET MAUI](https://img.shields.io/badge/.NET_MAUI-8.0-512BD4?style=for-the-badge&logo=dotnet)](https://dotnet.microsoft.com/apps/maui)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-336791?style=for-the-badge&logo=postgresql&logoColor=white)](https://www.postgresql.org/)
[![Docker](https://img.shields.io/badge/Docker-Enabled-2496ED?style=for-the-badge&logo=docker&logoColor=white)](https://www.docker.com/)
[![Render](https://img.shields.io/badge/Deployed_on-Render-46E3B7?style=for-the-badge&logo=render&logoColor=white)](https://render.com/)

</div>

---

## 📖 Giới Thiệu

**VinhKhanh Audio Tour** là ứng dụng hướng dẫn âm thanh tự động cho **phố ẩm thực Vĩnh Khánh, Quận 4, TP. Hồ Chí Minh** — một trong những con phố ẩm thực nổi tiếng nhất Sài Gòn.

Hệ thống sử dụng **GPS Geofencing** để phát hiện khi du khách đến gần các địa điểm ẩm thực và tự động phát thuyết minh bằng giọng nói theo ngôn ngữ của họ, hỗ trợ **12 ngôn ngữ** khác nhau.

---

## 🎯 Tính Năng Nổi Bật

| Tính năng | Mô tả |
|-----------|-------|
| 🎙️ **Audio Tự Động** | Tự động phát âm thanh thuyết minh khi đến gần POI (bán kính ~30m) |
| 🌏 **12 Ngôn Ngữ** | Tiếng Việt, Anh, Tây Ban Nha, Pháp, Đức, Trung, Nhật, Hàn, Nga, Ý, Bồ Đào Nha, Hindi |
| 🗺️ **Bản Đồ Tương Tác** | Hiển thị bản đồ với vị trí GPS thời gian thực |
| 🤖 **AI Dịch Thuật** | Tự động dịch mô tả POI sang 12 ngôn ngữ qua Google Translate |
| 🔊 **TTS Engine** | Tổng hợp giọng nói và lưu audio binary vào database |
| 📱 **QR Code** | Mỗi địa điểm có QR code riêng để du khách quét xem thông tin |
| 💼 **Cổng Chủ Quán** | Chủ quán tự đăng ký, đăng địa điểm sau khi mua gói dịch vụ |

---

## 🏗️ Kiến Trúc Hệ Thống

```
┌──────────────────────────────────────────────────────┐
│                  RENDER.COM (Cloud)                   │
│                                                       │
│  ┌─────────────────┐      ┌─────────────────────┐    │
│  │  VinhKhanhCMS   │ ◄───►│   VinhKhanhweb      │    │
│  │  (Backend API)  │      │  (Admin/Owner Panel) │    │
│  │  Port: dynamic  │      │  Port: dynamic       │    │
│  └────────┬────────┘      └─────────────────────┘    │
│           │                                           │
│  ┌────────▼────────┐                                  │
│  │   PostgreSQL    │                                  │
│  │   (Database)    │                                  │
│  └─────────────────┘                                  │
└──────────────────────────────────────────────────────┘
            ▲
            │ HTTPS REST API
            ▼
┌──────────────────────┐
│    VinhKhanhTour     │
│  (.NET MAUI Mobile)  │
│   Android / iOS      │
└──────────────────────┘
```

---

## 📦 Cấu Trúc Project

```
VinhKhanhweb/
├── 📁 VinhKhanhCMS/          # Backend REST API
│   ├── Controllers/           # Auth, POI, Translation, Unlock
│   ├── Models/                # AppUser, Poi, PoiTranslation, UserPoiUnlock
│   ├── Data/                  # AppDbContext (EF Core)
│   ├── Services/              # TtsService
│   ├── Dockerfile             # Docker build
│   └── Program.cs             # ASP.NET Core entry point
│
├── 📁 VinhKhanhweb/           # Admin/Owner Web Panel (MVC)
│   ├── Controllers/           # Admin, Owner, Auth, Poi, QRCode, Audio
│   ├── Views/                 # Razor Views
│   ├── Models/                # DTOs
│   ├── Dockerfile             # Docker build
│   └── Program.cs             # ASP.NET Core entry point
│
├── 📁 VinhKhanhTour/          # Mobile App (.NET MAUI)
│   ├── Pages/                 # MainPage, MapPage, SettingsPage
│   ├── Models/                # Poi (SQLite + Offline support)
│   ├── Services/              # ApiService, NarrationEngine, AudioService
│   ├── PageModels/            # MVVM ViewModels
│   └── MauiProgram.cs         # MAUI entry point
│
└── render.yaml                # Infrastructure as Code (Render.com)
```

---

## 🚀 Hướng Dẫn Cài Đặt & Chạy

### Yêu Cầu
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)
- [PostgreSQL 14+](https://www.postgresql.org/download/) (hoặc dùng Docker)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) với workload **.NET MAUI** (cho app mobile)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (để build image)

---

### 1️⃣ Chạy CMS API (Backend)

```bash
cd VinhKhanhCMS

# Tạo file appsettings.Development.json
# Điền connection string PostgreSQL của bạn

dotnet ef database update   # Chạy migrations

dotnet run                  # API chạy tại http://localhost:5137
# Swagger UI: http://localhost:5137/swagger
```

> **Lần đầu:** Gọi `POST /api/auth/seed-admin` với header `X-Setup-Key: vinhkhanh-setup-2026` để tạo tài khoản Admin mặc định.

---

### 2️⃣ Chạy Admin Web Panel

```bash
cd VinhKhanhweb

# Đảm bảo CMS API đang chạy tại localhost:5137

dotnet run                  # Web chạy tại http://localhost:7170
```

Đăng nhập với tài khoản Admin:
- **Username:** `admin`
- **Password:** `Admin@123`

---

### 3️⃣ Chạy Mobile App (MAUI)

Mở `VinhKhanhCMS.slnx` hoặc `VinhKhanhTour.slnx` trong **Visual Studio 2022**, chọn target là **Android Emulator** hoặc thiết bị thật, nhấn `F5` để build và chạy.

---

### 🐳 Chạy với Docker Compose (Khuyến nghị)

```bash
# Build và chạy toàn bộ hệ thống
docker-compose up --build
```

---

## ☁️ Triển Khai lên Render.com

File `render.yaml` tại root đã cấu hình sẵn Infrastructure as Code:

```yaml
# 3 services được deploy tự động:
# 1. vinhkhanh-cms      → CMS API
# 2. vinhkhanh-admin    → Admin Web
# 3. vinhkhanh-db       → PostgreSQL Database
```

**Sau khi deploy, cần set các biến môi trường trên Render Dashboard:**

| Service | Biến | Giá trị |
|---------|------|---------|
| CMS | `API_BASE_URL` | `https://vinhkhanh-cms.onrender.com` |
| Admin | `CMS_API_URL` | `https://vinhkhanh-cms.onrender.com` |
| Admin | `ADMIN_BASE_URL` | `https://vinhkhanh-admin.onrender.com` |

---

## 🌐 API Reference

### Authentication
```
POST   /api/auth/login                  Đăng nhập
POST   /api/auth/register-owner         Đăng ký tài khoản Owner
POST   /api/auth/seed-admin             Tạo Admin lần đầu (cần X-Setup-Key)
GET    /api/auth/users                  Danh sách users
PUT    /api/auth/users/{id}/approve     Duyệt/từ chối tài khoản
POST   /api/auth/users/{id}/subscribe   Gia hạn gói VIP
```

### Points of Interest
```
GET    /api/pois                        Lấy tất cả POI
GET    /api/pois/{id}                   Chi tiết POI
POST   /api/pois                        Tạo POI mới
PUT    /api/pois/{id}                   Cập nhật POI
DELETE /api/pois/{id}                   Xóa POI
PUT    /api/pois/{id}/approve           Duyệt/từ chối POI
POST   /api/pois/{id}/upload-image      Upload ảnh POI
```

### Translations & Audio
```
GET    /api/pois/{id}/translations              Lấy danh sách bản dịch
POST   /api/pois/{id}/translations/generate     Tự động tạo 12 ngôn ngữ + audio
POST   /api/pois/{id}/translations/{lang}/upload-audio  Upload audio thủ công
GET    /api/pois/audio/translation/{id}         Stream file audio
```

### Unlock (Thanh Toán Demo)
```
GET    /api/unlock/check        Kiểm tra quyền truy cập
POST   /api/unlock/mock-pay     Giả lập thanh toán (demo)
```

---

## 🌍 Ngôn Ngữ Hỗ Trợ

| Mã | Ngôn ngữ | | Mã | Ngôn ngữ |
|----|----------|-|----|----------|
| `vi` | 🇻🇳 Tiếng Việt | | `zh` | 🇨🇳 Tiếng Trung |
| `en` | 🇬🇧 Tiếng Anh | | `ja` | 🇯🇵 Tiếng Nhật |
| `es` | 🇪🇸 Tiếng Tây Ban Nha | | `ko` | 🇰🇷 Tiếng Hàn |
| `fr` | 🇫🇷 Tiếng Pháp | | `ru` | 🇷🇺 Tiếng Nga |
| `de` | 🇩🇪 Tiếng Đức | | `it` | 🇮🇹 Tiếng Ý |
| `pt` | 🇵🇹 Tiếng Bồ Đào Nha | | `hi` | 🇮🇳 Tiếng Hindi |

---

## 🛠️ Công Nghệ Sử Dụng

| Layer | Công nghệ |
|-------|-----------|
| **Backend API** | ASP.NET Core 8 Web API |
| **Admin Web** | ASP.NET Core 8 MVC + Razor Views |
| **Mobile App** | .NET MAUI 8 (Android/iOS) |
| **Database (Server)** | PostgreSQL 16 + Entity Framework Core |
| **Database (Mobile)** | SQLite + sqlite-net-pcl |
| **Authentication** | BCrypt.Net + Session Cookie |
| **Text-to-Speech** | Google Cloud TTS |
| **Translation** | Google Translate API |
| **Audio Player** | Plugin.Maui.Audio |
| **Maps** | .NET MAUI Maps |
| **MVVM** | CommunityToolkit.Mvvm |
| **Containerization** | Docker |
| **Cloud Deploy** | Render.com |

---

## 📸 Màn Hình Demo

### 🗺️ Bản Đồ + Geofencing (Mobile)
Ứng dụng hiển thị vị trí GPS thời gian thực và tự động phát audio khi đến gần POI.

### 🖥️ Trang Quản Trị (Web)
Admin quản lý toàn bộ hệ thống: duyệt quán, tạo audio, quản lý gói VIP.

---

## 🔐 Phân Quyền Hệ Thống

```
Admin
  ├── Quản lý tất cả POI (CRUD)
  ├── Duyệt / từ chối tài khoản Owner
  ├── Duyệt / từ chối POI
  ├── Bật/tắt hiển thị POI
  └── Tạo / xóa audio thuyết minh

Owner (Chủ quán)
  ├── Mua gói VIP (theo tháng)
  ├── Đăng / sửa / xóa POI của mình
  ├── Upload ảnh địa điểm
  ├── Upload audio hoặc tạo tự động
  └── Xem thống kê lượt xem

Khách du lịch (App)
  ├── Xem danh sách địa điểm
  ├── Nghe thuyết minh tự động (GPS)
  └── Chọn ngôn ngữ yêu thích
```

---

## 👨‍💻 Thành Viên Thực Hiện

<table>
  <tr>
    <td align="center">
      <b>Nguyễn Xuân Minh</b><br/>
      <sub>Backend API · Mobile App · Database · Deploy</sub>
    </td>
    <td align="center">
      <b>Trương Vũ Hoàng Lộc</b><br/>
      <sub>Admin Web · UI/UX · Authentication · Business Logic</sub>
    </td>
  </tr>
</table>

---

## 📄 Giấy Phép

Dự án được thực hiện cho mục đích học thuật.

---

<div align="center">

**🍜 Phố Ẩm Thực Vĩnh Khánh — Quận 4, TP. Hồ Chí Minh 🍜**

*"Thưởng thức ẩm thực với công nghệ — Taste food with technology"*

</div>
