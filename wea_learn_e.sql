-- 1. Bảng lưu trữ 6 cấp độ (Cap_Do)
CREATE TABLE cap_do (
    id_cap_do INT IDENTITY(1,1) PRIMARY KEY,
    ten_cap_do NVARCHAR(50) NOT NULL, -- Ví dụ: Cơ bản, Nâng cao...
    mo_ta NVARCHAR(MAX)
);

-- 2. Bảng Người dùng & Phân quyền (Nguoi_Dung)
CREATE TABLE nguoi_dung (
    id_nguoi_dung INT IDENTITY(1,1) PRIMARY KEY,
    ten_dang_nhap VARCHAR(50) UNIQUE NOT NULL,
    mat_khau VARCHAR(255) NOT NULL,
    ho_va_ten NVARCHAR(100) NOT NULL,
    email VARCHAR(100) UNIQUE NOT NULL,
    -- Giả lập ENUM bằng CHECK constraint
    vai_tro VARCHAR(20) DEFAULT 'nguoi_dung' CHECK (vai_tro IN ('nguoi_dung', 'quan_tri_vien')), 
    id_cap_do_hien_tai INT,
    ngay_tao DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (id_cap_do_hien_tai) REFERENCES cap_do(id_cap_do)
);

-- 3. Bảng Bài kiểm tra đầu vào (Bai_Kiem_Tra_Dau_Vao)
CREATE TABLE bai_kiem_tra_dau_vao (
    id_bai_kiem_tra INT IDENTITY(1,1) PRIMARY KEY,
    tieu_de NVARCHAR(200) NOT NULL,
    mo_ta NVARCHAR(MAX),
    nguoi_tao INT,
    FOREIGN KEY (nguoi_tao) REFERENCES nguoi_dung(id_nguoi_dung)
);

-- Bảng lưu trữ danh sách câu hỏi cho bài test
CREATE TABLE cau_hoi_kiem_tra (
    id_cau_hoi INT IDENTITY(1,1) PRIMARY KEY,
    id_bai_kiem_tra INT,
    noi_dung_cau_hoi NVARCHAR(MAX) NOT NULL,
    dap_an_a NVARCHAR(255),
    dap_an_b NVARCHAR(255),
    dap_an_c NVARCHAR(255),
    dap_an_d NVARCHAR(255),
    dap_an_dung CHAR(1), -- Lưu 'A', 'B', 'C', hoặc 'D'
    FOREIGN KEY (id_bai_kiem_tra) REFERENCES bai_kiem_tra_dau_vao(id_bai_kiem_tra)
);

-- 4. Bảng Kết quả kiểm tra đầu vào (Ket_Qua_Kiem_Tra)
CREATE TABLE ket_qua_kiem_tra (
    id_ket_qua INT IDENTITY(1,1) PRIMARY KEY,
    id_nguoi_dung INT,
    id_bai_kiem_tra INT,
    diem_so INT NOT NULL,
    id_cap_do_dat_duoc INT,
    ngay_lam_bai DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (id_nguoi_dung) REFERENCES nguoi_dung(id_nguoi_dung),
    FOREIGN KEY (id_bai_kiem_tra) REFERENCES bai_kiem_tra_dau_vao(id_bai_kiem_tra),
    FOREIGN KEY (id_cap_do_dat_duoc) REFERENCES cap_do(id_cap_do)
);

-- 5. Bảng Khóa học (Khoa_Hoc) - Chứa các mục như trong ảnh
CREATE TABLE khoa_hoc (
    id_khoa_hoc INT IDENTITY(1,1) PRIMARY KEY,
    id_cap_do INT, 
    ten_khoa_hoc NVARCHAR(200) NOT NULL, 
    duong_dan_icon VARCHAR(255), 
    mo_ta NVARCHAR(MAX),
    trang_thai_hoat_dong BIT DEFAULT 1, -- Dùng BIT thay cho BOOLEAN
    FOREIGN KEY (id_cap_do) REFERENCES cap_do(id_cap_do)
);

-- 6. Bảng Tiến độ học tập (Tien_Do_Hoc_Tap)
CREATE TABLE tien_do_hoc_tap (
    id_tien_do INT IDENTITY(1,1) PRIMARY KEY,
    id_nguoi_dung INT,
    id_khoa_hoc INT,
    phan_tram_hoan_thanh DECIMAL(5,2) DEFAULT 0.00,
    trang_thai VARCHAR(20) DEFAULT 'chua_bat_dau' CHECK (trang_thai IN ('chua_bat_dau', 'dang_hoc', 'da_hoan_thanh')),
    lan_truy_cap_cuoi DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (id_nguoi_dung) REFERENCES nguoi_dung(id_nguoi_dung),
    FOREIGN KEY (id_khoa_hoc) REFERENCES khoa_hoc(id_khoa_hoc)
);
CREATE TABLE tu_vung (
    id_tu_vung INT IDENTITY(1,1) PRIMARY KEY,
    id_khoa_hoc INT, -- Liên kết để biết từ này thuộc khóa học nào
    tu_tieng_anh VARCHAR(100) NOT NULL,
    loai_tu VARCHAR(50), -- Ví dụ: Danh từ, Động từ, Tính từ...
    nghia_tieng_viet NVARCHAR(255) NOT NULL,
    phien_am NVARCHAR(100),
    vi_du NVARCHAR(MAX),
    FOREIGN KEY (id_khoa_hoc) REFERENCES khoa_hoc(id_khoa_hoc)
);
CREATE TABLE bai_giang_video (
    id_video INT IDENTITY(1,1) PRIMARY KEY,
    id_khoa_hoc INT,
    tieu_de NVARCHAR(200) NOT NULL,
    duong_dan_video VARCHAR(MAX) NOT NULL, -- Lưu link Youtube, Vimeo, hoặc file MP4
    thoi_luong_phut INT,
    FOREIGN KEY (id_khoa_hoc) REFERENCES khoa_hoc(id_khoa_hoc)
);
CREATE TABLE bai_tap_thuc_hanh (
    id_bai_tap INT IDENTITY(1,1) PRIMARY KEY,
    id_khoa_hoc INT,
    tieu_de NVARCHAR(200) NOT NULL,
    -- Phân loại bài tập để code web biết cách hiển thị giao diện phù hợp
    loai_bai_tap VARCHAR(50) CHECK (loai_bai_tap IN ('trac_nghiem', 'tu_luan_viet', 'ghi_am_noi')),
    noi_dung_cau_hoi NVARCHAR(MAX) NOT NULL,
    dap_an_mau NVARCHAR(MAX), -- Dùng để chấm điểm hoặc cho user tham khảo
    FOREIGN KEY (id_khoa_hoc) REFERENCES khoa_hoc(id_khoa_hoc)
);
-- Thêm cột theo dõi vào bảng khóa học
ALTER TABLE khoa_hoc ADD 
    nguoi_tao INT FOREIGN KEY REFERENCES nguoi_dung(id_nguoi_dung),
    nguoi_cap_nhat INT FOREIGN KEY REFERENCES nguoi_dung(id_nguoi_dung),
    ngay_cap_nhat DATETIME;

-- Thêm cột theo dõi vào bảng câu hỏi kiểm tra
ALTER TABLE cau_hoi_kiem_tra ADD 
    nguoi_tao INT FOREIGN KEY REFERENCES nguoi_dung(id_nguoi_dung),
    nguoi_cap_nhat INT FOREIGN KEY REFERENCES nguoi_dung(id_nguoi_dung),
    ngay_cap_nhat DATETIME;
CREATE TABLE nhat_ky_quan_tri (
    id_nhat_ky INT IDENTITY(1,1) PRIMARY KEY,
    id_admin INT NOT NULL, -- ID của người quản trị (lấy từ bảng nguoi_dung)
    hanh_dong VARCHAR(50), -- Ví dụ: 'THEM_KHOA_HOC', 'SUA_CAU_HOI', 'XOA_TU_VUNG'
    ten_bang_tac_dong VARCHAR(50), -- Tên bảng bị thay đổi (vd: 'khoa_hoc')
    id_ban_ghi_tac_dong INT, -- ID của khóa học/câu hỏi cụ thể bị thay đổi
    chi_tiet_cu NVARCHAR(MAX), -- Dữ liệu trước khi sửa
    chi_tiet_moi NVARCHAR(MAX), -- Dữ liệu sau khi sửa
    ngay_thuc_hien DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (id_admin) REFERENCES nguoi_dung(id_nguoi_dung)
);
-- 1. Thêm 6 cấp độ tiếng Anh chuẩn
INSERT INTO cap_do (ten_cap_do, mo_ta) 
VALUES
(N'Beginner (A1)', N'Dành cho người mới bắt đầu, chưa biết gì về tiếng Anh.'),
(N'Elementary (A2)', N'Trình độ sơ cấp, giao tiếp được các tình huống cơ bản.'),
(N'Intermediate (B1)', N'Trình độ trung cấp, có thể diễn đạt ý kiến cá nhân.'),
(N'Upper-Intermediate (B2)', N'Trình độ trung cấp cao, giao tiếp trôi chảy với người bản xứ.'),
(N'Advanced (C1)', N'Trình độ cao cấp, sử dụng ngôn ngữ linh hoạt trong học thuật và công việc.'),
(N'Proficient (C2)', N'Trình độ thành thạo như người bản xứ.');

-- 2. Thêm 1 Quản trị viên (Admin) và 1 Người dùng (User)
-- Lưu ý: Mật khẩu thực tế nên được mã hóa (hash), ở đây để dạng chữ cho dễ nhìn
INSERT INTO nguoi_dung (ten_dang_nhap, mat_khau, ho_va_ten, email, vai_tro, id_cap_do_hien_tai) 
VALUES
('admin_hieu', '123456', N'Quản Trị Viên Hiếu', 'admin@example.com', 'quan_tri_vien', NULL),
('nguoidung01', 'password123', N'Nguyễn Văn Học', 'hocvien@example.com', 'nguoi_dung', 1);

-- 3. Thêm Bài kiểm tra đầu vào và Câu hỏi (Do Admin id=1 tạo)
INSERT INTO bai_kiem_tra_dau_vao (tieu_de, mo_ta, nguoi_tao) 
VALUES
(N'Bài Kiểm Tra Trình Độ Đầu Vào', N'Bài test gồm 20 câu hỏi giúp hệ thống phân loại cấp độ A1-C2 của bạn.', 1);

INSERT INTO cau_hoi_kiem_tra (id_bai_kiem_tra, noi_dung_cau_hoi, dap_an_a, dap_an_b, dap_an_c, dap_an_d, dap_an_dung) 
VALUES
(1, N'Hello, how _____ you?', 'is', 'am', 'are', 'be', 'C'),
(1, N'She _____ to the supermarket yesterday.', 'go', 'goes', 'went', 'going', 'C'),
(1, N'If I _____ you, I would study harder.', 'am', 'was', 'were', 'have been', 'C');

-- 4. Thêm các Khóa Học như trong ảnh thiết kế (Gán tạm cho cấp độ A1 - id_cap_do = 1)
INSERT INTO khoa_hoc (id_cap_do, ten_khoa_hoc, duong_dan_icon, mo_ta, trang_thai_hoat_dong) 
VALUES
(1, N'1000 Từ Vựng Thông Dụng', 'icon_book.png', N'Học và ghi nhớ 1000 từ vựng quan trọng nhất.', 1),
(1, N'Luyện Nghe', 'icon_headphone.png', N'Các bài luyện nghe từ cơ bản đến nâng cao.', 1),
(1, N'Luyện Nói', 'icon_speak.png', N'Thực hành giao tiếp tiếng Anh hàng ngày.', 1),
(1, N'Ngữ Pháp Cơ Bản', 'icon_grammar.png', N'Ôn tập và luyện tập các cấu trúc ngữ pháp.', 1),
(1, N'Mini Test Hàng Ngày', 'icon_medal.png', N'Các bài kiểm tra nhỏ giúp bạn luyện tập.', 1),
(1, N'Game Từ Vựng', 'icon_gamepad.png', N'Học từ vựng qua trò chơi thú vị.', 1),
(1, N'Học Qua Video', 'icon_clapperboard.png', N'Học qua video và hội thoại thực tế.', 1),
(1, N'Luyện Viết', 'icon_pen.png', N'Luyện viết câu và đoạn văn.', 1),
(1, N'Thành Ngữ Tiếng Anh', 'icon_bulb.png', N'Học các idioms thông dụng.', 1);

-- 5. Thêm dữ liệu chi tiết vào Bảng Từ Vựng (Cho khóa học ID = 1 là 1000 Từ vựng)
INSERT INTO tu_vung (id_khoa_hoc, tu_tieng_anh, loai_tu, nghia_tieng_viet, phien_am, vi_du) 
VALUES
(1, 'Database', N'Danh từ', N'Cơ sở dữ liệu', '/ˈdeɪ.tə.beɪs/', N'He is designing a database for the website.'),
(1, 'Develop', N'Động từ', N'Phát triển', '/dɪˈvel.əp/', N'She wants to develop a new application.'),
(1, 'Skill', N'Danh từ', N'Kỹ năng', '/skɪl/', N'Communication is an important skill.');

-- 6. Thêm dữ liệu Tiến độ học tập cho User (User ID = 2 đang học Khóa 1000 Từ Vựng)
INSERT INTO tien_do_hoc_tap (id_nguoi_dung, id_khoa_hoc, phan_tram_hoan_thanh, trang_thai) 
VALUES
(2, 1, 15.50, 'dang_hoc');