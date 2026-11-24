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

            if (Session["id_bai_test"] == null)
            {
                var activeTest = db.bai_kiem_tra_dau_vao.FirstOrDefault(x => x.trang_thai == 1);
                if (activeTest == null) return HttpNotFound("Không có bài test.");
                Session["id_bai_test"] = activeTest.id_bai_kiem_tra;
            }

            int idBai = Convert.ToInt32(Session["id_bai_test"]);
            var baiTestInfo = db.bai_kiem_tra_dau_vao.FirstOrDefault(x => x.id_bai_kiem_tra == idBai);
            int thoiGianPhut = (baiTestInfo != null && baiTestInfo.thoi_gian_phut.HasValue) ? baiTestInfo.thoi_gian_phut.Value : 15;
            int tongThoiGianGiay = thoiGianPhut * 60;

            if (Session["StartTime"] == null)
            {
                Session["StartTime"] = DateTime.Now;
            }

            DateTime startTime = (DateTime)Session["StartTime"];
            int timeElapsed = (int)(DateTime.Now - startTime).TotalSeconds;
            int timeLeft = tongThoiGianGiay - timeElapsed;

            if (timeLeft <= 0)
            {
                return RedirectToAction("kq_bai_test", "bai_test");
            }

            ViewBag.TimeLeft = timeLeft;

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
            var baiTestInfo = db.bai_kiem_tra_dau_vao.FirstOrDefault(x => x.id_bai_kiem_tra == idBai);
            int thoiGianPhut = (baiTestInfo != null && baiTestInfo.thoi_gian_phut.HasValue) ? baiTestInfo.thoi_gian_phut.Value : 15;
            int tongThoiGianGiay = thoiGianPhut * 60;

            if (Session["StartTime"] != null)
            {
                DateTime startTime = (DateTime)Session["StartTime"];
                int timeElapsed = (int)(DateTime.Now - startTime).TotalSeconds;
                int timeLeft = tongThoiGianGiay - timeElapsed;

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

            if (!string.IsNullOrEmpty(answer))
            {
                Session["q" + index] = true;
                Session["answer_" + index] = answer;
            }

            if (action == "next") index++;
            else if (action == "prev") index--;

            if (index < 0) index = 0;
            if (index >= list.Count) index = list.Count - 1;

            ViewBag.Index = index;
            ViewBag.Total = list.Count;

            return View(list[index]);
        }

        public ActionResult thong_tin_bai_test()
        {
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

            var baiTest = db.bai_kiem_tra_dau_vao.FirstOrDefault(x => x.trang_thai == 1);
            if (baiTest == null)
            {
                return HttpNotFound("Hiện tại chưa có bài kiểm tra nào được mở. Vui lòng quay lại sau!");
            }

            int id = baiTest.id_bai_kiem_tra;
            int tongSoCau = db.cau_hoi_kiem_tra.Count(x => x.id_bai_kiem_tra == id);
            ViewBag.TongSoCau = tongSoCau;
            Session["id_bai_test"] = id;

            return View(baiTest);
        }

        [HttpPost]
        public JsonResult DangKyKhoaHocAjax(int id_cap_do)
        {
            if (Session["user_id"] == null)
                return Json(new { success = false, message = "Vui lòng đăng nhập!" });

            int userId = Convert.ToInt32(Session["user_id"]);

            try
            {
                var user = db.nguoi_dung.FirstOrDefault(u => u.id_nguoi_dung == userId);
                if (user != null)
                {
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
            var list = db.cau_hoi_kiem_tra.Where(x => x.id_bai_kiem_tra == idBai).ToList();

            List<int> done = new List<int>();
            List<int> notdone = new List<int>();

            for (int i = 0; i < list.Count; i++)
            {
                if (Session["q" + i] != null) done.Add(i);
                else notdone.Add(i);
            }

            ViewBag.Done = done;
            ViewBag.NotDone = notdone;
            ViewBag.Total = list.Count;
            Session.Remove("StartTime");
            return View();
        }

        public ActionResult kq_bai_test()
        {
            if (Session["id_bai_test"] == null || Session["user_id"] == null)
                return RedirectToAction("dangnhap", "User");

            int idBai = Convert.ToInt32(Session["id_bai_test"]);
            int userId = Convert.ToInt32(Session["user_id"]);

            var list = db.cau_hoi_kiem_tra.Where(x => x.id_bai_kiem_tra == idBai).ToList();
            if (list.Count == 0) return HttpNotFound("Bài test không có dữ liệu.");

            int diem = 0;
            List<int> done = new List<int>();
            List<int> notdone = new List<int>();

            for (int i = 0; i < list.Count; i++)
            {
                string answer = Session["answer_" + i]?.ToString();

                if (Session["q" + i] != null) done.Add(i);
                else notdone.Add(i);

                if (answer != null && answer.Trim().ToUpper() == list[i].dap_an_dung?.Trim().ToUpper())
                {
                    diem++;
                }
            }

            double percent = (double)diem / list.Count;
            int capDo = 1;
            if (percent >= 0.8) capDo = 3;
            else if (percent >= 0.5) capDo = 2;

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
            ViewBag.Done = done;
            ViewBag.NotDone = notdone;

            Session.Remove("StartTime");
            return View();
        }

        public ActionResult review(int? index)
        {
            if (Session["user_id"] == null || Session["id_bai_test"] == null)
            {
                return RedirectToAction("dangnhap", "User");
            }

            int idBai = Convert.ToInt32(Session["id_bai_test"]);
            var questions = db.cau_hoi_kiem_tra.Where(x => x.id_bai_kiem_tra == idBai).ToList();

            if (questions == null || questions.Count == 0)
            {
                return RedirectToAction("thong_tin_bai_test", "bai_test");
            }

            int idx = index ?? 0;
            List<int> listDung = new List<int>();
            List<int> listSai = new List<int>();

            for (int i = 0; i < questions.Count; i++)
            {
                string userAns = Session["answer_" + i]?.ToString();
                string correctAns = questions[i].dap_an_dung?.Trim().ToUpper();

                if (!string.IsNullOrEmpty(userAns) && userAns == correctAns)
                    listDung.Add(i);
                else
                    listSai.Add(i);
            }

            ViewBag.ListDung = listDung;
            ViewBag.ListSai = listSai;
            ViewBag.Index = idx;
            ViewBag.Total = questions.Count;
            ViewBag.UserAnswer = Session["answer_" + idx]?.ToString();

            return View(questions[idx]);
        }

        // =========================================================================
        // PHẦN BÀI KIỂM TRA TIẾN ĐỘ (CHỈ XÉT 4 KỸ NĂNG CHÍNH: NGHE, NÓI, VIẾT, VIDEO)
        // =========================================================================
        private bool DaHoanThanhTatCaKyNang(int idNguoiDung, int idCapDo, out string lyDoVi, out string lyDoEn)
        {
            lyDoVi = "";
            lyDoEn = "";
            var khoaHocIds = db.khoa_hoc.Where(k => k.id_cap_do == idCapDo).Select(k => k.id_khoa_hoc).ToList();

            // 1. Luyện Nghe
            var dsNghe = db.bai_luyen_nghe.Where(x => x.id_khoa_hoc != null && khoaHocIds.Contains((int)x.id_khoa_hoc)).Select(x => x.id_bai_nghe).ToList();
            var daNghe = db.lich_su_luyen_nghe.Where(x => x.id_nguoi_dung == idNguoiDung && x.id_bai_nghe != null && dsNghe.Contains((int)x.id_bai_nghe)).Select(x => x.id_bai_nghe).Distinct().Count();
            if (dsNghe.Count > 0 && daNghe < dsNghe.Count)
            {
                lyDoVi = $"Bạn cần hoàn thành toàn bộ phần <b>Luyện Nghe</b> ({daNghe}/{dsNghe.Count} bài).";
                lyDoEn = $"Please complete all <b>Listening</b> lessons ({daNghe}/{dsNghe.Count} completed).";
                return false;
            }

            // 2. Luyện Nói
            var dsNoi = db.bai_luyen_noi.Where(x => x.id_khoa_hoc != null && khoaHocIds.Contains((int)x.id_khoa_hoc)).Select(x => x.id_bai_noi).ToList();
            var daNoi = db.lich_su_luyen_noi.Where(x => x.id_nguoi_dung == idNguoiDung && x.id_bai_noi != null && dsNoi.Contains((int)x.id_bai_noi)).Select(x => x.id_bai_noi).Distinct().Count();
            if (dsNoi.Count > 0 && daNoi < dsNoi.Count)
            {
                lyDoVi = $"Bạn cần hoàn thành toàn bộ phần <b>Luyện Nói</b> ({daNoi}/{dsNoi.Count} bài).";
                lyDoEn = $"Please complete all <b>Speaking</b> lessons ({daNoi}/{dsNoi.Count} completed).";
                return false;
            }

            // 3. Luyện Viết
            var dsViet = db.bai_luyen_viet.Where(x => x.id_khoa_hoc != null && khoaHocIds.Contains((int)x.id_khoa_hoc)).Select(x => x.id_bai_viet).ToList();
            var daViet = db.lich_su_luyen_viet.Where(x => x.id_nguoi_dung == idNguoiDung && x.id_bai_viet != null && dsViet.Contains((int)x.id_bai_viet)).Select(x => x.id_bai_viet).Distinct().Count();
            if (dsViet.Count > 0 && daViet < dsViet.Count)
            {
                lyDoVi = $"Bạn cần hoàn thành toàn bộ phần <b>Luyện Viết</b> ({daViet}/{dsViet.Count} bài).";
                lyDoEn = $"Please complete all <b>Writing</b> lessons ({daViet}/{dsViet.Count} completed).";
                return false;
            }

            // 4. Học Qua Video
            var dsVideo = db.bai_giang_video.Where(x => x.id_khoa_hoc != null && khoaHocIds.Contains((int)x.id_khoa_hoc)).Select(x => x.id_video).ToList();
            var daVideo = db.lich_su_video.Where(x => x.id_nguoi_dung == idNguoiDung && x.id_video != null && dsVideo.Contains((int)x.id_video)).Select(x => x.id_video).Distinct().Count();
            if (dsVideo.Count > 0 && daVideo < dsVideo.Count)
            {
                lyDoVi = $"Bạn cần xem hết các bài giảng <b>Học Qua Video</b> ({daVideo}/{dsVideo.Count} bài).";
                lyDoEn = $"Please finish all <b>Video Lessons</b> ({daVideo}/{dsVideo.Count} completed).";
                return false;
            }

            return true;
        }

        private List<CauHoiTongHopVM> TaoDeTongHop(int idCapDo, int soCauMoiKyNang = 5)
        {
            var cauHoiRieng = db.cau_hoi_kiem_tra_tien_do
          .Where(x => x.id_cap_do == idCapDo)
          .ToList()
          .Select(x => new CauHoiTongHopVM
          {
              LoaiCauHoi = x.loai_cau_hoi,
              NguonKyNang = x.loai_cau_hoi == "nghe" ? "🎧 Luyện Nghe"
                          : x.loai_cau_hoi == "noi" ? "🗣️ Luyện Nói"
                          : x.loai_cau_hoi == "viet" ? "✍️ Luyện Viết"
                          : "🎬 Trắc Nghiệm",
              NoiDung = x.noi_dung,
              FileAmThanh = x.file_am_thanh,
              GhiChu = x.ghi_chu,
              DapAnA = x.dap_an_a,
              DapAnB = x.dap_an_b,
              DapAnC = x.dap_an_c,
              DapAnD = x.dap_an_d,
              DapAnDung = x.dap_an_dung
          })
          .OrderBy(x => Guid.NewGuid())
          .Take(20)
          .ToList();

            if (cauHoiRieng.Count > 0)
                return cauHoiRieng;
            var listTongHop = new List<CauHoiTongHopVM>();
            var khoaHocIds = db.khoa_hoc.Where(k => k.id_cap_do == idCapDo).Select(k => k.id_khoa_hoc).ToList();

            // 1. LUYỆN NGHE (Kèm link file âm thanh từ bảng bai_luyen_nghe)
            var chNghe = (from c in db.cau_hoi_luyen_nghe
                          join b in db.bai_luyen_nghe on c.id_bai_nghe equals b.id_bai_nghe
                          where b.id_khoa_hoc != null && khoaHocIds.Contains((int)b.id_khoa_hoc)
                          select new CauHoiTongHopVM
                          {
                              LoaiCauHoi = "nghe",
                              NguonKyNang = "🎧 Luyện Nghe",
                              NoiDung = c.noi_dung_cau_hoi,
                              FileAmThanh = b.file_am_thanh,
                              DapAnA = c.dap_an_a,
                              DapAnB = c.dap_an_b,
                              DapAnC = c.dap_an_c,
                              DapAnD = c.dap_an_d,
                              DapAnDung = c.dap_an_dung
                          }).ToList().OrderBy(x => Guid.NewGuid()).Take(soCauMoiKyNang);
            listTongHop.AddRange(chNghe);

            // 2. LUYỆN VIẾT (Dịch Việt -> Anh)
            var chViet = (from c in db.cau_hoi_luyen_viet
                          join b in db.bai_luyen_viet on c.id_bai_viet equals b.id_bai_viet
                          where b.id_khoa_hoc != null && khoaHocIds.Contains((int)b.id_khoa_hoc)
                          select new CauHoiTongHopVM
                          {
                              LoaiCauHoi = "viet",
                              NguonKyNang = "✍️ Luyện Viết",
                              NoiDung = c.cau_tieng_viet,
                              DapAnDung = c.dap_an_tieng_anh
                          }).ToList().OrderBy(x => Guid.NewGuid()).Take(soCauMoiKyNang);
            listTongHop.AddRange(chViet);

            // 3. LUYỆN NÓI (Đọc câu tiếng Anh qua Mic)
            var chNoi = db.bai_luyen_noi
                .Where(x => x.id_khoa_hoc != null && khoaHocIds.Contains((int)x.id_khoa_hoc))
                .Select(x => new CauHoiTongHopVM
                {
                    LoaiCauHoi = "noi",
                    NguonKyNang = "🗣️ Luyện Nói",
                    NoiDung = x.noi_dung_goc,
                    GhiChu = x.nghia_tieng_viet,
                    DapAnDung = x.noi_dung_goc
                }).ToList().OrderBy(x => Guid.NewGuid()).Take(soCauMoiKyNang);
            listTongHop.AddRange(chNoi);

            // 4. VIDEO (Trắc nghiệm)
            var videoIds = db.bai_giang_video.Where(v => v.id_khoa_hoc != null && khoaHocIds.Contains((int)v.id_khoa_hoc)).Select(v => v.id_video).ToList();
            var chVideo = db.cau_hoi_video.Where(c => c.id_video != null && videoIds.Contains((int)c.id_video))
                .Select(c => new CauHoiTongHopVM
                {
                    LoaiCauHoi = "trac_nghiem",
                    NguonKyNang = "🎬 Học qua Video",
                    NoiDung = c.noi_dung_cau_hoi,
                    DapAnA = c.dap_an_a,
                    DapAnB = c.dap_an_b,
                    DapAnC = c.dap_an_c,
                    DapAnD = c.dap_an_d,
                    DapAnDung = c.dap_an_dung
                }).ToList().OrderBy(x => Guid.NewGuid()).Take(soCauMoiKyNang);
            listTongHop.AddRange(chVideo);

            return listTongHop.OrderBy(x => Guid.NewGuid()).ToList();
        }

        // Hàm phụ chấm điểm câu Viết & Nói (bỏ dấu câu, không phân biệt hoa thường, khớp >= 75% số từ là Đúng)
        private bool ChamCauTuLuan(string ansUser, string ansDung)
        {
            if (string.IsNullOrWhiteSpace(ansUser) || string.IsNullOrWhiteSpace(ansDung)) return false;
            Func<string, string[]> chuanHoa = s => System.Text.RegularExpressions.Regex
                .Replace(s.ToLower(), @"[^\w\s]", "")
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            var tuUser = chuanHoa(ansUser);
            var tuDung = chuanHoa(ansDung);
            if (tuDung.Length == 0) return false;

            int khop = tuDung.Count(t => tuUser.Contains(t));
            return ((double)khop / tuDung.Length) >= 0.75;
        }
        [HttpGet]
        public JsonResult KiemTraDieuKienTienDo(int id_cap_do)
        {
            if (Session["user_id"] == null)
            {
                return Json(new
                {
                    success = false,
                    title = "Yêu cầu đăng nhập / Sign In Required",
                    msgVi = "Vui lòng đăng nhập tài khoản để tham gia bài kiểm tra tiến độ.",
                    msgEn = "Please sign in to your account to take the Progress Test.",
                    redirectUrl = Url.Action("dangnhap", "User")
                }, JsonRequestBehavior.AllowGet);
            }

            int userId = Convert.ToInt32(Session["user_id"]);
            string lyDoVi, lyDoEn;

            if (!DaHoanThanhTatCaKyNang(userId, id_cap_do, out lyDoVi, out lyDoEn))
            {
                return Json(new
                {
                    success = false,
                    title = "Chưa mở khóa / Test Locked",
                    msgVi = lyDoVi,
                    msgEn = lyDoEn
                }, JsonRequestBehavior.AllowGet);
            }

            string testUrl = Url.Action("KiemTraTienDo", "bai_test", new { id_cap_do = id_cap_do, reset = true });
            // Kiểm tra xem học viên đã từng làm bài kiểm tra của cấp độ này chưa
            var lichSu = db.ket_qua_kiem_tra_tien_do
                           .Where(k => k.id_nguoi_dung == userId && k.id_cap_do == id_cap_do)
                           .OrderByDescending(k => k.ngay_lam_bai)
                           .ToList();

            if (lichSu.Any())
            {
                var lanGanNhat = lichSu.First();
                decimal diemCaoNhat = lichSu.Max(x => x.phan_tram);
                bool daDat = lichSu.Any(x => x.dat_yeu_cau);

                return Json(new
                {
                    success = true,
                    hasHistory = true,
                    title = daDat ? "Bạn đã hoàn thành bài kiểm tra!" : "Kết quả lần làm bài trước",
                    msgVi = $"Lần gần nhất: <b>{lanGanNhat.phan_tram}%</b> ({lanGanNhat.so_cau_dung}/{lanGanNhat.tong_so_cau} câu) | Cao nhất: <b>{diemCaoNhat}%</b>.<br/>Bạn có muốn làm lại bài kiểm tra này không?",
                    msgEn = $"Latest score: <b>{lanGanNhat.phan_tram}%</b> | Highest: <b>{diemCaoNhat}%</b>. Would you like to retake the test?",
                    daDat = daDat,
                    redirectUrl = testUrl
                }, JsonRequestBehavior.AllowGet);
            }

            // Chưa làm lần nào -> vào thẳng trang làm bài
            return Json(new
            {
                success = true,
                hasHistory = false,
                redirectUrl = testUrl
            }, JsonRequestBehavior.AllowGet);
        }
        [HttpGet]
        public ActionResult KiemTraTienDo(int id_cap_do, int index = 0, bool reset = false)
        {
            if (Session["user_id"] == null)
                return RedirectToAction("dangnhap", "User");

            int userId = Convert.ToInt32(Session["user_id"]);

            string lyDoVi, lyDoEn;
            if (!DaHoanThanhTatCaKyNang(userId, id_cap_do, out lyDoVi, out lyDoEn))
            {
                TempData["ThongBaoKhoa"] = "🔒 Bài kiểm tra tiến độ chưa mở! " + lyDoVi;
                return RedirectToAction("thong_tin_khoa_hoc", "Page", new { id = id_cap_do });
            }

            string keyDe = "DeTienDo_" + id_cap_do;
            if (reset)
            {
                Session.Remove(keyDe);
                Session.Remove("tiendo_end_time_" + id_cap_do);
            }
            var boDe = Session[keyDe] as List<CauHoiTongHopVM>;
            if (boDe == null || boDe.Count == 0)
            {
                boDe = TaoDeTongHop(id_cap_do, 5);
                if (boDe.Count == 0)
                {
                    TempData["ThongBaoKhoa"] = "Cấp độ này chưa có đủ câu hỏi trắc nghiệm để tổng hợp.";
                    return RedirectToAction("thong_tin_khoa_hoc", "Page", new { id_cap_do = id_cap_do });
                }
                Session[keyDe] = boDe;
                Session["tiendo_end_time_" + id_cap_do] = DateTime.Now.AddMinutes(15);
            }

            DateTime endTime = (DateTime)Session["tiendo_end_time_" + id_cap_do];
            int timeLeft = (int)(endTime - DateTime.Now).TotalSeconds;
            if (timeLeft <= 0) return RedirectToAction("KetQuaTienDo", new { id_cap_do = id_cap_do });

            if (index < 0) index = 0;
            if (index >= boDe.Count) index = boDe.Count - 1;

            ViewBag.IdCapDo = id_cap_do;
            ViewBag.Index = index;
            ViewBag.Total = boDe.Count;
            ViewBag.TimeLeft = timeLeft;

            return View(boDe[index]);
        }

        // =========================================================================
        // 4. POST: LƯU ĐÁP ÁN, CHUYỂN CÂU HOẶC NỘP BÀI
        // =========================================================================
        [HttpPost]
        public ActionResult KiemTraTienDo(int id_cap_do, int index, string answer, string action)
        {
            if (!string.IsNullOrEmpty(answer))
            {
                Session["tiendo_ans_" + id_cap_do + "_" + index] = answer;
                Session["tiendo_q_" + id_cap_do + "_" + index] = true;
            }

            // Nếu bấm Nộp Bài -> lưu xong đáp án câu hiện tại rồi mới chuyển sang chấm điểm
            if (action == "submit")
                return RedirectToAction("KetQuaTienDo", new { id_cap_do = id_cap_do });

            if (action == "prev") index--;
            else if (action == "next") index++;

            return RedirectToAction("KiemTraTienDo", new { id_cap_do = id_cap_do, index = index });
        }

        // =========================================================================
        // 5. CHẤM ĐIỂM, LƯU LỊCH SỬ & CẬP NHẬT TIẾN ĐỘ HOÀN THÀNH CHO USER
        // =========================================================================
        public ActionResult KetQuaTienDo(int id_cap_do)
        {
            if (Session["user_id"] == null)
                return RedirectToAction("dangnhap", "User");

            int userId = Convert.ToInt32(Session["user_id"]);

            string keyDe = "DeTienDo_" + id_cap_do;
            var boDe = Session[keyDe] as List<CauHoiTongHopVM>;
            if (boDe == null || boDe.Count == 0)
                return RedirectToAction("thong_tin_khoa_hoc", "Page", new { id = id_cap_do });

            int tongCau = boDe.Count;
            int soCauDung = 0;
            var danhSachChiTiet = new List<chi_tiet_kiem_tra_tien_do>();

            for (int i = 0; i < tongCau; i++)
            {
                string ansUser = Session["tiendo_ans_" + id_cap_do + "_" + i]?.ToString();
                string ansDung = boDe[i].DapAnDung?.Trim();

                bool isDung = (boDe[i].LoaiCauHoi == "viet" || boDe[i].LoaiCauHoi == "noi")
                    ? ChamCauTuLuan(ansUser, ansDung)
                    : (!string.IsNullOrEmpty(ansUser) && ansUser.Trim().Equals(ansDung, StringComparison.OrdinalIgnoreCase));

                if (isDung) soCauDung++;

                danhSachChiTiet.Add(new chi_tiet_kiem_tra_tien_do
                {
                    nguon_ky_nang = boDe[i].NguonKyNang,
                    noi_dung_cau_hoi = boDe[i].NoiDung,
                    dap_an_chon = ansUser,
                    dap_an_dung = ansDung,
                    is_dung = isDung
                });

                Session.Remove("tiendo_ans_" + id_cap_do + "_" + i);
                Session.Remove("tiendo_q_" + id_cap_do + "_" + i);
            }

            Session.Remove(keyDe);
            Session.Remove("tiendo_end_time_" + id_cap_do);

            decimal phanTram = tongCau > 0 ? Math.Round((soCauDung * 100m) / tongCau, 2) : 0;
            bool datYeuCau = phanTram >= 70;

            // 1. Lưu kết quả tổng quan (gắn chặt với id_nguoi_dung & id_cap_do)
            var ketQua = new ket_qua_kiem_tra_tien_do
            {
                id_nguoi_dung = userId,
                id_cap_do = id_cap_do,
                so_cau_dung = soCauDung,
                tong_so_cau = tongCau,
                phan_tram = phanTram,
                dat_yeu_cau = datYeuCau,
                ngay_lam_bai = DateTime.Now
            };
            db.ket_qua_kiem_tra_tien_do.Add(ketQua);
            db.SaveChanges();

            // 2. Lưu chi tiết từng câu làm bài
            foreach (var ct in danhSachChiTiet)
            {
                ct.id_ket_qua = ketQua.id_ket_qua;
                db.chi_tiet_kiem_tra_tien_do.Add(ct);
            }

            // 3. Nếu đạt >= 70%: Cập nhật (hoặc thêm mới nếu chưa có) toàn bộ tiến độ khóa học & cấp độ của User
            if (datYeuCau)
            {
                var khoaHocIds = db.khoa_hoc
                    .Where(k => k.id_cap_do == id_cap_do)
                    .Select(k => k.id_khoa_hoc)
                    .ToList();

                foreach (int khId in khoaHocIds)
                {
                    var td = db.tien_do_hoc_tap
                        .FirstOrDefault(t => t.id_nguoi_dung == userId && t.id_khoa_hoc == khId);

                    if (td != null)
                    {
                        td.phan_tram_hoan_thanh = 100;
                        td.trang_thai = "da_hoan_thanh";
                        td.lan_truy_cap_cuoi = DateTime.Now;
                    }
                    else
                    {
                        db.tien_do_hoc_tap.Add(new tien_do_hoc_tap
                        {
                            id_nguoi_dung = userId,
                            id_khoa_hoc = khId,
                            phan_tram_hoan_thanh = 100,
                            trang_thai = "da_hoan_thanh",
                            lan_truy_cap_cuoi = DateTime.Now
                        });
                    }
                }

                // Cập nhật cấp độ hiện tại đạt được của học viên trong bảng nguoi_dung
                var user = db.nguoi_dung.FirstOrDefault(u => u.id_nguoi_dung == userId);
                if (user != null && (user.id_cap_do_hien_tai == null || user.id_cap_do_hien_tai < id_cap_do))
                {
                    user.id_cap_do_hien_tai = id_cap_do;
                }
            }

            db.SaveChanges();

            ViewBag.IdKetQua = ketQua.id_ket_qua;
            ViewBag.IdCapDo = id_cap_do;
            ViewBag.SoCauDung = soCauDung;
            ViewBag.TongCau = tongCau;
            ViewBag.PhanTram = phanTram;
            ViewBag.DatYeuCau = datYeuCau;

            return View();
        }

        // =========================================================================
        // 6. QUẢN LÝ & LỊCH SỬ LÀM BÀI KIỂM TRA TIẾN ĐỘ (ADMIN XEM TẤT CẢ, USER XEM CỦA MÌNH)
        // =========================================================================
        public ActionResult LichSuKiemTraTienDo(int? id_cap_do)
        {
            if (Session["user_id"] == null)
                return RedirectToAction("dangnhap", "User");

            int userId = Convert.ToInt32(Session["user_id"]);
            var currentUser = db.nguoi_dung.FirstOrDefault(u => u.id_nguoi_dung == userId);
            bool isAdmin = currentUser != null && currentUser.vai_tro == "quan_tri_vien";

            var query = from kq in db.ket_qua_kiem_tra_tien_do
                        join nd in db.nguoi_dung on kq.id_nguoi_dung equals nd.id_nguoi_dung
                        join cd in db.cap_do on kq.id_cap_do equals cd.id_cap_do
                        select new LichSuTienDoVM
                        {
                            IdKetQua = kq.id_ket_qua,
                            IdNguoiDung = kq.id_nguoi_dung,
                            HoVaTen = nd.ho_va_ten,
                            TenDangNhap = nd.ten_dang_nhap,
                            IdCapDo = kq.id_cap_do,
                            TenCapDo = cd.ten_cap_do,
                            SoCauDung = kq.so_cau_dung,
                            TongSoCau = kq.tong_so_cau,
                            PhanTram = kq.phan_tram,
                            DatYeuCau = kq.dat_yeu_cau,
                            NgayLamBai = kq.ngay_lam_bai
                        };

            // Nếu không phải Admin thì chỉ lọc bài của chính học viên đó
            if (!isAdmin)
            {
                query = query.Where(x => x.IdNguoiDung == userId);
            }

            if (id_cap_do.HasValue)
            {
                query = query.Where(x => x.IdCapDo == id_cap_do.Value);
            }

            ViewBag.IsAdmin = isAdmin;
            ViewBag.DanhSachCapDo = db.cap_do.ToList();
            ViewBag.CapDoLoc = id_cap_do;

            return View(query.OrderByDescending(x => x.NgayLamBai).ToList());
        }

        // =========================================================================
        // 7. XEM CHI TIẾT TỪNG CÂU ĐÚNG / SAI CỦA MỘT LẦN LÀM BÀI
        // =========================================================================
        public ActionResult ChiTietBaiLamTienDo(int id_ket_qua)
        {
            if (Session["user_id"] == null)
                return RedirectToAction("dangnhap", "User");

            var thongTin = (from kq in db.ket_qua_kiem_tra_tien_do
                            join nd in db.nguoi_dung on kq.id_nguoi_dung equals nd.id_nguoi_dung
                            join cd in db.cap_do on kq.id_cap_do equals cd.id_cap_do
                            where kq.id_ket_qua == id_ket_qua
                            select new LichSuTienDoVM
                            {
                                IdKetQua = kq.id_ket_qua,
                                IdNguoiDung = kq.id_nguoi_dung,
                                HoVaTen = nd.ho_va_ten,
                                TenDangNhap = nd.ten_dang_nhap,
                                IdCapDo = kq.id_cap_do,
                                TenCapDo = cd.ten_cap_do,
                                SoCauDung = kq.so_cau_dung,
                                TongSoCau = kq.tong_so_cau,
                                PhanTram = kq.phan_tram,
                                DatYeuCau = kq.dat_yeu_cau,
                                NgayLamBai = kq.ngay_lam_bai
                            }).FirstOrDefault();

            if (thongTin == null) return HttpNotFound("Không tìm thấy bài làm.");

            var chiTiet = db.chi_tiet_kiem_tra_tien_do
                .Where(c => c.id_ket_qua == id_ket_qua)
                .ToList();

            ViewBag.ThongTin = thongTin;
            return View(chiTiet);
        }

       
    }
}