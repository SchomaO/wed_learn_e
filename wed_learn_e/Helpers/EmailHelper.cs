using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;

namespace wed_learn_e.Helpers
{
    public static class EmailHelper
    {
        private static void GuiEmailQuaMailKit(string emailNhan, string hoTen, string tieuDe, string noiDungHtml)
        {
            string emailGui = ConfigurationManager.AppSettings["EmailGuiHeThong"]?.Trim();
            string matKhau = ConfigurationManager.AppSettings["MatKhauUngDungEmail"]?.Replace(" ", "").Trim();

            if (string.IsNullOrWhiteSpace(emailGui) || string.IsNullOrWhiteSpace(matKhau))
                throw new Exception($"Thiếu cấu hình: Email='{emailGui}', PassLength={matKhau?.Length ?? 0}");

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("KityLearn Support", emailGui));
            message.To.Add(new MailboxAddress(hoTen, emailNhan.Trim()));
            message.Subject = tieuDe;
            message.Body = new TextPart("html") { Text = noiDungHtml };

            using (var logStream = new System.IO.MemoryStream())
            {
                try
                {
                    using (var client = new MailKit.Net.Smtp.SmtpClient(new MailKit.ProtocolLogger(logStream)))
                    {
                        client.ServerCertificateValidationCallback = (s, c, h, e) => true;
                        client.CheckCertificateRevocation = false;

                        // Đổi sang cổng 587 với StartTls
                        client.Connect("smtp.gmail.com", 587, SecureSocketOptions.StartTls);
                        client.Authenticate(emailGui, matKhau);
                        client.Send(FormatOptions.Default, message);
                        client.Disconnect(true);
                    }
                }
                catch (Exception ex)
                {
                    string logChiTiet = System.Text.Encoding.UTF8.GetString(logStream.ToArray());
                    throw new Exception($"{ex.Message} | EmailGui={emailGui} | Log: {logChiTiet}");
                }
            }
        }

        public static bool GuiMailXacNhanThanhToan(
            string emailNhan, string hoTen, string maDonHang,
            string loaiGiaoDich, string tenDichVu, decimal soTien, out string loiChiTiet)
        {
            loiChiTiet = "";
            if (string.IsNullOrWhiteSpace(emailNhan))
            {
                loiChiTiet = "Tài khoản học viên chưa có địa chỉ email.";
                return false;
            }

            try
            {
                string tieuDe = loaiGiaoDich == "NANG_CAP"
                    ? "[KityLearn] Xác nhận nâng cấp tài khoản VIP thành công!"
                    : $"[KityLearn] Mở khóa cấp độ {tenDichVu} thành công!";

                string moTaQuyenLoi = loaiGiaoDich == "NANG_CAP"
                    ? "Tài khoản của bạn đã được nâng cấp lên <strong>VIP vĩnh viễn</strong>. Bạn sẽ được giảm ngay <strong>40% học phí</strong> khi mở khóa tất cả các cấp độ trên hệ thống."
                    : $"Cấp độ <strong>{tenDichVu}</strong> đã được mở khóa <strong>vĩnh viễn trọn đời</strong>. Bạn có thể vào luyện tập đầy đủ các kỹ năng ngay bây giờ.";

                string noiDungHtml = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #dbeafe; border-radius: 12px; overflow: hidden;'>
                    <div style='background: #0d6efd; color: #ffffff; padding: 20px 24px; text-align: center;'>
                        <h2 style='margin: 0;'>KityLearn - Xác Nhận Thanh Toán</h2>
                    </div>
                    <div style='padding: 24px; color: #1e293b; line-height: 1.6;'>
                        <p>Xin chào <strong>{hoTen}</strong>,</p>
                        <p>Quản trị viên đã xác nhận giao dịch thanh toán của bạn thành công. Chi tiết đơn hàng:</p>
                        <table style='width: 100%; border-collapse: collapse; margin: 16px 0; background: #f8fafc;'>
                            <tr><td style='padding: 10px; border: 1px solid #e2e8f0;'><strong>Mã đơn hàng:</strong></td><td style='padding: 10px; border: 1px solid #e2e8f0; color: #0d6efd; font-weight: bold;'>{maDonHang}</td></tr>
                            <tr><td style='padding: 10px; border: 1px solid #e2e8f0;'><strong>Dịch vụ kích hoạt:</strong></td><td style='padding: 10px; border: 1px solid #e2e8f0;'>{tenDichVu}</td></tr>
                            <tr><td style='padding: 10px; border: 1px solid #e2e8f0;'><strong>Số tiền đã thanh toán:</strong></td><td style='padding: 10px; border: 1px solid #e2e8f0; color: #16a34a; font-weight: bold;'>{soTien:N0}đ</td></tr>
                            <tr><td style='padding: 10px; border: 1px solid #e2e8f0;'><strong>Thời gian kích hoạt:</strong></td><td style='padding: 10px; border: 1px solid #e2e8f0;'>{DateTime.Now:dd/MM/yyyy HH:mm}</td></tr>
                        </table>
                        <p style='background: #eef6ff; padding: 12px 16px; border-radius: 8px; border-left: 4px solid #0d6efd;'>{moTaQuyenLoi}</p>
                    </div>
                </div>";

                GuiEmailQuaMailKit(emailNhan, hoTen, tieuDe, noiDungHtml);
                return true;
            }
            catch (Exception ex)
            {
                loiChiTiet = ex.InnerException != null ? $"{ex.Message} -> {ex.InnerException.Message}" : ex.Message;
                return false;
            }
        }

        public static bool GuiMailHuyGiaoDich(
            string emailNhan, string hoTen, string maDonHang,
            string tenDichVu, decimal soTien, string lyDo, out string loiChiTiet)
        {
            loiChiTiet = "";
            if (string.IsNullOrWhiteSpace(emailNhan))
            {
                loiChiTiet = "Tài khoản học viên chưa có địa chỉ email.";
                return false;
            }

            try
            {
                string tieuDe = $"[KityLearn] Thông báo hủy đơn hàng {maDonHang}";
                string noiDungHtml = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #fecaca; border-radius: 12px; overflow: hidden;'>
                    <div style='background: #dc2626; color: #ffffff; padding: 20px 24px; text-align: center;'>
                        <h2 style='margin: 0;'>KityLearn - Thông Báo Hủy Giao Dịch</h2>
                    </div>
                    <div style='padding: 24px; color: #1e293b; line-height: 1.6;'>
                        <p>Xin chào <strong>{hoTen}</strong>,</p>
                        <p>Đơn hàng thanh toán của bạn trên hệ thống <strong>KityLearn</strong> đã bị hủy. Chi tiết đơn hàng:</p>
                        <table style='width: 100%; border-collapse: collapse; margin: 16px 0; background: #f8fafc;'>
                            <tr><td style='padding: 10px; border: 1px solid #e2e8f0;'><strong>Mã đơn hàng:</strong></td><td style='padding: 10px; border: 1px solid #e2e8f0; color: #dc2626; font-weight: bold;'>{maDonHang}</td></tr>
                            <tr><td style='padding: 10px; border: 1px solid #e2e8f0;'><strong>Dịch vụ đăng ký:</strong></td><td style='padding: 10px; border: 1px solid #e2e8f0;'>{tenDichVu}</td></tr>
                            <tr><td style='padding: 10px; border: 1px solid #e2e8f0;'><strong>Số tiền đơn hàng:</strong></td><td style='padding: 10px; border: 1px solid #e2e8f0; font-weight: bold;'>{soTien:N0}đ</td></tr>
                            <tr><td style='padding: 10px; border: 1px solid #e2e8f0;'><strong>Thời gian hủy:</strong></td><td style='padding: 10px; border: 1px solid #e2e8f0;'>{DateTime.Now:dd/MM/yyyy HH:mm}</td></tr>
                        </table>
                        <div style='background: #fef2f2; padding: 12px 16px; border-radius: 8px; border-left: 4px solid #dc2626; color: #991b1b;'>
                            <strong>Lý do hủy:</strong> {lyDo}
                        </div>
                    </div>
                </div>";

                GuiEmailQuaMailKit(emailNhan, hoTen, tieuDe, noiDungHtml);
                return true;
            }
            catch (Exception ex)
            {
                loiChiTiet = ex.InnerException != null ? $"{ex.Message} -> {ex.InnerException.Message}" : ex.Message;
                return false;
            }
        }
    }
}