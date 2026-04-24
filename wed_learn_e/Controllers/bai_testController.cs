    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web;
    using System.Web.Mvc;
    using wed_learn_e.Models;
    namespace wed_learn_e.Controllers
    {
        public class bai_testController : Controller
        {
            wed_learn_eEntities db = new wed_learn_eEntities();
            // GET: bai_test
            public ActionResult nav_baitest()
            {
                return View();
            }
        public ActionResult bai_test(int? index)
        {
            if (Session["user_id"] == null)
            {
                TempData["Warning"] = "Bạn cần đăng nhập để làm bài test!";
                return RedirectToAction("dangnhap", "User");
            }

            // Nếu lỡ mất Session ID bài test thì tìm lại bài đang bật
            if (Session["id_bai_test"] == null)
            {
                var activeTest = db.bai_kiem_tra_dau_vao.FirstOrDefault(x => x.trang_thai == 1);
                if (activeTest == null) return HttpNotFound("Không có bài test.");
                Session["id_bai_test"] = activeTest.id_bai_kiem_tra;
            }

            int idBai = Convert.ToInt32(Session["id_bai_test"]);

            // --- BƯỚC QUAN TRỌNG: LẤY THỜI GIAN TỪ DATABASE ---
            var baiTestInfo = db.bai_kiem_tra_dau_vao.FirstOrDefault(x => x.id_bai_kiem_tra == idBai);
            // Nếu không có dữ liệu thời gian thì mặc định cho 15 phút
            int thoiGianPhut = (baiTestInfo != null && baiTestInfo.thoi_gian_phut.HasValue) ? baiTestInfo.thoi_gian_phut.Value : 15;
            int tongThoiGianGiay = thoiGianPhut * 60;

            // Xử lý đếm thời gian
            if (Session["StartTime"] == null)
            {
                Session["StartTime"] = DateTime.Now;
            }

            DateTime startTime = (DateTime)Session["StartTime"];
            int timeElapsed = (int)(DateTime.Now - startTime).TotalSeconds;

            // Tính số giây còn lại dựa trên thời gian Admin cài đặt
            int timeLeft = tongThoiGianGiay - timeElapsed;

            if (timeLeft <= 0)
            {
                return RedirectToAction("kq_bai_test", "bai_test");
            }

            ViewBag.TimeLeft = timeLeft;

            // Lọc câu hỏi và xử lý index
            var list = db.cau_hoi_kiem_tra.Where(x => x.id_bai_kiem_tra == idBai).ToList();

            if (list.Count == 0)
            {
                return HttpNotFound("Bài test này chưa có câu hỏi nào!");
            }

            int i = index ?? 0;
            if (i < 0) i = 0;
            if (i >= list.Count) i = list.Count - 1;

            ViewBag.Index = i;
            ViewBag.Total = list.Count;

            return View(list[i]);
        }
        [HttpPost]
        public ActionResult bai_test(int index, string answer, string action)
        {
            int idBai = Convert.ToInt32(Session["id_bai_test"]);
            
            // --- LẤY THỜI GIAN TỪ DATABASE ---
            var baiTestInfo = db.bai_kiem_tra_dau_vao.FirstOrDefault(x => x.id_bai_kiem_tra == idBai);
            int thoiGianPhut = (baiTestInfo != null && baiTestInfo.thoi_gian_phut.HasValue) ? baiTestInfo.thoi_gian_phut.Value : 15;
            int tongThoiGianGiay = thoiGianPhut * 60;

            if (Session["StartTime"] != null)
            {
                DateTime startTime = (DateTime)Session["StartTime"];
                int timeElapsed = (int)(DateTime.Now - startTime).TotalSeconds;
                
                int timeLeft = tongThoiGianGiay - timeElapsed; // Dùng thời gian DB trừ đi

                if (timeLeft <= 0)
                {
                    return RedirectToAction("kq_bai_test", "bai_test");
                }
                ViewBag.TimeLeft = timeLeft;
            }
            else
            {
                ViewBag.TimeLeft = tongThoiGianGiay;
            }

            var list = db.cau_hoi_kiem_tra.Where(x => x.id_bai_kiem_tra == idBai).ToList();

            // lưu đáp án
            if (!string.IsNullOrEmpty(answer))
            {
                Session["q" + index] = true; 
                Session["answer_" + index] = answer; 
            }

            // chuyển câu
            if (action == "next") index++;
            else if (action == "prev") index--;

            if (index < 0) index = 0;
            if (index >= list.Count) index = list.Count - 1;

            ViewBag.Index = index;
            ViewBag.Total = list.Count;

            return View(list[index]);
        }
        // 1. HÀM RESET BÀI TEST
        // 1. HÀM RESET VÀ HIỂN THỊ THÔNG TIN BÀI TEST
        public ActionResult thong_tin_bai_test()
        {
            // 1. Tìm và xóa toàn bộ Session lưu đáp án cũ
            List<string> keysToClear = new List<string>();
            foreach (string key in Session.Keys)
            {
                if (key.StartsWith("answer_") || key.StartsWith("q"))
                {
                    keysToClear.Add(key);
                }
            }
            foreach (var key in keysToClear) { Session.Remove(key); }
            Session.Remove("StartTime");

            // 2. TỰ ĐỘNG TÌM BÀI TEST ĐANG ĐƯỢC ADMIN BẬT (trang_thai = 1)
            var baiTest = db.bai_kiem_tra_dau_vao.FirstOrDefault(x => x.trang_thai == 1);

            if (baiTest == null)
            {
                return HttpNotFound("Hiện tại chưa có bài kiểm tra nào được mở. Vui lòng quay lại sau!");
            }

            // Lấy ID của bài test đang bật
            int id = baiTest.id_bai_kiem_tra;

            // Đếm số lượng câu hỏi
            int tongSoCau = db.cau_hoi_kiem_tra.Count(x => x.id_bai_kiem_tra == id);
            ViewBag.TongSoCau = tongSoCau;

            // Gán ID bài test vào Session để dùng cho các hàm sau
            Session["id_bai_test"] = id;

            return View(baiTest);
        }
        // 2. HÀM AJAX XỬ LÝ ĐĂNG KÝ KHÓA HỌC VÀ CỘNG SỐ LƯỢNG
        [HttpPost]
            public JsonResult DangKyKhoaHocAjax(int id_cap_do)
            {
                if (Session["user_id"] == null)
                    return Json(new { success = false, message = "Vui lòng đăng nhập!" });

                int userId = Convert.ToInt32(Session["user_id"]);

                try
                {
                    // === TẠI ĐÂY BẠN THÊM CODE LƯU DATABASE CỦA BẠN VÀO ===
                    // Ví dụ: Tìm user và cộng số lượng khóa học
                    var user = db.nguoi_dung.FirstOrDefault(u => u.id_nguoi_dung == userId);
                    if (user != null)
                    {
                        // user.so_luong_khoa_hoc += 1; // (Bỏ comment và sửa tên cột cho khớp với DB của bạn)

                        // Hoặc nếu bạn có bảng chi tiết đăng ký:
                        // db.chi_tiet_dang_ky.Add(new chi_tiet_dang_ky { id_nguoi_dung = userId, id_cap_do = id_cap_do });

                        db.SaveChanges();
                    }

                    return Json(new { success = true, message = "🎉 Đăng ký thành công! Khóa học đã được thêm vào tài khoản của bạn." });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
                }
            }
            public ActionResult end_bai_test()
            {
                int idBai = Convert.ToInt32(Session["id_bai_test"]);

                var list = db.cau_hoi_kiem_tra
                            .Where(x => x.id_bai_kiem_tra == idBai)
                            .ToList();

                List<int> done = new List<int>();
                List<int> notdone = new List<int>();

                for (int i = 0; i < list.Count; i++)
                {
                    if (Session["q" + i] != null)
                        done.Add(i);
                    else
                        notdone.Add(i);
                }

                ViewBag.Done = done;
                ViewBag.NotDone = notdone;
                ViewBag.Total = list.Count;
                Session.Remove("StartTime"); // Thêm dòng này
                return View();
            }
            public ActionResult kq_bai_test()
            {
                // 1. Kiểm tra đăng nhập và ID bài test
                if (Session["id_bai_test"] == null || Session["user_id"] == null)
                    return RedirectToAction("dangnhap", "User");

                int idBai = Convert.ToInt32(Session["id_bai_test"]);
                int userId = Convert.ToInt32(Session["user_id"]);

                var list = db.cau_hoi_kiem_tra.Where(x => x.id_bai_kiem_tra == idBai).ToList();

                if (list.Count == 0) return HttpNotFound("Bài test không có dữ liệu.");

                int diem = 0;

                // 2. KHAI BÁO DANH SÁCH THỐNG KÊ
                List<int> done = new List<int>();
                List<int> notdone = new List<int>();

                // 3. CHẤM ĐIỂM VÀ PHÂN LOẠI CÂU HỎI
                for (int i = 0; i < list.Count; i++)
                {
                    string answer = Session["answer_" + i]?.ToString();

                    // Phân loại: Đã làm hay Chưa làm (dựa vào Session "q" + i)
                    if (Session["q" + i] != null)
                    {
                        done.Add(i);
                    }
                    else
                    {
                        notdone.Add(i);
                    }

                    // Chấm điểm: So sánh đáp án
                    if (answer != null && answer.Trim().ToUpper() == list[i].dap_an_dung?.Trim().ToUpper())
                    {
                        diem++;
                    }
                }

                // 4. TÍNH CẤP ĐỘ
                double percent = (double)diem / list.Count;
                int capDo = 1;

                if (percent >= 0.8) capDo = 3;
                else if (percent >= 0.5) capDo = 2;

                // 5. LƯU KẾT QUẢ VÀO DATABASE
                ket_qua_kiem_tra kq = new ket_qua_kiem_tra()
                {
                    id_nguoi_dung = userId,
                    id_bai_kiem_tra = idBai,
                    diem_so = diem,
                    id_cap_do_dat_duoc = capDo,
                    ngay_lam_bai = DateTime.Now
                };

                db.ket_qua_kiem_tra.Add(kq);
                db.SaveChanges();

                // 6. TRUYỀN TẤT CẢ DỮ LIỆU RA VIEW
                ViewBag.Diem = diem;
                ViewBag.Tong = list.Count;
                ViewBag.CapDo = capDo;
                ViewBag.Done = done;       // Truyền danh sách câu đã trả lời
                ViewBag.NotDone = notdone; // Truyền danh sách câu chưa trả lời

                // Quan trọng: Xóa bộ đếm thời gian để lần thi sau tính lại từ đầu
                Session.Remove("StartTime");

                return View();
            }
            public ActionResult review(int? index)
            {
                // 1. Kiểm tra đã đăng nhập và đã có bài test chưa
                if (Session["user_id"] == null || Session["id_bai_test"] == null)
                {
                    return RedirectToAction("dangnhap", "User");
                }

                // 2. LẤY DANH SÁCH CÂU HỎI TỪ DATABASE (Thay vì dùng Session ảo)
                int idBai = Convert.ToInt32(Session["id_bai_test"]);
                var questions = db.cau_hoi_kiem_tra.Where(x => x.id_bai_kiem_tra == idBai).ToList();

                // Nếu lỡ Database không có câu nào thì mới đá về trang thông tin
                if (questions == null || questions.Count == 0)
                {
                    return RedirectToAction("thong_tin_bai_test", "bai_test");
                }

                int idx = index ?? 0;

                // --- TÍNH TOÁN DANH SÁCH ĐÚNG / SAI ---
                List<int> listDung = new List<int>();
                List<int> listSai = new List<int>();

                for (int i = 0; i < questions.Count; i++)
                {
                    string userAns = Session["answer_" + i]?.ToString();
                    string correctAns = questions[i].dap_an_dung?.Trim().ToUpper();

                    if (!string.IsNullOrEmpty(userAns) && userAns == correctAns)
                    {
                        listDung.Add(i); // Nạp vào danh sách câu Đúng
                    }
                    else
                    {
                        listSai.Add(i);  // Bỏ trống hoặc làm sai đều tính là Sai
                    }
                }

                // Truyền dữ liệu sang View
                ViewBag.ListDung = listDung;
                ViewBag.ListSai = listSai;

                // Giữ nguyên các ViewBag cũ của bạn
                ViewBag.Index = idx;
                ViewBag.Total = questions.Count;
                ViewBag.UserAnswer = Session["answer_" + idx]?.ToString();

                return View(questions[idx]);
            }
        }
    }