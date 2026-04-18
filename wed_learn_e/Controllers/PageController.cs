using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.UI;
using wed_learn_e.Models;
using PagedList;


namespace wed_learn_e.Controllers
{
    public class PageController : Controller
    {
        // GET: bai_test
        wed_learn_eEntities db = new wed_learn_eEntities();
        public ActionResult thong_tin_khoa_hoc(int? id)
        {
            // Nếu không có id truyền vào thì đuổi về trang chọn cấp độ
            if (id == null) return RedirectToAction("khoadaotao");

            // Lưu lại id_cap_do_hien_tai vào Session để các trang sau (Từ vựng, Luyện nghe...) biết đường lấy bài
            Session["id_cap_do_hien_tai"] = id;

            // Lấy tên cấp độ để hiển thị ra View cho đẹp (Ví dụ: Khóa Học: Beginner (A1))
            var capDo = db.cap_do.FirstOrDefault(x => x.id_cap_do == id);
            ViewBag.TenCapDo = capDo != null ? capDo.ten_cap_do : "Khóa Học";
            ViewBag.IdCapDo = id; // Truyền ID sang View để gắp vào link

            return View(); // Không cần truyền List<khoa_hoc> sang nữa
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
                if (user.loai_tai_khoan == 1 && user.so_luong_khoa_hoc >= 2)
                {
                    return Json(new
                    {
                        success = false,
                        type = "limit_reached",
                        message = "Tài khoản của bạn là bản Thường, chỉ được đăng ký tối đa 2 cấp độ.\n\nVui lòng nâng cấp lên tài khoản VIP để học không giới hạn!",
                        redirect = Url.Action("TrangMuaVIP", "Page") // Link chuyển tới trang nạp tiền
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
                // GIẢ LẬP GỌI API THANH TOÁN (VNPAY/MOMO) Ở ĐÂY
                // ... (Sau khi API báo giao dịch thành công thì chạy code bên dưới)

                // Cập nhật loại tài khoản lên VIP (loai_tai_khoan = 2)
                user.loai_tai_khoan = 2;
                db.SaveChanges();

                // Trả về thông báo thành công
                return Json(new
                {
                    success = true,
                    message = "🎉 Thanh toán thành công! Chào mừng bạn đến với đặc quyền VIP.",
                    redirect = Url.Action("khoadaotao", "Page")
                });
            }

            return Json(new { success = false, message = "Lỗi hệ thống!" });
        }
        //Khóa học bổ sung 
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
        public ActionResult _NguPhap()
        {
            return View();
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