using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web.Mvc;
using wed_learn_e.Controllers;
using wed_learn_e.Models;

namespace wed_learn_e.Areas.Admin.Controllers
{
    public class QuanLyTaiChinhController : Controller
    {
        private readonly string _connStr = LayChuoiKetNoiSql();

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

        public ActionResult Index()
        {
            var vm = new QuanLyTaiChinhViewModel
            {
                GoiNangCap = new GoiNangCapAdminItem
                {
                    TenGoi = "Gói Nâng Cấp Tài Khoản Vĩnh Viễn",
                    GiaNangCap = 199000,
                    PhanTramGiam = 40,
                    MoTa = "",
                    NgayCapNhat = DateTime.Now
                }
            };

            using (SqlConnection conn = new SqlConnection(_connStr))
            {
                conn.Open();
                vm.SoCapDoMienPhi = MuaKhoaHocController.LayGioiHanCapDoMienPhi(conn);

                // 1. Thống kê tổng quan
                string sqlStats = @"
                    SELECT 
                        ISNULL((SELECT SUM(thanh_tien) FROM giao_dich WHERE trang_thai = 'SUCCESS'), 0) AS DoanhThu,
                        (SELECT COUNT(*) FROM giao_dich WHERE trang_thai = 'PENDING') AS DonCho,
                        (SELECT COUNT(*) FROM nguoi_dung_nang_cap) AS TongVip";
                using (SqlCommand cmd = new SqlCommand(sqlStats, conn))
                using (SqlDataReader rd = cmd.ExecuteReader())
                {
                    if (rd.Read())
                    {
                        vm.TongDoanhThu = Convert.ToDecimal(rd["DoanhThu"]);
                        vm.SoDonChoDuyet = Convert.ToInt32(rd["DonCho"]);
                        vm.TongTaiKhoanVip = Convert.ToInt32(rd["TongVip"]);
                    }
                }

                // 2. Cấu hình gói nâng cấp
                string sqlGoi = "SELECT * FROM goi_nang_cap WHERE id_goi = 1";
                using (SqlCommand cmd = new SqlCommand(sqlGoi, conn))
                using (SqlDataReader rd = cmd.ExecuteReader())
                {
                    if (rd.Read())
                    {
                        vm.GoiNangCap = new GoiNangCapAdminItem
                        {
                            TenGoi = rd["ten_goi"].ToString(),
                            GiaNangCap = Convert.ToDecimal(rd["gia_nang_cap"]),
                            PhanTramGiam = Convert.ToInt32(rd["phan_tram_giam_khoa_hoc"]),
                            MoTa = rd["mo_ta"] == DBNull.Value ? "" : rd["mo_ta"].ToString(),
                            NgayCapNhat = Convert.ToDateTime(rd["ngay_cap_nhat"])
                        };
                    }
                }

                // 3. Danh sách giao dịch
                string sqlGd = @"
                    SELECT gd.*, nd.ho_va_ten, nd.ten_dang_nhap,
                           CASE WHEN gd.loai_giao_dich = 'NANG_CAP' THEN N'Nâng cấp tài khoản VIP'
                                ELSE ISNULL(cd.ten_cap_do, N'Cấp độ #' + CAST(gd.id_cap_do AS NVARCHAR)) END AS ten_dich_vu
                    FROM giao_dich gd
                    INNER JOIN nguoi_dung nd ON gd.id_nguoi_dung = nd.id_nguoi_dung
                    LEFT JOIN cap_do cd ON gd.id_cap_do = cd.id_cap_do
                    ORDER BY CASE WHEN gd.trang_thai = 'PENDING' THEN 0 ELSE 1 END, gd.ngay_tao DESC";
                using (SqlCommand cmd = new SqlCommand(sqlGd, conn))
                using (SqlDataReader rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        vm.DanhSachGiaoDich.Add(new GiaoDichAdminItem
                        {
                            MaDonHang = rd["ma_don_hang"].ToString(),
                            HoVaTen = rd["ho_va_ten"].ToString(),
                            TenDangNhap = rd["ten_dang_nhap"].ToString(),
                            LoaiGiaoDich = rd["loai_giao_dich"].ToString(),
                            TenDichVu = rd["ten_dich_vu"].ToString(),
                            GiaGoc = Convert.ToDecimal(rd["gia_goc"]),
                            SoTienGiam = Convert.ToDecimal(rd["so_tien_giam"]),
                            ThanhTien = Convert.ToDecimal(rd["thanh_tien"]),
                            TrangThai = rd["trang_thai"].ToString(),
                            NgayTao = Convert.ToDateTime(rd["ngay_tao"]),
                            NgayThanhToan = rd["ngay_thanh_toan"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(rd["ngay_thanh_toan"])
                        });
                    }
                }

                // 4. Bảng giá theo Cấp độ (N cấp độ đầu miễn phí theo cấu hình Giới hạn học thử)
                string sqlCapDo = @"
                    SELECT cd.id_cap_do, cd.ten_cap_do,
                           ISNULL(gcd.gia_tien, 299000) AS gia_tien,
                           gcd.ngay_cap_nhat,
                           CASE WHEN EXISTS (
                               SELECT 1 FROM giao_dich gd 
                               WHERE gd.id_cap_do = cd.id_cap_do AND gd.loai_giao_dich = 'MUA_CAP_DO' AND gd.trang_thai = 'PENDING'
                           ) THEN 1 ELSE 0 END AS dang_cho
                    FROM cap_do cd
                    LEFT JOIN gia_cap_do gcd ON cd.id_cap_do = gcd.id_cap_do
                    ORDER BY cd.id_cap_do ASC";
                using (SqlCommand cmd = new SqlCommand(sqlCapDo, conn))
                using (SqlDataReader rd = cmd.ExecuteReader())
                {
                    int idx = 0;
                    while (rd.Read())
                    {
                        vm.DanhSachGiaCapDo.Add(new GiaCapDoAdminItem
                        {
                            IdCapDo = Convert.ToInt32(rd["id_cap_do"]),
                            TenCapDo = rd["ten_cap_do"].ToString(),
                            GiaTien = Convert.ToDecimal(rd["gia_tien"]),
                            IsMienPhiTheoGioiHan = idx < vm.SoCapDoMienPhi,
                            DangCoDonCho = Convert.ToInt32(rd["dang_cho"]) == 1,
                            NgayCapNhat = rd["ngay_cap_nhat"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(rd["ngay_cap_nhat"])
                        });
                        idx++;
                    }
                }

                // 5. Danh sách tài khoản đã nâng cấp VIP
                string sqlVip = @"
                    SELECT nc.id_nguoi_dung, nc.ngay_nang_cap, nd.ho_va_ten, nd.ten_dang_nhap, nd.email,
                           (SELECT COUNT(*) FROM nguoi_dung_cap_do WHERE id_nguoi_dung = nc.id_nguoi_dung) AS so_cap_do
                    FROM nguoi_dung_nang_cap nc
                    INNER JOIN nguoi_dung nd ON nc.id_nguoi_dung = nd.id_nguoi_dung
                    ORDER BY nc.ngay_nang_cap DESC";
                using (SqlCommand cmd = new SqlCommand(sqlVip, conn))
                using (SqlDataReader rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        vm.DanhSachTaiKhoanVip.Add(new TaiKhoanVipAdminItem
                        {
                            IdNguoiDung = Convert.ToInt32(rd["id_nguoi_dung"]),
                            HoVaTen = rd["ho_va_ten"].ToString(),
                            TenDangNhap = rd["ten_dang_nhap"].ToString(),
                            Email = rd["email"].ToString(),
                            NgayNangCap = Convert.ToDateTime(rd["ngay_nang_cap"]),
                            SoCapDoDaMua = Convert.ToInt32(rd["so_cap_do"])
                        });
                    }
                }
            }

            return View(vm);
        }

        [HttpPost]
        public JsonResult DuyetGiaoDich(string maDonHang)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_connStr))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("sp_XacNhanThanhToan", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@ma_don_hang", maDonHang);
                        cmd.ExecuteNonQuery();
                    }
                }
                return Json(new { success = true, message = $"Đã xác nhận thanh toán đơn {maDonHang}." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult HuyGiaoDich(string maDonHang)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_connStr))
                {
                    conn.Open();
                    string sql = "UPDATE giao_dich SET trang_thai = 'FAILED' WHERE ma_don_hang = @ma AND trang_thai = 'PENDING'";
                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@ma", maDonHang);
                        int rows = cmd.ExecuteNonQuery();
                        if (rows == 0)
                            return Json(new { success = false, message = "Chỉ có thể hủy giao dịch đang ở trạng thái chờ (PENDING)." });
                    }
                }
                return Json(new { success = true, message = $"Đã hủy đơn hàng {maDonHang}." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult CapNhatGoiNangCap(string tenGoi, decimal giaNangCap, int phanTramGiam, string moTa)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_connStr))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("sp_AdminCapNhatGoiNangCap", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@ten_goi", tenGoi?.Trim() ?? "");
                        cmd.Parameters.AddWithValue("@gia_nang_cap", giaNangCap);
                        cmd.Parameters.AddWithValue("@phan_tram_giam", phanTramGiam);
                        cmd.Parameters.AddWithValue("@mo_ta", moTa?.Trim() ?? "");
                        cmd.ExecuteNonQuery();
                    }
                }
                return Json(new { success = true, message = "Cập nhật cấu hình gói nâng cấp thành công." });
            }
            catch (SqlException ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult CapNhatGiaCapDo(int idCapDo, decimal giaTien)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_connStr))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("sp_AdminCapNhatGiaCapDo", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@id_cap_do", idCapDo);
                        cmd.Parameters.AddWithValue("@gia_tien", giaTien);
                        cmd.ExecuteNonQuery();
                    }
                }
                return Json(new { success = true, message = "Đã cập nhật giá cấp độ." });
            }
            catch (SqlException ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult ThuHoiVip(int idNguoiDung)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_connStr))
                {
                    conn.Open();
                    string sql = @"
                        IF EXISTS (SELECT 1 FROM giao_dich WHERE id_nguoi_dung = @uid AND trang_thai = 'PENDING')
                            RAISERROR(N'Người dùng này đang có giao dịch chờ thanh toán, không thể thu hồi trạng thái VIP.', 16, 1);
                        ELSE
                            DELETE FROM nguoi_dung_nang_cap WHERE id_nguoi_dung = @uid;";
                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@uid", idNguoiDung);
                        cmd.ExecuteNonQuery();
                    }
                }
                return Json(new { success = true, message = "Đã thu hồi trạng thái nâng cấp của tài khoản." });
            }
            catch (SqlException ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}