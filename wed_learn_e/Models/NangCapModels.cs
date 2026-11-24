using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace wed_learn_e.Models
{
    public class GoiNangCap
    {
        public int IdGoi { get; set; }
        public string TenGoi { get; set; }
        public decimal GiaNangCap { get; set; }
        public int PhanTramGiamKhoaHoc { get; set; }
        public string MoTa { get; set; }
    }

    public class TrangNangCapViewModel
    {
        public GoiNangCap Goi { get; set; }
        public bool DaNangCap { get; set; }
        public DateTime? NgayNangCap { get; set; }
    }

    public class DonHangThanhToanViewModel
    {
        public string MaDonHang { get; set; }
        public string TenGoi { get; set; }
        public int PhanTramGiamKhoaHoc { get; set; }
        public decimal ThanhTien { get; set; }
        public string TrangThai { get; set; }
        public DateTime NgayTao { get; set; }

        // Thông tin tài khoản ngân hàng nhận tiền
        public string TenNganHang { get; set; }
        public string SoTaiKhoan { get; set; }
        public string ChuTaiKhoan { get; set; }
        public string LinkQrCode { get; set; }
    }
}