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
        // 4. Hàm POST: Cập nhật thông tin học viên
        [HttpPost]
        public JsonResult SuaNguoiDung(int id, string ho_ten, string email, string vai_tro, int loai_tk)
        {
            try
            {
                var user = db.nguoi_dung.Find(id);
                if (user != null)
                {
                    user.ho_va_ten = ho_ten;
                    user.email = email;
                    user.vai_tro = vai_tro;
                    user.loai_tai_khoan = loai_tk;

                    // Nếu bảng nguoi_dung của bạn có cột ngay_cap_nhat, hãy bỏ comment dòng dưới:
                    // user.ngay_cap_nhat = DateTime.Now;

                    db.SaveChanges();
                    return Json(new { success = true, message = "Cập nhật tài khoản thành công!" });
                }
                return Json(new { success = false, message = "Không tìm thấy người dùng!" });
            }
            // 1. Bắt lỗi do thiếu trường bắt buộc (Validation)
            catch (System.Data.Entity.Validation.DbEntityValidationException ex)
            {
                string err = string.Join(" | ", ex.EntityValidationErrors.SelectMany(v => v.ValidationErrors).Select(e => e.ErrorMessage));
                return Json(new { success = false, message = "Thiếu dữ liệu bắt buộc: " + err });
            }
            // 2. Lôi cổ lỗi tàng hình (InnerException) của SQL Server ra
            catch (Exception ex)
            {
                string innerMessage = ex.InnerException != null ? (ex.InnerException.InnerException != null ? ex.InnerException.InnerException.Message : ex.InnerException.Message) : ex.Message;
                return Json(new { success = false, message = "Lỗi SQL: " + innerMessage });
            }
        }
        // 5. Hàm POST: Xóa tài khoản người dùng
        [HttpPost]
        public JsonResult XoaNguoiDung(int id)
        {
            try
            {
                var user = db.nguoi_dung.Find(id);
                if (user != null)
                {
                    // BƯỚC QUAN TRỌNG: Xóa các dữ liệu liên quan ở các bảng khác trước (khóa ngoại)

                    // 1. Xóa tiến độ học tập (Dùng ToList() và foreach thay cho RemoveRange)
                    var tienDo = db.tien_do_hoc_tap.Where(t => t.id_nguoi_dung == id).ToList();
                    foreach (var item in tienDo)
                    {
                        db.tien_do_hoc_tap.Remove(item);
                    }

                    // 2. Xóa kết quả kiểm tra (Dùng ToList() và foreach thay cho RemoveRange)
                    var ketQua = db.ket_qua_kiem_tra.Where(k => k.id_nguoi_dung == id).ToList();
                    foreach (var item in ketQua)
                    {
                        db.ket_qua_kiem_tra.Remove(item);
                    }

                    // Cuối cùng mới xóa user
                    db.nguoi_dung.Remove(user);
                    db.SaveChanges();

                    return Json(new { success = true, message = "Đã xóa tài khoản vĩnh viễn!" });
                }
                return Json(new { success = false, message = "Người dùng không tồn tại!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Không thể xóa do vi phạm ràng buộc dữ liệu: " + ex.Message });
            }
        }
        // Hàm POST: Đổi mật khẩu người dùng
        [HttpPost]
        public JsonResult DoiMatKhauNguoiDung(int id, string mat_khau_moi)
        {
            try
            {
                var user = db.nguoi_dung.Find(id);
                if (user != null)
                {
                    if (string.IsNullOrEmpty(mat_khau_moi))
                    {
                        return Json(new { success = false, message = "Mật khẩu không được để trống!" });
                    }

                    // Cập nhật mật khẩu mới
                    user.mat_khau = mat_khau_moi;
                    db.SaveChanges();

                    return Json(new { success = true, message = "Đã đổi mật khẩu thành công cho học viên: " + user.ho_va_ten });
                }
                return Json(new { success = false, message = "Không tìm thấy người dùng!" });
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

                // ---> 1. THÊM DÒNG NÀY ĐỂ LẤY DANH SÁCH KHÓA HỌC <---
                var khoahoc = db.khoa_hoc.Where(x => x.id_cap_do == id)
                                         .Select(x => new { x.id_khoa_hoc, x.ten_khoa_hoc, x.mo_ta }).ToList();

                // Thay thế dòng lấy dữ liệu Luyện Nói cũ bằng dòng này:
                // Thay dòng var noi = ... hiện tại bằng dòng này:
                var noi = db.bai_luyen_noi.Where(x => x.id_khoa_hoc != null && ids.Contains((int)x.id_khoa_hoc))
                            .Select(x => new { x.id_bai_noi, x.noi_dung_goc, x.nghia_tieng_viet }).ToList();

                var nghe = db.bai_luyen_nghe.Where(x => x.id_khoa_hoc != null && ids.Contains((int)x.id_khoa_hoc))
                      .Select(x => new { x.id_bai_nghe, x.tieu_de, x.mo_ta, x.file_am_thanh }).ToList();

                var video = db.bai_giang_video.Where(x => ids.Contains((int)x.id_khoa_hoc))
                       .Select(x => new { x.id_video, x.tieu_de, x.duong_dan_video, x.thoi_luong_phut }).ToList();

                var viet = db.bai_luyen_viet.Where(x => ids.Contains((int)x.id_khoa_hoc))
                             .Select(x => new { x.id_bai_viet, x.tieu_de, x.mo_ta }).ToList();
                var minitest = db.bai_sap_xep.Where(x => ids.Contains((int)x.id_khoa_hoc))
                      .Select(x => new { x.id_bai_sap_xep, x.tieu_de, x.mo_ta }).ToList();

                var tuvung = db.Vocabularies.Where(x => x.id_cap_do == id)
                               .Select(x => new {
                                   id_tu_vung = x.Id,
                                   tu_tieng_anh = x.Word,
                                   nghia_tieng_viet = x.Meaning,
                                   vi_du = x.Example // Lấy thêm cột này
                               }).ToList();

                var nguphap = db.ngu_phap.Where(x => x.id_cap_do == id)
                               .Select(x => new { x.id_ngu_phap, x.tieu_de, x.cach_dung, x.cong_thuc, x.vi_du })
                               .ToList();
                var thanhngu = db.Idioms.Where(x => x.id_cap_do == id)
                        .Select(x => new {
                            id_thanh_ngu = x.Id,
                            thanh_ngu = x.IdiomText,
                            nghia = x.Meaning,
                            vi_du = x.Example
                        }).ToList();
                var games = db.game_kho_bau.Where(x => x.id_cap_do == id)
                       .Select(x => new { x.id_game, x.tieu_de, x.mo_ta, x.tu_khoa_cuoi }).ToList();

                // Thêm chữ: thanh_ngu = thanhngu vào cuối
                return Json(new
                {
                    success = true,
                    data = new { khoa_hoc = khoahoc, noi, nghe, video, viet, game = games, tu_vung = tuvung, ngu_phap = nguphap, thanh_ngu = thanhngu, mini_test = minitest }
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
                    if (item != null)
                    {
                        // Xóa sạch câu hỏi con trước khi xóa Video
                        var questions = db.cau_hoi_video.Where(q => q.id_video == id);
                        foreach (var q in questions) db.cau_hoi_video.Remove(q);

                        db.bai_giang_video.Remove(item);
                    }
                }
                else if (type == "tu-vung")
                {
                    var item = db.Vocabularies.Find(id);
                    if (item != null) db.Vocabularies.Remove(item);
                }
                else if (type == "viet")
                {
                    var item = db.bai_luyen_viet.Find(id);
                    if (item != null)
                    {
                        // Xóa sạch câu hỏi con trước khi xóa Bài viết
                        var questions = db.cau_hoi_luyen_viet.Where(q => q.id_bai_viet == id);
                        foreach (var q in questions) db.cau_hoi_luyen_viet.Remove(q);

                        db.bai_luyen_viet.Remove(item);
                    }
                }
                else if (type == "mini-test")
                {
                    var item = db.bai_sap_xep.Find(id);
                    if (item != null)
                    {
                        var qs = db.cau_hoi_sap_xep.Where(q => q.id_bai_sap_xep == id);
                        foreach (var q in qs) db.cau_hoi_sap_xep.Remove(q);
                        db.bai_sap_xep.Remove(item);
                    }
                }
                else if (type == "khoa-hoc")
                {
                    var item = db.khoa_hoc.Find(id);
                    if (item != null) db.khoa_hoc.Remove(item);
                }
                else if (type == "ngu-phap")
                {
                    var item = db.ngu_phap.Find(id);
                    if (item != null) db.ngu_phap.Remove(item);
                }
                else if (type == "thanh-ngu")
                {
                    var item = db.Idioms.Find(id);
                    if (item != null) db.Idioms.Remove(item);
                }
                else if (type == "game")
                {
                    var item = db.game_kho_bau.Find(id);
                    if (item != null)
                    {
                        // Xóa câu hỏi con trước
                        var questions = db.cau_hoi_kho_bau.Where(q => q.id_game == id);
                        foreach (var q in questions) db.cau_hoi_kho_bau.Remove(q);

                        db.game_kho_bau.Remove(item);
                    }
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
        [HttpPost]
        public JsonResult AddLevel(string ten_cap_do, string mo_ta)
        {
            try
            {
                // Tạo một object cấp độ mới
                var capDoMoi = new cap_do();
                capDoMoi.ten_cap_do = ten_cap_do;
                capDoMoi.mo_ta = mo_ta;

                // Thêm vào Database và lưu lại
                db.cap_do.Add(capDoMoi);
                db.SaveChanges();

                return Json(new { success = true, message = "Thêm cấp độ thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi Database: " + ex.Message });
            }
        }

        // 2. Xóa cấp độ
        [HttpPost]
        public JsonResult DeleteLevel(int id)
        {
            try
            {
                var capDo = db.cap_do.Find(id);
                if (capDo != null)
                {
                    db.cap_do.Remove(capDo);
                    db.SaveChanges();
                    return Json(new { success = true, message = "Đã xóa cấp độ!" });
                }
                return Json(new { success = false, message = "Không tìm thấy cấp độ này." });
            }
            catch (Exception ex)
            {
                // Nếu dính lỗi khóa ngoại (Cấp độ đang chứa khóa học) thì báo lỗi tiếng Việt
                return Json(new { success = false, message = "Không thể xóa! Cấp độ này đang chứa khóa học bên trong. Vui lòng xóa hết khóa học trước." });
            }
        }
        // 1. Hàm phụ: Lấy danh sách khóa học của một Cấp độ
        [HttpGet]
        public JsonResult GetDanhSachKhoaHoc(int id_cap_do)
        {
            db.Configuration.ProxyCreationEnabled = false;
            var list = db.khoa_hoc.Where(k => k.id_cap_do == id_cap_do)
                                  .Select(k => new { k.id_khoa_hoc, k.ten_khoa_hoc }).ToList();
            return Json(new { success = true, data = list }, JsonRequestBehavior.AllowGet);
        }

        // 2. HÀM VẠN NĂNG: Xử lý lưu tất cả 7 loại nội dung
        [HttpPost]
        // Đã thêm tham số "int? id_item" vào đây
        public JsonResult LuuNoiDungChiTiet(string type, int id_cap_do, int? id_khoa_hoc, int? id_item, string data1, string data2, string data3, string data4)
        {
            try
            {

                // Tự động tìm Khóa học ngầm cho Nói, Nghe, Video và bây giờ là Viết
                if (type == "noi" || type == "nghe" || type == "video" || type == "viet" || type == "mini-test")
                {
                    if (id_khoa_hoc == null || id_khoa_hoc == 0)
                    {
                        var khoaHocMacDinh = db.khoa_hoc.FirstOrDefault(k => k.id_cap_do == id_cap_do);
                        if (khoaHocMacDinh != null) id_khoa_hoc = khoaHocMacDinh.id_khoa_hoc;
                        else return Json(new { success = false, message = "Cấp độ này chưa có khóa học nào để chứa dữ liệu. Vui lòng tạo ít nhất 1 khóa trước!" });
                    }
                }
                switch (type)
                {
                    case "khoa-hoc":
                        if (id_item.HasValue && id_item > 0)
                        {
                            // NẾU CÓ ID -> SỬA KHÓA HỌC
                            var kh = db.khoa_hoc.Find(id_item);
                            if (kh != null)
                            {
                                kh.ten_khoa_hoc = data1;
                                kh.mo_ta = data2;
                            }
                        }
                        else
                        {
                            // NẾU KHÔNG CÓ ID -> THÊM MỚI
                            db.khoa_hoc.Add(new khoa_hoc { id_cap_do = id_cap_do, ten_khoa_hoc = data1, mo_ta = data2 });
                        }
                        break;
                    case "tu-vung":
                        // NẾU CÓ ID TỨC LÀ SỬA
                        if (id_item.HasValue && id_item > 0)
                        {
                            var tv = db.Vocabularies.Find(id_item);
                            if (tv != null)
                            {
                                tv.Word = data1;
                                tv.Meaning = data2;
                                tv.Example = data3;
                            }
                        }
                        // NẾU KHÔNG CÓ ID THÌ LÀ THÊM MỚI
                        else
                        {
                            db.Vocabularies.Add(new Vocabulary { id_cap_do = id_cap_do, Word = data1, Meaning = data2, Example = data3 });
                        }
                        break;
                    case "noi":
                        if (id_item.HasValue && id_item > 0)
                        {
                            var bn = db.bai_luyen_noi.Find(id_item);
                            if (bn != null)
                            {
                                bn.noi_dung_goc = data1;
                                bn.nghia_tieng_viet = data2;
                            }
                        }
                        else
                        {
                            db.bai_luyen_noi.Add(new bai_luyen_noi
                            {
                                id_khoa_hoc = id_khoa_hoc, // Trả lại id_khoa_hoc cho đúng DB
                                noi_dung_goc = data1,
                                nghia_tieng_viet = data2
                            });
                        }
                        break;
                    case "mini-test":
                        if (id_item.HasValue && id_item > 0)
                        {
                            var mt = db.bai_sap_xep.Find(id_item);
                            if (mt != null) { mt.tieu_de = data1; mt.mo_ta = data2; }
                        }
                        else
                        {
                            db.bai_sap_xep.Add(new bai_sap_xep { id_khoa_hoc = id_khoa_hoc, tieu_de = data1, mo_ta = data2, ngay_tao = DateTime.Now });
                        }
                        break;
                    case "nghe":
                        if (id_item.HasValue && id_item > 0)
                        {
                            // Sửa bài nghe
                            var baiNghe = db.bai_luyen_nghe.Find(id_item);
                            if (baiNghe != null)
                            {
                                baiNghe.tieu_de = data1;
                                baiNghe.mo_ta = data2;
                                baiNghe.file_am_thanh = data3;
                            }
                        }
                        else
                        {
                            // Thêm bài nghe mới
                            db.bai_luyen_nghe.Add(new bai_luyen_nghe
                            {
                                id_khoa_hoc = id_khoa_hoc,  // Bắt buộc phải có id_khoa_hoc theo Database
                                tieu_de = data1,
                                mo_ta = data2,
                                file_am_thanh = data3
                            });
                        }
                        break;
                    case "video":
                        int thoiLuong = 0;
                        int.TryParse(data3, out thoiLuong); // Dùng data3 làm thời lượng phút

                        if (id_item.HasValue && id_item > 0)
                        {
                            var vid = db.bai_giang_video.Find(id_item);
                            if (vid != null)
                            {
                                vid.tieu_de = data1;
                                vid.duong_dan_video = data2;
                                vid.thoi_luong_phut = thoiLuong;
                            }
                        }
                        else
                        {
                            db.bai_giang_video.Add(new bai_giang_video
                            {
                                id_khoa_hoc = id_khoa_hoc,
                                tieu_de = data1,
                                duong_dan_video = data2,
                                thoi_luong_phut = thoiLuong
                            });
                        }
                        break;
                    case "viet":
                        if (id_item.HasValue && id_item > 0)
                        {
                            var baiViet = db.bai_luyen_viet.Find(id_item);
                            if (baiViet != null)
                            {
                                baiViet.tieu_de = data1;
                                baiViet.mo_ta = data2;
                            }
                        }
                        else
                        {
                            db.bai_luyen_viet.Add(new bai_luyen_viet
                            {
                                id_khoa_hoc = id_khoa_hoc,
                                tieu_de = data1,
                                mo_ta = data2
                            });
                        }
                        break;
                    case "ngu-phap":
                        // Ngữ pháp lưu theo id_cap_do
                        db.ngu_phap.Add(new ngu_phap { id_cap_do = id_cap_do, tieu_de = data1, cach_dung = data2, cong_thuc = data3, vi_du = data4 });
                        break;
                    case "thanh-ngu":
                        if (id_item.HasValue && id_item > 0)
                        {
                            var tn = db.Idioms.Find(id_item);
                            if (tn != null)
                            {
                                tn.IdiomText = data1;
                                tn.Meaning = data2;
                                tn.Example = data3;
                            }
                        }
                        else
                        {
                            db.Idioms.Add(new Idiom { id_cap_do = id_cap_do, IdiomText = data1, Meaning = data2, Example = data3 });
                        }
                        break;
                    case "game":
                        if (id_item.HasValue && id_item > 0)
                        {
                            var game = db.game_kho_bau.Find(id_item);
                            if (game != null)
                            {
                                game.tieu_de = data1;
                                game.mo_ta = data2;
                                game.tu_khoa_cuoi = data3;
                            }
                        }
                        else
                        {
                            db.game_kho_bau.Add(new game_kho_bau
                            {
                                id_cap_do = id_cap_do,
                                tieu_de = data1,
                                mo_ta = data2,
                                tu_khoa_cuoi = data3
                            });
                        }
                        break;
                    default:
                        return Json(new { success = false, message = "Loại nội dung không hợp lệ." });
                }

                db.SaveChanges();
                return Json(new { success = true, message = "Đã lưu nội dung thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi Database: " + ex.InnerException?.Message ?? ex.Message });
            }
        }
        // ===============================================
        // QUẢN LÝ CÂU HỎI MINI TEST (SẮP XẾP)
        // ===============================================
        [HttpGet]
        public JsonResult GetCauHoiMiniTest(int id_bai)
        {
            db.Configuration.ProxyCreationEnabled = false;
            var list = db.cau_hoi_sap_xep.Where(x => x.id_bai_sap_xep == id_bai).ToList();
            return Json(new { success = true, data = list }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult LuuCauHoiMiniTest(int id_bai, int? id_cau_hoi, string cau_vn, string dap_an)
        {
            try
            {
                if (id_cau_hoi.HasValue && id_cau_hoi > 0)
                {
                    var ch = db.cau_hoi_sap_xep.Find(id_cau_hoi);
                    if (ch != null) { ch.cau_tieng_viet = cau_vn; ch.dap_an_dung = dap_an; }
                }
                else
                {
                    db.cau_hoi_sap_xep.Add(new cau_hoi_sap_xep { id_bai_sap_xep = id_bai, cau_tieng_viet = cau_vn, dap_an_dung = dap_an });
                }
                db.SaveChanges();
                return Json(new { success = true });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpPost]
        public JsonResult XoaCauHoiMiniTest(int id)
        {
            try
            {
                var ch = db.cau_hoi_sap_xep.Find(id);
                if (ch != null) { db.cau_hoi_sap_xep.Remove(ch); db.SaveChanges(); }
                return Json(new { success = true });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }
        // ===============================================
        // QUẢN LÝ CÂU HỎI GAME KHÓ BÁU
        // ===============================================
        [HttpGet]
        public JsonResult GetCauHoiGame(int id_game)
        {
            db.Configuration.ProxyCreationEnabled = false;
            // Sắp xếp theo Thứ tự câu hỏi
            var list = db.cau_hoi_kho_bau.Where(x => x.id_game == id_game).OrderBy(x => x.thu_tu).ToList();
            return Json(new { success = true, data = list }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult LuuCauHoiGame(int id_game, int? id_cau_hoi, int thu_tu, string loai, string noi_dung, string a, string b, string c, string d, string dung, string manh_ghep)
        {
            try
            {
                if (id_cau_hoi.HasValue && id_cau_hoi > 0)
                {
                    var ch = db.cau_hoi_kho_bau.Find(id_cau_hoi);
                    if (ch != null)
                    {
                        ch.thu_tu = thu_tu; ch.loai_cau_hoi = loai; ch.noi_dung = noi_dung;
                        ch.dap_an_a = a; ch.dap_an_b = b; ch.dap_an_c = c; ch.dap_an_d = d;
                        ch.dap_an_dung = dung; ch.manh_ghep = manh_ghep;
                    }
                }
                else
                {
                    db.cau_hoi_kho_bau.Add(new cau_hoi_kho_bau
                    {
                        id_game = id_game,
                        thu_tu = thu_tu,
                        loai_cau_hoi = loai,
                        noi_dung = noi_dung,
                        dap_an_a = a,
                        dap_an_b = b,
                        dap_an_c = c,
                        dap_an_d = d,
                        dap_an_dung = dung,
                        manh_ghep = manh_ghep
                    });
                }
                db.SaveChanges();
                return Json(new { success = true });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpPost]
        public JsonResult XoaCauHoiGame(int id)
        {
            try
            {
                var ch = db.cau_hoi_kho_bau.Find(id);
                if (ch != null) { db.cau_hoi_kho_bau.Remove(ch); db.SaveChanges(); }
                return Json(new { success = true });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }
        // ===============================================
        // QUẢN LÝ CÂU HỎI LUYỆN VIẾT
        // ===============================================
        [HttpGet]
        public JsonResult GetCauHoiViet(int id_bai_viet)
        {
            db.Configuration.ProxyCreationEnabled = false;
            var list = db.cau_hoi_luyen_viet.Where(x => x.id_bai_viet == id_bai_viet).ToList();
            return Json(new { success = true, data = list }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult LuuCauHoiViet(int id_bai_viet, int? id_cau_hoi, string cau_vn, string cau_en)
        {
            try
            {
                if (id_cau_hoi.HasValue && id_cau_hoi > 0)
                {
                    var ch = db.cau_hoi_luyen_viet.Find(id_cau_hoi);
                    if (ch != null) { ch.cau_tieng_viet = cau_vn; ch.dap_an_tieng_anh = cau_en; }
                }
                else
                {
                    db.cau_hoi_luyen_viet.Add(new cau_hoi_luyen_viet
                    {
                        id_bai_viet = id_bai_viet,
                        cau_tieng_viet = cau_vn,
                        dap_an_tieng_anh = cau_en
                    });
                }
                db.SaveChanges();
                return Json(new { success = true });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpPost]
        public JsonResult XoaCauHoiViet(int id)
        {
            try
            {
                var ch = db.cau_hoi_luyen_viet.Find(id);
                if (ch != null) { db.cau_hoi_luyen_viet.Remove(ch); db.SaveChanges(); }
                return Json(new { success = true });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }
        [HttpGet]
        public JsonResult GetCauHoiVideo(int id_video)
        {
            db.Configuration.ProxyCreationEnabled = false;
            var list = db.cau_hoi_video.Where(x => x.id_video == id_video).ToList();
            return Json(new { success = true, data = list }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult LuuCauHoiVideo(int id_video, int? id_cau_hoi, string noi_dung, string a, string b, string c, string d, string dung)
        {
            try
            {
                if (id_cau_hoi.HasValue && id_cau_hoi > 0)
                {
                    var ch = db.cau_hoi_video.Find(id_cau_hoi);
                    if (ch != null)
                    {
                        ch.noi_dung_cau_hoi = noi_dung; ch.dap_an_a = a; ch.dap_an_b = b; ch.dap_an_c = c; ch.dap_an_d = d; ch.dap_an_dung = dung;
                    }
                }
                else
                {
                    db.cau_hoi_video.Add(new cau_hoi_video
                    {
                        id_video = id_video,
                        noi_dung_cau_hoi = noi_dung,
                        dap_an_a = a,
                        dap_an_b = b,
                        dap_an_c = c,
                        dap_an_d = d,
                        dap_an_dung = dung
                    });
                }
                db.SaveChanges();
                return Json(new { success = true });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpPost]
        public JsonResult XoaCauHoiVideo(int id)
        {
            try
            {
                var ch = db.cau_hoi_video.Find(id);
                if (ch != null) { db.cau_hoi_video.Remove(ch); db.SaveChanges(); }
                return Json(new { success = true });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }
        [HttpGet]
        public JsonResult GetCauHoiNghe(int id_bai_nghe)
        {
            db.Configuration.ProxyCreationEnabled = false;
            var list = db.cau_hoi_luyen_nghe.Where(x => x.id_bai_nghe == id_bai_nghe).ToList();
            return Json(new { success = true, data = list }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult LuuCauHoiNghe(int id_bai_nghe, int? id_cau_hoi, string noi_dung, string a, string b, string c, string d, string dung)
        {
            try
            {
                if (id_cau_hoi.HasValue && id_cau_hoi > 0)
                {
                    var ch = db.cau_hoi_luyen_nghe.Find(id_cau_hoi);
                    if (ch != null)
                    {
                        ch.noi_dung_cau_hoi = noi_dung; ch.dap_an_a = a; ch.dap_an_b = b; ch.dap_an_c = c; ch.dap_an_d = d; ch.dap_an_dung = dung;
                    }
                }
                else
                {
                    db.cau_hoi_luyen_nghe.Add(new cau_hoi_luyen_nghe
                    {
                        id_bai_nghe = id_bai_nghe,
                        noi_dung_cau_hoi = noi_dung,
                        dap_an_a = a,
                        dap_an_b = b,
                        dap_an_c = c,
                        dap_an_d = d,
                        dap_an_dung = dung
                    });
                }
                db.SaveChanges();
                return Json(new { success = true });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpPost]
        public JsonResult XoaCauHoiNghe(int id)
        {
            try
            {
                var ch = db.cau_hoi_luyen_nghe.Find(id);
                if (ch != null) { db.cau_hoi_luyen_nghe.Remove(ch); db.SaveChanges(); }
                return Json(new { success = true });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }
        [HttpGet]
        public JsonResult GetThongTinKhoaHoc(int id_khoa_hoc)
        {
            db.Configuration.ProxyCreationEnabled = false;
            var list = db.thong_tin_khoa_hoc.Where(x => x.id_khoa_hoc == id_khoa_hoc)
                         .Select(x => new { id = x.id, noi_dung_html = x.noi_dung_html }).ToList();
            return Json(new { success = true, data = list }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ValidateInput(false)] // <--- BẮT BUỘC PHẢI CÓ ĐỂ LƯU HTML TỪ CKEDITOR
        public JsonResult LuuThongTinKhoaHoc(int id_khoa_hoc, int? id_info, string noi_dung_html)
        {
            try
            {
                if (id_info.HasValue && id_info > 0)
                {
                    var info = db.thong_tin_khoa_hoc.Find(id_info);
                    if (info != null) info.noi_dung_html = noi_dung_html;
                }
                else
                {
                    db.thong_tin_khoa_hoc.Add(new thong_tin_khoa_hoc
                    {
                        id_khoa_hoc = id_khoa_hoc,
                        noi_dung_html = noi_dung_html
                    });
                }
                db.SaveChanges();
                return Json(new { success = true });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpPost]
        public JsonResult XoaThongTinKhoaHoc(int id)
        {
            try
            {
                var info = db.thong_tin_khoa_hoc.Find(id);
                if (info != null) { db.thong_tin_khoa_hoc.Remove(info); db.SaveChanges(); }
                return Json(new { success = true });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
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
        public JsonResult TaoThongBao(string noi_dung, string chu_de)
        {
            try
            {
                if (Session["Admin_ID"] == null)
                    return Json(new { success = false, message = "Vui lòng đăng nhập lại Admin!" });

                binh_luan tbMoi = new binh_luan();
                tbMoi.id_nguoi_dung = Convert.ToInt32(Session["Admin_ID"]);
                tbMoi.noi_dung = noi_dung;
                tbMoi.chu_de = string.IsNullOrEmpty(chu_de) ? "Thông báo chung" : chu_de;
                tbMoi.ngay_tao = DateTime.Now;
                tbMoi.trang_thai = true;

                // ---> CÁCH GIẢI QUYẾT LỖI KHÓA NGOẠI Ở ĐÂY <---
                // Tìm 1 Cấp độ bất kỳ có sẵn trong hệ thống để gán vào
                var capDoHopLe = db.cap_do.FirstOrDefault();
                if (capDoHopLe != null)
                {
                    tbMoi.id_cap_do = capDoHopLe.id_cap_do;
                }
                else
                {
                    // Dự phòng lỡ DB chưa có cấp độ nào
                    tbMoi.id_cap_do = 1;
                }
                // ---------------------------------------------

                db.binh_luan.Add(tbMoi);
                db.SaveChanges();

                return Json(new { success = true, message = "Đã đăng thông báo thành công!" });
            }
            catch (Exception ex)
            {
                string loiThatSu = ex.Message;
                if (ex.InnerException != null)
                {
                    loiThatSu = ex.InnerException.Message;
                    if (ex.InnerException.InnerException != null)
                    {
                        loiThatSu = ex.InnerException.InnerException.Message;
                    }
                }
                return Json(new { success = false, message = "Lỗi SQL: " + loiThatSu });
            }
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
        // ===============================================
        // TRANG QUẢN LÝ BÀI TEST CHÍNH THỨC
        // ===============================================
        public ActionResult QuanLyBaiTest()
        {
            if (Session["Admin_VaiTro"] == null || Session["Admin_VaiTro"].ToString() != "quan_tri_vien")
                return RedirectToAction("DangNhap", "User", new { area = "" });

            return View();
        }

        
        [HttpGet]
        public JsonResult GetDanhSachBaiTest()
        {
            db.Configuration.ProxyCreationEnabled = false;
            // ĐỔI THÀNH bai_kiem_tra_dau_vao
            var tests = db.bai_kiem_tra_dau_vao.Select(x => new {
                x.id_bai_kiem_tra,
                x.tieu_de,
                x.mo_ta,
                x.thoi_gian_phut,
                x.trang_thai
            }).ToList();
            return Json(new { success = true, data = tests }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult DoiTrangThaiBaiTest(int id)
        {
            try
            {
                // ĐỔI THÀNH bai_kiem_tra_dau_vao
                var allTests = db.bai_kiem_tra_dau_vao.Where(x => x.trang_thai == 1).ToList();
                foreach (var t in allTests) { t.trang_thai = 0; }

                var targetTest = db.bai_kiem_tra_dau_vao.Find(id);
                if (targetTest != null) { targetTest.trang_thai = 1; }

                db.SaveChanges();
                return Json(new { success = true });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpPost]
        public JsonResult LuuThongTinBaiTest(int? id, string tieu_de, string mo_ta, int thoi_gian)
        {
            try
            {
                if (id.HasValue && id > 0)
                {
                    // ĐỔI THÀNH bai_kiem_tra_dau_vao
                    var test = db.bai_kiem_tra_dau_vao.Find(id);
                    if (test != null) { test.tieu_de = tieu_de; test.mo_ta = mo_ta; test.thoi_gian_phut = thoi_gian; }
                }
                else
                {
                    db.bai_kiem_tra_dau_vao.Add(new bai_kiem_tra_dau_vao
                    {
                        tieu_de = tieu_de,
                        mo_ta = mo_ta,
                        thoi_gian_phut = thoi_gian,
                        trang_thai = 0
                    });
                }
                db.SaveChanges();
                return Json(new { success = true });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpPost]
        public JsonResult XoaBaiTest(int id)
        {
            try
            {
                // ĐỔI THÀNH bai_kiem_tra_dau_vao
                var test = db.bai_kiem_tra_dau_vao.Find(id);
                if (test != null)
                {
                    var qs = db.cau_hoi_kiem_tra.Where(q => q.id_bai_kiem_tra == id);
                    foreach (var q in qs) db.cau_hoi_kiem_tra.Remove(q);
                    db.bai_kiem_tra_dau_vao.Remove(test);
                    db.SaveChanges();
                }
                return Json(new { success = true });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        // --- QUẢN LÝ CÂU HỎI BÊN TRONG BÀI TEST ---
        [HttpGet]
        public JsonResult GetCauHoiKiemTra(int id_bai)
        {
            db.Configuration.ProxyCreationEnabled = false;
            var list = db.cau_hoi_kiem_tra.Where(x => x.id_bai_kiem_tra == id_bai).ToList();
            return Json(new { success = true, data = list }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
      
        public JsonResult LuuCauHoiKiemTra(int id_bai, int? id_cau, string nd, string a, string b, string c, string d, string dung)
        {
            try
            {
                if (id_cau.HasValue && id_cau > 0)
                {
                    var ch = db.cau_hoi_kiem_tra.Find(id_cau);
                    if (ch != null)
                    {
                        ch.noi_dung_cau_hoi = nd;
                        ch.dap_an_a = a;
                        ch.dap_an_b = b;
                        ch.dap_an_c = c;
                        ch.dap_an_d = d;
                        ch.dap_an_dung = dung;

                        // Tự động gán thời gian cập nhật để SQL không báo lỗi
                        ch.ngay_cap_nhat = DateTime.Now;
                    }
                }
                else
                {
                    db.cau_hoi_kiem_tra.Add(new cau_hoi_kiem_tra
                    {
                        id_bai_kiem_tra = id_bai,
                        noi_dung_cau_hoi = nd,
                        dap_an_a = a,
                        dap_an_b = b,
                        dap_an_c = c,
                        dap_an_d = d,
                        dap_an_dung = dung,

                        // Tự động gán thời gian tạo để SQL không báo lỗi
                        ngay_cap_nhat = DateTime.Now
                    });
                }
                db.SaveChanges();
                return Json(new { success = true });
            }
            // 1. Khối này bắt lỗi do thiếu trường bắt buộc (Validation)
            catch (System.Data.Entity.Validation.DbEntityValidationException ex)
            {
                string err = string.Join(" | ", ex.EntityValidationErrors.SelectMany(v => v.ValidationErrors).Select(e => e.ErrorMessage));
                return Json(new { success = false, message = "Thiếu dữ liệu bắt buộc: " + err });
            }
            // 2. Khối này lôi cổ lỗi tàng hình (InnerException) của SQL Server ra ánh sáng
            catch (Exception ex)
            {
                string innerMessage = ex.InnerException != null ? (ex.InnerException.InnerException != null ? ex.InnerException.InnerException.Message : ex.InnerException.Message) : ex.Message;
                return Json(new { success = false, message = "Lỗi SQL: " + innerMessage });
            }
        }
        [HttpPost]
        public JsonResult XoaCauHoiKiemTra(int id)
        {
            try
            {
                var ch = db.cau_hoi_kiem_tra.Find(id);
                if (ch != null) { db.cau_hoi_kiem_tra.Remove(ch); db.SaveChanges(); }
                return Json(new { success = true });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }
    }
}