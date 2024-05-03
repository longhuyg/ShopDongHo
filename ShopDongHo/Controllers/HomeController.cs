using log4net;
using Microsoft.Azure.Documents;
using MoMo;
using Newtonsoft.Json.Linq;
using ShopDongHo.Libs;
using ShopDongHo.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Runtime.Remoting.Messaging;
using System.Web.Mvc;
using System.Web.Services.Description;
using VNPAY_CS_ASPX;

namespace ShopDongHo.Controllers
{
    public class HomeController : Controller
	{
		private ShopDongHoEntities db = new ShopDongHoEntities();

		public string DienThoaiGiaoHangVP, DiaChiGiaoHangVP;
        // GET: Home
        public ActionResult Index()
		{
			var dongho = db.DongHo.Where(r => r.SoLuong > 0).ToList();
			return View(dongho);
		}

		public ActionResult _Catelogies()
		{
			var loai = db.ThuongHieu.OrderBy(r => r.TenThuongHieu).ToList();
			return PartialView(loai);
		}

		public ActionResult SubPosts(int id)
		{
			var baiviet = db.DongHo.Where(r => r.ThuongHieu_ID == id ).ToList();
			return View(baiviet);
		}

		[HttpPost]
		public ActionResult Search(FormCollection collection)
		{
			string tukhoa = collection["Tukhoa"].ToString();
			var baiViet = db.DongHo.Where(r=>r.TenDongHo.Contains(tukhoa) || r.MoTa.Contains(tukhoa)).ToList();
			return View(baiViet);
		}

		public ActionResult Details(int maSP)
		{
			var dongho = db.DongHo.Where(r => r.ID == maSP ).SingleOrDefault();
			return View(dongho);
		}

		public ActionResult BuyMost()
		{
			var bestSale = (from dh in db.DongHo
							join ct in db.DatHang_ChiTiet on dh.ID equals ct.DongHo_ID
							join dhang in db.DatHang on ct.DatHang_ID equals dhang.ID
							where (ct.SoLuong > 0)							
							select new BestSaleModels(){ 
								TenDongHo =dh.TenDongHo,
								HinhAnhDH=dh.HinhAnhDH,
								DonGia =dh.DonGia,
								ID=dh.ID,
								SoLuong=ct.SoLuong
							}).OrderByDescending(ct =>ct.SoLuong).Distinct().ToList();
			
			return View(bestSale);
		}

		public ActionResult MyOrders()
		{
			int makh = Convert.ToInt32(Session["MaKhachHang"]);
			var Myorders = (from dh in db.DongHo
							join ct in db.DatHang_ChiTiet on dh.ID equals ct.DongHo_ID
							join dhang in db.DatHang on ct.DatHang_ID equals dhang.ID 
							join kh in db.KhachHang on  dhang.KhachHang_ID equals kh.ID 
							where(kh.ID == makh)

							select new Myorders()
							{
								TenDongHo = dh.TenDongHo,
								HinhAnhDH = dh.HinhAnhDH,
								DonGia = ct.DonGia,
								ID = kh.ID,
								SoLuong = ct.SoLuong,
								NgayDatHang = dhang.NgayDatHang

							}).OrderByDescending(dhang => dhang.NgayDatHang).ToList();

			return View(Myorders);
		}

		public ActionResult ChangePassword()
		{
			//ModelState.AddModelError("ErrorChangePassword", "");
			return View();
			
		}
		[HttpPost]
		[ValidateAntiForgeryToken]
		public ActionResult ChangePassword([Bind(Include = "MatKhau,MatKhauMoi,XacNhanMatKhauMoi")] KhachHangChangePassword khachHangChangePassword )
		{	
			if (ModelState.IsValid)
			{
				int makh = Convert.ToInt32(Session["MaKhachHang"]);
				KhachHang khachHang = db.KhachHang.Find(makh);				
				if(khachHang == null)
                {
					return HttpNotFound();
                }
				khachHangChangePassword.MatKhau = SHA1.ComputeHash(khachHangChangePassword.MatKhau);
				if(khachHang.MatKhau == khachHangChangePassword.MatKhau)
                {
					khachHangChangePassword.MatKhauMoi = SHA1.ComputeHash(khachHangChangePassword.MatKhauMoi);
					khachHangChangePassword.XacNhanMatKhauMoi = khachHangChangePassword.MatKhauMoi;

					khachHang.MatKhau = khachHangChangePassword.MatKhauMoi;
					//khachHang.XacNhanMatKhau = khachHangChangePassword.MatKhauMoi;

					db.Entry(khachHang).State = EntityState.Modified;
					db.SaveChanges();
					return RedirectToAction("Login");
				}
                else
                {
					ViewBag.error = "Mật khẩu cũ không đúng !!!";
					return View();
				}

				
			}
			return View(khachHangChangePassword);
		}

		public ActionResult success()
		{
			return View();
		}
		//GET: Register
		public ActionResult Register()
		{
			return View();
		}

		//POST: Register
		[HttpPost]
		[ValidateAntiForgeryToken]
		public ActionResult Register(KhachHang khachHang)
		{
			if (ModelState.IsValid)
			{
				var check = db.KhachHang.FirstOrDefault(r => r.TenDangNhap == khachHang.TenDangNhap);
				if (check == null)
				{
					khachHang.MatKhau = SHA1.ComputeHash(khachHang.MatKhau);
					//khachHang.XacNhanMatKhau = SHA1.ComputeHash(khachHang.XacNhanMatKhau);

					db.Configuration.ValidateOnSaveEnabled = false;
					db.KhachHang.Add(khachHang);
					db.SaveChanges();
					return RedirectToAction("success");
				}
				else
				{
					ViewBag.error = "Tên đăng nhập đã tồn tại !!!";
					return View();
				}
			}
			return View();
		}

		// GET: Home/ThanhToan
		public ActionResult ThanhToan()
		{
			if (Session["MaKhachHang"] == null)
			{
				return RedirectToAction("Login", "Home");
			}
			else
			{
				return View();
			}
		}

		// POST: Home/ThanhToan
		[HttpPost]
		[ValidateAntiForgeryToken]
		public ActionResult ThanhToan(DatHang datHang)
		{
			if (ModelState.IsValid)
			{
				// Lưu vào bảng DatHang
				DatHang dh = new DatHang();
				dh.DiaChiGiaoHang = datHang.DiaChiGiaoHang;
				dh.DienThoaiGiaoHang = datHang.DienThoaiGiaoHang;
				dh.NgayDatHang = DateTime.Now;
				dh.KhachHang_ID = Convert.ToInt32(Session["MaKhachHang"]);
				dh.TinhTrang = 0;
				db.DatHang.Add(dh);
				db.SaveChanges();

				// Lưu vào bảng DatHang_ChiTiet
				List<SanPhamTrongGio> cart = (List<SanPhamTrongGio>)Session["cart"];
				foreach (var item in cart)
				{
					DatHang_ChiTiet ct = new DatHang_ChiTiet();
					ct.DatHang_ID = dh.ID;
					ct.DongHo_ID = item.dongho.ID;
					ct.SoLuong = Convert.ToInt16(item.soLuongTrongGio);
					ct.DonGia = item.dongho.DonGia;
					db.DatHang_ChiTiet.Add(ct);
					var dongho = db.DongHo.Find(item.dongho.ID);
					dongho.SoLuong -= item.soLuongTrongGio;
					db.SaveChanges();
				}

				// Xóa giỏ hàng
				cart.Clear();

				// Quay về trang chủ
				return RedirectToAction("Index", "Home");
			}

			return View(datHang);
		}

		public ActionResult ThanhToanMoMo()
		{
            List<SanPhamTrongGio> cart = (List<SanPhamTrongGio>)Session["cart"];

			//string endpoint = ConfigurationManager.AppSettings["endpoint"].ToString();
			//string partnerCode = ConfigurationManager.AppSettings["partnerCode"].ToString();
			//         string accessKey = ConfigurationManager.AppSettings["accessKey"].ToString();
			//         string serectKey = ConfigurationManager.AppSettings["serectKey"].ToString();
			//string orderInfo = "DH" + DateTime.Now.ToString("yyyyMMddHHmmss");
			//         string returnUrl = ConfigurationManager.AppSettings["returnUrl"].ToString();
			//string notifyurl = ConfigurationManager.AppSettings["notifyurl"].ToString();
			/// 
			string endpoint = "https://test-payment.momo.vn/gw_payment/transactionProcessor";
			string partnerCode = "MOMOOJOI20210710";
			string accessKey = "iPXneGmrJH0G8FOP";
			string serectKey = "sFcbSGRSJjwGxwhhcEktCHWYUuTuPNDB";
			string orderInfo = "test";
			string returnUrl = "https://localhost:44399/Home/ReturnUrl";
			string notifyurl = "https://webhook.site/f70e0cd1-078e-4ef4-891f-a82978758e9a";

            string amount = cart.Sum(n => n.dongho.DonGia).ToString();
            string orderid = Guid.NewGuid().ToString();
            string requestId = Guid.NewGuid().ToString();
            string extraData = "";

			string rawHash = "partnerCode=" + partnerCode + 
				"&accessKey=" + accessKey +
                "&requestId=" + requestId +
                "&amount=" + amount +
                "&orderId=" + orderid +
                "&orderInfo=" + orderInfo + 
				"&returnUrl=" + returnUrl + 
				"&notifyUrl=" + notifyurl +
				"&extraData=" + extraData;
            MoMoSecurity crypto = new MoMoSecurity();
			string signature = crypto.signSHA256(rawHash, serectKey);
			JObject message = new JObject
			{
				{ "partnerCode", partnerCode },
				{ "accessKey", accessKey },
				{ "requestId", requestId },
				{ "amount", amount },
				{ "orderId", orderid },
				{ "orderInfo", orderInfo },
				{ "returnUrl", returnUrl },
				{ "notifyUrl", notifyurl },
				{ "requestType", "captureMoMoWallet" },
				{ "signature", signature }
			};
			string responseFromMomo = PaymentRequest.sendPaymentRequest(endpoint, message.ToString());
			
			JObject jmessage = JObject.Parse(responseFromMomo);

            return Redirect(jmessage.GetValue("payUrl").ToString());
        }

		public ActionResult ReturnUrl() {
            string param = Request.QueryString.ToString().Substring(0, Request.QueryString.ToString().IndexOf("signature") - 1);
			param = Server.UrlEncode(param);
			MoMoSecurity crypto = new MoMoSecurity();
			string serectKey = "sFcbSGRSJjwGxwhhcEktCHWYUuTuPNDB";
            string signature = crypto.signSHA256(param, serectKey);
			if (signature != Request["signature"].ToString())
			{
				ViewBag.message = "Thông tin Request không hợp lệ";
				return View();
			}
			if (!Request["errorCode"].Equals("0"))
			{
				ViewBag.message = "Thanh toán thất bại";
				return View();
			}
			else
			{
				ViewBag.message = "Thanh toán thành công";
                Session["cart"] = new List<SanPhamTrongGio>();
            }

            return View();
		} 

		public JsonResult Notifyurl()
		{
			string param = "";
            param = "partner_code=" + Request["partner_code"] +
				"&access_key = " + Request["access_key"] +
				"&amount=" + Request["amount"] +
				"&order_id=" + Request["order_id"] +
				"&order_info = " + Request["order_info"] +
				"&order_type = " + Request["order_type"] +
				"&transaction_id = " + Request["transaction_id"] +
				"&message = " + Request["message"] +
				"&response_time = " + Request["response_time"] +
				"&status_code=" + Request["status_code"];

			param = Server.UrlEncode(param);
			MoMoSecurity crypto = new MoMoSecurity();
			
            string serectKey = "sFcbSGRSJjwGxwhhcEktCHWYUuTuPNDB";
            string signature = crypto.signSHA256(param, serectKey);
			string status_code = Request["status_code"].ToString();
			if (status_code != "0")
			{
				//Cập nhật trạng thái đơn
			}
			else { 
				// Cập nhật trạng thái đơn thất bại
			}

            return Json("",JsonRequestBehavior.AllowGet);
		}

        //[ValidateAntiForgeryToken]
        public ActionResult ThanhToanVnpayPage(DatHang TH)
		{
			if (ModelState.IsValid)
			{
				Session["DiaChiGiaoHang"] = TH.DiaChiGiaoHang;
				Session["DienThoaiGiaoHang"] = TH.DienThoaiGiaoHang;

                if (TH.DiaChiGiaoHang != null && TH.DienThoaiGiaoHang != null)
				{
                    return RedirectToAction("ThanhToanVnpay", "Home" , new { TypePaymentVnp = 2 });
                }
            }
			return View(TH);
		}

        public ActionResult ThanhToanVnpay(int TypePaymentVnp)
		{
            string vnp_Returnurl = ConfigurationManager.AppSettings["vnp_Returnurl"]; //URL nhan ket qua tra ve 
            string vnp_Url = ConfigurationManager.AppSettings["vnp_Url"]; //URL thanh toan cua VNPAY 
            string vnp_TmnCode = ConfigurationManager.AppSettings["vnp_TmnCode"]; //Ma định danh merchant kết nối (Terminal Id)
            string vnp_HashSecret = ConfigurationManager.AppSettings["vnp_HashSecret"]; //Secret Key

            //Get payment input
			List<SanPhamTrongGio> cart = (List<SanPhamTrongGio>)Session["cart"];
            string OrderId = Guid.NewGuid().ToString(); // Giả lập mã giao dịch hệ thống merchant gửi sang VNPAY
			int Amount = cart.Sum(n => n.dongho.DonGia).Value; // Giả lập số tiền thanh toán hệ thống merchant gửi sang VNPAY 100,000 VND
			string Status = "0"; //0: Trạng thái thanh toán "chờ thanh toán" hoặc "Pending" khởi tạo giao dịch chưa có IPN
   //         //Save order to db

            //Build URL for VNPAY
            VnPayLibrary vnpay = new VnPayLibrary();

            vnpay.AddRequestData("vnp_Version", VnPayLibrary.VERSION);
            vnpay.AddRequestData("vnp_Command", "pay");
            vnpay.AddRequestData("vnp_TmnCode", vnp_TmnCode);
            vnpay.AddRequestData("vnp_Amount", Amount.ToString()); //Số tiền thanh toán. Số tiền không mang các ký tự phân tách thập phân, phần nghìn, ký tự tiền tệ. Để gửi số tiền thanh toán là 100,000 VND (một trăm nghìn VNĐ) thì merchant cần nhân thêm 100 lần (khử phần thập phân), sau đó gửi sang VNPAY là: 10000000
            if (TypePaymentVnp == 1)
            {
                vnpay.AddRequestData("vnp_BankCode", "VNPAYQR");
            }
            else if (TypePaymentVnp == 2)
            {
                vnpay.AddRequestData("vnp_BankCode", "VNBANK");
            }
            else if (TypePaymentVnp == 3)
            {
                vnpay.AddRequestData("vnp_BankCode", "INTCARD");
            }
            vnpay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
            vnpay.AddRequestData("vnp_CurrCode", "VND");
            vnpay.AddRequestData("vnp_IpAddr", Utils.GetIpAddress());

            vnpay.AddRequestData("vnp_Locale", "vn");
            
            vnpay.AddRequestData("vnp_OrderInfo", "Thanh toan don hang");
            vnpay.AddRequestData("vnp_OrderType", "other"); //default value: other

            vnpay.AddRequestData("vnp_ReturnUrl", vnp_Returnurl);
            vnpay.AddRequestData("vnp_TxnRef", DateTime.Now.Ticks.ToString()); // Mã tham chiếu của giao dịch tại hệ thống của merchant. Mã này là duy nhất dùng để phân biệt các đơn hàng gửi sang VNPAY. Không được trùng lặp trong ngày

            //Add Params of 2.1.0 Version
            //Billing

            string paymentUrl = vnpay.CreateRequestUrl(vnp_Url, vnp_HashSecret);
			
			return Redirect(paymentUrl);
		}
		public  ActionResult ReturnUrlVnpay()
        {
            if (Request.QueryString.Count > 0)
            {
                string hashSecret = ConfigurationManager.AppSettings["vnp_HashSecret"]; //Chuỗi bí mật
                var vnpayData = Request.QueryString;
                VnPayLibrary pay = new VnPayLibrary();

                //lấy toàn bộ dữ liệu được trả về
                foreach (string s in vnpayData)
                {
                    if (!string.IsNullOrEmpty(s) && s.StartsWith("vnp_"))
                    {
                        pay.AddResponseData(s, vnpayData[s]);
                    }
                }

                long orderId = Convert.ToInt64(pay.GetResponseData("vnp_TxnRef")); //mã hóa đơn
                long vnpayTranId = Convert.ToInt64(pay.GetResponseData("vnp_TransactionNo")); //mã giao dịch tại hệ thống VNPAY
                string vnp_ResponseCode = pay.GetResponseData("vnp_ResponseCode"); //response code: 00 - thành công, khác 00 - xem thêm https://sandbox.vnpayment.vn/apis/docs/bang-ma-loi/
                string vnp_SecureHash = Request.QueryString["vnp_SecureHash"]; //hash của dữ liệu trả về

                bool checkSignature = pay.ValidateSignature(vnp_SecureHash, hashSecret); //check chữ ký đúng hay không?

                if (checkSignature)
                {
                    if (vnp_ResponseCode == "00")
                    {
                        //Thanh toán thành công
                        ViewBag.Message = "Thanh toán thành công hóa đơn " + orderId + " | Mã giao dịch: " + vnpayTranId;

                        // Lưu vào bảng DatHang
                        DatHang dh = new DatHang();
						dh.DiaChiGiaoHang = Session["DiaChiGiaoHang"].ToString();
						dh.DienThoaiGiaoHang = Session["DienThoaiGiaoHang"].ToString();
                        dh.NgayDatHang = DateTime.Now;
                        dh.KhachHang_ID = Convert.ToInt32(Session["MaKhachHang"]);
                        dh.TinhTrang = 0;
                        db.DatHang.Add(dh);
                        db.SaveChanges();

                        // Lưu vào bảng DatHang_ChiTiet
                        List<SanPhamTrongGio> cart = (List<SanPhamTrongGio>)Session["cart"];
                        foreach (var item in cart)
                        {
                            DatHang_ChiTiet ct = new DatHang_ChiTiet();
                            ct.DatHang_ID = dh.ID;
                            ct.DongHo_ID = item.dongho.ID;
                            ct.SoLuong = Convert.ToInt16(item.soLuongTrongGio);
                            ct.DonGia = item.dongho.DonGia;
                            db.DatHang_ChiTiet.Add(ct);
                            var dongho = db.DongHo.Find(item.dongho.ID);
                            dongho.SoLuong -= item.soLuongTrongGio;
                            db.SaveChanges();
                        }

                        // Xóa giỏ hàng
                        cart.Clear();
                        Session["DiaChiGiaoHang"] = null;
                        Session["DienThoaiGiaoHang"] = null;

                        // Quay về trang chủ
                        //return RedirectToAction("Index", "Home");
                    }
                    else
                    {
                        //Thanh toán không thành công. Mã lỗi: vnp_ResponseCode
                        ViewBag.Message = "Có lỗi xảy ra trong quá trình xử lý hóa đơn " + orderId + " | Mã giao dịch: " + vnpayTranId + " | Mã lỗi: " + vnp_ResponseCode;
                    }
                }
                else
                {
                    ViewBag.Message = "Có lỗi xảy ra trong quá trình xử lý";
                }
            }
            return View();
		}

        // GET: Home/Login
        public ActionResult Login()
		{
			ModelState.AddModelError("LoginError", "");
			return View();
		}

		// POST: Home/Login
		[HttpPost]
		[ValidateAntiForgeryToken]
		public ActionResult Login(KhachHangLogin khachHang)
		{
			if (ModelState.IsValid)
			{
				string matKhauMaHoa = SHA1.ComputeHash(khachHang.MatKhau);
				var taiKhoan = db.KhachHang.Where(r => r.TenDangNhap == khachHang.TenDangNhap && r.MatKhau == matKhauMaHoa).SingleOrDefault();

				if (taiKhoan == null)
				{
					ModelState.AddModelError("LoginError", "Tên đăng nhập hoặc mật khẩu không chính xác!");
					return View(khachHang);
				}
				else
				{
					// Đăng ký SESSION
					Session["MaKhachHang"] = taiKhoan.ID;
					Session["HoTenKhachHang"] = taiKhoan.HoVaten;

					// Quay về trang chủ
					return RedirectToAction("Index", "Home");
				}
			}

			return View(khachHang);
		}
		public ActionResult Logout()
		{
			// Xóa SESSION
			Session.RemoveAll();

			// Quay về trang chủ
			return RedirectToAction("Index", "Home");
		}
	}
}