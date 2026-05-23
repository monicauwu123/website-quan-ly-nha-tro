# Quan Ly Phong Tro Website

Ung dung web quan ly phong tro duoc xay dung bang ASP.NET Core 8, Entity Framework Core va SQL Server/PostgreSQL. Du an ho tro cac nghiep vu co ban trong quan ly nha tro nhu quan ly nha tro, phong, loai phong, khach thue, hop dong, hoa don, thanh toan, dich vu, chi so dien nuoc, thong bao va bao cao su co.

Tai lieu nay dung de cai dat va chay nhanh source code tren may moi, dac biet phu hop cho giang vien hoac nguoi cham do an can kiem tra he thong.

## Cong nghe su dung

- ASP.NET Core 8
- Entity Framework Core 8
- SQL Server LocalDB/Express/Developer hoac PostgreSQL
- JWT Authentication
- Swagger/OpenAPI
- HTML, CSS va JavaScript thuan o phia frontend

## Yeu cau he thong

Truoc khi chay du an, can cai dat:

- .NET 8 SDK
- SQL Server LocalDB, SQL Server Express, SQL Server Developer hoac mot PostgreSQL database online
- Git neu muon clone source code tu GitHub

Kiem tra phien ban .NET:

```cmd
dotnet --version
```

Neu ket qua tra ve dang `8.x.x` thi moi truong .NET da san sang.

## Cau truc thu muc chinh

```text
Controllers/       API controllers
Models/            Entity models va DTOs
Data/              DbContext va sample data seeder
Services/          Xu ly nghiep vu
Migrations/        EF Core migrations
wwwroot/           Giao dien HTML/CSS/JavaScript
appsettings.json   Cau hinh ung dung va database
Program.cs         Diem khoi dong ung dung
```

## Chay nhanh tren may moi

### 1. Tai source code

Tai hoac clone source code ve may, vi du:

```cmd
git clone https://github.com/monicauwu123/Quan-Ly-phong-tro-website.git
cd Quan-Ly-phong-tro-website
```

Neu da co san folder source code, mo CMD/PowerShell tai dung thu muc du an.

### 2. Cau hinh connection string

Mo file:

```text
appsettings.json
```

Tim phan:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=QuanLyPhongTro;Trusted_Connection=True;TrustServerCertificate=True"
}
```

Sua gia tri `Server=...` cho dung voi SQL Server dang chay tren may.

Neu dung SQL Server LocalDB:

```json
"DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=QuanLyPhongTro;Trusted_Connection=True;TrustServerCertificate=True"
```

Neu dung SQL Server Express:

```json
"DefaultConnection": "Server=.\\SQLEXPRESS;Database=QuanLyPhongTro;Trusted_Connection=True;TrustServerCertificate=True"
```

Neu dung SQL Server default instance:

```json
"DefaultConnection": "Server=localhost;Database=QuanLyPhongTro;Trusted_Connection=True;TrustServerCertificate=True"
```

Neu dung tai khoan SQL Server:

```json
"DefaultConnection": "Server=localhost;Database=QuanLyPhongTro;User Id=sa;Password=YOUR_PASSWORD;TrustServerCertificate=True"
```

### 3. Restore package va chay ung dung

```cmd
dotnet restore
dotnet run
```

Sau khi chay thanh cong, mo link hien trong terminal, vi du:

```text
http://localhost:5000
https://localhost:5001
```

Neu trinh duyet bao loi chung chi HTTPS local, co the dung link `http://...`.

## Chay bang cau hinh demo

Du an co san file `run-demo.ps1` de restore package va chay voi environment `Demo`:

```powershell
.\run-demo.ps1
```

Lenh nay tuong duong:

```powershell
dotnet restore
dotnet run --environment Demo
```

## Cau hinh database

Trong `appsettings.json`, phan quan trong:

```json
"Database": {
  "Provider": "SqlServer",
  "RecreateOnStartup": false,
  "SeedSampleData": true
}
```

Neu can cau hinh rieng cho may local, khong sua truc tiep `appsettings.json`. Hay copy file mau:

```cmd
copy appsettings.Local.example.json appsettings.Local.json
```

Sau do sua `ConnectionStrings:DefaultConnection` trong `appsettings.Local.json` theo SQL Server tren may dang chay. File `appsettings.Local.json` da duoc dua vao `.gitignore`, nen thay doi server name cua tung may se khong bi commit len GitHub.

### Provider

Dung SQL Server local:

```json
"Provider": "SqlServer"
```

Dung PostgreSQL online, vi du Neon hoac Render:

```json
"Provider": "PostgreSql"
```

Khi dung PostgreSQL, connection string se duoc lay tu bien moi truong `DATABASE_URL` hoac `ConnectionStrings__DefaultConnection` tren hosting.

### RecreateOnStartup

```json
"RecreateOnStartup": true
```

- `true`: moi lan chay ung dung se xoa database cu va tao lai database moi.
- `false`: giu database hien co, khong xoa du lieu cu.

Khuyen nghi khi chay demo lan dau:

```json
"RecreateOnStartup": true
```

Sau khi da co du lieu va muon giu lai:

```json
"RecreateOnStartup": false
```

### SeedSampleData

```json
"SeedSampleData": true
```

- `true`: tu dong them du lieu mau de demo ngay.
- `false`: chi tao database va tai khoan admin mac dinh.

## Cac cau hinh thuong dung

### Demo nhanh co du lieu mau

Phu hop khi chay du an lan dau:

```json
"Database": {
  "Provider": "SqlServer",
  "RecreateOnStartup": true,
  "SeedSampleData": true
}
```

### Giu lai du lieu sau khi test

Phu hop khi da thao tac va khong muon mat du lieu:

```json
"Database": {
  "Provider": "SqlServer",
  "RecreateOnStartup": false,
  "SeedSampleData": true
}
```

### Khoi tao database rong

Phu hop khi muon tu tao nha tro, phong va nguoi dung tu dau:

```json
"Database": {
  "Provider": "SqlServer",
  "RecreateOnStartup": true,
  "SeedSampleData": false
}
```

### Dung PostgreSQL online

Phu hop khi deploy len Render voi Neon PostgreSQL:

```json
"Database": {
  "Provider": "PostgreSql",
  "RecreateOnStartup": false,
  "SeedSampleData": true
}
```

Tren Render can khai bao cac bien moi truong:

```text
Database__Provider=PostgreSql
DATABASE_URL=postgresql://...
ASPNETCORE_ENVIRONMENT=Production
```

## Tai khoan demo

Neu bat `SeedSampleData = true`, co the dang nhap bang cac tai khoan sau:

```text
Admin
Ten dang nhap: Admin
Email: admin@demo.local
Mat khau: Admin123

Chu tro
Ten dang nhap: chutro
Mat khau: 123456

Nguoi thue
Ten dang nhap: nguoithue
Mat khau: 123456
```

Man hinh dang nhap cho phep nhap ten dang nhap hoac email.

## Loi thuong gap

### Khong ket noi duoc SQL Server

Kiem tra:

- Da cai SQL Server/LocalDB chua.
- Gia tri `Server=...` trong connection string co dung khong.
- SQL Server service co dang chay khong.

Kiem tra LocalDB:

```cmd
sqllocaldb info
```

### Port da duoc su dung

Tat ung dung cu dang chay hoac doi port trong:

```text
Properties/launchSettings.json
```

### Build loi do file exe/dll bi khoa

Loi nay thuong xay ra khi ung dung van dang chay. Hay tat terminal dang chay `dotnet run`, sau do chay lai:

```cmd
dotnet build
dotnet run
```

## Tom tat lenh chay nhanh

```cmd
dotnet restore
dotnet run
```

Neu gap loi database, sua lai `Server=...` trong `appsettings.json`, sau do chay lai:

```cmd
dotnet run
```

## Luu y khi deploy

- Khong nen dua mat khau database, JWT secret, SMTP password hoac API secret truc tiep len repository public.
- Nen cau hinh cac gia tri nhay cam bang bien moi truong tren hosting.
- Khi dung PostgreSQL tren Render/Neon, can dam bao `Database__Provider=PostgreSql` va `DATABASE_URL` da duoc khai bao dung.
