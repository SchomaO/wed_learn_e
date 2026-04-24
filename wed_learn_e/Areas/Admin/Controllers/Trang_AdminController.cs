using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using wed_learn_e.Models;
namespace wed_learn_e.Areas.Admin.Controllers
{
    public class Trang_AdminController : Controller
    {
        private wed_learn_eEntities db = new wed_learn_eEntities();
        // GET: Admin/Trang_Admin
        public ActionResult Index()
        {
            ViewBag.Title = "Admin - Quản lý người dùng";

            // Đổi chữ Session["VaiTro"] thành Session["Admin_VaiTro"]
            if (Session["Admin_VaiTro"] == null || Session["Admin_VaiTro"].ToString() != "quan_tri_vien")
            {
                return RedirectToAction("DangNhap", "User", new { area = "" });
            }

            // Đổi thành Session Admin
            ViewBag.ho_va_ten = Session["Admin_HoTen"];

            var users = db.nguoi_dung.Where(u => u.vai_tro != "quan_tri_vien").ToList();
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
            if (Session["Admin_VaiTro"] == null || Session["Admin_VaiTro"].ToString() != "quan_tri_vien")
            {
                return RedirectToAction("DangNhap", "User", new { area = "" });
            }

            var listCapDo = db.cap_do.ToList();
            return View(listCapDo);
        }
        [HttpGet]
        public JsonResult GetContentByLevel(int id)
        {
            db.Configuration.ProxyCreationEnabled = false;
            try
            {
                // Lấy danh sách ID các khóa học thuộc Level này
                var ids = db.khoa_hoc.Where(x => x.id_cap_do == id).Select(x => x.id_khoa_hoc).ToList();

                var noi = db.bai_luyen_noi.Where(x => ids.Contains((int)x.id_khoa_hoc))
                            .Select(x => new { x.id_bai_noi, x.noi_dung_goc, x.nghia_tieng_viet }).ToList();

                var nghe = db.bai_luyen_nghe.Where(x => ids.Contains((int)x.id_khoa_hoc))
                             .Select(x => new { x.id_bai_nghe, x.tieu_de, x.file_am_thanh }).ToList();

                var video = db.bai_giang_video.Where(x => ids.Contains((int)x.id_khoa_hoc))
                              .Select(x => new { x.id_video, x.tieu_de, x.duong_dan_video }).ToList();

                var viet = db.bai_luyen_viet.Where(x => ids.Contains((int)x.id_khoa_hoc))
                             .Select(x => new { x.id_bai_viet, x.tieu_de, x.mo_ta }).ToList();

                var tuvung = db.tu_vung.Where(x => ids.Contains((int)x.id_khoa_hoc))
                               .Select(x => new { x.id_tu_vung, x.tu_tieng_anh, x.nghia_tieng_viet }).ToList();

                var nguphap = db.ngu_phap.Where(x => x.id_cap_do == id)
                               .Select(x => new { x.id_ngu_phap, x.tieu_de, x.cach_dung, x.cong_thuc, x.vi_du })
                               .ToList();
                var games = db.game_kho_bau.Where(x => x.id_cap_do == id)
                               .Select(x => new { x.id_game, x.cau_hoi_kho_bau, x.mo_ta }).ToList();

                return Json(new
                {
                    success = true,
                    data = new { noi, nghe, video, viet, game = games, tu_vung = tuvung, ngu_phap = nguphap }
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
                else if (type == "tu-vung")
                {
                    var item = db.tu_vung.Find(id);
                    if (item != null) db.tu_vung.Remove(item);
                }
                else if (type == "viet")
                {
                    var item = db.bai_luyen_viet.Find(id);
                    if (item != null) db.bai_luyen_viet.Remove(item);
                }
                else if (type == "ngu-phap")
                {
                    var item = db.ngu_phap.Find(id);
                    if (item != null) db.ngu_phap.Remove(item);
                }
                else if (type == "game")
                {
                    var item = db.game_kho_bau.Find(id);
                    if (item != null) db.game_kho_bau.Remove(item);
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
        public JsonResult CreateContent(int id_cap_do, string type, string content, string extra)
        {
            try
            {
                // Ở đây View của bạn đang gửi 'content' làm tên và 'extra' làm mô tả
                var moi = new khoa_hoc
                {
                    id_cap_do = id_cap_do,
                    ten_khoa_hoc = content, // Khớp với biến 'content' từ JS gửi lên
                    mo_ta = extra           // Khớp với biến 'extra' từ JS gửi lên
                };

                db.khoa_hoc.Add(moi);
                db.SaveChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                // Ghi log lỗi nếu cần và trả về thông báo
                return Json(new { success = false, message = "Lỗi Database: " + ex.Message });
            }
        }
        public ActionResult CaiDat()
        {
            // Kiểm tra quyền Admin (nên có)
            if (Session["Admin_VaiTro"] == null || Session["Admin_VaiTro"].ToString() != "quan_tri_vien")
            {
                return RedirectToAction("DangNhap", "User", new { area = "" });
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
        public ActionResult QuanLyBinhLuan(string chuDe = "")
        {
            if (Session["Admin_VaiTro"] == null || Session["Admin_VaiTro"].ToString() != "quan_tri_vien")
            {
                return RedirectToAction("DangNhap", "User", new { area = "" });
            }

            var query = db.binh_luan.Include(b => b.nguoi_dung).AsQueryable();

            if (!string.IsNullOrEmpty(chuDe))
            {
                query = query.Where(b => b.chu_de == chuDe);
            }

            // 1. TÁCH DANH SÁCH HỌC VIÊN (Không phải Admin)
            ViewBag.ListHocVien = query.Where(b => b.nguoi_dung.vai_tro != "quan_tri_vien")
                                       .OrderByDescending(b => b.ngay_tao).ToList();

            // 2. TÁCH DANH SÁCH ADMIN
            ViewBag.ListAdmin = query.Where(b => b.nguoi_dung.vai_tro == "quan_tri_vien")
                                     .OrderByDescending(b => b.ngay_tao).ToList();

            ViewBag.ListChuDe = db.binh_luan.Select(b => b.chu_de).Distinct().ToList();
            ViewBag.ChuDeHienTai = chuDe;

            // Không cần truyền Model nữa vì đã có ViewBag
            return View();
        }

        [HttpPost]
        public JsonResult XoaBinhLuan(int id)
        {
            try
            {
                var cmt = db.binh_luan.Find(id);
                if (cmt != null)
                {
                    db.binh_luan.Remove(cmt);
                    db.SaveChanges();
                    return Json(new { success = true, message = "Đã xóa bình luận!" });
                }
                return Json(new { success = false, message = "Không tìm thấy bình luận." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }
        [HttpPost]
        public JsonResult TraLoiBinhLuan(int id_binh_luan, string noi_dung)
        {
            try
            {
                if (Session["Admin_ID"] == null)
                    return Json(new { success = false, message = "Vui lòng đăng nhập lại Admin!" });

                var cmtGoc = db.binh_luan.Include(b => b.nguoi_dung).FirstOrDefault(b => b.id_binh_luan == id_binh_luan);

                if (cmtGoc != null)
                {
                    // 1. Đánh dấu bình luận học viên đã được duyệt
                    cmtGoc.trang_thai = true;

                    // 2. Tạo bình luận mới của Admin
                    binh_luan adminComment = new binh_luan();
                    adminComment.id_nguoi_dung = Convert.ToInt32(Session["Admin_ID"]);
                    adminComment.id_cap_do = cmtGoc.id_cap_do;
                    adminComment.chu_de = cmtGoc.chu_de;

                    // ---> MẸO KHÔNG SỬA DATABASE NẰM Ở ĐÂY <---
                    // Lấy tên học viên đang được trả lời
                    string tenHocVien = (cmtGoc.nguoi_dung != null) ? cmtGoc.nguoi_dung.ho_va_ten : "Học viên";

                    // Ghép nối chuỗi: "Trả lời @Tên_Học_Viên: Nội dung"
                    adminComment.noi_dung = $"👉 Trả lời @{tenHocVien}: {noi_dung}";
                    // ------------------------------------------

                    adminComment.ngay_tao = DateTime.Now;
                    adminComment.trang_thai = true;

                    db.binh_luan.Add(adminComment);
                    db.SaveChanges();

                    return Json(new { success = true, message = "Đã đăng phản hồi lên hệ thống!" });
                }

                return Json(new { success = false, message = "Không tìm thấy bình luận." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi Database: " + ex.Message });
            }
        }
        public ActionResult Analytics()
        {
            // 1. Chặn người dùng lạ
            if (Session["Admin_VaiTro"] == null || Session["Admin_VaiTro"].ToString() != "quan_tri_vien")
            {
                return RedirectToAction("DangNhap", "User", new { area = "" });
            }

            try
            {
                // ==========================================
                // PHẦN 1: KPI CARDS (Giữ nguyên)
                // ==========================================
                ViewBag.TongHocVien = db.nguoi_dung.Count(u => u.vai_tro != "quan_tri_vien");
                ViewBag.TongKhoaHoc = db.khoa_hoc.Count();
                ViewBag.BinhLuanCho = db.binh_luan.Count(b => b.trang_thai == false);

                // ==========================================
                // PHẦN 2: DỮ LIỆU BIỂU ĐỒ CỘT (Khắc phục lỗi SQL)
                // ==========================================
                // BƯỚC 1: Lấy ID và số lượng (Chạy dưới SQL Server)
                var topKhoaHocRaw = db.tien_do_hoc_tap
                    .GroupBy(t => t.id_khoa_hoc)
                    .Select(g => new {
                        IdKhoaHoc = g.Key,
                        SoLuong = g.Count()
                    })
                    .OrderByDescending(x => x.SoLuong)
                    .Take(5)
                    .ToList(); // <-- Ép thực thi ngay tại đây

                // BƯỚC 2: Tìm tên khóa học tương ứng (Chạy trên RAM bằng C#)
                var topKhoaHoc = topKhoaHocRaw.Select(x => new {
                    TenKhoa = db.khoa_hoc.FirstOrDefault(k => k.id_khoa_hoc == x.IdKhoaHoc)?.ten_khoa_hoc ?? "Khóa học ẩn",
                    SoLuong = x.SoLuong
                }).ToList();

                ViewBag.LabelKhoaHoc = topKhoaHoc.Select(k => k.TenKhoa).ToArray();
                ViewBag.DataKhoaHoc = topKhoaHoc.Select(k => k.SoLuong).ToArray();

                // ==========================================
                // PHẦN 3: DỮ LIỆU BIỂU ĐỒ TRÒN (Khắc phục lỗi Nullable)
                // ==========================================
                // BƯỚC 1: Đếm số lượng theo Cấp độ (Chạy dưới SQL Server)
                var phanBoCapDoRaw = db.nguoi_dung
                    .Where(u => u.vai_tro != "quan_tri_vien" && u.id_cap_do_hien_tai != null)
                    .GroupBy(u => u.id_cap_do_hien_tai)
                    .Select(g => new {
                        CapDo = g.Key,
                        SoLuong = g.Count()
                    })
                    .ToList(); // <-- Ép thực thi ngay tại đây

                // BƯỚC 2: Nối chữ "Cấp độ" (Chạy trên RAM bằng C#)
                var phanBoCapDo = phanBoCapDoRaw.Select(c => new {
                    TenCapDo = "Cấp độ " + c.CapDo,
                    SoLuong = c.SoLuong
                }).ToList();

                ViewBag.LabelCapDo = phanBoCapDo.Select(c => c.TenCapDo).ToArray();
                ViewBag.DataCapDo = phanBoCapDo.Select(c => c.SoLuong).ToArray();

                return View();
            }
            catch (Exception ex)
            {
                return Content("Đã xảy ra lỗi khi thống kê dữ liệu: " + ex.Message);
            }
        }
        // 1. GIAO DIỆN QUẢN LÝ TRANG CHỦ
        public ActionResult QuanLyGiaoDien()
        {
            if (Session["Admin_VaiTro"] == null || Session["Admin_VaiTro"].ToString() != "quan_tri_vien")
                return RedirectToAction("DangNhap", "User", new { area = "" });

            // Lấy danh sách để Admin xem
            ViewBag.ListFeature = db.feature_trang_chu.OrderBy(f => f.thu_tu).ToList();
            ViewBag.ListFooter = db.footer_cot.OrderBy(f => f.thu_tu).ToList();
            return View();
        }

        // 1. THÊM HÀNG GIỚI THIỆU (UPLOAD ẢNH)
        [HttpPost]
        [ValidateInput(false)]
        public JsonResult ThemFeature(string tieu_de, string noi_dung, HttpPostedFileBase hinh_anh_file, string anh_ben_trai, string thu_tu)
        {
            try
            {
                // Tự ép kiểu an toàn (Không sợ bị Crash 500 nữa)
                bool viTri = (anh_ben_trai == "true");
                int thuTuInt = 1;
                int.TryParse(thu_tu, out thuTuInt);

                string duongDanAnh = "";

                if (hinh_anh_file != null && hinh_anh_file.ContentLength > 0)
                {
                    string dir = Server.MapPath("~/Img/Features");
                    if (!Directory.Exists(dir)) { Directory.CreateDirectory(dir); }

                    string fileName = DateTime.Now.ToString("yyyyMMdd_HHmmss_") + Path.GetFileName(hinh_anh_file.FileName);
                    string path = Path.Combine(dir, fileName);
                    hinh_anh_file.SaveAs(path);
                    duongDanAnh = "/Img/Features/" + fileName;
                }

                var f = new feature_trang_chu
                {
                    tieu_de = tieu_de,
                    noi_dung = noi_dung,
                    hinh_anh = duongDanAnh,
                    anh_ben_trai = viTri,
                    thu_tu = thuTuInt
                };
                db.feature_trang_chu.Add(f);
                db.SaveChanges();
                return Json(new { success = true, message = "Thêm hàng giới thiệu thành công!" });
            }
            catch (Exception ex) { return Json(new { success = false, message = "Lỗi Database: " + ex.Message }); }
        }

        // 2. CẬP NHẬT HÀNG GIỚI THIỆU
        [HttpPost]
        [ValidateInput(false)]
        public JsonResult UpdateFeature(string id, string tieu_de, string noi_dung, HttpPostedFileBase hinh_anh_file, string hinh_anh_cu, string anh_ben_trai, string thu_tu)
        {
            try
            {
                int idFeature = 0;
                int.TryParse(id, out idFeature);

                var f = db.feature_trang_chu.Find(idFeature);
                if (f != null)
                {
                    int thuTuInt = 1;
                    int.TryParse(thu_tu, out thuTuInt);

                    f.tieu_de = tieu_de;
                    f.noi_dung = noi_dung;
                    f.anh_ben_trai = (anh_ben_trai == "true");
                    f.thu_tu = thuTuInt;

                    if (hinh_anh_file != null && hinh_anh_file.ContentLength > 0)
                    {
                        string dir = Server.MapPath("~/Img/Features");
                        if (!Directory.Exists(dir)) { Directory.CreateDirectory(dir); }

                        string fileName = DateTime.Now.ToString("yyyyMMdd_HHmmss_") + Path.GetFileName(hinh_anh_file.FileName);
                        string path = Path.Combine(dir, fileName);
                        hinh_anh_file.SaveAs(path);
                        f.hinh_anh = "/Img/Features/" + fileName;
                    }
                    else
                    {
                        f.hinh_anh = hinh_anh_cu;
                    }

                    db.SaveChanges();
                    return Json(new { success = true, message = "Đã cập nhật hàng giới thiệu!" });
                }
                return Json(new { success = false, message = "Không tìm thấy dữ liệu." });
            }
            catch (Exception ex) { return Json(new { success = false, message = "Lỗi Database: " + ex.Message }); }
        }
        // 3. THÊM CỘT FOOTER MỚI (Nâng cấp chống lỗi)
        [HttpPost]
        [ValidateInput(false)]
        public JsonResult ThemFooter(string tieu_de, string noi_dung_html, string thu_tu)
        {
            try
            {
                int thuTuInt = 1;
                int.TryParse(thu_tu, out thuTuInt); // Tự ép kiểu an toàn

                var f = new footer_cot
                {
                    tieu_de = tieu_de,
                    noi_dung_html = noi_dung_html,
                    thu_tu = thuTuInt
                };
                db.footer_cot.Add(f);
                db.SaveChanges();
                return Json(new { success = true, message = "Thêm cột Footer thành công!" });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        // 4. CẬP NHẬT CỘT FOOTER (Nâng cấp chống lỗi)
        [HttpPost]
        [ValidateInput(false)]
        public JsonResult UpdateFooter(string id, string tieu_de, string noi_dung_html, string thu_tu)
        {
            try
            {
                int idFooter = 0;
                int.TryParse(id, out idFooter);

                var f = db.footer_cot.Find(idFooter);
                if (f != null)
                {
                    int thuTuInt = 1;
                    int.TryParse(thu_tu, out thuTuInt);

                    f.tieu_de = tieu_de;
                    f.noi_dung_html = noi_dung_html;
                    f.thu_tu = thuTuInt;
                    db.SaveChanges();
                    return Json(new { success = true, message = "Đã cập nhật Footer!" });
                }
                return Json(new { success = false, message = "Không tìm thấy dữ liệu." });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        // 4. HÀM XÓA CHUNG
        [HttpPost]
        public JsonResult XoaThanhPhan(string loai, int id)
        {
            if (loai == "feature")
            {
                var f = db.feature_trang_chu.Find(id);
                if (f != null) { db.feature_trang_chu.Remove(f); db.SaveChanges(); }
            }
            else
            {
                var f = db.footer_cot.Find(id);
                if (f != null) { db.footer_cot.Remove(f); db.SaveChanges(); }
            }
            return Json(new { success = true });
        }
    }
}