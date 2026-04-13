using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using wed_learn_e.Models;

namespace wed_learn_e.Controllers
{
    public class UserController : Controller
    {
        wed_learn_eEntities db = new wed_learn_eEntities();
        public ActionResult Index()
        {
            return View();
        }
        // đăng ký
        public ActionResult dangky()
        {
            return View();
        }


        [HttpPost]

        public ActionResult dangky(nguoi_dung model)
        {
            // 1. Kiểm tra các điều kiện [Required], [Compare] trong file .cs
            if (ModelState.IsValid)
            {
                // 2. Kiểm tra xem tên đăng nhập đã bị ai dùng chưa
                var check = db.nguoi_dung.FirstOrDefault(s => s.ten_dang_nhap == model.ten_dang_nhap);

                if (check == null)
                {
                    // 3. Gán các giá trị hệ thống tự sinh
                    model.ngay_tao = DateTime.Now;
                    model.vai_tro = "nguoi_dung"; // Mặc định đăng ký luôn là user thường

                    // Lưu ý: Nếu cột MatKhauNL trong SQL cho phép NULL, code sẽ chạy rất êm.
                    db.nguoi_dung.Add(model);
                    db.SaveChanges();

                    // 4. Thông báo và chuyển hướng sang trang đăng nhập
                    TempData["Success"] = "Đăng ký thành công! Mời bạn đăng nhập.";
                    return RedirectToAction("dangnhap", "User");
                }
                else
                {
                    // Nếu trùng tên đăng nhập, báo lỗi cụ thể
                    ViewBag.error = "Tên đăng nhập này đã tồn tại, vui lòng chọn tên khác!";
                }
            }

            // Nếu có lỗi (trùng tên hoặc gõ sai mật khẩu nhập lại), trả về lại trang đăng ký kèm dữ liệu đã nhập
            return View(model);
        }
        // GET đăng nhập
        public ActionResult dangnhap()
        {
            return View();
        }

        // POST đăng nhập
        [HttpPost]
        public ActionResult DangNhap(string ten_dang_nhap, string mat_khau)
        {
            // Tìm người dùng có tài khoản và mật khẩu khớp với dữ liệu nhập vào
            var user = db.nguoi_dung.FirstOrDefault(u => u.ten_dang_nhap == ten_dang_nhap && u.mat_khau == mat_khau);

            if (user != null)
            {
                // 1. Lưu các thông tin cần thiết vào Session
                Session["user_id"] = user.id_nguoi_dung;
                Session["TenDN"] = user.ten_dang_nhap;
                Session["HoTen"] = user.ho_va_ten;
                Session["VaiTro"] = user.vai_tro; // Quan trọng: Lưu lại vai trò để các trang khác kiểm tra

                // Lấy cấp độ hiện tại nếu có (Dành cho User)
                if (user.id_cap_do_hien_tai != null)
                {
                    Session["id_cap_do_hien_tai"] = user.id_cap_do_hien_tai;
                }

                // 2. BẺ LÁI DỰA TRÊN VAI TRÒ (PHÂN QUYỀN)
                if (user.vai_tro == "quan_tri_vien")
                {
                    // Đá sang hàm Index, của Controller Trang_Admin, nằm trong khu vực Area Admin
                    return RedirectToAction("Index", "Trang_Admin", new { area = "Admin" });
                }
                else
                {
                    // Nếu là User bình thường -> Đá về Trang Chủ
                    return RedirectToAction("Index", "Trang_chu");
                }
            }
            else
            {
                // Đăng nhập thất bại
                ViewBag.Error = "Tài khoản hoặc mật khẩu không chính xác!";
                return View();
            }
        }
        public ActionResult DangXuat()
        {
            Session.Clear();
            Session.Abandon();

            // Xóa cookie
            if (Request.Cookies["UserLogin"] != null)
            {
                HttpCookie ck = new HttpCookie("UserLogin");
                ck.Expires = DateTime.Now.AddDays(-1);
                Response.Cookies.Add(ck);
            }

            return RedirectToAction("Index", "Trang_chu");
        }
       public ActionResult TrangCaNhan()
{
    if (Session["user_id"] == null) return RedirectToAction("dangnhap", "User");
    int userId = Convert.ToInt32(Session["user_id"]);
    var nguoiDung = db.nguoi_dung.FirstOrDefault(u => u.id_nguoi_dung == userId);

    // Lấy toàn bộ danh sách Cấp độ
    var danhSachCapDo = db.cap_do.ToList();

    // Các "rổ" chứa dữ liệu của từng Cấp Độ truyền ra View
    Dictionary<int, string> dictTrangThai = new Dictionary<int, string>();
    Dictionary<int, decimal> dictPhanTram = new Dictionary<int, decimal>();
    Dictionary<int, int> dictTongBai = new Dictionary<int, int>();
    Dictionary<int, int> dictDaLam = new Dictionary<int, int>();
    Dictionary<int, List<string>> dictChiTietDaLam = new Dictionary<int, List<string>>();
    Dictionary<int, List<string>> dictChiTietChuaLam = new Dictionary<int, List<string>>();

    foreach (var cap in danhSachCapDo)
    {
        int idCap = cap.id_cap_do;

        // Tìm tất cả Khóa học thuộc Cấp độ này
        var listKhoa = db.khoa_hoc.Where(k => k.id_cap_do == idCap).ToList();

        int tongBaiCapDo = 0;
        int daLamCapDo = 0;
        List<string> chiTietDaLam = new List<string>();
        List<string> chiTietChuaLam = new List<string>();

        // Quét từng Khóa học để gom TẤT CẢ các loại bài tập
        foreach (var khoa in listKhoa)
        {
            int idKhoaHienTai = khoa.id_khoa_hoc;

            // 1. GOM BÀI LUYỆN VIẾT
            var listViet = db.bai_luyen_viet.Where(b => b.id_khoa_hoc == idKhoaHienTai).ToList();
            foreach (var bai in listViet)
            {
                tongBaiCapDo++;
                bool daLam = db.lich_su_luyen_viet.Any(ls => ls.id_nguoi_dung == userId && ls.id_bai_viet == bai.id_bai_viet);
                if (daLam)
                {
                    daLamCapDo++;
                    chiTietDaLam.Add("📝 [Viết] " + bai.tieu_de);
                }
                else
                {
                    chiTietChuaLam.Add("📝 [Viết] " + bai.tieu_de);
                }
            }

            // 2. GOM BÀI SẮP XẾP
            var listSapXep = db.bai_sap_xep.Where(b => b.id_khoa_hoc == idKhoaHienTai).ToList();
            foreach (var bai in listSapXep)
            {
                tongBaiCapDo++;
                bool daLam = db.lich_su_sap_xep.Any(ls => ls.id_nguoi_dung == userId && ls.id_bai_sap_xep == bai.id_bai_sap_xep);
                if (daLam)
                {
                    daLamCapDo++;
                    chiTietDaLam.Add("🧩 [Sắp xếp] " + bai.tieu_de);
                }
                else
                {
                    chiTietChuaLam.Add("🧩 [Sắp xếp] " + bai.tieu_de);
                }
            }

            // 3. GOM BÀI LUYỆN NGHE (BỔ SUNG)
            var listNghe = db.bai_luyen_nghe.Where(b => b.id_khoa_hoc == idKhoaHienTai).ToList();
            foreach (var bai in listNghe)
            {
                tongBaiCapDo++;
                bool daLam = db.lich_su_luyen_nghe.Any(ls => ls.id_nguoi_dung == userId && ls.id_bai_nghe == bai.id_bai_nghe);
                if (daLam)
                {
                    daLamCapDo++;
                    chiTietDaLam.Add("🎧 [Nghe] " + bai.tieu_de);
                }
                else
                {
                    chiTietChuaLam.Add("🎧 [Nghe] " + bai.tieu_de);
                }
            }

            // 4. GOM BÀI GIẢNG VIDEO (BỔ SUNG)
            var listVideo = db.bai_giang_video.Where(b => b.id_khoa_hoc == idKhoaHienTai).ToList();
            foreach (var bai in listVideo)
            {
                tongBaiCapDo++;
                bool daLam = db.lich_su_video.Any(ls => ls.id_nguoi_dung == userId && ls.id_video == bai.id_video);
                if (daLam)
                {
                    daLamCapDo++;
                    chiTietDaLam.Add("🎬 [Video] " + bai.tieu_de);
                }
                else
                {
                    chiTietChuaLam.Add("🎬 [Video] " + bai.tieu_de);
                }
            }

            // 5. GOM BÀI LUYỆN NÓI (BỔ SUNG)
            var listNoi = db.bai_luyen_noi.Where(b => b.id_khoa_hoc == idKhoaHienTai).ToList();
            foreach (var bai in listNoi)
            {
                tongBaiCapDo++;
                bool daLam = db.lich_su_luyen_noi.Any(ls => ls.id_nguoi_dung == userId && ls.id_bai_noi == bai.id_bai_noi);
                
                // Do bảng bai_luyen_noi không có cột tieu_de, ta dùng noi_dung_goc
                string tenBaiNoi = bai.noi_dung_goc;
                if(tenBaiNoi.Length > 30) tenBaiNoi = tenBaiNoi.Substring(0, 30) + "..."; // Cắt ngắn nếu câu quá dài

                if (daLam)
                {
                    daLamCapDo++;
                    chiTietDaLam.Add("🗣️ [Nói] " + tenBaiNoi);
                }
                else
                {
                    chiTietChuaLam.Add("🗣️ [Nói] " + tenBaiNoi);
                }
            }
        }

        // Tính toán trạng thái cho thẻ Cấp Độ này
        dictTongBai[idCap] = tongBaiCapDo;
        dictDaLam[idCap] = daLamCapDo;
        dictChiTietDaLam[idCap] = chiTietDaLam;
        dictChiTietChuaLam[idCap] = chiTietChuaLam;

        if (tongBaiCapDo == 0)
        {
            dictTrangThai[idCap] = "chua_bat_dau"; // Chưa có bài nào
            dictPhanTram[idCap] = 0;
        }
        else if (daLamCapDo == 0)
        {
            dictTrangThai[idCap] = "chua_bat_dau"; // Có bài nhưng chưa đụng tới
            dictPhanTram[idCap] = 0;
        }
        else if (daLamCapDo < tongBaiCapDo)
        {
            dictTrangThai[idCap] = "dang_hoc";     // Đang làm dở dang
            dictPhanTram[idCap] = Math.Round((decimal)daLamCapDo / tongBaiCapDo * 100, 2);
        }
        else
        {
            dictTrangThai[idCap] = "da_hoan_thanh"; // Đã làm hết toàn bộ
            dictPhanTram[idCap] = 100;
        }
    }

    // Đẩy hết ra View
    ViewBag.DanhSachCapDo = danhSachCapDo;
    ViewBag.DictTrangThai = dictTrangThai;
    ViewBag.DictPhanTram = dictPhanTram;
    ViewBag.DictTongBai = dictTongBai;
    ViewBag.DictDaLam = dictDaLam;
    ViewBag.DictChiTietDaLam = dictChiTietDaLam;
    ViewBag.DictChiTietChuaLam = dictChiTietChuaLam;

    return View(nguoiDung);
}
        [HttpPost]
        public ActionResult DoiMatKhau(string mat_khau_cu, string mat_khau_moi, string xac_nhan_mk)
        {
            // 1. Kiểm tra đăng nhập
            if (Session["user_id"] == null)
                return RedirectToAction("dangnhap", "User");

            int userId = Convert.ToInt32(Session["user_id"]);
            var user = db.nguoi_dung.FirstOrDefault(u => u.id_nguoi_dung == userId);

            if (user != null)
            {
                // 2. Kiểm tra Mật khẩu cũ có đúng không?
                if (user.mat_khau != mat_khau_cu)
                {
                    TempData["LoiDoiMK"] = "Mật khẩu cũ không chính xác!";
                    TempData["ActiveTab"] = "doi-mat-khau"; // Giữ tab Đổi MK luôn mở để user thấy lỗi
                    return RedirectToAction("TrangCaNhan");
                }

                // 3. Kiểm tra bảo mật (Phòng trường hợp user tắt Javascript trên trình duyệt)
                if (mat_khau_moi.Length < 6)
                {
                    TempData["LoiDoiMK"] = "Mật khẩu mới phải có ít nhất 6 ký tự!";
                    TempData["ActiveTab"] = "doi-mat-khau";
                    return RedirectToAction("TrangCaNhan");
                }

                if (mat_khau_moi != xac_nhan_mk)
                {
                    TempData["LoiDoiMK"] = "Xác nhận mật khẩu không khớp!";
                    TempData["ActiveTab"] = "doi-mat-khau";
                    return RedirectToAction("TrangCaNhan");
                }

                // 4. Lưu Mật khẩu mới vào Database
                user.mat_khau = mat_khau_moi;
                db.SaveChanges();

                // 5. Báo thành công
                TempData["ThanhCongMK"] = "Đổi mật khẩu thành công!";
                TempData["ActiveTab"] = "doi-mat-khau";
            }

            return RedirectToAction("TrangCaNhan");
        }
    }
}