using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using wed_learn_e.Models;
using System.Data.Entity;
namespace wed_learn_e.Areas.Admin.Controllers
{
    public class Trang_AdminController : Controller
    {
        private wed_learn_eEntities db = new wed_learn_eEntities();
        // GET: Admin/Trang_Admin
        public ActionResult Index()
        {
            ViewBag.Title = "Admin - Quản lý người dùng";

            // 1. Chặn người chưa đăng nhập hoặc không phải admin
            if (Session["VaiTro"] == null || Session["VaiTro"].ToString() != "quan_tri_vien")
            {
                return RedirectToAction("DangNhap", "User", new { area = "" });
            }

            ViewBag.ho_va_ten = Session["HoTen"];

            // 2. LẤY DỮ LIỆU ĐÃ LỌC TỪ DATABASE
            // Dùng .Where() để bảo SQL Server chỉ lấy những người KHÔNG PHẢI là "quan_tri_vien"
            var users = db.nguoi_dung.Where(u => u.vai_tro != "quan_tri_vien").ToList();

            // Hoặc nếu bạn muốn chỉ đích danh luôn: 
            // var users = db.nguoi_dung.Where(u => u.vai_tro == "hoc_vien" || u.vai_tro == "nguoi_dung").ToList();

            // 3. Truyền biến 'users' đã lọc sạch sẽ vào View
            return View(users);
        }
        // 1. Hàm GET: Lấy ra danh sách khóa học CỦA 1 HỌC VIÊN CỤ THỂ
        public ActionResult ChiTietKhoaHoc(int id_nguoi_dung)
        {
            // Lấy thông tin học viên để in ra tên
            var user = db.nguoi_dung.Find(id_nguoi_dung);
            ViewBag.HocVien = user;

            // Lấy danh sách các khóa học mà học viên này ĐÃ đăng ký
            var listKhoaDaHoc = db.tien_do_hoc_tap.Where(t => t.id_nguoi_dung == id_nguoi_dung).ToList();

            // Lấy TẤT CẢ khóa học trong hệ thống (để hiện ra cái Dropdown cho Admin chọn thêm mới)
            // Mẹo: Lọc ra những khóa chưa học để Admin không thêm trùng
            var listIdDaHoc = listKhoaDaHoc.Select(t => t.id_khoa_hoc).ToList();
            ViewBag.KhoaChuaHoc = db.khoa_hoc.Where(k => !listIdDaHoc.Contains(k.id_khoa_hoc)).ToList();

            return View(listKhoaDaHoc);
        }

        // 2. Hàm POST: Thêm khóa học mới cho học viên
        [HttpPost]
        public ActionResult ThemKhoaHocChoHocVien(int id_nguoi_dung, int id_khoa_hoc)
        {
            try
            {
                // Kiểm tra xem đã có chưa (đề phòng bấm trùng)
                var check = db.tien_do_hoc_tap.FirstOrDefault(t => t.id_nguoi_dung == id_nguoi_dung && t.id_khoa_hoc == id_khoa_hoc);
                if (check == null)
                {
                    tien_do_hoc_tap tienDoMoi = new tien_do_hoc_tap();
                    tienDoMoi.id_nguoi_dung = id_nguoi_dung;
                    tienDoMoi.id_khoa_hoc = id_khoa_hoc;
                    // Nếu bảng tiến độ của bạn có ngày đăng ký, gán thêm: tienDoMoi.ngay_dang_ky = DateTime.Now;

                    db.tien_do_hoc_tap.Add(tienDoMoi);

                    // Tăng số lượng khóa học của user lên 1
                    var user = db.nguoi_dung.Find(id_nguoi_dung);
                    if (user != null) { user.so_luong_khoa_hoc = (user.so_luong_khoa_hoc ?? 0) + 1; }

                    db.SaveChanges();
                    return Json(new { success = true, message = "Thêm khóa học thành công!" });
                }
                return Json(new { success = false, message = "Học viên này đã có khóa học này rồi!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        // 3. Hàm POST: Xóa khóa học khỏi học viên
        [HttpPost]
        public ActionResult XoaKhoaHocCuaHocVien(int id_nguoi_dung, int id_khoa_hoc)
        {
            try
            {
                var tienDo = db.tien_do_hoc_tap.FirstOrDefault(t => t.id_nguoi_dung == id_nguoi_dung && t.id_khoa_hoc == id_khoa_hoc);
                if (tienDo != null)
                {
                    db.tien_do_hoc_tap.Remove(tienDo);

                    // Giảm số lượng khóa học xuống 1
                    var user = db.nguoi_dung.Find(id_nguoi_dung);
                    if (user != null && user.so_luong_khoa_hoc > 0) { user.so_luong_khoa_hoc -= 1; }

                    db.SaveChanges();
                    return Json(new { success = true, message = "Đã gỡ khóa học!" });
                }
                return Json(new { success = false, message = "Không tìm thấy dữ liệu khóa học này!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }
        public ActionResult Course_Setting()
        {
            ViewBag.Title = "Admin - Quản lý khóa học";
            // Kiểm tra quyền Admin (giống hàm Index của bạn)
            if (Session["VaiTro"] == null || Session["VaiTro"].ToString() != "quan_tri_vien")
                return RedirectToAction("DangNhap", "User", new { area = "" });

            var listCapDo = db.cap_do.ToList();
            return View(listCapDo);
        }
        [HttpGet]
        public JsonResult GetContentByLevel(int id)
        {
            db.Configuration.ProxyCreationEnabled = false;
            try
            {
                // 1. Lấy danh sách ID các khóa học thuộc Level này (A1, A2...)
                var ids = db.khoa_hoc.Where(x => x.id_cap_do == id).Select(x => x.id_khoa_hoc).ToList();

                // 2. Lấy dữ liệu từ tất cả các bảng liên quan
                var noi = db.bai_luyen_noi.Where(x => ids.Contains((int)x.id_khoa_hoc))
                            .Select(x => new { x.id_bai_noi, x.noi_dung_goc, x.nghia_tieng_viet }).ToList();

                var nghe = db.bai_luyen_nghe.Where(x => ids.Contains((int)x.id_khoa_hoc))
                             .Select(x => new { x.id_bai_nghe, x.tieu_de, x.file_am_thanh }).ToList();

                var video = db.bai_giang_video.Where(x => ids.Contains((int)x.id_khoa_hoc))
                              .Select(x => new { x.id_video, x.tieu_de, x.duong_dan_video }).ToList();

                var viet = db.bai_luyen_viet.Where(x => ids.Contains((int)x.id_khoa_hoc))
                             .Select(x => new { x.id_bai_viet, x.tieu_de, x.lich_su_luyen_viet }).ToList();

                // Giả sử bảng từ vựng của bạn là 'tu_vung'
                var tuvung = db.tu_vung.Where(x => ids.Contains((int)x.id_khoa_hoc))
                               .Select(x => new { x.id_tu_vung, x.tu_tieng_anh, x.nghia_tieng_viet }).ToList();

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        noi = noi,
                        nghe = nghe,
                        video = video,
                        viet = viet,
                        tu_vung = tuvung
                    }
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        // Hàm xóa "Vạn năng"
        [HttpPost]
        public JsonResult DeleteItem(string type, int id)
        {
            try
            {
                if (type == "noi")
                {
                    var item = db.bai_luyen_noi.Find(id);
                    if (item != null) db.bai_luyen_noi.Remove(item);
                }
                else if (type == "nghe")
                {
                    var item = db.bai_luyen_nghe.Find(id);
                    if (item != null)
                    {
                        // Xóa câu hỏi con trước khi xóa bài nghe (tránh lỗi khóa ngoại)
                        var questions = db.cau_hoi_luyen_nghe.Where(q => q.id_bai_nghe == id);
                        foreach (var q in questions) db.cau_hoi_luyen_nghe.Remove(q);
                        db.bai_luyen_nghe.Remove(item);
                    }
                }
                else if (type == "video")
                {
                    var item = db.bai_giang_video.Find(id);
                    if (item != null) db.bai_giang_video.Remove(item);
                }
                db.SaveChanges();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        [HttpPost]
        public JsonResult CreateNoi(int id_cap_do, string noi_dung_goc, string nghia_tieng_viet)
        {
            // Tìm đại 1 khóa học "Luyện Nói" của cấp độ này để gán vào
            var khoaHoc = db.khoa_hoc.FirstOrDefault(x => x.id_cap_do == id_cap_do && x.ten_khoa_hoc.Contains("Nói"));

            if (khoaHoc == null) return Json(new { success = false, message = "Không tìm thấy khóa học Luyện Nói cho cấp độ này" });

            var moi = new bai_luyen_noi
            {
                id_khoa_hoc = khoaHoc.id_khoa_hoc,
                noi_dung_goc = noi_dung_goc,
                nghia_tieng_viet = nghia_tieng_viet,
                loai_bai_noi = "Vocabulary" // Mặc định
            };

            db.bai_luyen_noi.Add(moi);
            db.SaveChanges();
            return Json(new { success = true });
        }
        [HttpPost]
        public JsonResult CreateNewCourse(int id_cap_do, string ten_khoa_hoc, string mo_ta)
        {
            try
            {
                // Tạo đối tượng khóa học mới
                var moi = new khoa_hoc
                {
                    id_cap_do = id_cap_do,
                    ten_khoa_hoc = ten_khoa_hoc,
                    mo_ta = mo_ta
                    // Nếu bạn có cột hình ảnh hoặc icon, có thể gán mặc định ở đây
                };

                db.khoa_hoc.Add(moi);
                db.SaveChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }
        public ActionResult CaiDat()
        {
            // Kiểm tra quyền Admin (nên có)
            if (Session["VaiTro"] == null || Session["VaiTro"].ToString() != "quan_tri_vien")
            {
                return RedirectToAction("dangnhap", "User", new { area = "" });
            }
            return View();
        }

        [HttpPost]
        public JsonResult LuuCaiDat(bool BaoTri, bool ChoPhepDangKy, string ThongBaoBanner, int GiaVipThang, int GiaVipNam)
        {
            try
            {
                // Lưu vào bộ nhớ Application (tồn tại xuyên suốt phiên chạy của Server)
                HttpContext.Application["BaoTri"] = BaoTri;
                HttpContext.Application["ChoPhepDangKy"] = ChoPhepDangKy;
              
                HttpContext.Application["ThongBaoBanner"] = ThongBaoBanner;
                HttpContext.Application["GiaVipThang"] = GiaVipThang;
                HttpContext.Application["GiaVipNam"] = GiaVipNam;
                return Json(new { success = true, message = "Hệ thống đã cập nhật cấu hình mới!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }
        [HttpPost]
        public JsonResult DonDepDuLieuCu()
        {
            try
            {
                // Lấy thời điểm 1 năm trước
                DateTime motNamTruoc = DateTime.Now.AddYears(-1);

                // Ví dụ: Xóa các bình luận cũ hơn 1 năm (Bạn có thể thêm các bảng lịch sử khác vào đây)
                var oldComments = db.binh_luan.Where(b => b.ngay_tao < motNamTruoc).ToList();

                // Dùng vòng lặp để xóa từng dòng thay vì xóa 1 cục
                foreach (var comment in oldComments)
                {
                    db.binh_luan.Remove(comment);
                }

                db.SaveChanges();

                return Json(new { success = true, message = $"Đã dọn dẹp {oldComments.Count} bản ghi cũ thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi dọn dẹp: " + ex.Message });
            }
        }
    }
}