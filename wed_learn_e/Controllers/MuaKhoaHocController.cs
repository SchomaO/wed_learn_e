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
    public class MuaKhoaHocController : Controller
    {
        private const string MA_NGAN_HANG = "AGRIBANK";
        private const string TEN_NGAN_HANG = "Agribank";
        private const string SO_TAI_KHOAN = "5507205212847";
        private const string CHU_TAI_KHOAN = "NGUYEN HIEU MINH TAM";

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
                using (SqlConnection conn = new SqlConnection(LayChuoiKetNoiSql()))
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

        // Lấy số lượng cấp độ đầu tiên được miễn phí từ cấu hình Admin (GetCaiDatHeThong)
        public static int LayGioiHanCapDoMienPhi(SqlConnection conn)
        {
            int limit = 2; // Mặc định giống Trang_AdminController
            try
            {
                string path = System.Web.Hosting.HostingEnvironment.MapPath("~/App_Data/limit.txt");
                if (!string.IsNullOrEmpty(path) && System.IO.File.Exists(path))
                {
                    string content = System.IO.File.ReadAllText(path).Trim();
                    int.TryParse(content, out limit);
                }
            }
            catch { }

            return Math.Max(0, limit);
        }

        public static DanhSachCapDoViewModel LayDuLieuCuaHangCapDo(int? userId)
        {
            var vm = new DanhSachCapDoViewModel
            {
                DaDangNhap = userId.HasValue,
                PhanTramGiamVip = 40,
                GiaGoiNangCap = 199000
            };

            using (SqlConnection conn = new SqlConnection(LayChuoiKetNoiSql()))
            {
                conn.Open();

                vm.SoCapDoMienPhi = LayGioiHanCapDoMienPhi(conn);

                // 1. Lấy cấu hình gói nâng cấp
                string sqlGoi = "SELECT gia_nang_cap, phan_tram_giam_khoa_hoc FROM goi_nang_cap WHERE id_goi = 1";
                using (SqlCommand cmd = new SqlCommand(sqlGoi, conn))
                using (SqlDataReader rd = cmd.ExecuteReader())
                {
                    if (rd.Read())
                    {
                        vm.GiaGoiNangCap = Convert.ToDecimal(rd["gia_nang_cap"]);
                        vm.PhanTramGiamVip = Convert.ToInt32(rd["phan_tram_giam_khoa_hoc"]);
                    }
                }

                // 2. Kiểm tra trạng thái nâng cấp & danh sách cấp độ đã mua
                var capDoDaMua = new Dictionary<int, DateTime>();
                if (userId.HasValue)
                {
                    string sqlCheckVip = "SELECT COUNT(1) FROM nguoi_dung_nang_cap WHERE id_nguoi_dung = @uid";
                    using (SqlCommand cmd = new SqlCommand(sqlCheckVip, conn))
                    {
                        cmd.Parameters.AddWithValue("@uid", userId.Value);
                        vm.DaNangCap = Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                    }

                    string sqlOwned = "SELECT id_cap_do, ngay_mua FROM nguoi_dung_cap_do WHERE id_nguoi_dung = @uid";
                    using (SqlCommand cmd = new SqlCommand(sqlOwned, conn))
                    {
                        cmd.Parameters.AddWithValue("@uid", userId.Value);
                        using (SqlDataReader rd = cmd.ExecuteReader())
                        {
                            while (rd.Read())
                            {
                                capDoDaMua[Convert.ToInt32(rd["id_cap_do"])] = Convert.ToDateTime(rd["ngay_mua"]);
                            }
                        }
                    }
                }

                // 3. Lấy danh sách cấp độ (N cấp độ đầu tiên được miễn phí theo cấu hình Admin)
                string sqlCd = @"
                    SELECT cd.id_cap_do, cd.ten_cap_do, cd.mo_ta,
                           ISNULL(gcd.gia_tien, 299000) AS gia_tien
                    FROM cap_do cd
                    LEFT JOIN gia_cap_do gcd ON cd.id_cap_do = gcd.id_cap_do
                    ORDER BY cd.id_cap_do ASC";

                using (SqlCommand cmd = new SqlCommand(sqlCd, conn))
                using (SqlDataReader rd = cmd.ExecuteReader())
                {
                    int index = 0;
                    while (rd.Read())
                    {
                        int idCd = Convert.ToInt32(rd["id_cap_do"]);
                        decimal giaGoc = Convert.ToDecimal(rd["gia_tien"]);
                        bool isMienPhi = (index < vm.SoCapDoMienPhi) || (giaGoc <= 0);

                        int phanTramApDung = (vm.DaNangCap && !isMienPhi) ? vm.PhanTramGiamVip : 0;
                        decimal soTienGiam = phanTramApDung > 0
                            ? Math.Round(giaGoc * phanTramApDung / 100m, 0, MidpointRounding.AwayFromZero)
                            : 0m;
                        decimal giaSauGiam = isMienPhi ? 0 : (giaGoc - soTienGiam);

                        bool daSoHuu = capDoDaMua.ContainsKey(idCd);

                        vm.DanhSachCapDo.Add(new CapDoStoreItem
                        {
                            IdCapDo = idCd,
                            TenCapDo = rd["ten_cap_do"].ToString(),
                            MoTa = rd["mo_ta"] == DBNull.Value ? "" : rd["mo_ta"].ToString(),
                            GiaGoc = giaGoc,
                            IsMienPhiTheoGioiHan = isMienPhi,
                            PhanTramGiam = phanTramApDung,
                            SoTienGiam = soTienGiam,
                            GiaSauGiam = giaSauGiam,
                            DaSoHuu = daSoHuu,
                            NgayMua = daSoHuu ? capDoDaMua[idCd] : (DateTime?)null
                        });

                        index++;
                    }
                }
            }

            return vm;
        }

        // POST: Tạo đơn mua Cấp độ trả phí
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult TaoDonCapDo(int idCapDo)
        {
            int? userId = GetCurrentUserId();
            if (userId == null)
                return RedirectToAction("dangnhap", "User");

            string maDonHang = "CD" + DateTime.Now.ToString("ddMMyy") + new Random().Next(1000, 9999);

            try
            {
                using (SqlConnection conn = new SqlConnection(LayChuoiKetNoiSql()))
                {
                    conn.Open();

                    // Chặn mua nếu cấp độ đang nằm trong nhóm miễn phí học thử
                    int gioiHanMienPhi = LayGioiHanCapDoMienPhi(conn);
                    string sqlCheckFree = @"
                        SELECT COUNT(1) FROM (
                            SELECT TOP (@top) id_cap_do FROM cap_do ORDER BY id_cap_do ASC
                        ) t WHERE t.id_cap_do = @id";
                    using (SqlCommand cmdFree = new SqlCommand(sqlCheckFree, conn))
                    {
                        cmdFree.Parameters.AddWithValue("@top", gioiHanMienPhi);
                        cmdFree.Parameters.AddWithValue("@id", idCapDo);
                        if (Convert.ToInt32(cmdFree.ExecuteScalar()) > 0)
                        {
                            TempData["Error"] = "Cấp độ này đang được miễn phí học thử, bạn có thể vào học trực tiếp.";
                            return RedirectToAction("khoadaotao", "Page");
                        }
                    }

                    using (SqlCommand cmd = new SqlCommand("sp_TaoDonHang", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@ma_don_hang", maDonHang);
                        cmd.Parameters.AddWithValue("@id_nguoi_dung", userId.Value);
                        cmd.Parameters.AddWithValue("@loai_giao_dich", "MUA_CAP_DO");
                        cmd.Parameters.AddWithValue("@id_cap_do", idCapDo);
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
                return RedirectToAction("khoadaotao", "Page");
            }

            return RedirectToAction("ThanhToan", new { maDonHang = maDonHang });
        }

        // GET: /MuaKhoaHoc/ThanhToan?maDonHang=CD...
        public ActionResult ThanhToan(string maDonHang)
        {
            int? userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("dangnhap", "User");
            if (string.IsNullOrEmpty(maDonHang)) return RedirectToAction("khoadaotao", "Page");

            DonHangCapDoViewModel vm = null;

            using (SqlConnection conn = new SqlConnection(LayChuoiKetNoiSql()))
            {
                conn.Open();
                string sql = @"SELECT gd.ma_don_hang, gd.id_cap_do, gd.gia_goc, gd.so_tien_giam, gd.thanh_tien, gd.trang_thai, gd.ngay_tao,
                                      cd.ten_cap_do, g.phan_tram_giam_khoa_hoc
                               FROM giao_dich gd
                               INNER JOIN cap_do cd ON gd.id_cap_do = cd.id_cap_do
                               CROSS JOIN goi_nang_cap g
                               WHERE gd.ma_don_hang = @ma
                                 AND gd.id_nguoi_dung = @uid
                                 AND gd.loai_giao_dich = 'MUA_CAP_DO'
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
                            decimal soTienGiam = Convert.ToDecimal(rd["so_tien_giam"]);
                            long soTienNguyen = Convert.ToInt64(thanhTien);

                            string qrUrl = string.Format(
                                "https://img.vietqr.io/image/{0}-{1}-compact2.png?amount={2}&addInfo={3}&accountName={4}",
                                MA_NGAN_HANG,
                                SO_TAI_KHOAN,
                                soTienNguyen,
                                Uri.EscapeDataString(maDonHang),
                                Uri.EscapeDataString(CHU_TAI_KHOAN)
                            );

                            vm = new DonHangCapDoViewModel
                            {
                                MaDonHang = rd["ma_don_hang"].ToString(),
                                IdCapDo = Convert.ToInt32(rd["id_cap_do"]),
                                TenCapDo = rd["ten_cap_do"].ToString(),
                                GiaGoc = Convert.ToDecimal(rd["gia_goc"]),
                                PhanTramGiam = soTienGiam > 0 ? Convert.ToInt32(rd["phan_tram_giam_khoa_hoc"]) : 0,
                                SoTienGiam = soTienGiam,
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