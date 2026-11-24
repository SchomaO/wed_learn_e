using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace wed_learn_e.Models
{
    public class QuanLyTaiChinhViewModel
    {
        public decimal TongDoanhThu { get; set; }
        public int SoDonChoDuyet { get; set; }
        public int TongTaiKhoanVip { get; set; }
        public int SoCapDoMienPhi { get; set; }
        public bool CoDonDangCho => SoDonChoDuyet > 0;

        public GoiNangCapAdminItem GoiNangCap { get; set; }
        public List<GiaoDichAdminItem> DanhSachGiaoDich { get; set; } = new List<GiaoDichAdminItem>();
        public List<GiaCapDoAdminItem> DanhSachGiaCapDo { get; set; } = new List<GiaCapDoAdminItem>();
        public List<TaiKhoanVipAdminItem> DanhSachTaiKhoanVip { get; set; } = new List<TaiKhoanVipAdminItem>();
    }

    public class GoiNangCapAdminItem
    {
        public string TenGoi { get; set; }
        public decimal GiaNangCap { get; set; }
        public int PhanTramGiam { get; set; }
        public string MoTa { get; set; }
        public DateTime NgayCapNhat { get; set; }
    }

    public class GiaoDichAdminItem
    {
        public string MaDonHang { get; set; }
        public string HoVaTen { get; set; }
        public string TenDangNhap { get; set; }
        public string LoaiGiaoDich { get; set; }
        public string TenDichVu { get; set; }
        public decimal GiaGoc { get; set; }
        public decimal SoTienGiam { get; set; }
        public decimal ThanhTien { get; set; }
        public string TrangThai { get; set; }
        public DateTime NgayTao { get; set; }
        public DateTime? NgayThanhToan { get; set; }
    }

    public class GiaCapDoAdminItem
    {
        public int IdCapDo { get; set; }
        public string TenCapDo { get; set; }
        public decimal GiaTien { get; set; }
        public bool IsMienPhiTheoGioiHan { get; set; }
        public bool DangCoDonCho { get; set; }
        public DateTime? NgayCapNhat { get; set; }
    }

    public class TaiKhoanVipAdminItem
    {
        public int IdNguoiDung { get; set; }
        public string HoVaTen { get; set; }
        public string TenDangNhap { get; set; }
        public string Email { get; set; }
        public DateTime NgayNangCap { get; set; }
        public int SoCapDoDaMua { get; set; }
    }
}