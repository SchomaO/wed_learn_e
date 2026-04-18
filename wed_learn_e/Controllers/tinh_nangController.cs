using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using wed_learn_e.Models;
using PagedList;
namespace wed_learn_e.Controllers
{
    public class tinh_nangController : Controller
    {
        // GET: tinh_nang
        wed_learn_eEntities db = new wed_learn_eEntities();
        public ActionResult list_hoc_video(int? id_cap_do, string searchString, int? page)
        {
            if (Session["user_id"] == null) return RedirectToAction("dangnhap", "User");

            // 1. CẬP NHẬT & LẤY SESSION CẤP ĐỘ
            if (id_cap_do != null)
            {
                // Ghi nhớ lại ID cấp độ mới nhất người dùng vừa chọn
                Session["id_cap_do_hien_tai"] = id_cap_do;
            }
            else if (Session["id_cap_do_hien_tai"] != null)
            {
                id_cap_do = Convert.ToInt32(Session["id_cap_do_hien_tai"]);
            }

            var queryVideo = db.bai_giang_video.AsQueryable();

            if (id_cap_do != null)
            {
                // Tìm ID khóa học thuộc cấp độ này
                var listIdKhoaHoc = db.khoa_hoc.Where(k => k.id_cap_do == id_cap_do).Select(k => k.id_khoa_hoc).ToList();
                // Lọc Video
                queryVideo = queryVideo.Where(v => v.id_khoa_hoc != null && listIdKhoaHoc.Contains(v.id_khoa_hoc.Value));
            }

            // 2. ÉP VỀ TRANG 1 KHI TÌM KIẾM
            if (!String.IsNullOrEmpty(searchString))
            {
                queryVideo = queryVideo.Where(v => v.tieu_de.Contains(searchString));
                // Fix lỗi: Khi gõ tìm kiếm, tự động reset về trang 1
                page = 1;
            }

            ViewBag.CurrentFilter = searchString;
            ViewBag.IdCapDo = id_cap_do;
            ViewBag.TenCapDo = db.cap_do.FirstOrDefault(c => c.id_cap_do == id_cap_do)?.ten_cap_do ?? "Tất cả";

            // --- PHÂN TRANG ---
            queryVideo = queryVideo.OrderBy(v => v.id_video);

            // 3. SỬA PAGESIZE CHO ĐẸP GIAO DIỆN LƯỚI
            int pageSize = 6; // Mình khuyên dùng 6, 9 hoặc 12
            int pageNumber = (page ?? 1);

            return View(queryVideo.ToPagedList(pageNumber, pageSize));
        }
        public ActionResult bai_video(int? id_video)
        {
            if (Session["user_id"] == null)
            {
                return RedirectToAction("dangnhap", "User");
            }

            var baiNghe = db.bai_giang_video.FirstOrDefault(x => x.id_video == id_video);

            if (baiNghe == null)
            {
                baiNghe = db.bai_giang_video.FirstOrDefault();
            }

            if (baiNghe == null)
            {
                return HttpNotFound("Hệ thống chưa có bài luyện nghe nào.");
            }

            // --- ĐOẠN THÊM MỚI ---
            // Lấy danh sách câu hỏi thuộc về đúng video này
            ViewBag.ListCauHoi = db.cau_hoi_video.Where(x => x.id_video == baiNghe.id_video).ToList();

            return View(baiNghe);
        }
        public ActionResult LuyenNoi(int? id_cap_do, int? id_bai_noi)
        {
            // 1. Kiểm tra đăng nhập
            if (Session["user_id"] == null) return RedirectToAction("dangnhap", "User");

            // Lấy id_cap_do từ Session nếu trên URL không có
            if (id_cap_do == null && Session["id_cap_do_hien_tai"] != null)
            {
                id_cap_do = Convert.ToInt32(Session["id_cap_do_hien_tai"]);
            }

            // 2. Tìm danh sách các khóa học của cấp độ này
            var listIdKhoaHoc = db.khoa_hoc.Where(k => k.id_cap_do == id_cap_do).Select(k => k.id_khoa_hoc).ToList();

            // 3. Lấy tất cả bài luyện nói thuộc cấp độ này
            var danhSachBaiNoi = db.bai_luyen_noi.Where(x => x.id_khoa_hoc != null && listIdKhoaHoc.Contains(x.id_khoa_hoc.Value)).ToList();

            if (danhSachBaiNoi.Count == 0) return HttpNotFound("Chưa có bài luyện nói nào cho cấp độ này.");

            // 4. Lấy bài đang được chọn (nếu không chọn thì lấy bài đầu tiên làm mặc định)
            var baiHienTai = id_bai_noi.HasValue ? danhSachBaiNoi.FirstOrDefault(x => x.id_bai_noi == id_bai_noi) : danhSachBaiNoi.First();

            // Truyền danh sách ra View thông qua ViewBag
            ViewBag.DanhSachBaiNoi = danhSachBaiNoi;
            ViewBag.IdCapDo = id_cap_do;

            // Truyền bài hiện tại vào Model
            return View(baiHienTai);
        }

        public ActionResult LuyenNghe(int? id_bai_nghe)
        {
            // Kiểm tra đăng nhập
            if (Session["user_id"] == null) return RedirectToAction("dangnhap", "User");

            // Lấy bài nghe hiện tại (Nếu không có ID truyền vào thì lấy bài đầu tiên trong DB để test)
            var baiNghe = id_bai_nghe.HasValue
                ? db.bai_luyen_nghe.FirstOrDefault(x => x.id_bai_nghe == id_bai_nghe)
                : db.bai_luyen_nghe.FirstOrDefault();

            if (baiNghe == null) return HttpNotFound("Chưa có bài luyện nghe nào trong hệ thống.");

            // Lấy danh sách câu hỏi ĐI KÈM VỚI bài nghe đó
            var danhSachCauHoi = db.cau_hoi_luyen_nghe.Where(x => x.id_bai_nghe == baiNghe.id_bai_nghe).ToList();

            // Truyền câu hỏi qua ViewBag
            ViewBag.DanhSachCauHoi = danhSachCauHoi;

            // Trả model bài nghe ra View
            return View(baiNghe);
        }
        [HttpPost] // Hàm này chỉ chạy khi bấm nút submit
        public ActionResult LuyenNghe(int id_bai_nghe, FormCollection form)
        {
            if (Session["user_id"] == null) return RedirectToAction("dangnhap", "User");
            int userId = Convert.ToInt32(Session["user_id"]);

            var baiNghe = db.bai_luyen_nghe.FirstOrDefault(x => x.id_bai_nghe == id_bai_nghe);
            var cauHois = db.cau_hoi_luyen_nghe.Where(x => x.id_bai_nghe == id_bai_nghe).ToList();

            int score = 0;
            int total = cauHois.Count;
            // Tạo 1 bộ nhớ tạm để giữ lại các đáp án người dùng vừa khoanh
            Dictionary<int, string> userAnswers = new Dictionary<int, string>();

            // 1. CHẤM ĐIỂM BẰNG C#
            foreach (var q in cauHois)
            {
                string answer = form["q_" + q.id_cau_hoi]; // Lấy đáp án A, B, C, D từ form gửi lên
                userAnswers[q.id_cau_hoi] = answer ?? "";  // Nếu user để trống thì lưu chuỗi rỗng

                // So sánh với đáp án chuẩn trong DB
                if (!string.IsNullOrEmpty(answer) && answer == q.dap_an_dung.Trim())
                {
                    score++;
                }
            }

            // 2. LƯU LỊCH SỬ NẾU ĐÚNG 100%
            bool isPerfect = false;
            if (score == total && total > 0)
            {
                isPerfect = true;
                if (!db.lich_su_luyen_nghe.Any(x => x.id_nguoi_dung == userId && x.id_bai_nghe == id_bai_nghe))
                {
                    var ls = new lich_su_luyen_nghe { id_nguoi_dung = userId, id_bai_nghe = id_bai_nghe, ngay_hoan_thanh = DateTime.Now };
                    db.lich_su_luyen_nghe.Add(ls);
                    db.SaveChanges(); // Lưu cái rụp vào DB bằng C#
                }
            }

            // 3. ĐẨY KẾT QUẢ NGƯỢC LẠI RA VIEW ĐỂ HIỂN THỊ MÀU
            ViewBag.DanhSachCauHoi = cauHois;
            ViewBag.Score = score;
            ViewBag.Total = total;
            ViewBag.UserAnswers = userAnswers;
            ViewBag.IsPerfect = isPerfect;

            return View(baiNghe); // Render lại trang để báo kết quả
        }
        public ActionResult list_luyen_nghe(int? id_cap_do, string searchString, int? page)
        {
            // 1. Kiểm tra đăng nhập
            if (Session["user_id"] == null) return RedirectToAction("dangnhap", "User");

            // 2. Lấy id_cap_do từ Session nếu trên URL bị mất (giúp web không lỗi khi F5)
            if (id_cap_do == null && Session["id_cap_do_hien_tai"] != null)
            {
                id_cap_do = Convert.ToInt32(Session["id_cap_do_hien_tai"]);
            }

            // 3. Truy vấn lấy danh sách các bài nghe từ DB
            var queryNghe = db.bai_luyen_nghe.AsQueryable();

            // 4. BỘ LỌC CẤP ĐỘ: Chỉ lấy các bài nghe thuộc cấp độ hiện tại
            if (id_cap_do != null)
            {
                // Tìm các khóa học thuộc id_cap_do này
                var listIdKhoaHoc = db.khoa_hoc
                                      .Where(k => k.id_cap_do == id_cap_do)
                                      .Select(k => k.id_khoa_hoc)
                                      .ToList();

                // Lọc bài nghe theo khóa học tìm được
                queryNghe = queryNghe.Where(v => v.id_khoa_hoc != null && listIdKhoaHoc.Contains(v.id_khoa_hoc.Value));
            }

            // --- BỘ LỌC TÌM KIẾM ---
            if (!String.IsNullOrEmpty(searchString))
            {
                queryNghe = queryNghe.Where(v => v.tieu_de.Contains(searchString));
            }

            ViewBag.CurrentFilter = searchString;
            ViewBag.IdCapDo = id_cap_do;

            var capDo = db.cap_do.FirstOrDefault(c => c.id_cap_do == id_cap_do);
            ViewBag.TenCapDo = capDo != null ? capDo.ten_cap_do : "Tất cả";

            int userId = Convert.ToInt32(Session["user_id"]);
            ViewBag.ListDaHoanThanh = db.lich_su_luyen_nghe.Where(x => x.id_nguoi_dung == userId).Select(x => x.id_bai_nghe).ToList();

            // --- PHÂN TRANG ---
            queryNghe = queryNghe.OrderBy(v => v.id_bai_nghe); // Sắp xếp
            int pageSize = 5;
            int pageNumber = (page ?? 1);

            return View(queryNghe.ToPagedList(pageNumber, pageSize));
        }
        // 1. TRANG DANH SÁCH BÀI VIẾT (Tô màu bài đã làm)
        public ActionResult list_luyen_viet(int? id_cap_do, string searchString, int? page)
        {
            if (Session["user_id"] == null) return RedirectToAction("dangnhap", "User");
            int userId = Convert.ToInt32(Session["user_id"]); // Lấy ID user đang đăng nhập

            if (id_cap_do == null && Session["id_cap_do_hien_tai"] != null)
            {
                id_cap_do = Convert.ToInt32(Session["id_cap_do_hien_tai"]);
            }

            var queryViet = db.bai_luyen_viet.AsQueryable();

            if (id_cap_do != null)
            {
                var listIdKhoaHoc = db.khoa_hoc.Where(k => k.id_cap_do == id_cap_do).Select(k => k.id_khoa_hoc).ToList();
                queryViet = queryViet.Where(v => v.id_khoa_hoc != null && listIdKhoaHoc.Contains(v.id_khoa_hoc.Value));
            }

            // --- BỘ LỌC TÌM KIẾM ---
            if (!String.IsNullOrEmpty(searchString))
            {
                queryViet = queryViet.Where(v => v.tieu_de.Contains(searchString));
            }

            ViewBag.CurrentFilter = searchString;
            ViewBag.IdCapDo = id_cap_do;

            var listDaHoanThanh = db.lich_su_luyen_viet.Where(ls => ls.id_nguoi_dung == userId).Select(ls => ls.id_bai_viet).ToList();
            ViewBag.ListDaHoanThanh = listDaHoanThanh;

            var capDo = db.cap_do.FirstOrDefault(c => c.id_cap_do == id_cap_do);
            ViewBag.TenCapDo = capDo != null ? capDo.ten_cap_do : "Tất cả";

            // --- PHÂN TRANG ---
            queryViet = queryViet.OrderBy(v => v.id_bai_viet);
            int pageSize = 5;
            int pageNumber = (page ?? 1);

            return View(queryViet.ToPagedList(pageNumber, pageSize));
        
        }

        // 2. TRANG THỰC HÀNH VIẾT
        public ActionResult LuyenViet(int id_bai_viet)
        {
            if (Session["user_id"] == null) return RedirectToAction("dangnhap", "User");

            var baiViet = db.bai_luyen_viet.FirstOrDefault(x => x.id_bai_viet == id_bai_viet);
            if (baiViet == null) return HttpNotFound();

            // Lấy danh sách câu hỏi
            ViewBag.DanhSachCauHoi = db.cau_hoi_luyen_viet.Where(x => x.id_bai_viet == id_bai_viet).ToList();

            return View(baiViet);
        }
        [HttpPost]
        public ActionResult LuyenViet(int id_bai_viet, FormCollection form)
        {
            if (Session["user_id"] == null) return RedirectToAction("dangnhap", "User");
            int userId = Convert.ToInt32(Session["user_id"]);

            var baiViet = db.bai_luyen_viet.FirstOrDefault(x => x.id_bai_viet == id_bai_viet);
            var cauHois = db.cau_hoi_luyen_viet.Where(x => x.id_bai_viet == id_bai_viet).ToList();

            int score = 0;
            int total = cauHois.Count;
            Dictionary<int, string> userAnswers = new Dictionary<int, string>();

            foreach (var q in cauHois)
            {
                // Lấy câu trả lời người dùng nhập từ form
                string userAns = form["q_" + q.id_cau_hoi_viet] ?? "";
                userAnswers[q.id_cau_hoi_viet] = userAns;

                // Dọn dẹp chuỗi (xóa dấu câu, viết thường) để so sánh bằng C#
                string cleanUser = System.Text.RegularExpressions.Regex.Replace(userAns.ToLower(), @"[.,!?]", "").Trim();
                string cleanCorrect = System.Text.RegularExpressions.Regex.Replace(q.dap_an_tieng_anh.ToLower(), @"[.,!?]", "").Trim();

                // Xóa khoảng trắng thừa
                cleanUser = System.Text.RegularExpressions.Regex.Replace(cleanUser, @"\s+", " ");
                cleanCorrect = System.Text.RegularExpressions.Regex.Replace(cleanCorrect, @"\s+", " ");

                if (cleanUser == cleanCorrect && !string.IsNullOrEmpty(cleanUser))
                {
                    score++;
                }
            }

            bool isPerfect = (score == total && total > 0);
            if (isPerfect)
            {
                // Lưu lịch sử
                if (!db.lich_su_luyen_viet.Any(x => x.id_nguoi_dung == userId && x.id_bai_viet == id_bai_viet))
                {
                    db.lich_su_luyen_viet.Add(new lich_su_luyen_viet { id_nguoi_dung = userId, id_bai_viet = id_bai_viet, ngay_hoan_thanh = DateTime.Now });
                    db.SaveChanges();
                }
            }

            ViewBag.DanhSachCauHoi = cauHois;
            ViewBag.UserAnswers = userAnswers;
            ViewBag.IsSubmitted = true;
            ViewBag.IsPerfect = isPerfect;

            return View(baiViet);
        }

        // 3. HÀM LƯU KẾT QUẢ NGẦM BẰNG AJAX
        [HttpPost]
        public JsonResult LuuKetQuaViet(int id_bai_viet)
        {
            if (Session["user_id"] == null) return Json(new { success = false, message = "Chưa đăng nhập" });
            int userId = Convert.ToInt32(Session["user_id"]);

            // Kiểm tra xem đã lưu chưa (tránh tình trạng user bấm nộp 2 lần bị lưu trùng)
            var daLuu = db.lich_su_luyen_viet.Any(x => x.id_nguoi_dung == userId && x.id_bai_viet == id_bai_viet);

            if (!daLuu)
            {
                var lichSu = new lich_su_luyen_viet();
                lichSu.id_nguoi_dung = userId;
                lichSu.id_bai_viet = id_bai_viet;
                lichSu.ngay_hoan_thanh = DateTime.Now;

                db.lich_su_luyen_viet.Add(lichSu);
                db.SaveChanges(); // LƯU VÀO DATABASE
            }

            return Json(new { success = true });
        }
        [HttpPost]
        public JsonResult LuuKetQuaNghe(int id_bai_nghe)
        {
            if (Session["user_id"] == null) return Json(new { success = false });
            int userId = Convert.ToInt32(Session["user_id"]);

            if (!db.lich_su_luyen_nghe.Any(x => x.id_nguoi_dung == userId && x.id_bai_nghe == id_bai_nghe))
            {
                var ls = new lich_su_luyen_nghe { id_nguoi_dung = userId, id_bai_nghe = id_bai_nghe, ngay_hoan_thanh = DateTime.Now };
                db.lich_su_luyen_nghe.Add(ls);
                db.SaveChanges();
            }
            return Json(new { success = true });
        }

        [HttpPost]
        public JsonResult LuuKetQuaNoi(int id_bai_noi)
        {
            if (Session["user_id"] == null) return Json(new { success = false });
            int userId = Convert.ToInt32(Session["user_id"]);

            if (!db.lich_su_luyen_noi.Any(x => x.id_nguoi_dung == userId && x.id_bai_noi == id_bai_noi))
            {
                var ls = new lich_su_luyen_noi { id_nguoi_dung = userId, id_bai_noi = id_bai_noi, ngay_hoan_thanh = DateTime.Now };
                db.lich_su_luyen_noi.Add(ls);
                db.SaveChanges();
            }
            return Json(new { success = true });
        }
        // 1. HÀM GET
        [HttpGet]
        public ActionResult MiniTestHangNgay(int? id_cap_do)
        {
            if (Session["user_id"] == null) return RedirectToAction("dangnhap", "User");
            if (id_cap_do == null && Session["id_cap_do_hien_tai"] != null)
                id_cap_do = Convert.ToInt32(Session["id_cap_do_hien_tai"]);

            DateTime homNay = DateTime.Now; // Để test ngày mai thì thêm .AddDays(1)
            int seedHomNay = homNay.Year * 10000 + homNay.Month * 100 + homNay.Day;
            Random rnd = new Random(seedHomNay);

            // --- BỘ LỌC CẤP ĐỘ ---
            // 1. Tìm các khóa học thuộc cấp độ này
            var listIdKhoaHoc = db.khoa_hoc.Where(k => k.id_cap_do == id_cap_do).Select(k => k.id_khoa_hoc).ToList();

            // 2. Tìm các Bài Sắp xếp nằm trong các khóa học đó
            var listIdBaiSapXep = db.bai_sap_xep
                                    .Where(b => b.id_khoa_hoc != null && listIdKhoaHoc.Contains(b.id_khoa_hoc.Value))
                                    .Select(b => b.id_bai_sap_xep)
                                    .ToList();

            // 3. Lấy 3 CÂU SẮP XẾP chuẩn của cấp độ này
            var sapXep = db.cau_hoi_sap_xep
                           .Where(c => c.id_bai_sap_xep != null && listIdBaiSapXep.Contains(c.id_bai_sap_xep.Value))
                           .ToList()
                           .OrderBy(x => rnd.Next())
                           .Take(3) // CHỈ LẤY 3 CÂU
                           .ToList();

            // 4. Lấy 3 CÂU TRẮC NGHIỆM (Tạm lấy từ cau_hoi_kiem_tra)
            var tracNghiem = db.cau_hoi_kiem_tra.ToList().OrderBy(x => rnd.Next()).Take(3).ToList();

            // Xáo trộn chữ cho phần sắp xếp
            Dictionary<int, List<string>> scrambledWords = new Dictionary<int, List<string>>();
            Random rndScramble = new Random(seedHomNay);
            foreach (var q in sapXep)
            {
                List<string> words = q.dap_an_dung.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                scrambledWords.Add(q.id_cau_hoi, words.OrderBy(x => rndScramble.Next()).ToList());
            }

            ViewBag.TracNghiem = tracNghiem;
            ViewBag.SapXep = sapXep;
            ViewBag.ScrambledWords = scrambledWords;
            ViewBag.NgayTest = homNay.ToString("dd/MM/yyyy");
            ViewBag.IdCapDo = id_cap_do;

            return View();
        }

        // 2. HÀM POST (Chấm điểm)
        [HttpPost]
        public ActionResult MiniTestHangNgay(int? id_cap_do, FormCollection form)
        {
            if (Session["user_id"] == null) return RedirectToAction("dangnhap", "User");

            DateTime homNay = DateTime.Now;
            int seedHomNay = homNay.Year * 10000 + homNay.Month * 100 + homNay.Day;
            Random rnd = new Random(seedHomNay);

            // --- BỘ LỌC CẤP ĐỘ CHO HÀM CHẤM ĐIỂM ---
            var listIdKhoaHoc = db.khoa_hoc.Where(k => k.id_cap_do == id_cap_do).Select(k => k.id_khoa_hoc).ToList();
            var listIdBaiSapXep = db.bai_sap_xep.Where(b => b.id_khoa_hoc != null && listIdKhoaHoc.Contains(b.id_khoa_hoc.Value)).Select(b => b.id_bai_sap_xep).ToList();

            // Bốc lại ĐÚNG 3 câu đó ra chấm
            var sapXep = db.cau_hoi_sap_xep.Where(c => c.id_bai_sap_xep != null && listIdBaiSapXep.Contains(c.id_bai_sap_xep.Value))
                           .ToList().OrderBy(x => rnd.Next()).Take(3).ToList();

            var tracNghiem = db.cau_hoi_kiem_tra.ToList().OrderBy(x => rnd.Next()).Take(3).ToList();

            int score = 0;
            int total = tracNghiem.Count + sapXep.Count;

            Dictionary<int, string> userAnswersTN = new Dictionary<int, string>();
            Dictionary<int, string> userAnswersSX = new Dictionary<int, string>();

            // Chấm Trắc nghiệm
            foreach (var q in tracNghiem)
            {
                string ans = form["q_" + q.id_cau_hoi];
                userAnswersTN[q.id_cau_hoi] = ans ?? "";
                if (!string.IsNullOrEmpty(ans) && ans == q.dap_an_dung.Trim()) score++;
            }

            // Chấm Kéo thả
            foreach (var q in sapXep)
            {
                string ans = form["sapxep_" + q.id_cau_hoi] ?? "";
                userAnswersSX[q.id_cau_hoi] = ans;

                string cleanUser = System.Text.RegularExpressions.Regex.Replace(ans.ToLower(), @"\s+", " ").Trim();
                string cleanCorrect = System.Text.RegularExpressions.Regex.Replace(q.dap_an_dung.ToLower(), @"\s+", " ").Trim();

                if (cleanUser == cleanCorrect && cleanUser != "") score++;
            }

            // Xếp lại chữ đã kéo để hiển thị
            Dictionary<int, List<string>> scrambledWords = new Dictionary<int, List<string>>();
            foreach (var q in sapXep)
            {
                if (userAnswersSX.ContainsKey(q.id_cau_hoi) && !string.IsNullOrWhiteSpace(userAnswersSX[q.id_cau_hoi]))
                {
                    scrambledWords.Add(q.id_cau_hoi, userAnswersSX[q.id_cau_hoi].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList());
                }
            }

            ViewBag.TracNghiem = tracNghiem;
            ViewBag.SapXep = sapXep;
            ViewBag.ScrambledWords = scrambledWords;
            ViewBag.UserAnswersTN = userAnswersTN;
            ViewBag.UserAnswersSX = userAnswersSX;
            ViewBag.Score = score;
            ViewBag.Total = total;
            ViewBag.IsSubmitted = true;
            ViewBag.NgayTest = homNay.ToString("dd/MM/yyyy");
            ViewBag.IdCapDo = id_cap_do;

            return View();
        }
        // 1. HIỂN THỊ BẢN ĐỒ KHO BÁU
        public ActionResult BanDoKhoBau(int? id_cap_do)
        {
            if (Session["user_id"] == null) return RedirectToAction("dangnhap", "User");
            int userId = Convert.ToInt32(Session["user_id"]);
            if (id_cap_do == null && Session["id_cap_do_hien_tai"] != null)
                id_cap_do = Convert.ToInt32(Session["id_cap_do_hien_tai"]);

            var game = db.game_kho_bau.FirstOrDefault(g => g.id_cap_do == id_cap_do);
            if (game == null) return HttpNotFound("Chưa có sự kiện kho báu cho cấp độ này.");

            var cauHois = db.cau_hoi_kho_bau.Where(c => c.id_game == game.id_game).OrderBy(c => c.thu_tu).ToList();
            var daHoanThanh = db.lich_su_kho_bau.Where(l => l.id_nguoi_dung == userId).Select(l => l.id_cau_hoi).ToList();

            ViewBag.DaHoanThanh = daHoanThanh;
            ViewBag.CauHois = cauHois;
            ViewBag.SoCauDaGiai = cauHois.Count(c => daHoanThanh.Contains(c.id_cau_hoi));

            return View(game);
        }

        // 2. VÀO GIẢI MÃ TỪNG CÂU HỎI
        [HttpGet]
        public ActionResult GiaiMaCauHoi(int id_cau_hoi)
        {
            if (Session["user_id"] == null) return RedirectToAction("dangnhap", "User");
            var cauHoi = db.cau_hoi_kho_bau.FirstOrDefault(c => c.id_cau_hoi == id_cau_hoi);

            // Nếu là câu kéo thả thì xáo trộn chữ
            if (cauHoi.loai_cau_hoi == "sap_xep")
            {
                ViewBag.ScrambledWords = cauHoi.dap_an_dung.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).OrderBy(x => Guid.NewGuid()).ToList();
            }
            ViewBag.SoLanSai = Session["Sai_Cau_" + id_cau_hoi] ?? 0;
            return View(cauHoi);
        }

        [HttpPost]
        public ActionResult GiaiMaCauHoi(int id_cau_hoi, string dap_an, string dap_an_sap_xep)
        {
            if (Session["user_id"] == null) return RedirectToAction("dangnhap", "User");
            int userId = Convert.ToInt32(Session["user_id"]);
            var cauHoi = db.cau_hoi_kho_bau.FirstOrDefault(c => c.id_cau_hoi == id_cau_hoi);
            int soLanSai = Session["Sai_Cau_" + id_cau_hoi] != null ? (int)Session["Sai_Cau_" + id_cau_hoi] : 0;

            string userAns = (cauHoi.loai_cau_hoi == "sap_xep") ? dap_an_sap_xep : dap_an;
            userAns = userAns ?? "";

            // CHẤM ĐIỂM DỰA TRÊN LOẠI CÂU
            bool isCorrect = false;
            if (cauHoi.loai_cau_hoi == "trac_nghiem" || cauHoi.loai_cau_hoi == "tim_loi")
            {
                isCorrect = (userAns.Trim().ToUpper() == cauHoi.dap_an_dung.Trim().ToUpper());
            }
            else
            {
                string cleanUser = System.Text.RegularExpressions.Regex.Replace(userAns.ToLower(), @"[.,!?]", "").Trim();
                string cleanCorrect = System.Text.RegularExpressions.Regex.Replace(cauHoi.dap_an_dung.ToLower(), @"[.,!?]", "").Trim();
                cleanUser = System.Text.RegularExpressions.Regex.Replace(cleanUser, @"\s+", " ");
                cleanCorrect = System.Text.RegularExpressions.Regex.Replace(cleanCorrect, @"\s+", " ");
                isCorrect = (cleanUser == cleanCorrect && cleanUser != "");
            }

            if (isCorrect) // NẾU ĐÚNG
            {
                Session.Remove("Sai_Cau_" + id_cau_hoi); // Qua môn, reset đếm sai
                if (!db.lich_su_kho_bau.Any(l => l.id_nguoi_dung == userId && l.id_cau_hoi == id_cau_hoi))
                {
                    db.lich_su_kho_bau.Add(new lich_su_kho_bau { id_nguoi_dung = userId, id_cau_hoi = id_cau_hoi, ngay_giai_ma = DateTime.Now });
                    db.SaveChanges();
                }
                TempData["Message"] = "🎉 Trạm " + cauHoi.thu_tu + " giải mã thành công! Bạn nhận được mảnh ghép: " + cauHoi.manh_ghep;
                return RedirectToAction("BanDoKhoBau", new { id_cap_do = cauHoi.game_kho_bau.id_cap_do });
            }

            // NẾU SAI
            soLanSai++;
            Session["Sai_Cau_" + id_cau_hoi] = soLanSai;

            if (soLanSai >= 3) // SAI 3 LẦN -> XÓA SẠCH LỊCH SỬ GAME NÀY
            {
                var listIdCauHoiGame = db.cau_hoi_kho_bau.Where(c => c.id_game == cauHoi.id_game).Select(c => c.id_cau_hoi).ToList();
                var lichSuCanXoa = db.lich_su_kho_bau.Where(l => l.id_nguoi_dung == userId && listIdCauHoiGame.Contains(l.id_cau_hoi.Value)).ToList();
               foreach (var item in lichSuCanXoa)
{
    db.lich_su_kho_bau.Remove(item);
}
                db.SaveChanges();
                Session.Remove("Sai_Cau_" + id_cau_hoi);

                TempData["GameOver"] = "💀 BẠN ĐÃ SAI 3 LẦN! Bản đồ đã bị Reset. Hãy bắt đầu lại từ Trạm 1!";
                return RedirectToAction("BanDoKhoBau", new { id_cap_do = cauHoi.game_kho_bau.id_cap_do });
            }

            // NẾU CHƯA CHẾT -> TRỘN LẠI CHỮ VÀ BÁO LỖI
            if (cauHoi.loai_cau_hoi == "sap_xep") ViewBag.ScrambledWords = cauHoi.dap_an_dung.Split(' ').OrderBy(x => Guid.NewGuid()).ToList();
            ViewBag.Error = $"❌ Sai rồi! Bạn chỉ còn {3 - soLanSai} lần thử.";
            ViewBag.SoLanSai = soLanSai;
            return View(cauHoi);
        }

        // 3. MỞ RƯƠNG (Xác nhận Keyword cuối)
        [HttpPost]
        public JsonResult MoRuongKhoBau(int id_game, string keyword)
        {
            var game = db.game_kho_bau.FirstOrDefault(g => g.id_game == id_game);
            if (game == null)
            {
                return Json(new { success = false, message = "Lỗi dữ liệu game!" });
            }

            if (keyword != null && keyword.Trim().ToUpper() == game.tu_khoa_cuoi.ToUpper())
            {
                // MẬT KHẨU ĐÚNG: Trả về success = true và kèm theo id_cap_do để lát làm nút Quay Về
                return Json(new
                {
                    success = true,
                    message = "🏆 CHÚC MỪNG! BẠN ĐÃ MỞ ĐƯỢC KHO BÁU TỐI THƯỢNG!",
                    idCapDo = game.id_cap_do
                });
            }
            else
            {
                // MẬT KHẨU SAI
                return Json(new
                {
                    success = false,
                    message = "❌ Sai mật khẩu! Hãy ghép lại các mảnh ghép cẩn thận."
                });
            }
        }
    }
}