using System.ComponentModel.DataAnnotations;

public class ChiSoDienDtoUpdate
{
    [Required]
    public int MaDien { get; set; }

    [Required]
    public int MaPhong { get; set; }

    [Required]
    public int SoDienCu { get; set; }

    [Required]
    public int SoDienMoi { get; set; }

    [Required]
    [Range(0, double.MaxValue)]
    public decimal GiaDien { get; set; }

    [Required]
    public DateTime NgayThangDien { get; set; }
}

