using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using wed_learn_e.Models;
// 1. Thêm dòng này lên tít trên cùng của file (dưới các dòng using khác)
using System.Data.Entity;
using PagedList;
using PagedList.Mvc;


namespace wed_learn_e.Controllers
{
    public class Trang_chuController : Controller
    {
        wed_learn_eEntities db = new wed_learn_eEntities();

        // 2. Thêm tham số int? page vào hàm
        public ActionResult Index(int? page)
        {
            // Nếu không truyền số trang (mới vào) thì mặc định là trang 1
            int pageNumber = (page ?? 1);
            int pageSize = 3; // Cài đặt hiển thị 3 bình luận / 1 trang
           
            // Lấy dữ liệu và cắt trang
            var listBinhLuan = db.binh_luan
                                 .Include(b => b.nguoi_dung)
                                 .Include(b => b.cap_do)
                                 .Where(b => b.trang_thai == true)
                                 .OrderBy(b => b.ngay_tao) // Lệnh này giúp sắp xếp TỪ CŨ ĐẾN MỚI
                                 .ToPagedList(pageNumber, pageSize); // Chia thành từng trang

            // Đưa ra ViewBag
            ViewBag.DanhSachBinhLuan = listBinhLuan;

            return View();
        }

        // ... các hàm khác giữ nguyên ...
        // 1. Mở trang Liên Hệ
        public ActionResult LienHe()
        {
            return View();
        }

        // 2. Nhận dữ liệu AJAX khi người dùng bấm Gửi

        [HttpPost]
        public JsonResult GuiLienHe(string ho_ten, string email, string chu_de, string noi_dung)
        {
            try
            {
                // 1. KIỂM TRA ĐĂNG NHẬP: Bảng binh_luan bắt buộc phải có id_nguoi_dung
                if (Session["user_id"] == null)
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập tài khoản trước khi gửi yêu cầu hỗ trợ!" });
                }

                int userId = Convert.ToInt32(Session["user_id"]);

                // (ĐÃ XÓA PHẦN GỘP CHUỖI Ở ĐÂY)

                // 3. LƯU VÀO DATABASE
                db.binh_luan.Add(new binh_luan
                {
                    id_nguoi_dung = userId,
                    id_cap_do = 1, // Bắt buộc có do khóa ngoại (Bạn phải chắc chắn DB bảng cap_do có ID = 1)
                    chu_de = chu_de,

                    // LƯU ĐÚNG NỘI DUNG NGƯỜI DÙNG GÕ VÀO
                    noi_dung = noi_dung,

                    ngay_tao = DateTime.Now,
                    trang_thai = true // false để bình luận này bị ẩn, KHÔNG hiển thị ra ngoài trang chủ
                });

                db.SaveChanges();

                return Json(new { success = true, message = "Cảm ơn bạn! Yêu cầu hỗ trợ đã được gửi thành công. Chúng tôi sẽ phản hồi sớm nhất." });
            }
            catch (Exception ex)
            {
                // Hiển thị lỗi ex.Message nếu Database có vấn đề để dễ fix
                return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }
    }
}