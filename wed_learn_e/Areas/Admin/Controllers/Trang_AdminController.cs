using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace wed_learn_e.Areas.Admin.Controllers
{
    public class Trang_AdminController : Controller
    {
        // GET: Admin/Trang_Admin
        public ActionResult Index()
        {
            // Chặn người chưa đăng nhập hoặc không phải admin
            if (Session["VaiTro"] == null || Session["VaiTro"].ToString() != "quan_tri_vien")
            {
                return RedirectToAction("DangNhap", "User", new { area = "" });
                // area = "" nghĩa là đá ngược ra ngoài phần User bình thường
            }

            return View();
        }
    }
}