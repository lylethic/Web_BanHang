﻿using ClothesWeb.Data;
using ClothesWeb.Helpers;
using ClothesWeb.Migrations;
using ClothesWeb.Models;
using ClothesWeb.Models.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.ComponentModel;
using System.Security.Claims;

namespace ClothesWeb.Areas.Customer.Controllers
{
  [Area("Customer")]
  [Authorize]
  public class CartController : Controller
  {
    private readonly ApplicationDbContext _db;
    private readonly IVnPayService _vnPayService;

    public CartController(ApplicationDbContext db, IVnPayService vnPayService)
    {
      _db = db;
      _vnPayService = vnPayService;
    }

    public List<Cart> Cart = new List<Cart>();
    double totalPrice = 0;

    public IActionResult Index()
    {
      //Get Infor of Account
      var identity = (ClaimsIdentity)User.Identity;
      var claim = identity.FindFirst(ClaimTypes.NameIdentifier);

      //Get List of Product in Cart of User
      CartViewModel cart = new CartViewModel()
      {
        listCart = _db.Carts
        .Include("Product")
        .Where(x => x.ApplicationUserId == claim.Value).ToList(),
        HoaDon = new HoaDon()
      };

      foreach (var item in cart.listCart)
      {
        if (item.isCheckout == false)
        {
          return RedirectToAction("NoProductPage");

        }
        else
        {
          //Pay by product quantity.
          item.ProductPrice = item.Quantity * item.Product.Price;

          //Check out the shopping cart.
          totalPrice += item.ProductPrice;
          ViewBag.SubTotal = totalPrice;
          cart.HoaDon.TotalPrice = totalPrice + 32000;
        }
      }


      if (cart.listCart.Count() == 0)
      {
        return RedirectToAction("NoProductPage");
      }

      return View(cart);
    }

    [HttpGet]
    public IActionResult ThanhToan()
    {
      //Get Infor of Account
      var identity = (ClaimsIdentity)User.Identity;
      var claim = identity.FindFirst(ClaimTypes.NameIdentifier);

      //Get List of Product in Cart of User
      CartViewModel cart = new CartViewModel()
      {
        listCart = _db.Carts
        .Include("Product")
        .Where(x => x.ApplicationUserId == claim.Value).ToList(),
        HoaDon = new HoaDon()
      };

      foreach (var item in cart.listCart)
      {
        // SL * Gia
        item.ProductPrice = item.Quantity * item.Product.Price;
        //Check out the shopping cart.
        totalPrice += item.ProductPrice;
      }
      cart.HoaDon.TotalPrice = totalPrice + 32000;

      cart.HoaDon.User = _db.ApplicationUser.FirstOrDefault(user => user.Id == claim.Value);
      cart.HoaDon.Name = cart.HoaDon.User.Name;
      cart.HoaDon.Address = cart.HoaDon.User.Address;
      cart.HoaDon.PhoneNumber = cart.HoaDon.User.PhoneNumber;

      return View(cart);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ThanhToan(CartViewModel cart, string payment = "COD")
    {
      //Get Infor of Account
      var identity = (ClaimsIdentity)User.Identity;
      var claim = identity.FindFirst(ClaimTypes.NameIdentifier);

      // Tim id KH trong table Cart
      cart.listCart = _db.Carts
        .Include("Product")
          .Where(cart => cart.ApplicationUserId == claim.Value)
          .ToList();

      // Dem sp trong Cart
      if (cart.listCart.Count() == 0)
      {
        TempData["EmptyCartMessage"] = "Chưa có sản phẩm trong giỏ hàng.";
        return RedirectToAction("Index", "Home");
      }

      // Chon phuong thuc thanh toan va thanh toan
      if (payment == "Thanh toán VNPay")
      {
        var vnPaymentModel = new VnPaymentRequestModel();
        foreach (var item in cart.listCart)
        {
          item.ProductPrice = item.Quantity * item.Product.Price;
          vnPaymentModel.Amount += item.ProductPrice;
        }
        vnPaymentModel.CreatedDate = DateTime.Now;
        vnPaymentModel.Amount += 32000;
        vnPaymentModel.OrderId = new Random().Next(1000, 100000);

        return Redirect(_vnPayService.CreatePaymentUrl(HttpContext, vnPaymentModel));
      }

      cart.HoaDon.ApplicationUserId = claim.Value; // Lay id KH gans = id KH vao HoaDon
      cart.HoaDon.OrderStatus = "Đang xác nhận"; // Trang thai cho xac nhan
      cart.HoaDon.OrderDate = DateTime.Now; // Thoi gian hien tại
      cart.HoaDon.GhiChu = cart.HoaDon.GhiChu; // Ghi chu
      cart.HoaDon.PayMentMethod = payment; // PT thanh toan
      cart.HoaDon.ShippingUnit = "LiliExpress"; // Cứng
      cart.HoaDon.ShippingCost = 32000; // Cứng

      foreach (var item in cart.listCart)
      {
        // Số lượng * Giá SP
        item.ProductPrice = item.Quantity * item.Product.Price;
        // Sum
        //cart.HoaDon.TotalPrice += item.ProductPrice;
        totalPrice += item.ProductPrice;
      }
      // Cộng thêm tiền vận chuyển
      totalPrice += 32000;

      // Lưu tổng tiền vào cart.HoaDon.TotalPrice
      cart.HoaDon.TotalPrice = totalPrice;

      _db.Database.BeginTransaction();
      try
      {
        _db.HoaDons.Add(cart.HoaDon);
        _db.SaveChanges();

        foreach (var item in cart.listCart)
        {
          ChiTietHoaDon chiTietHoaDon = new ChiTietHoaDon()
          {
            ProductId = item.ProductId,
            HoaDonId = cart.HoaDon.Id,
            Username = cart.HoaDon.Name,
            ProductName = item.Product.Name,
            ProductPrice = item.ProductPrice,
            Quantity = item.Quantity,
            ImgURL = item.Product.ImageUrl,
          };
          _db.ChiTietHoaDons.Add(chiTietHoaDon);
          _db.Carts.RemoveRange(cart.listCart);
          _db.SaveChanges();
          _db.Database.CommitTransaction();

          return RedirectToAction("PaymentSuccess");
        }
      }
      catch (Exception ex)
      {
        _db.Database.RollbackTransaction();
      }
      //_db.SaveChanges();
      return View(cart);
    }

    // Xoa sp khoi Gio Hang
    public IActionResult DeleteProduct(int cartId)
    {
      var cartItem = _db.Carts.FirstOrDefault(x => x.Id == cartId);

      _db.Carts.Remove(cartItem);
      _db.SaveChanges();
      return RedirectToAction("Index");
    }

    // Giam so luong san pham
    public IActionResult Giam(int cartId)
    {
      var cart = _db.Carts.FirstOrDefault(cart => cart.Id == cartId);
      cart.Quantity -= 1;

      if (cart.Quantity == 0)
      {
        _db.Carts.Remove(cart);
      }
      _db.SaveChanges();

      return RedirectToAction("Index");
    }

    // Tang so luong san pham
    public IActionResult Tang(int cartId)
    {
      var cart = _db.Carts.FirstOrDefault(cart => cart.Id == cartId);
      cart.Quantity += 1;
      _db.SaveChanges();

      return RedirectToAction("Index");
    }

    // Khong co sp trong Gio Hang Redirect 
    public IActionResult NoProductPage()
    {
      return View();
    }

    public IActionResult PaymentFail()
    {
      return View();
    }

    public IActionResult PaymentSuccess()
    {
      return View();
    }

    public IActionResult PaymentCallBack()
    {
      var response = _vnPayService.PaymentExecute(Request.Query);

      if (response == null || response.VnPayResponseCode != "00")
      {
        TempData["Message"] = $"Lỗi thanh toán VNPay: {response.VnPayResponseCode}";
        return RedirectToAction("PaymentFail");
      }

      // Success: Lưu đơn hàng vao DB
      
      TempData["Message"] = $"Thanh toán VNPay thành công";
      return RedirectToAction("PaymentSuccess");
    }
  }
}
