# Bankout CMS

Hệ thống quản trị Bank-out với .NET 8 API, Angular 19, SQL Server và ASP.NET Identity.

## Cấu trúc dự án

```
Bankout/
├── src/Bankout.API/     # Backend .NET 8 Web API
├── client/              # Frontend Angular 19
└── Bankout.sln
```

## Yêu cầu

- .NET 8 SDK
- Node.js 18+
- SQL Server hoặc LocalDB

## Cấu hình Database

Chỉnh connection string trong `src/Bankout.API/appsettings.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=BankoutDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
}
```

Database sẽ tự động migrate và seed khi chạy API lần đầu.

## Chạy Backend

```bash
cd src/Bankout.API
dotnet run
```

API chạy tại: http://localhost:5000

## Chạy Frontend

```bash
cd client
npm install
npm start
```

Ứng dụng chạy tại: http://localhost:4200

## Tài khoản mặc định

| Username | Password  | Role  |
|----------|-----------|-------|
| admin    | Admin@123 | ADMIN |
| staff    | Staff@123 | STAFF |

## Chức năng

### Đăng nhập / Đăng xuất
- ASP.NET Identity + JWT Bearer
- Lockout sau 5 lần đăng nhập sai
- Mật khẩu yêu cầu: 8+ ký tự, hoa/thường/số/ký tự đặc biệt

### Dashboard
- Hiển thị Balance và Tổng Giao Dịch
- ADMIN có thể cộng/trừ Balance (ghi BalanceHistory)

### Bank-out
- Form nhập yêu cầu (validate, convert tên TK viết hoa không dấu)
- Nút Import Excel (placeholder)
- Lịch sử với filter, pagination
- Action Duyệt/Hủy

### Agent (ADMIN only)
- Thêm, sửa, xóa Agent

## API Endpoints

| Method | Endpoint | Mô tả |
|--------|----------|-------|
| POST | /api/auth/login | Đăng nhập |
| POST | /api/auth/logout | Đăng xuất |
| GET | /api/auth/me | Thông tin user |
| GET | /api/dashboard | Dashboard data |
| POST | /api/dashboard/balance/add | Cộng balance (ADMIN) |
| POST | /api/dashboard/balance/subtract | Trừ balance (ADMIN) |
| GET/POST | /api/bankout | Danh sách / Tạo mới |
| POST | /api/bankout/{id}/approve | Duyệt |
| POST | /api/bankout/{id}/cancel | Hủy |
| GET | /api/bankout/agents | Dropdown agents |
| CRUD | /api/agents | Quản lý Agent (ADMIN) |

## Database Schema

- **Balance** - Số dư hệ thống
- **BalanceHistory** - Lịch sử thay đổi balance
- **BankoutRequest** - Yêu cầu bank-out
- **Agent** - Đối tác
- **AspNetUsers/Roles** - Identity tables
