# Huong Dan Chay Va Demo Do An

Tai lieu nay dung de giao vien hoac nguoi cham do an co the chay nhanh source code tren may moi.

## 1. Yeu Cau Cai Dat

- .NET 8 SDK
- SQL Server LocalDB
  - Neu da cai Visual Studio ban day du thi thuong da co san LocalDB.
  - Neu khong co LocalDB, co the dung SQL Server Express/Developer va sua connection string.

Kiem tra .NET:

```powershell
dotnet --version
```

## 2. Cach Chay Nhanh Nhat

Mo PowerShell tai thu muc source code, sau do chay:

```powershell
dotnet restore
dotnet run --environment Demo
```

Hoac chay file co san:

```powershell
.\run-demo.ps1
```

Sau khi ung dung chay, mo URL hien trong terminal, vi du:

```text
https://localhost:5001
http://localhost:5000
```

Neu trinh duyet bao loi chung chi HTTPS local, co the chon tiep tuc truy cap hoac dung URL `http://...`.

## 3. Cau Hinh Demo

File cau hinh demo nam tai:

```text
appsettings.Demo.json
```

Noi dung quan trong:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=QuanLyPhongTro_Demo;Trusted_Connection=True;TrustServerCertificate=True"
  },
  "Database": {
    "RecreateOnStartup": true,
    "SeedSampleData": true
  }
}
```

## 4. Option Tao Lai Database

Option:

```json
"RecreateOnStartup": true
```

Y nghia:

- `true`: moi lan chay app se xoa database cu va tao lai database moi.
- `false`: giu database hien co, khong xoa du lieu cu.

Khuyen nghi khi nop demo:

```json
"RecreateOnStartup": true
```

giao vien chay len se luon co database sach, tranh loi do du lieu cu hoac schema cu.

Khi da demo xong va muon giu du lieu da thao tac, doi thanh:

```json
"RecreateOnStartup": false
```

## 5. Option Du Lieu Mau

Option:

```json
"SeedSampleData": true
```

Y nghia:

Tu them du lieu mau de demo ngay, gom nha tro, phong, khach thue, hop dong, hoa don, bao cao su co...
- `false`: chi tao database va tai khoan admin mac dinh, khong them du lieu mau.


## 6. Cac Che Do Chay Thuong Dung

### Che do 1: Demo sach moi lan chay

Phu hop de nop bai:

```json
"Database": {
  "RecreateOnStartup": true,
  "SeedSampleData": true
}
```

Ket qua:

- Xoa DB cu neu co.
- Tao DB moi.
- Them du lieu mau.
- Co the dang nhap va demo ngay.

### Che do 2: Giu lai du lieu sau khi thao tac

Phu hop khi muon test lau dai:

```json
"Database": {
  "RecreateOnStartup": false,
  "SeedSampleData": true
}
```

Ket qua:

- Khong xoa DB cu.
- Du lieu da thao tac van con.
- Du lieu mau chi duoc seed khi database dang trong theo dieu kien cua source.

### Che do 3: Database rong

Phu hop khi muon tu tao du lieu tu dau:

```json
"Database": {
  "RecreateOnStartup": true,
  "SeedSampleData": false
}
```

Ket qua:

- Tao database moi.
- Chi co tai khoan Admin mac dinh.
- Khong co phong, nha tro, hoa don mau.

## 7. Tai Khoan Demo

Khi `SeedSampleData = true`, co the dung cac tai khoan sau:

```text
Admin
Ten dang nhap: Admin
Mat khau: Admin123

Chu tro
Ten dang nhap: chutro
Mat khau: 123456

Nguoi thue
Ten dang nhap: nguoithue
Mat khau: 123456
```

Mat khau trong database duoc luu bang BCrypt hash, khong luu plain text.

## 8. Neu May Khong Co LocalDB

Sua connection string trong `appsettings.Demo.json`.

Vi du SQL Server Express:

```json
"DefaultConnection": "Server=.\\SQLEXPRESS;Database=QuanLyPhongTro_Demo;Trusted_Connection=True;TrustServerCertificate=True"
```

Vi du SQL Server local default instance:

```json
"DefaultConnection": "Server=localhost;Database=QuanLyPhongTro_Demo;Trusted_Connection=True;TrustServerCertificate=True"
```

Neu dung SQL authentication:

```json
"DefaultConnection": "Server=localhost;Database=QuanLyPhongTro_Demo;User Id=sa;Password=YOUR_PASSWORD;TrustServerCertificate=True"
```

## 9. Loi Thuong Gap

### Loi khong ket noi duoc SQL Server

Kiem tra:

- May da cai SQL Server/LocalDB chua.
- Connection string co dung instance name khong.
- SQL Server service co dang chay khong.

Co the kiem tra LocalDB:

```powershell
sqllocaldb info
```

### Loi port da duoc su dung

Tat ung dung dang chay cu hoac doi port trong `Properties/launchSettings.json`.

### Loi file exe/dll bi khoa khi build

Thuong do ung dung dang chay. Tat terminal dang `dotnet run`, roi build lai:

```powershell
dotnet build
```


