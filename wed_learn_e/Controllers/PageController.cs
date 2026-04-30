using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.UI;
using wed_learn_e.Models;
using PagedList;

using System.Net;
using System.Net.Mail;
namespace wed_learn_e.Controllers
{
    public class PageController : Controller
    {
        // GET: bai_test
        wed_learn_eEntities db = new wed_learn_eEntities();
        // 1. SỬA LẠI HÀM NÀY
        public ActionResult thong_tin_khoa_hoc(int? id)
        {
            if (id == null) return RedirectToAction("khoadaotao");

            Session["id_cap_do_hien_tai"] = id;

            var capDo = db.cap_do.FirstOrDefault(x => x.id_cap_do == id);
            ViewBag.TenCapDo = capDo != null ? capDo.ten_cap_do : "Khóa Học";
            ViewBag.IdCapDo = id;

            // FIX: Đổi id_cap_do thành id. 
            // Thêm OrderBy và Skip(9) để bỏ qua 9 khóa học cũ, chỉ lấy các khóa tạo thêm phía sau.
            ViewBag.DanhSachKhoaHocMoi = db.khoa_hoc
                                           .Where(k => k.id_cap_do == id)
                                           .OrderBy(k => k.id_khoa_hoc) // Bắt buộc phải sắp xếp trước khi Skip
                                           .Skip(9)
                                           .ToList();
            return View();
        }

        // 2. THÊM HÀM MỚI NÀY ĐỂ HIỂN THỊ NỘI DUNG (CKEDITOR)
        public ActionResult NoiDungKhoaHoc(int id_khoa_hoc)
        {
            var khoaHoc = db.khoa_hoc.Find(id_khoa_hoc);
            ViewBag.TenKhoaHoc = khoaHoc != null ? khoaHoc.ten_khoa_hoc : "Bài học mới";

            // Lấy toàn bộ nội dung HTML Admin đã soạn
            var danhSachNoiDung = db.thong_tin_khoa_hoc.Where(t => t.id_khoa_hoc == id_khoa_hoc).ToList();

            return View(danhSachNoiDung);
        }
        public ActionResult Index()
        {
            return View();
        }
        public ActionResult khaosat()
        {
            return View();
        }
        public ActionResult nav_choncapdo()
        {
            return View();
        }
        public ActionResult khoadaotao()
        {
            var danhSachCapDo = db.cap_do.ToList();

            // 1. Khởi tạo danh sách các cấp độ đã đăng ký (ban đầu rỗng)
            List<int> listCapDoDaDangKy = new List<int>();

            // 2. Nếu người dùng đã đăng nhập, tiến hành tìm các khóa đã mua
            if (Session["user_id"] != null)
            {
                int userId = Convert.ToInt32(Session["user_id"]);

                // Tìm ID của các bài học nhỏ đã có trong bảng Tiến độ
                var idKhoaHocs = db.tien_do_hoc_tap
                                   .Where(t => t.id_nguoi_dung == userId)
                                   .Select(t => t.id_khoa_hoc)
                                   .ToList();

                // Từ bài học nhỏ, truy ngược ra ID Cấp độ (A1, A2...)
                if (idKhoaHocs.Any())
                {
                    listCapDoDaDangKy = db.khoa_hoc
                                          .Where(k => idKhoaHocs.Contains(k.id_khoa_hoc))
                                          .Select(k => k.id_cap_do ?? 0)
                                          .Distinct()
                                          .ToList();
                }
            }

            // 3. Ném danh sách này ra ViewBag để file HTML biết đường xử lý
            ViewBag.DaDangKy = listCapDoDaDangKy;

            return View(danhSachCapDo);
        }

        [HttpPost]
   
        public JsonResult DangKyKhoaHoc(int id_cap_do)
        {
            // 1. Kiểm tra xem người dùng đã đăng nhập chưa
            if (Session["user_id"] == null)
            {
                return Json(new { success = false, type = "not_logged_in", message = "Bạn cần đăng nhập để tham gia khóa học.", redirect = Url.Action("dangnhap", "User") });
            }

            int userId = Convert.ToInt32(Session["user_id"]);
            var user = db.nguoi_dung.FirstOrDefault(u => u.id_nguoi_dung == userId);

            if (user != null)
            {
                // =========================================================
                // FIX LỖI 1: BẢO VỆ TÀI KHOẢN CŨ BỊ THIẾU DỮ LIỆU
                // Nếu database cũ bị NULL, ép nó về bản Thường (1) và 0 khóa học
                // =========================================================
                if (user.loai_tai_khoan == null) user.loai_tai_khoan = 1;
                if (user.so_luong_khoa_hoc == null) user.so_luong_khoa_hoc = 0;


                // =========================================================
                // FIX LỖI 2: TRÁNH LỖI CRASH ENTITY FRAMEWORK (id_khoa_hoc)
                // =========================================================
                var listKhoaHocCuaCapDo = db.khoa_hoc
                                            .Where(k => k.id_cap_do == id_cap_do)
                                            .Select(k => (int?)k.id_khoa_hoc) // Ép kiểu an toàn
                                            .ToList();

                // Kiểm tra xem đã đăng ký chưa (So sánh trực tiếp, bỏ ?? 0)
                bool daDangKy = db.tien_do_hoc_tap.Any(t => t.id_nguoi_dung == userId && listKhoaHocCuaCapDo.Contains(t.id_khoa_hoc));

                if (daDangKy)
                {
                    // ĐÃ ĐĂNG KÝ RỒI -> Cho vào học luôn! KHÔNG đếm thêm.
                    Session["id_cap_do_hien_tai"] = id_cap_do;
                    return Json(new { success = true, redirect = Url.Action("thong_tin_khoa_hoc", "Page", new { id = id_cap_do }) });
                }


                // =========================================================
                // 3. KIỂM TRA GIỚI HẠN TÀI KHOẢN BẢN THƯỜNG (Mức chặn: 2 khóa)
                // =========================================================
                // =========================================================
                // 3. KIỂM TRA GIỚI HẠN TÀI KHOẢN BẢN THƯỜNG (ĐỌC TỪ FILE TXT)
                // =========================================================

                int gioiHanThuong = 2; // Mặc định là 2
                string limitPath = Server.MapPath("~/App_Data/limit.txt");

                // Mở file ra đọc xem Admin đang cài số mấy
                if (System.IO.File.Exists(limitPath))
                {
                    int.TryParse(System.IO.File.ReadAllText(limitPath), out gioiHanThuong);
                }

                // Kiểm tra xem học viên có vượt quá con số Admin vừa cài không
                if (user.loai_tai_khoan == 1 && user.so_luong_khoa_hoc >= gioiHanThuong)
                {
                    return Json(new
                    {
                        success = false,
                        type = "limit_reached",
                        message = $"Tài khoản của bạn là bản Thường, chỉ được đăng ký tối đa {gioiHanThuong} cấp độ.\n\nVui lòng nâng cấp lên tài khoản VIP để học không giới hạn!",
                        redirect = Url.Action("TrangMuaVIP", "Page") // Sửa lại Link nếu cần
                    });
                }    


                // =========================================================
                // 4. TIẾN HÀNH ĐĂNG KÝ MỚI (CHƯA VƯỢT GIỚI HẠN)
                // =========================================================
                user.so_luong_khoa_hoc += 1; // Đếm số lượng lên 1

                // Đổ dữ liệu vào bảng tiến độ
                foreach (var idKhoa in listKhoaHocCuaCapDo)
                {
                    if (idKhoa != null)
                    {
                        db.tien_do_hoc_tap.Add(new tien_do_hoc_tap
                        {
                            id_nguoi_dung = userId,
                            id_khoa_hoc = idKhoa,
                            phan_tram_hoan_thanh = 0,
                            trang_thai = "chua_bat_dau",
                            lan_truy_cap_cuoi = DateTime.Now
                        });
                    }
                }

                db.SaveChanges(); // Lưu vào SQL

                Session["id_cap_do_hien_tai"] = id_cap_do;

                // Trả về thành công để nhảy sang trang bài học
                return Json(new { success = true, redirect = Url.Action("thong_tin_khoa_hoc", "Page", new { id = id_cap_do }) });
            }

            return Json(new { success = false, type = "error", message = "Lỗi hệ thống!" });
        }

        // Hàm giả định cho Trang mua VIP (nơi bạn có thể tích hợp VNPAY)
        // 1. Hàm gọi giao diện Trang Mua VIP
        public ActionResult TrangMuaVIP()
        {
            // Bắt buộc phải đăng nhập mới được mua VIP
            if (Session["user_id"] == null) return RedirectToAction("dangnhap", "User");

            int userId = Convert.ToInt32(Session["user_id"]);
            var user = db.nguoi_dung.FirstOrDefault(u => u.id_nguoi_dung == userId);

            return View(user); // Truyền model user ra để điền sẵn tên vào form thanh toán
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
        // Hàm hỗ trợ gửi Email tự động
        private void GuiEmailThongBao(string emailNhan, string hoTen, string tenGoi, string phuongThuc, DateTime? hanSuDung)
        {
            try
            {
                // 1. Cấu hình Email gửi đi (Bạn cần thay bằng Email thật của bạn)
                string emailGui = "2424802010340@student.tdmu.edu.vn";
                string matKhauApp = "bgpw qezn lsvu biyn"; // Lưu ý: Không dùng mật khẩu đăng nhập bình thường
                
                var senderEmail = new MailAddress(emailGui, "Hệ Thống KityLearn");
                var receiverEmail = new MailAddress(emailNhan, hoTen);

                string subject = "🎉 Xác nhận nâng cấp VIP thành công - KityLearn";

                // 2. Nội dung Email (Được thiết kế bằng HTML cho đẹp)
                string body = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #ddd; border-radius: 10px; padding: 20px; box-shadow: 0 4px 8px rgba(0,0,0,0.05);'>
                        <h2 style='color: #0d6efd; text-align: center;'>Thanh toán thành công!</h2>
                        <p>Chào <b>{hoTen}</b>,</p>
                        <p>Cảm ơn bạn đã tin tưởng và nâng cấp tài khoản VIP tại hệ thống học tiếng Anh <b>KityLearn</b>. Chúng tôi xin xác nhận giao dịch của bạn đã hoàn tất.</p>
                        
                        <div style='background: #f4f9ff; padding: 15px; border-radius: 8px; margin: 20px 0;'>
                            <h3 style='margin-top:0; color: #1a73e8;'>📄 Thông tin giao dịch:</h3>
                            <ul style='list-style: none; padding: 0;'>
                                <li style='margin-bottom: 10px;'>💎 <b>Gói đăng ký:</b> {tenGoi}</li>
                                <li style='margin-bottom: 10px;'>💳 <b>Phương thức:</b> {phuongThuc}</li>
                                <li style='margin-bottom: 10px;'>⏳ <b>Thời hạn sử dụng:</b> {(hanSuDung.HasValue ? hanSuDung.Value.ToString("dd/MM/yyyy") : "Không thời hạn")}</li>
                            </ul>
                        </div>

                        <p>Bây giờ bạn đã có thể truy cập toàn bộ khóa học từ A1 đến C2 không giới hạn. Hãy bắt đầu hành trình chinh phục tiếng Anh ngay hôm nay!</p>
                        
                        <div style='text-align: center; margin-top: 30px;'>
                            <a href='https://localhost:44395/Page/khoadaotao' style='background: #28a745; color: white; padding: 12px 25px; text-decoration: none; border-radius: 8px; font-weight: bold;'>Vào Học Ngay</a>
                        </div>
                        
                        <hr style='border: none; border-top: 1px solid #eee; margin-top: 30px;' />
                        <p style='color: #888; font-size: 12px; text-align: center;'>Đây là email tự động, vui lòng không trả lời email này.</p>
                    </div>";

                // 3. Khởi tạo dịch vụ SMTP của Google
                var smtp = new SmtpClient
                {
                    Host = "smtp.gmail.com",
                    Port = 587,
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(senderEmail.Address, matKhauApp)
                };

                // 4. Gửi mail
                using (var mess = new MailMessage(senderEmail, receiverEmail)
                {
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true // Kích hoạt đọc HTML
                })
                {
                    smtp.Send(mess);
                }
            }
            catch (Exception ex)
            {
                // Bắt lỗi ngầm để nếu gửi mail có lỗi thì web vẫn chạy tiếp không bị văng
                Console.WriteLine("Lỗi gửi mail: " + ex.Message);
            }
        }
        // 2. Hàm xử lý AJAX khi bấm nút "Thanh toán"
        [HttpPost]
        public JsonResult XuLyThanhToan(string goi_vip, string phuong_thuc)
        {
            if (Session["user_id"] == null)
                return Json(new { success = false, message = "Phiên đăng nhập đã hết hạn!" });

            int userId = Convert.ToInt32(Session["user_id"]);
            var user = db.nguoi_dung.FirstOrDefault(u => u.id_nguoi_dung == userId);

            if (user != null)
            {
                string tenGoi = "";

                if (goi_vip == "VIP_THANG")
                {
                    user.loai_tai_khoan = 2;
                    user.ngay_het_han_vip = DateTime.Now.AddDays(30);
                    tenGoi = "VIP 1 Tháng (99.000 VNĐ)";
                }
                else if (goi_vip == "VIP_NAM")
                {
                    user.loai_tai_khoan = 3;
                    user.ngay_het_han_vip = DateTime.Now.AddDays(365);
                    tenGoi = "VIP 1 Năm (799.000 VNĐ)";
                }
                else
                {
                    return Json(new { success = false, message = "Gói VIP không hợp lệ!" });
                }

                db.SaveChanges(); // Lưu vào cơ sở dữ liệu

                // --- GỬI EMAIL THÔNG BÁO ---
                // Chuyển mã phương thức thành chữ dễ đọc
                string tenPhuongThuc = "Không xác định";
                if (phuong_thuc == "VNPAY") tenPhuongThuc = "Thanh toán qua VNPAY";
                else if (phuong_thuc == "MOMO") tenPhuongThuc = "Ví điện tử MoMo";
                else if (phuong_thuc == "BANK") tenPhuongThuc = "Chuyển khoản ngân hàng";

                // Gọi hàm gửi mail (chạy ngầm)
                GuiEmailThongBao(user.email, user.ho_va_ten, tenGoi, tenPhuongThuc, user.ngay_het_han_vip);
                // -----------------------------

                // Cập nhật lại Session ngay lập tức để giao diện thay đổi mà không cần đăng nhập lại
                Session["LoaiTaiKhoan"] = user.loai_tai_khoan;

                return Json(new
                {
                    success = true,
                    message = "🎉 Thanh toán thành công! Chào mừng bạn đến với đặc quyền VIP.",
                    redirect = Url.Action("khoadaotao", "Page")
                });
            }

            return Json(new { success = false, message = "Không tìm thấy thông tin người dùng!" });
        }
        
        // Thêm tham số id_cap_do vào hàm
        public ActionResult _1000TuVung(int? id_cap_do, int page = 1, string search = "")
        {
            wed_learn_eEntities db = new wed_learn_eEntities();
            var query = db.Vocabularies.AsQueryable();

            // 1. Nếu có truyền ID Cấp độ thì lọc theo đúng khóa học đó
            if (id_cap_do.HasValue)
            {
                query = query.Where(x => x.id_cap_do == id_cap_do);
            }

            // 2. Nếu có tìm kiếm
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(x => x.Word.Contains(search) || x.Meaning.Contains(search));
                page = 1;
            }

            int pageSize = 16;
            int totalItems = query.Count();
            int totalPage = (int)Math.Ceiling((double)totalItems / pageSize);

            if (page > totalPage && totalPage > 0) page = 1;

            // Phân trang
            var data = query.OrderBy(x => x.Word)
                            .Skip((page - 1) * pageSize)
                            .Take(pageSize)
                            .ToList();

            ViewBag.Page = page;
            ViewBag.TotalPage = totalPage;
            ViewBag.Search = search;

            // Lưu lại ID Cấp độ để dùng cho nút "Quay lại" hoặc nút Phân trang
            ViewBag.IdCapDo = id_cap_do;

            return View(data);
        }
        // 1. Hàm hiển thị danh sách các bài ngữ pháp
        public ActionResult _NguPhap(int? id_cap_do)
        {
            wed_learn_eEntities db = new wed_learn_eEntities();
            var listNguPhap = db.ngu_phap.AsQueryable();

            if (id_cap_do.HasValue)
            {
                listNguPhap = listNguPhap.Where(x => x.id_cap_do == id_cap_do);
            }

            ViewBag.IdCapDo = id_cap_do;
            return View(listNguPhap.ToList());
        }

        // 2. Hàm hiển thị nội dung chi tiết
        public ActionResult ChiTietNguPhap(int id, int? id_cap_do)
        {
            wed_learn_eEntities db = new wed_learn_eEntities();
            var baiHoc = db.ngu_phap.FirstOrDefault(x => x.id_ngu_phap == id);

            if (baiHoc == null) return HttpNotFound();

            ViewBag.IdCapDo = id_cap_do;
            return View(baiHoc);
        }
        public ActionResult _ThanhNgu(int? id_cap_do, int? page, string search = "")
        {
            wed_learn_eEntities db = new wed_learn_eEntities();
            var query = db.Idioms.AsQueryable();

            if (id_cap_do.HasValue)
            {
                query = query.Where(x => x.id_cap_do == id_cap_do);
            }

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(x => x.IdiomText.Contains(search) || x.Meaning.Contains(search));
            }

            // Ghi nhớ các tham số để truyền ra View
            ViewBag.Search = search;
            ViewBag.IdCapDo = id_cap_do;

            // Cài đặt PagedList
            int pageSize = 20; // 20 từ mỗi trang như bạn muốn
            int pageNumber = (page ?? 1); // Nếu không có số trang thì mặc định là trang 1

            // Bắt buộc phải OrderBy trước khi phân trang
            query = query.OrderBy(x => x.IdiomText);

            return View(query.ToPagedList(pageNumber, pageSize));
        }
    }
}