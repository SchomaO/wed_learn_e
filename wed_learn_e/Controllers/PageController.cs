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
            // Lấy toàn bộ danh sách 6 cấp độ từ database (A1 -> C2)
            var danhSachCapDo = db.cap_do.ToList();

            // Truyền danh sách này ra View
            return View(danhSachCapDo);
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