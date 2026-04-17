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
            // 1. KIỂM TRA ĐĂNG NHẬP: Chưa đăng nhập thì đuổi về trang Đăng nhập
            if (Session["user_id"] == null)
            {
                TempData["Warning"] = "Bạn cần đăng nhập để làm bài test!";
                return RedirectToAction("dangnhap", "User");
            }

            // TẠO BỘ ĐẾM THỜI GIAN SERVER-SIDE (15 Phút = 900 giây)
            if (Session["StartTime"] == null)
            {
                Session["StartTime"] = DateTime.Now;
            }

            DateTime startTime = (DateTime)Session["StartTime"];
            int timeElapsed = (int)(DateTime.Now - startTime).TotalSeconds;
            int timeLeft = 10 - timeElapsed; // 900 giây là 15 phút

            // Nếu đã hết thời gian trên server thì ép nộp bài luôn
            if (timeLeft <= 0)
            {
                return RedirectToAction("kq_bai_test", "bai_test");
            }

            ViewBag.TimeLeft = timeLeft;
            // 2. GÁN TẠM ID BÀI TEST: 
            // Nếu bạn chưa làm chức năng bấm từ khóa học truyền ID sang, thì gán mặc định là 1 để test
            if (Session["id_bai_test"] == null)
            {
                Session["id_bai_test"] = 1;
            }

            int idBai = Convert.ToInt32(Session["id_bai_test"]);

            // 3. LỌC CÂU HỎI: Chỉ lấy các câu hỏi thuộc bài test hiện tại
            var list = db.cau_hoi_kiem_tra.Where(x => x.id_bai_kiem_tra == idBai).ToList();

            // 4. KIỂM TRA DỮ LIỆU RỖNG: Tránh lỗi crash web nếu DB chưa có câu hỏi nào cho bài test này
            if (list.Count == 0)
            {
                return HttpNotFound("Không tìm thấy câu hỏi nào cho bài test số " + idBai + ". Vui lòng kiểm tra lại database!");
            }

            // 5. XỬ LÝ CHỈ SỐ CÂU HỎI (INDEX)
            int i = index ?? 0; // Nếu index null thì mặc định là câu đầu tiên (vị trí 0)

            // Chặn lỗi out of range (người dùng cố tình nhập số âm hoặc số lớn hơn tổng câu hỏi trên URL)
            if (i < 0) i = 0;
            if (i >= list.Count) i = list.Count - 1;

            // 6. TRUYỀN DỮ LIỆU RA VIEW
            ViewBag.Index = i;
            ViewBag.Total = list.Count;

            return View(list[i]);
        }
        [HttpPost]
        public ActionResult bai_test(int index, string answer, string action)
        {
            if (Session["StartTime"] != null)
            {
                DateTime startTime = (DateTime)Session["StartTime"];
                int timeElapsed = (int)(DateTime.Now - startTime).TotalSeconds;
                int timeLeft = 10 - timeElapsed;

                if (timeLeft <= 0)
                {
                    return RedirectToAction("kq_bai_test", "bai_test");
                }
                ViewBag.TimeLeft = timeLeft;
            }
            else
            {
                ViewBag.TimeLeft = 10;
            }
            var list = db.cau_hoi_kiem_tra.ToList();

            // lưu đáp án nếu có chọn
            if (!string.IsNullOrEmpty(answer))
            {
                Session["q" + index] = true; // đánh dấu đã làm
                Session["answer_" + index] = answer; // lưu đáp án
            }

            // xử lý next / prev
            if (action == "next")
            {
                index++;
            }
            else if (action == "prev")
            {
                index--;
            }

            // chặn out range
            if (index < 0) index = 0;
            if (index >= list.Count) index = list.Count - 1;

            ViewBag.Index = index;
            ViewBag.Total = list.Count;

            return View(list[index]);
        }
        // 1. HÀM RESET BÀI TEST
        public ActionResult thong_tin_bai_test()
        {
            // Tìm và xóa toàn bộ Session lưu đáp án (answer_0, answer_1...) và trạng thái (q0, q1...)
            List<string> keysToClear = new List<string>();
            foreach (string key in Session.Keys)
            {
                if (key.StartsWith("answer_") || key.StartsWith("q"))
                {
                    keysToClear.Add(key);
                }
            }

            foreach (var key in keysToClear)
            {
                Session.Remove(key);
            }

            // Xóa ID bài test cũ
            Session.Remove("id_bai_test");
            Session.Remove("StartTime"); // Thêm dòng này
            return View();
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