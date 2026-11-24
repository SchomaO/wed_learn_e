using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace wed_learn_e.Models
{
    public class CauHoiTongHopVM
    {
        public string LoaiCauHoi { get; set; }  // "nghe", "noi", "viet", "trac_nghiem"
        public string NguonKyNang { get; set; }
        public string NoiDung { get; set; }
        public string FileAmThanh { get; set; } // Link audio cho câu Luyện Nghe
        public string GhiChu { get; set; }      // Nghĩa tiếng Việt cho câu Luyện Nói
        public string DapAnA { get; set; }
        public string DapAnB { get; set; }
        public string DapAnC { get; set; }
        public string DapAnD { get; set; }
        public string DapAnDung { get; set; }
    }
}