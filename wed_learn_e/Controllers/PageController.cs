using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.UI;
using wed_learn_e.Models;


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
        public ActionResult _1000TuVung(int page = 1, string search = "")
        {
            List<Vocabulary> words = new List<Vocabulary>()
            {
                new Vocabulary { Word="Ability", Meaning="Khả năng", Example="She has the ability to learn quickly."},
                new Vocabulary { Word="Accept", Meaning="Chấp nhận", Example="He accepted the invitation."},
                new Vocabulary { Word="Achieve", Meaning="Đạt được", Example="She achieved her goal."},
                new Vocabulary { Word="Active", Meaning="Năng động", Example="He is an active student."},
                new Vocabulary { Word="Advice", Meaning="Lời khuyên", Example="She gave me good advice."},
                new Vocabulary { Word="Agree", Meaning="Đồng ý", Example="I agree with you."},
                new Vocabulary { Word="Allow", Meaning="Cho phép", Example="The teacher allowed us to leave early."},
                new Vocabulary { Word="Answer", Meaning="Câu trả lời", Example="I know the answer."},

                new Vocabulary { Word="Beautiful", Meaning="Đẹp", Example="The city is beautiful."},
                new Vocabulary { Word="Believe", Meaning="Tin tưởng", Example="I believe in you."},
                new Vocabulary { Word="Build", Meaning="Xây dựng", Example="They build a new house."},
                new Vocabulary { Word="Call", Meaning="Gọi", Example="Call me tomorrow."},
                new Vocabulary { Word="Change", Meaning="Thay đổi", Example="Things change over time."},
                new Vocabulary { Word="Choose", Meaning="Chọn", Example="Choose the correct answer."},
                new Vocabulary { Word="Create", Meaning="Tạo ra", Example="Artists create beautiful works."},
                new Vocabulary { Word="Decide", Meaning="Quyết định", Example="She decided to study English."},
                new Vocabulary { Word="Develop", Meaning="Phát triển", Example="They develop new skills every day."},
                new Vocabulary { Word="Discover", Meaning="Khám phá", Example="Scientists discover new things."},
                new Vocabulary { Word="Discuss", Meaning="Thảo luận", Example="We discuss the problem together."},
                new Vocabulary { Word="Drive", Meaning="Lái xe", Example="He drives to work every morning."},
                new Vocabulary { Word="Earn", Meaning="Kiếm được", Example="She earns a good salary."},
                new Vocabulary { Word="Educate", Meaning="Giáo dục", Example="Teachers educate students."},
                new Vocabulary { Word="Encourage", Meaning="Khuyến khích", Example="Parents encourage their children."},
                new Vocabulary { Word="Enjoy", Meaning="Thưởng thức", Example="I enjoy reading books."},
                new Vocabulary { Word="Explain", Meaning="Giải thích", Example="The teacher explains the lesson."},
                new Vocabulary { Word="Explore", Meaning="Khám phá", Example="They explore the forest."},

                new Vocabulary { Word="Fail", Meaning="Thất bại", Example="He failed the exam."},
                new Vocabulary { Word="Follow", Meaning="Theo dõi", Example="Follow the instructions carefully."},
                new Vocabulary { Word="Gain", Meaning="Đạt được", Example="She gained experience from the job."},
                new Vocabulary { Word="Grow", Meaning="Phát triển", Example="Plants grow quickly in summer."},
                new Vocabulary { Word="Help", Meaning="Giúp đỡ", Example="He helps his friends."},
                new Vocabulary { Word="Improve", Meaning="Cải thiện", Example="Practice improves your skills."},
                new Vocabulary { Word="Include", Meaning="Bao gồm", Example="The price includes breakfast."},
                new Vocabulary { Word="Increase", Meaning="Tăng lên", Example="The population increases every year."},
                new Vocabulary { Word="Influence", Meaning="Ảnh hưởng", Example="Parents influence their children."},
                new Vocabulary { Word="Introduce", Meaning="Giới thiệu", Example="She introduced her friend."},

                new Vocabulary { Word="Join", Meaning="Tham gia", Example="Many students join the club."},
                new Vocabulary { Word="Keep", Meaning="Giữ", Example="Keep your room clean."},
                new Vocabulary { Word="Learn", Meaning="Học", Example="Children learn quickly."},
                new Vocabulary { Word="Listen", Meaning="Lắng nghe", Example="Listen to the teacher."},
                new Vocabulary { Word="Manage", Meaning="Quản lý", Example="She manages the project well."},
                new Vocabulary { Word="Measure", Meaning="Đo lường", Example="They measure the distance."},
                new Vocabulary { Word="Meet", Meaning="Gặp", Example="I will meet him tomorrow."},
                new Vocabulary { Word="Move", Meaning="Di chuyển", Example="They move to another city."},
                new Vocabulary { Word="Notice", Meaning="Chú ý", Example="Did you notice the change?"},
                new Vocabulary { Word="Offer", Meaning="Đề nghị", Example="He offered me help."},

                new Vocabulary { Word="Organize", Meaning="Tổ chức", Example="They organize the event."},
                new Vocabulary { Word="Participate", Meaning="Tham gia", Example="Students participate in activities."},
                new Vocabulary { Word="Plan", Meaning="Lập kế hoạch", Example="We plan our trip."},
                new Vocabulary { Word="Practice", Meaning="Luyện tập", Example="She practices English daily."},
                new Vocabulary { Word="Prepare", Meaning="Chuẩn bị", Example="They prepare for the exam."},
                new Vocabulary { Word="Produce", Meaning="Sản xuất", Example="Factories produce goods."},
                new Vocabulary { Word="Protect", Meaning="Bảo vệ", Example="We protect the environment."},
                new Vocabulary { Word="Provide", Meaning="Cung cấp", Example="The school provides books."},
                new Vocabulary { Word="Reach", Meaning="Đạt tới", Example="He reached the top."},
                new Vocabulary { Word="Receive", Meaning="Nhận", Example="She received a gift."},

                new Vocabulary { Word="Reduce", Meaning="Giảm", Example="They reduce waste."},
                new Vocabulary { Word="Remember", Meaning="Nhớ", Example="Remember the rules."},
                new Vocabulary { Word="Replace", Meaning="Thay thế", Example="Replace the old parts."},
                new Vocabulary { Word="Report", Meaning="Báo cáo", Example="He reported the results."},
                new Vocabulary { Word="Respond", Meaning="Phản hồi", Example="She responded quickly."},
                new Vocabulary { Word="Save", Meaning="Tiết kiệm", Example="Save your money."},
                new Vocabulary { Word="Share", Meaning="Chia sẻ", Example="Share your ideas."},
                new Vocabulary { Word="Solve", Meaning="Giải quyết", Example="They solved the problem."},
                new Vocabulary { Word="Support", Meaning="Hỗ trợ", Example="Friends support each other."},
                new Vocabulary { Word="Understand", Meaning="Hiểu", Example="I understand the lesson."}
            };
            if (!string.IsNullOrEmpty(search))
            {
                words = words.Where(x =>
                    x.Word.ToLower().Contains(search.ToLower()) ||
                    x.Meaning.ToLower().Contains(search.ToLower())
                ).ToList();

                page = 1;
            }
            int pageSize = 16;

            int totalPage = (int)Math.Ceiling((double)words.Count / pageSize);

            if (page > totalPage) page = 1;

            var data = words
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.Page = page;
            ViewBag.TotalPage = Math.Ceiling((double)words.Count / pageSize);
            ViewBag.Search = search;

            return View(data);
        }
        public ActionResult _NguPhap()
        {
            return View();
        }
        public ActionResult _ThanhNgu()
        {
            return View();
        }

    }
}