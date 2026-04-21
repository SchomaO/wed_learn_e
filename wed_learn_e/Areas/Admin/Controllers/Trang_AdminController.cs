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
            // Chặn người chưa đăng nhập hoặc không phải admin
            if (Session["VaiTro"] == null || Session["VaiTro"].ToString() != "quan_tri_vien")
            {
                return RedirectToAction("DangNhap", "User", new { area = "" });
                // area = "" nghĩa là đá ngược ra ngoài phần User bình thường
            }
            ViewBag.ho_va_ten = Session["HoTen"];
            var users = db.nguoi_dung.ToList();

            // 4. Truyền biến 'users' vào View
            return View(users);
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

                // SỬA LỖI NGỮ PHÁP: Ngữ pháp đi theo id_cap_do chứ không phải ids của khóa học
                var nguphap = db.ngu_phap.Where(x => x.id_cap_do == id)
                               .Select(x => new { x.id_ngu_phap, x.tieu_de, x.cach_dung, x.cong_thuc, x.vi_du })
                               .ToList();

                return Json(new
                {
                    success = true,
                    data = new { noi, nghe, video, viet, tu_vung = tuvung, ngu_phap = nguphap }
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
    }
}