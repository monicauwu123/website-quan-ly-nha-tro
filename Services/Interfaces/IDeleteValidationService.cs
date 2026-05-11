using DoAnSE104.Services;

namespace DoAnSE104.Services.Interfaces
{
    public interface IDeleteValidationService
    {
        Task<DeleteResult> DeleteNhaTroAsync(int id);
        Task<DeleteResult> DeletePhongAsync(int id);
        Task<DeleteResult> DeleteNguoiThueAsync(int id);
        Task<DeleteResult> DeleteHopDongAsync(int id);
        Task<DeleteResult> KetThucHopDongAsync(int id);
        Task<DeleteResult> HuyHopDongAsync(int id);
        Task<DeleteResult> DeleteHoaDonAsync(int id);
        Task<DeleteResult> DeleteThanhToanAsync(int id);
        Task<DeleteResult> DeleteLoaiPhongAsync(int id);
        Task<DeleteResult> DeleteDichVuAsync(int id);
        Task<DeleteResult> DeleteDangKyDichVuAsync(int id);
        Task<DeleteResult> DeleteYeuCauThueAsync(int id);
        Task<DeleteResult> DeleteYeuCauGiaHanAsync(int id);
        Task<DeleteResult> DeleteBaoCaoSuCoAsync(int id);
        Task<DeleteResult> DeleteChiSoDienAsync(int id);
        Task<DeleteResult> DeleteChiSoNuocAsync(int id);
    }
}
