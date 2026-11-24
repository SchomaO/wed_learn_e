using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;


namespace wed_learn_e.Models
{
    public class LichSuTienDoVM
    {
        public int IdKetQua { get; set; }
        public int IdNguoiDung { get; set; }
        public string HoVaTen { get; set; }
        public string TenDangNhap { get; set; }
        public int IdCapDo { get; set; }
        public string TenCapDo { get; set; }
        public int SoCauDung { get; set; }
        public int TongSoCau { get; set; }
        public decimal PhanTram { get; set; }
        public bool DatYeuCau { get; set; }
        public DateTime? NgayLamBai { get; set; }
    }
}