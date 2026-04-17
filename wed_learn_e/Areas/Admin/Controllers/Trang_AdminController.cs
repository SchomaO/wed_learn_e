using System;
using System.Collections.Generic;
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
    }
}