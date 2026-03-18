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
        public ActionResult dangnhap(string TenDN, string MatKhau, bool? RememberMe)
        {
            var user = db.nguoi_dung
                  .FirstOrDefault(x => x.ten_dang_nhap == TenDN && x.mat_khau == MatKhau);

            if (user != null)
            {
                Session["TenDN"] = user.ten_dang_nhap;
                Session["HoTen"] = user.ho_va_ten;

                if (RememberMe == true)
                {
                    HttpCookie ck = new HttpCookie("UserLogin");
                    ck["TenDN"] = user.ten_dang_nhap;

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