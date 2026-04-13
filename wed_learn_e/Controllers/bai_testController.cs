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
                return RedirectToAction("dangnhap", "User"); // Chuyển hướng sang Controller User, Action dangnhap
            }

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
        public ActionResult thong_tin_bai_test()
        {
            return View();
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

            return View();
        }
        public ActionResult kq_bai_test()
        {
            // Bắt buộc phải có cả user_id và id_bai_test
            if (Session["id_bai_test"] == null || Session["user_id"] == null)
                return RedirectToAction("dangnhap", "User");

            int idBai = Convert.ToInt32(Session["id_bai_test"]);
            int userId = Convert.ToInt32(Session["user_id"]);

            var list = db.cau_hoi_kiem_tra.Where(x => x.id_bai_kiem_tra == idBai).ToList();

            if (list.Count == 0) return HttpNotFound("Bài test không có dữ liệu.");

            int diem = 0;

            for (int i = 0; i < list.Count; i++)
            {
                string answer = Session["answer_" + i]?.ToString();

                if (answer != null && answer.Trim().ToUpper() == list[i].dap_an_dung?.Trim().ToUpper())
                {
                    diem++;
                }
            }

            // Tính cấp độ
            double percent = (double)diem / list.Count;
            int capDo = 1;

            if (percent >= 0.8) capDo = 3;
            else if (percent >= 0.5) capDo = 2;

            // Lưu DB
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

            ViewBag.Diem = diem;
            ViewBag.Tong = list.Count;
            ViewBag.CapDo = capDo;

            //// Chỉ xóa các session liên quan đến đáp án câu hỏi
            //for (int i = 0; i < list.Count; i++)
            //{
            //    Session.Remove("q" + i);
            //    Session.Remove("answer_" + i);
            //}
            //Session.Remove("id_bai_test"); // Làm xong thì xóa ID bài test hiện tại

            return View();
        }
        public ActionResult review(int? index)
        {
            // Kiểm tra đã đăng nhập và đã có bài test chưa
            if (Session["user_id"] == null || Session["id_bai_test"] == null)
            {
                return RedirectToAction("dangnhap", "User");
            }

            int idBai = Convert.ToInt32(Session["id_bai_test"]);
            var list = db.cau_hoi_kiem_tra.Where(x => x.id_bai_kiem_tra == idBai).ToList();

            if (list.Count == 0) return HttpNotFound("Không có dữ liệu.");

            int i = index ?? 0;

            // Chặn out range
            if (i < 0) i = 0;
            if (i >= list.Count) i = list.Count - 1;

            ViewBag.Index = i;
            ViewBag.Total = list.Count;

            // Truyền đáp án người dùng đã chọn lúc nãy ra View
            ViewBag.UserAnswer = Session["answer_" + i]?.ToString();

            return View(list[i]);
        }
    }
}