using Microsoft.Ajax.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web;
using System.Web.Mvc;
using wed_learn_e.Models;
using System.Web.Script.Serialization;
namespace wed_learn_e.Controllers
{
    public class UserController : Controller
    {
        wed_learn_eEntities db = new wed_learn_eEntities();
       
        // đăng ký
        public ActionResult dangky()
        {
            return View();
        }

        [HttpPost]
        public ActionResult dangky(string ho_va_ten, string ten_dang_nhap, string mat_khau, string MatKhauNL, string email)
        {
            if (HttpContext.Application["ChoPhepDangKy"] != null && (bool)HttpContext.Application["ChoPhepDangKy"] == false)
            {
                return Json(new
                {
                    success = false,
                    message = "⚠️ Hiện tại hệ thống đang tạm đóng chức năng đăng ký để bảo trì. Vui lòng quay lại sau!"
                });
            }
            try
            {
                // 1. KIỂM TRA MẬT KHẨU
                if (mat_khau != MatKhauNL)
                {
                    // Thêm trường field = "MatKhauNL"
                    return Json(new { success = false, field = "MatKhauNL", message = "❌ Mật khẩu nhập lại không giống nhau!" });
                }

                // 2. KIỂM TRA TÊN ĐĂNG NHẬP
                var checkUsername = db.nguoi_dung.FirstOrDefault(s => s.ten_dang_nhap == ten_dang_nhap);
                if (checkUsername != null)
                {
                    return Json(new { success = false, message = "❌ Tên đăng nhập này đã có người dùng!" });
                }

                // ---> THÊM ĐOẠN NÀY VÀO ĐỂ KIỂM TRA TRÙNG EMAIL <---
                var checkEmail = db.nguoi_dung.FirstOrDefault(s => s.email == email);
                if (checkEmail != null)
                {
                    return Json(new { success = false, message = "❌ Email này đã được sử dụng cho tài khoản khác!" });
                }

                // 3. LƯU DATABASE NẾU ĐÚNG HẾT
                nguoi_dung newUser = new nguoi_dung();
                newUser.ho_va_ten = ho_va_ten;
                newUser.ten_dang_nhap = ten_dang_nhap;
                newUser.mat_khau = mat_khau;
                newUser.email = email;
                newUser.vai_tro = "nguoi_dung";
                newUser.ngay_tao = DateTime.Now;
                newUser.so_luong_khoa_hoc = 0;
                newUser.loai_tai_khoan = 1;

                db.nguoi_dung.Add(newUser);
                db.SaveChanges();

                // 5. GỌI HÀM GỬI MAIL KHI ĐÃ LƯU THÀNH CÔNG
                bool isMailSent = GuiMailChaoMung(email, ho_va_ten, ten_dang_nhap);

                if (isMailSent)
                {
                    return Json(new { success = true, message = "✔ Đăng ký thành công! Vui lòng kiểm tra Email. Đang chuyển hướng..." });
                }
                else
                {
                    // Tài khoản đã lưu DB, nhưng mail bị lỗi (ví dụ Google đổi chính sách bảo mật)
                    return Json(new { success = true, message = "✔ Đăng ký thành công! (Không thể gửi mail xác nhận lúc này). Đang chuyển hướng..." });
                }
            }
            catch (Exception ex)
            {
                string loiChiTiet = ex.Message;
                if (ex.InnerException != null) loiChiTiet = ex.InnerException.Message;
                return Json(new { success = false, message = "❌ Lỗi hệ thống: " + loiChiTiet });
            }
        }
        // Hàm hỗ trợ gửi mail (đặt là private vì chỉ dùng nội bộ trong Controller này)
        private bool GuiMailChaoMung(string emailNhan, string hoTen, string tenDangNhap)
        {
            try
            {
                string senderEmail = "2424802010340@student.tdmu.edu.vn";
                string senderPassword = "bgpw qezn lsvu biyn";

                MailMessage mail = new MailMessage();
                mail.From = new MailAddress(senderEmail, "Hệ thống KityLearn");
                mail.To.Add(emailNhan);
                mail.Subject = "🎉 Chào mừng bạn gia nhập KityLearn!";

                mail.Body = $"<h3>Xin chào {hoTen},</h3>" +
                            $"<p>Chúc mừng bạn đã tạo tài khoản thành công trên hệ thống KityLearn.</p>" +
                            $"<p>Tên đăng nhập của bạn là: <b>{tenDangNhap}</b></p>" +
                            $"<p>Hãy đăng nhập ngay để trải nghiệm các khóa học và bài test thú vị nhé.</p>" +
                            $"<p>Trân trọng,<br>Đội ngũ KityLearn.</p>";
                mail.IsBodyHtml = true;

                SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587);
                smtp.Credentials = new NetworkCredential(senderEmail, senderPassword);
                smtp.EnableSsl = true;

                smtp.Send(mail);

                return true; // Báo gửi mail thành công
            }
            catch (Exception)
            {
                return false; // Báo gửi mail thất bại (sai pass, mất mạng...)
            }
        }
        public ActionResult dangnhap()
        {
            return View();
        }
        // Hàm hỗ trợ tự động kiểm tra và tước quyền VIP nếu hết hạn
        private void KiemTraVaThuHoiVIP(nguoi_dung user)
        {
            // CẬP NHẬT: Kiểm tra nếu là VIP loại 2 (Tháng) HOẶC loại 3 (Năm)
            if ((user.loai_tai_khoan == 2 || user.loai_tai_khoan == 3) && user.ngay_het_han_vip != null)
            {
                // Nếu ngày hiện tại đã vượt qua ngày hết hạn
                if (DateTime.Now > user.ngay_het_han_vip)
                {
                    user.loai_tai_khoan = 1;      // Đưa về tài khoản thường (1)
                    user.ngay_het_han_vip = null; // Xóa ngày hết hạn đi

                    db.SaveChanges(); // Lưu cập nhật xuống Database
                }
            }
        }
        // POST đăng nhập
        [HttpPost]
        public ActionResult DangNhap(string ten_dang_nhap, string mat_khau)
        {
            var user = db.nguoi_dung.FirstOrDefault(u => u.ten_dang_nhap == ten_dang_nhap && u.mat_khau == mat_khau);

            if (user != null)
            {
                string urlChuyenHuong = "";

                // TÁCH BIỆT SESSION DỰA TRÊN VAI TRÒ
                if (user.vai_tro == "quan_tri_vien")
                {
                    // 1. NẾU LÀ ADMIN: Tạo Session có tiền tố "Admin_"
                    Session["Admin_ID"] = user.id_nguoi_dung;
                    Session["Admin_TenDN"] = user.ten_dang_nhap;
                    Session["Admin_HoTen"] = user.ho_va_ten;
                    Session["Admin_VaiTro"] = user.vai_tro;

                    // Chuyển hướng về Admin
                    urlChuyenHuong = Url.Action("Index", "Trang_Admin", new { area = "Admin" });
                }
                else
                {
                    // 2. NẾU LÀ HỌC VIÊN: Tạo Session bình thường như cũ
                    KiemTraVaThuHoiVIP(user);
                    Session["user_id"] = user.id_nguoi_dung;
                    Session["TenDN"] = user.ten_dang_nhap;
                    Session["HoTen"] = user.ho_va_ten;
                    Session["VaiTro"] = user.vai_tro;
                    Session["Email"] = user.email;
                    Session["Avatar"] = user.anh_dai_dien;

                    string[] bangMau = { "1a73e8", "f39c12", "28a745", "dc3545", "6f42c1", "e83e8c" };
                    Session["AvatarColor"] = bangMau[user.id_nguoi_dung % bangMau.Length];

                    if (user.id_cap_do_hien_tai != null)
                    {
                        Session["id_cap_do_hien_tai"] = user.id_cap_do_hien_tai;
                    }

                    // Chuyển hướng về User
                    urlChuyenHuong = Url.Action("Index", "Trang_chu");
                }

                return Json(new { success = true, redirectUrl = urlChuyenHuong });
            }
            else
            {
                return Json(new { success = false, message = "❌ Tài khoản hoặc mật khẩu không chính xác!" });
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

            // 1. Lấy các Cấp độ mà người dùng đã đăng ký (có trong bảng tien_do_hoc_tap)
            var listIdKhoaHocDaDangKy = db.tien_do_hoc_tap
                                          .Where(t => t.id_nguoi_dung == userId && t.id_khoa_hoc != null)
                                          .Select(t => (int)t.id_khoa_hoc)
                                          .ToList();

            var listIdCapDoDaDangKy = db.khoa_hoc
                                        .Where(k => listIdKhoaHocDaDangKy.Contains(k.id_khoa_hoc) && k.id_cap_do != null)
                                        .Select(k => (int)k.id_cap_do)
                                        .Distinct()
                                        .ToList();

            var danhSachCapDo = db.cap_do
                                  .Where(c => listIdCapDoDaDangKy.Contains(c.id_cap_do))
                                  .OrderBy(c => c.id_cap_do)
                                  .ToList();

            Dictionary<int, string> dictTrangThai = new Dictionary<int, string>();
            Dictionary<int, decimal> dictPhanTram = new Dictionary<int, decimal>();
            Dictionary<int, int> dictTongBai = new Dictionary<int, int>();
            Dictionary<int, int> dictDaLam = new Dictionary<int, int>();
            Dictionary<int, List<string>> dictChiTietDaLam = new Dictionary<int, List<string>>();
            Dictionary<int, List<string>> dictChiTietChuaLam = new Dictionary<int, List<string>>();

            foreach (var cap in danhSachCapDo)
            {
                int idCap = cap.id_cap_do;

                // Lấy toàn bộ khóa học thuộc cấp độ này để không bị sót kỹ năng nào
                var khoaHocIds = db.khoa_hoc
                                   .Where(k => k.id_cap_do == idCap)
                                   .Select(k => k.id_khoa_hoc)
                                   .ToList();

                // 1. LUYỆN NGHE
                var dsNghe = db.bai_luyen_nghe.Where(b => b.id_khoa_hoc != null && khoaHocIds.Contains((int)b.id_khoa_hoc)).Select(b => b.id_bai_nghe).ToList();
                int ngheDaLam = db.lich_su_luyen_nghe.Where(ls => ls.id_nguoi_dung == userId && ls.id_bai_nghe != null && dsNghe.Contains((int)ls.id_bai_nghe)).Select(ls => ls.id_bai_nghe).Distinct().Count();

                // 2. LUYỆN NÓI
                var dsNoi = db.bai_luyen_noi.Where(b => b.id_khoa_hoc != null && khoaHocIds.Contains((int)b.id_khoa_hoc)).Select(b => b.id_bai_noi).ToList();
                int noiDaLam = db.lich_su_luyen_noi.Where(ls => ls.id_nguoi_dung == userId && ls.id_bai_noi != null && dsNoi.Contains((int)ls.id_bai_noi)).Select(ls => ls.id_bai_noi).Distinct().Count();

                // 3. LUYỆN VIẾT
                var dsViet = db.bai_luyen_viet.Where(b => b.id_khoa_hoc != null && khoaHocIds.Contains((int)b.id_khoa_hoc)).Select(b => b.id_bai_viet).ToList();
                int vietDaLam = db.lich_su_luyen_viet.Where(ls => ls.id_nguoi_dung == userId && ls.id_bai_viet != null && dsViet.Contains((int)ls.id_bai_viet)).Select(ls => ls.id_bai_viet).Distinct().Count();

                // 4. HỌC QUA VIDEO
                var dsVideo = db.bai_giang_video.Where(b => b.id_khoa_hoc != null && khoaHocIds.Contains((int)b.id_khoa_hoc)).Select(b => b.id_video).ToList();
                int videoDaLam = db.lich_su_video.Where(ls => ls.id_nguoi_dung == userId && ls.id_video != null && dsVideo.Contains((int)ls.id_video)).Select(ls => ls.id_video).Distinct().Count();

                // 5. BÀI KIỂM TRA TIẾN ĐỘ (Cần đạt >= 70%)
                var ketQuaThi = db.ket_qua_kiem_tra_tien_do
                                  .Where(k => k.id_nguoi_dung == userId && k.id_cap_do == idCap)
                                  .ToList();
                bool daDatThi = ketQuaThi.Any(k => k.dat_yeu_cau);
                decimal diemCaoNhat = ketQuaThi.Any() ? ketQuaThi.Max(k => k.phan_tram) : 0;

                // Tổng hợp chi tiết hiển thị cho từng kỹ năng
                List<string> chiTietDaLam = new List<string>();
                List<string> chiTietChuaLam = new List<string>();

                // Luyện Nghe
                if (dsNghe.Count > 0)
                {
                    if (ngheDaLam >= dsNghe.Count) chiTietDaLam.Add($"🎧 Luyện Nghe: {ngheDaLam}/{dsNghe.Count} bài");
                    else chiTietChuaLam.Add($"🎧 Luyện Nghe: {ngheDaLam}/{dsNghe.Count} bài");
                }

                // Luyện Nói
                if (dsNoi.Count > 0)
                {
                    if (noiDaLam >= dsNoi.Count) chiTietDaLam.Add($"🗣️ Luyện Nói: {noiDaLam}/{dsNoi.Count} bài");
                    else chiTietChuaLam.Add($"🗣️ Luyện Nói: {noiDaLam}/{dsNoi.Count} bài");
                }

                // Luyện Viết
                if (dsViet.Count > 0)
                {
                    if (vietDaLam >= dsViet.Count) chiTietDaLam.Add($"📝 Luyện Viết: {vietDaLam}/{dsViet.Count} bài");
                    else chiTietChuaLam.Add($"📝 Luyện Viết: {vietDaLam}/{dsViet.Count} bài");
                }

                // Video
                if (dsVideo.Count > 0)
                {
                    if (videoDaLam >= dsVideo.Count) chiTietDaLam.Add($"🎬 Học Qua Video: {videoDaLam}/{dsVideo.Count} bài");
                    else chiTietChuaLam.Add($"🎬 Học Qua Video: {videoDaLam}/{dsVideo.Count} bài");
                }

                // Bài Kiểm Tra Tiến Độ
                if (daDatThi)
                {
                    chiTietDaLam.Add($"🎓 Kiểm Tra Tiến Độ: Đã đạt ({diemCaoNhat}%)");
                }
                else
                {
                    string ghiChuDiem = ketQuaThi.Any() ? $"Cao nhất: {diemCaoNhat}% - Cần ≥ 70%" : "Chưa làm - Cần ≥ 70%";
                    chiTietChuaLam.Add($"🎓 Kiểm Tra Tiến Độ ({ghiChuDiem})");
                }

                // Tính tổng số bài (4 kỹ năng chính + 1 bài kiểm tra tiến độ)
                int tongBaiCapDo = dsNghe.Count + dsNoi.Count + dsViet.Count + dsVideo.Count + 1;
                int daLamCapDo = ngheDaLam + noiDaLam + vietDaLam + videoDaLam + (daDatThi ? 1 : 0);

                dictTongBai[idCap] = tongBaiCapDo;
                dictDaLam[idCap] = daLamCapDo;
                dictChiTietDaLam[idCap] = chiTietDaLam;
                dictChiTietChuaLam[idCap] = chiTietChuaLam;

                if (daLamCapDo == 0)
                {
                    dictTrangThai[idCap] = "chua_bat_dau";
                    dictPhanTram[idCap] = 0;
                }
                else if (daLamCapDo < tongBaiCapDo)
                {
                    dictTrangThai[idCap] = "dang_hoc";
                    dictPhanTram[idCap] = Math.Round((decimal)daLamCapDo / tongBaiCapDo * 100, 1);
                }
                else
                {
                    dictTrangThai[idCap] = "da_hoan_thanh";
                    dictPhanTram[idCap] = 100;
                }
            }

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
        [HttpPost]
        public ActionResult CapNhatThongTin(string ho_va_ten, string email)
        {
            // Kiểm tra đăng nhập
            if (Session["user_id"] == null)
            {
                return RedirectToAction("dangnhap");
            }

            int userId = Convert.ToInt32(Session["user_id"]);
            var user = db.nguoi_dung.Find(userId);

            if (user != null)
            {
                // Cập nhật Database
                user.ho_va_ten = ho_va_ten;
                user.email = email;
                db.SaveChanges();

                // Cập nhật lại Session để Header hiển thị đúng tên/email mới
                Session["HoTen"] = ho_va_ten;
                Session["Email"] = email;

                TempData["ThanhCongThongTin"] = "Cập nhật thông tin cá nhân thành công!";
            }
            else
            {
                TempData["LoiThongTin"] = "Lỗi hệ thống, không tìm thấy tài khoản!";
            }

            // Đánh dấu Tab Thông tin là tab đang mở để khi load lại trang không bị nhảy sang tab khác
            TempData["ActiveTab"] = "thong-tin";

            return RedirectToAction("TrangCaNhan");
        }
        [HttpPost]
        public ActionResult CapNhatAvatar(HttpPostedFileBase file_anh)
        {
            // Kiểm tra đăng nhập
            if (Session["user_id"] == null) return RedirectToAction("dangnhap");

            int userId = Convert.ToInt32(Session["user_id"]);
            var user = db.nguoi_dung.Find(userId);

            // Kiểm tra xem có file gửi lên không
            if (user != null && file_anh != null && file_anh.ContentLength > 0)
            {
                string folderPath = Server.MapPath("~/Images/Avatars/");
                if (!System.IO.Directory.Exists(folderPath))
                {
                    System.IO.Directory.CreateDirectory(folderPath);
                }

                string fileName = DateTime.Now.Ticks.ToString() + "_" + System.IO.Path.GetFileName(file_anh.FileName);
                string fullPath = System.IO.Path.Combine(folderPath, fileName);

                // Lưu ảnh
                file_anh.SaveAs(fullPath);
                user.anh_dai_dien = fileName;
                db.SaveChanges();

                // Cập nhật lại Session
                Session["Avatar"] = fileName;
                TempData["ThanhCongThongTin"] = "Đã cập nhật ảnh đại diện mới!";
            }

            // Đổi ảnh xong thì tự load lại đúng trang cá nhân
            return RedirectToAction("TrangCaNhan");
        }
        // GET: Hiển thị giao diện trang Quên mật khẩu riêng biệt
        public ActionResult QuenMatKhau()
        {
            return View();
        }
        [HttpPost]
        public ActionResult QuenMatKhau(string email)
        {
            try
            {
                // 1. Kiểm tra Email có tồn tại trong hệ thống không?
                var user = db.nguoi_dung.FirstOrDefault(u => u.email == email);
                if (user == null)
                {
                    return Json(new { success = false, message = "❌ Email này không tồn tại trong hệ thống!" });
                }

                // 2. Cấp mật khẩu mặc định mới
                user.mat_khau = "123456";
                db.SaveChanges(); // Lưu mật khẩu mới vào Database

                // 3. Tiến hành gửi mail thông báo
                string senderEmail = "2424802010340@student.tdmu.edu.vn";
                string senderPassword = "bgpw qezn lsvu biyn";

                MailMessage mail = new MailMessage();
                mail.From = new MailAddress(senderEmail, "Hệ thống KityLearn");
                mail.To.Add(email);
                mail.Subject = "🔐 Khôi phục mật khẩu KityLearn";

                // Nội dung mail
                mail.Body = $"<h3>Xin chào {user.ho_va_ten},</h3>" +
                            $"<p>Hệ thống đã nhận được yêu cầu cấp lại mật khẩu của bạn.</p>" +
                            $"<p>Mật khẩu mới của bạn đã được đặt lại thành: <b style='color:red; font-size: 18px;'>123456</b></p>" +
                            $"<p>Vui lòng đăng nhập và đổi lại mật khẩu cá nhân ngay để bảo mật tài khoản nhé!</p>" +
                            $"<p>Trân trọng,<br>Đội ngũ KityLearn.</p>";
                mail.IsBodyHtml = true;

                SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587);
                smtp.Credentials = new NetworkCredential(senderEmail, senderPassword);
                smtp.EnableSsl = true;

                smtp.Send(mail); // Thực thi gửi mail

                return Json(new { success = true, message = "✔ Đã gửi mật khẩu mới về Email của bạn!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "❌ Hệ thống gửi mail đang gặp sự cố: " + ex.Message });
            }
        }
        [HttpPost]
        public JsonResult DangNhapGoogle(string credential)
        {
            if (string.IsNullOrEmpty(credential))
                return Json(new { success = false, message = "❌ Không nhận được token từ Google!" });

            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                using (var client = new WebClient())
                {
                    client.Encoding = System.Text.Encoding.UTF8;
                    string json = client.DownloadString("https://oauth2.googleapis.com/tokeninfo?id_token=" + credential);

                    var serializer = new JavaScriptSerializer();
                    var payload = serializer.Deserialize<Dictionary<string, object>>(json);

                    string email = payload.ContainsKey("email") ? payload["email"].ToString() : null;
                    string hoTen = payload.ContainsKey("name") ? payload["name"].ToString() : "Học viên Google";

                    if (string.IsNullOrEmpty(email))
                        return Json(new { success = false, message = "❌ Không lấy được email từ tài khoản Google!" });

                    var user = db.nguoi_dung.FirstOrDefault(u => u.email == email);
                    if (user == null)
                    {
                        string baseUsername = email.Split('@')[0];
                        string username = baseUsername;
                        int count = 1;
                        while (db.nguoi_dung.Any(u => u.ten_dang_nhap == username))
                        {
                            username = baseUsername + count;
                            count++;
                        }

                        user = new nguoi_dung
                        {
                            ten_dang_nhap = username,
                            mat_khau = Guid.NewGuid().ToString("N").Substring(0, 10),
                            ho_va_ten = hoTen,
                            email = email,
                            vai_tro = "nguoi_dung",
                            ngay_tao = DateTime.Now,
                            so_luong_khoa_hoc = 0,
                            loai_tai_khoan = 1
                        };

                        db.nguoi_dung.Add(user);
                        db.SaveChanges();
                    }

                    KiemTraVaThuHoiVIP(user);
                    Session["user_id"] = user.id_nguoi_dung;
                    Session["TenDN"] = user.ten_dang_nhap;
                    Session["HoTen"] = user.ho_va_ten;
                    Session["VaiTro"] = user.vai_tro;
                    Session["Email"] = user.email;
                    Session["Avatar"] = user.anh_dai_dien;

                    string[] bangMau = { "1a73e8", "f39c12", "28a745", "dc3545", "6f42c1", "e83e8c" };
                    Session["AvatarColor"] = bangMau[user.id_nguoi_dung % bangMau.Length];

                    if (user.id_cap_do_hien_tai != null)
                        Session["id_cap_do_hien_tai"] = user.id_cap_do_hien_tai;

                    return Json(new { success = true, redirectUrl = Url.Action("Index", "Trang_chu") });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "❌ Lỗi xác thực Google: " + ex.Message });
            }
        }

    }
}