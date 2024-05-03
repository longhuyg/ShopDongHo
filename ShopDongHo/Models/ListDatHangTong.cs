using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using static log4net.Appender.RollingFileAppender;

namespace ShopDongHo.Models
{
    public class ListDatHangTong
    {
        public int? ID { get; set; }
        public int? NhanVien_ID { get; set; }
        public int? KhachHang_ID { get; set; }
        public string HoVaten { get; set; }
        public string DienThoaiGiaoHang { get; set; }
        public string DiaChiGiaoHang { get; set; }
        public int? TinhTrang { get; set; }
        public int? DatHang_ID { get; set; }
        public int? DongHo_ID { get; set; }
        public string TenDongHo { get; set; }
        public Nullable<int> SoLuong { get; set; }
        public Nullable<int> DonGia { get; set; }
        public string HinhAnhDH { get; set; }
        public string MoTa { get; set; }
        public DateTime? NgayDatHang { get; set; }

    }
}