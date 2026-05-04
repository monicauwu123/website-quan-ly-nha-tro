-- Bổ sung cột phục vụ reset/chốt kỳ thuê tháng.
-- Chạy script này nếu database hiện tại đã tồn tại và không muốn xóa/tạo lại database.

IF COL_LENGTH('DangKyDichVu', 'NgayHetHan') IS NULL
BEGIN
    ALTER TABLE DangKyDichVu ADD NgayHetHan datetime2 NULL;
END
GO

IF COL_LENGTH('DangKyDichVu', 'KyDangKy') IS NULL
BEGIN
    ALTER TABLE DangKyDichVu ADD KyDangKy nvarchar(7) NULL;
END
GO

IF COL_LENGTH('YeuCauThue', 'SoThangMuonThue') IS NULL
BEGIN
    ALTER TABLE YeuCauThue ADD SoThangMuonThue int NOT NULL CONSTRAINT DF_YeuCauThue_SoThangMuonThue DEFAULT(1);
END
GO

IF COL_LENGTH('YeuCauThue', 'NgayBatDauMongMuon') IS NULL
BEGIN
    ALTER TABLE YeuCauThue ADD NgayBatDauMongMuon datetime2 NULL;
END
GO
