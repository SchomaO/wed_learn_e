using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using wed_learn_e.Models;

namespace wed_learn_e.Models
{
    public class CapDoStoreItem
    {
        public int IdCapDo { get; set; }
        public string TenCapDo { get; set; }
        public string MoTa { get; set; }

        public decimal GiaGoc { get; set; }
        public bool IsMienPhiTheoGioiHan { get; set; }
        public int PhanTramGiam { get; set; }
        public decimal SoTienGiam { get; set; }
        public decimal GiaSauGiam { get; set; }

        public bool DaSoHuu { get; set; }
        public DateTime? NgayMua { get; set; }
    }

    public class DanhSachCapDoViewModel
    {
        public bool DaDangNhap { get; set; }
        public bool DaNangCap { get; set; }
        public int PhanTramGiamVip { get; set; }
        public decimal GiaGoiNangCap { get; set; }
        public int SoCapDoMienPhi { get; set; }
        public List<CapDoStoreItem> DanhSachCapDo { get; set; } = new List<CapDoStoreItem>();
    }

    public class DonHangCapDoViewModel
    {
        public string MaDonHang { get; set; }
        public int IdCapDo { get; set; }
        public string TenCapDo { get; set; }
        public decimal GiaGoc { get; set; }
        public int PhanTramGiam { get; set; }
        public decimal SoTienGiam { get; set; }
        public decimal ThanhTien { get; set; }
        public string TrangThai { get; set; }
        public DateTime NgayTao { get; set; }

        public string TenNganHang { get; set; }
        public string SoTaiKhoan { get; set; }
        public string ChuTaiKhoan { get; set; }
        public string LinkQrCode { get; set; }
    }
}