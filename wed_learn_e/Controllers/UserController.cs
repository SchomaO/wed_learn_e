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
        public ActionResult dangky(NGUOIDUNG nd)
        {
            var kt = db.NGUOIDUNGs.FirstOrDefault(x => x.TenDN == nd.TenDN);
            if (kt != null)
            {
                ModelState.AddModelError("TenDN", "Tên đăng nhập đã tồn tại");
            }
            var ktEmail = db.NGUOIDUNGs.FirstOrDefault(x => x.Email == nd.Email);
            if (ktEmail != null)
            {
                ModelState.AddModelError("Email", "Email đã được sử dụng");
            }
            if (ModelState.IsValid)
            {
                db.NGUOIDUNGs.Add(nd);
                db.SaveChanges();
                return RedirectToAction("dangnhap");
            }

            return View(nd);
        }
        // GET đăng nhập
        public ActionResult dangnhap()
        {
            return View();
        }

        // POST đăng nhập
        [HttpPost]
        public ActionResult dangnhap(string TenDN, string MatKhau, bool? RememberMe)
        {
            var user = db.NGUOIDUNGs
                         .FirstOrDefault(x => x.TenDN == TenDN && x.MatKhau == MatKhau);

            if (user != null)
            {
                Session["TenDN"] = user.TenDN;
                Session["HoTen"] = user.HoTen;
                
                if (RememberMe == true)
                {
                    HttpCookie ck = new HttpCookie("UserLogin");
                    ck["TenDN"] = user.TenDN;
                 
                    ck.Expires = DateTime.Now.AddDays(7); // nhớ 7 ngày
                    Response.Cookies.Add(ck);

                }

                return RedirectToAction("khoadaotao", "Page");
            }
            else
            {
                ViewBag.ThongBao = "Tên đăng nhập hoặc mật khẩu sai";
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
    }
}