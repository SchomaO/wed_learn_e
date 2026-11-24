using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using wed_learn_e.Models;

namespace wed_learn_e.Controllers
{
    public class NangCapController : Controller
    {
        private const string MA_NGAN_HANG = "AGRIBANK";
        private const string TEN_NGAN_HANG = "Agribank";
        private const string SO_TAI_KHOAN = "5507205212847";
        private const string CHU_TAI_KHOAN = "NGUYEN HIEU MINH TAM";

        private readonly string _connStr = LayChuoiKetNoiSql();

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            base.OnActionExecuting(filterContext);

            var listFooter = new System.Collections.Generic.List<footer_cot>();
            try
            {
                using (SqlConnection conn = new SqlConnection(_connStr))
                {
                    conn.Open();
                    string sql = "SELECT tieu_de, noi_dung_html FROM footer_cot";
                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    using (SqlDataReader rd = cmd.ExecuteReader())
                    {
                        while (rd.Read())
                        {
                            listFooter.Add(new footer_cot
                            {
                                tieu_de = rd["tieu_de"]?.ToString(),
                                noi_dung_html = rd["noi_dung_html"]?.ToString()
                            });
                        }
                    }
                }
            }
            catch { }

            ViewBag.ListFooter = listFooter;
        }
        private static string LayChuoiKetNoiSql()
        {
            string efConn = ConfigurationManager.ConnectionStrings["wed_learn_eEntities"].ConnectionString;
            int idx = efConn.IndexOf("data source=", StringComparison.OrdinalIgnoreCase);
            if (idx >= 0)
            {
                string sub = efConn.Substring(idx);
                int endQuote = sub.IndexOf('"');
                return endQuote >= 0 ? sub.Substring(0, endQuote) : sub;
            }
            return efConn;
        }

        private int? GetCurrentUserId()
        {
            if (Session["id_nguoi_dung"] != null)
                return Convert.ToInt32(Session["id_nguoi_dung"]);

            if (Session["TenDN"] != null)
            {
                string tenDn = Session["TenDN"].ToString();
                using (SqlConnection conn = new SqlConnection(_connStr))
                {
                    conn.Open();
                    string sql = "SELECT id_nguoi_dung FROM nguoi_dung WHERE ten_dang_nhap = @ten";
                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@ten", tenDn);
                        object res = cmd.ExecuteScalar();
                        if (res != null && res != DBNull.Value)
                        {
                            int uid = Convert.ToInt32(res);
                            Session["id_nguoi_dung"] = uid;
                            return uid;
                        }
                    }
                }
            }
            return null;
        }

        // GET: /NangCap/Index
        public ActionResult Index()
        {
            int? userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("dangnhap", "User");
            }

            var vm = new TrangNangCapViewModel();

            using (SqlConnection conn = new SqlConnection(_connStr))
            {
                conn.Open();

                // 1. Lấy thông tin gói nâng cấp duy nhất (id_goi = 1)
                string sqlGoi = "SELECT id_goi, ten_goi, gia_nang_cap, phan_tram_giam_khoa_hoc, mo_ta FROM goi_nang_cap WHERE id_goi = 1";
                using (SqlCommand cmd = new SqlCommand(sqlGoi, conn))
                using (SqlDataReader rd = cmd.ExecuteReader())
                {
                    if (rd.Read())
                    {
                        vm.Goi = new GoiNangCap
                        {
                            IdGoi = Convert.ToInt32(rd["id_goi"]),
                            TenGoi = rd["ten_goi"].ToString(),
                            GiaNangCap = Convert.ToDecimal(rd["gia_nang_cap"]),
                            PhanTramGiamKhoaHoc = Convert.ToInt32(rd["phan_tram_giam_khoa_hoc"]),
                            MoTa = rd["mo_ta"] == DBNull.Value ? "" : rd["mo_ta"].ToString()
                        };
                    }
                }

                // 2. Kiểm tra người dùng đã nâng cấp vĩnh viễn hay chưa
                string sqlCheck = "SELECT ngay_nang_cap FROM nguoi_dung_nang_cap WHERE id_nguoi_dung = @uid";
                using (SqlCommand cmd = new SqlCommand(sqlCheck, conn))
                {
                    cmd.Parameters.AddWithValue("@uid", userId.Value);
                    object result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        vm.DaNangCap = true;
                        vm.NgayNangCap = Convert.ToDateTime(result);
                    }
                }
            }

            return View(vm);
        }

        // POST: /NangCap/TaoDonNangCap
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult TaoDonNangCap()
        {
            int? userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("dangnhap", "User");
            }

            string maDonHang = "KL" + DateTime.Now.ToString("ddMMyy") + new Random().Next(1000, 9999);

            try
            {
                using (SqlConnection conn = new SqlConnection(_connStr))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("sp_TaoDonHang", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@ma_don_hang", maDonHang);
                        cmd.Parameters.AddWithValue("@id_nguoi_dung", userId.Value);
                        cmd.Parameters.AddWithValue("@loai_giao_dich", "NANG_CAP");
                        cmd.Parameters.AddWithValue("@id_cap_do", DBNull.Value);
                        cmd.Parameters.AddWithValue("@phuong_thuc", "BANK_QR");

                        object res = cmd.ExecuteScalar();
                        if (res != null && res != DBNull.Value)
                            maDonHang = res.ToString();
                    }
                }
            }
            catch (SqlException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("Index");
            }

            return RedirectToAction("ThanhToan", new { maDonHang = maDonHang });
        }

        // GET: /NangCap/ThanhToan?maDonHang=KL...
        public ActionResult ThanhToan(string maDonHang)
        {
            int? userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("dangnhap", "User");
            if (string.IsNullOrEmpty(maDonHang)) return RedirectToAction("Index");

            DonHangThanhToanViewModel vm = null;

            using (SqlConnection conn = new SqlConnection(_connStr))
            {
                conn.Open();
                string sql = @"SELECT gd.ma_don_hang, gd.thanh_tien, gd.trang_thai, gd.ngay_tao,
                                      g.ten_goi, g.phan_tram_giam_khoa_hoc
                               FROM giao_dich gd
                               CROSS JOIN goi_nang_cap g
                               WHERE gd.ma_don_hang = @ma 
                                 AND gd.id_nguoi_dung = @uid 
                                 AND gd.loai_giao_dich = 'NANG_CAP'
                                 AND g.id_goi = 1";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@ma", maDonHang);
                    cmd.Parameters.AddWithValue("@uid", userId.Value);
                    using (SqlDataReader rd = cmd.ExecuteReader())
                    {
                        if (rd.Read())
                        {
                            decimal thanhTien = Convert.ToDecimal(rd["thanh_tien"]);
                            long soTienNguyen = Convert.ToInt64(thanhTien);
                            string qrUrl = string.Format(
                                "https://img.vietqr.io/image/{0}-{1}-compact2.png?amount={2}&addInfo={3}&accountName={4}",
                                MA_NGAN_HANG,
                                SO_TAI_KHOAN,
                                soTienNguyen,
                                Uri.EscapeDataString(maDonHang),
                                Uri.EscapeDataString(CHU_TAI_KHOAN)
                            );

                            vm = new DonHangThanhToanViewModel
                            {
                                MaDonHang = rd["ma_don_hang"].ToString(),
                                TenGoi = rd["ten_goi"].ToString(),
                                PhanTramGiamKhoaHoc = Convert.ToInt32(rd["phan_tram_giam_khoa_hoc"]),
                                ThanhTien = thanhTien,
                                TrangThai = rd["trang_thai"].ToString(),
                                NgayTao = Convert.ToDateTime(rd["ngay_tao"]),
                                TenNganHang = TEN_NGAN_HANG,
                                SoTaiKhoan = SO_TAI_KHOAN,
                                ChuTaiKhoan = CHU_TAI_KHOAN,
                                LinkQrCode = qrUrl
                            };
                        }
                    }
                }
            }

            if (vm == null) return HttpNotFound();
            return View(vm);
        }
    }
}