using System.ComponentModel.DataAnnotations;

public class ChiSoNuocDtoUpdate
{
    [Required]
    public int MaNguoiThue { get; set; }
    [Required]
    public int MaNuoc { get; set; }

    [Required]
    public int MaPhong { get; set; }

    [Required]
    public int SoNuocCu { get; set; }

    [Required]
    public int SoNuocMoi { get; set; }

    [Required]
    [Range(0, double.MaxValue)]
    public decimal GiaNuoc { get; set; }

    [Required]
    public DateTime NgayThangNuoc { get; set; }
}

