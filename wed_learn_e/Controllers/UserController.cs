using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using wed_learn_e.Models;
using System.Net;
using System.Net.Mail;
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

        // POST đăng nhập
        [HttpPost]
        public ActionResult DangNhap(string ten_dang_nhap, string mat_khau)
        {
            // Tìm người dùng có tài khoản và mật khẩu khớp với dữ liệu nhập vào
            var user = db.nguoi_dung.FirstOrDefault(u => u.ten_dang_nhap == ten_dang_nhap && u.mat_khau == mat_khau);

            if (user != null)
            {
                // 1. Lưu các thông tin cần thiết vào Session
                Session["user_id"] = user.id_nguoi_dung;
                Session["TenDN"] = user.ten_dang_nhap;
                Session["HoTen"] = user.ho_va_ten;
                Session["VaiTro"] = user.vai_tro; // Quan trọng: Lưu lại vai trò để các trang khác kiểm tra
                Session["Email"] = user.email;
                Session["Avatar"] = user.anh_dai_dien;

                string[] bangMau = { "1a73e8", "f39c12", "28a745", "dc3545", "6f42c1", "e83e8c" };
                // Dùng ID của user chia lấy dư để chọn màu -> Đảm bảo màu luôn cố định với user đó!
                Session["AvatarColor"] = bangMau[user.id_nguoi_dung % bangMau.Length];

                // Lấy cấp độ hiện tại nếu có (Dành cho User)
                if (user.id_cap_do_hien_tai != null)
                {
                    Session["id_cap_do_hien_tai"] = user.id_cap_do_hien_tai;
                }

                // 2. BẺ LÁI DỰA TRÊN VAI TRÒ (PHÂN QUYỀN) - XỬ LÝ CHO AJAX
                string urlChuyenHuong = "";

                if (user.vai_tro == "quan_tri_vien")
                {
                    // Tạo link tới trang Admin
                    urlChuyenHuong = Url.Action("Index", "Trang_Admin", new { area = "Admin" });
                }
                else
                {
                    // Tạo link tới Trang Chủ cho User bình thường
                    urlChuyenHuong = Url.Action("Index", "Trang_chu");
                }

                // Trả về JSON thông báo thành công kèm đường link để Javascript tự động chuyển trang
                return Json(new { success = true, redirectUrl = urlChuyenHuong });
            }
            else
            {
                // Đăng nhập thất bại -> Trả về JSON báo lỗi để Javascript hiển thị chữ đỏ
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

            // =========================================================================
            // YÊU CẦU 1: CHỈ LẤY NHỮNG CẤP ĐỘ MÀ NGƯỜI DÙNG ĐÃ ĐĂNG KÝ (CÓ TRONG TIẾN ĐỘ)
            // =========================================================================
            // Lấy ID các khóa học đã đăng ký
            var listIdKhoaHocDaDangKy = db.tien_do_hoc_tap
                                          .Where(t => t.id_nguoi_dung == userId)
                                          .Select(t => t.id_khoa_hoc)
                                          .ToList();

            // Truy ngược ra ID Cấp độ của các khóa học đó
            var listIdCapDoDaDangKy = db.khoa_hoc
                                        .Where(k => listIdKhoaHocDaDangKy.Contains(k.id_khoa_hoc))
                                        .Select(k => k.id_cap_do)
                                        .Distinct()
                                        .ToList();

            // Chỉ lấy danh sách Cấp độ thỏa mãn ID ở trên
            var danhSachCapDo = db.cap_do.Where(c => listIdCapDoDaDangKy.Contains(c.id_cap_do)).ToList();

            // Các "rổ" chứa dữ liệu của từng Cấp Độ truyền ra View
            Dictionary<int, string> dictTrangThai = new Dictionary<int, string>();
            Dictionary<int, decimal> dictPhanTram = new Dictionary<int, decimal>();
            Dictionary<int, int> dictTongBai = new Dictionary<int, int>();
            Dictionary<int, int> dictDaLam = new Dictionary<int, int>();
            Dictionary<int, List<string>> dictChiTietDaLam = new Dictionary<int, List<string>>();
            Dictionary<int, List<string>> dictChiTietChuaLam = new Dictionary<int, List<string>>();

            foreach (var cap in danhSachCapDo)
            {
                int idCap = cap.id_cap_do;

                // Tìm tất cả Khóa học thuộc Cấp độ này (Nhưng chỉ lấy khóa đã đăng ký)
                var listKhoa = db.khoa_hoc.Where(k => k.id_cap_do == idCap && listIdKhoaHocDaDangKy.Contains(k.id_khoa_hoc)).ToList();

                int tongBaiCapDo = 0;
                int daLamCapDo = 0;

                // =========================================================================
                // YÊU CẦU 2: TẠO CÁC BIẾN ĐẾM (GOM NHÓM KỸ NĂNG THAY VÌ LIỆT KÊ TÊN BÀI)
                // =========================================================================
                int vietDaLam = 0, vietChuaLam = 0;
                int sapXepDaLam = 0, sapXepChuaLam = 0;
                int ngheDaLam = 0, ngheChuaLam = 0;
                int videoDaLam = 0, videoChuaLam = 0;
                int noiDaLam = 0, noiChuaLam = 0;

                foreach (var khoa in listKhoa)
                {
                    int idKhoaHienTai = khoa.id_khoa_hoc;

                    // 1. ĐẾM BÀI LUYỆN VIẾT
                    var listViet = db.bai_luyen_viet.Where(b => b.id_khoa_hoc == idKhoaHienTai).ToList();
                    foreach (var bai in listViet)
                    {
                        tongBaiCapDo++;
                        if (db.lich_su_luyen_viet.Any(ls => ls.id_nguoi_dung == userId && ls.id_bai_viet == bai.id_bai_viet))
                        { daLamCapDo++; vietDaLam++; }
                        else { vietChuaLam++; }
                    }

                    // 2. ĐẾM BÀI SẮP XẾP
                    var listSapXep = db.bai_sap_xep.Where(b => b.id_khoa_hoc == idKhoaHienTai).ToList();
                    foreach (var bai in listSapXep)
                    {
                        tongBaiCapDo++;
                        if (db.lich_su_sap_xep.Any(ls => ls.id_nguoi_dung == userId && ls.id_bai_sap_xep == bai.id_bai_sap_xep))
                        { daLamCapDo++; sapXepDaLam++; }
                        else { sapXepChuaLam++; }
                    }

                    // 3. ĐẾM BÀI LUYỆN NGHE
                    var listNghe = db.bai_luyen_nghe.Where(b => b.id_khoa_hoc == idKhoaHienTai).ToList();
                    foreach (var bai in listNghe)
                    {
                        tongBaiCapDo++;
                        if (db.lich_su_luyen_nghe.Any(ls => ls.id_nguoi_dung == userId && ls.id_bai_nghe == bai.id_bai_nghe))
                        { daLamCapDo++; ngheDaLam++; }
                        else { ngheChuaLam++; }
                    }

                    // 4. ĐẾM BÀI GIẢNG VIDEO
                    var listVideo = db.bai_giang_video.Where(b => b.id_khoa_hoc == idKhoaHienTai).ToList();
                    foreach (var bai in listVideo)
                    {
                        tongBaiCapDo++;
                        if (db.lich_su_video.Any(ls => ls.id_nguoi_dung == userId && ls.id_video == bai.id_video))
                        { daLamCapDo++; videoDaLam++; }
                        else { videoChuaLam++; }
                    }

                    // 5. ĐẾM BÀI LUYỆN NÓI
                    var listNoi = db.bai_luyen_noi.Where(b => b.id_khoa_hoc == idKhoaHienTai).ToList();
                    foreach (var bai in listNoi)
                    {
                        tongBaiCapDo++;
                        if (db.lich_su_luyen_noi.Any(ls => ls.id_nguoi_dung == userId && ls.id_bai_noi == bai.id_bai_noi))
                        { daLamCapDo++; noiDaLam++; }
                        else { noiChuaLam++; }
                    }
                }

                // =========================================================================
                // SAU KHI ĐẾM XONG -> XUẤT RA CHUỖI HIỂN THỊ NGẮN GỌN
                // =========================================================================
                List<string> chiTietDaLam = new List<string>();
                List<string> chiTietChuaLam = new List<string>();

                // Xuất danh sách Đã làm
                if (vietDaLam > 0) chiTietDaLam.Add($"📝 Hoàn thành {vietDaLam} bài Luyện Viết");
                if (sapXepDaLam > 0) chiTietDaLam.Add($"🧩 Hoàn thành {sapXepDaLam} bài Sắp Xếp");
                if (ngheDaLam > 0) chiTietDaLam.Add($"🎧 Hoàn thành {ngheDaLam} bài Luyện Nghe");
                if (videoDaLam > 0) chiTietDaLam.Add($"🎬 Hoàn thành {videoDaLam} Video");
                if (noiDaLam > 0) chiTietDaLam.Add($"🗣️ Hoàn thành {noiDaLam} bài Luyện Nói");

                // Xuất danh sách Chưa làm
                if (vietChuaLam > 0) chiTietChuaLam.Add($"📝 Còn {vietChuaLam} bài Luyện Viết");
                if (sapXepChuaLam > 0) chiTietChuaLam.Add($"🧩 Còn {sapXepChuaLam} bài Sắp Xếp");
                if (ngheChuaLam > 0) chiTietChuaLam.Add($"🎧 Còn {ngheChuaLam} bài Luyện Nghe");
                if (videoChuaLam > 0) chiTietChuaLam.Add($"🎬 Còn {videoChuaLam} Video");
                if (noiChuaLam > 0) chiTietChuaLam.Add($"🗣️ Còn {noiChuaLam} bài Luyện Nói");

                // Tính toán trạng thái cho thẻ Cấp Độ này
                dictTongBai[idCap] = tongBaiCapDo;
                dictDaLam[idCap] = daLamCapDo;
                dictChiTietDaLam[idCap] = chiTietDaLam;
                dictChiTietChuaLam[idCap] = chiTietChuaLam;

                if (tongBaiCapDo == 0 || daLamCapDo == 0)
                {
                    dictTrangThai[idCap] = "chua_bat_dau";
                    dictPhanTram[idCap] = 0;
                }
                else if (daLamCapDo < tongBaiCapDo)
                {
                    dictTrangThai[idCap] = "dang_hoc";
                    dictPhanTram[idCap] = Math.Round((decimal)daLamCapDo / tongBaiCapDo * 100, 2);
                }
                else
                {
                    dictTrangThai[idCap] = "da_hoan_thanh";
                    dictPhanTram[idCap] = 100;
                }
            }

            // Đẩy hết ra View
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
    }
}