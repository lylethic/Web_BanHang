using Azure;
using ClothesWeb.Data;
using ClothesWeb.Models;
using ClothesWeb.Models.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Drawing.Printing;
using System.Globalization;
using System.Net;
using System.Security.Claims;
using X.PagedList;

namespace ClothesWeb.Areas.Admin.Controllers
{
  [Area("Owner")]
  [Authorize(Roles = "Admin")]
  public class AdminController : Controller
  {
    private readonly ApplicationDbContext _db;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly ILogger<AdminController> _logger;

    public AdminController(ILogger<AdminController> logger, ApplicationDbContext db, RoleManager<IdentityRole> roleManager, UserManager<IdentityUser> userManager)
    {
      _db = db;
      _roleManager = roleManager;
      _userManager = userManager;
      _logger = logger;
    }
    public IActionResult Index()
    {
      return View();
    }

    public IActionResult Dashboard()
    {
      IEnumerable<Product> products = _db.Products.Include("Category").ToList();
      return View(products);
    }

    [HttpGet]
    public IActionResult Analytics()
    {
      DateTime defaultDate = DateTime.Today;

      // Tổng SP
      var totalProduct = CalculateTotalQuantityOfProducts();

      // Tổng loại sp
      var totalCategories = CalculateTotalQuantityOfCategories();
      // Tổng SL đơn hàng
      var totalQuantity = CalculateTotalQuantityOfOrders();
      // Tổng Loại SP

      // Tổng đơn hàng theo ngày
      //var totalDay = CalculateNumberOfOrdersPerDay();
      //Dictionary<DateTime, int> totalDay = CalculateNumberOfOrdersPerDay();
      //int ordersForSelectedDate = CalculateNumberOfOrdersPerDay(defaultDate);

      // Tổng đơn hàng theo tháng
      var totalMonth = CalculateNumberOfOrdersPerMonth();

      // Tổng đơn hàng theo năm
      var totalYear = CalculateNumberOfOrdersPerYear();

      //if (selectedDate.HasValue)
      //{
      //  // Nếu người dùng đã chọn một ngày, sử dụng ngày đó
      //  defaultDate = selectedDate.Value;
      //}

      //ViewBag.SelectedDate = defaultDate.ToString("yyyy-MM-dd");
      //ViewBag.OrdersForSelectedDate = ordersForSelectedDate;

      ViewBag.TotalProduct = totalProduct;
      ViewBag.TotalCategories = totalCategories;
      ViewBag.TotalSL = totalQuantity;
      ViewBag.TotalMonth = totalMonth;
      ViewBag.TotalYear = totalYear;

      // Call the MonthlySales action to get the data
      var monthlySalesData = MonthlySales();

      // Log the monthlySalesData
      //_logger.LogInformation("MonthlySales data: {@MonthlySalesData}", monthlySalesData.Value);

      // Check if the MonthlySales action returned a JsonResult
      if (monthlySalesData is JsonResult jsonResult)
      {
        // Extract the data from the JsonResult
        var monthlySalesList = jsonResult.Value as List<object>;

        // Pass the data to the view
        return View(monthlySalesList);
      }

      return View();
    }

    [HttpGet]
    public IActionResult Upsert(int id)
    {
      Product product = new Product();
      IEnumerable<SelectListItem> dstheloai = _db.Categories.Select(
        product => new SelectListItem
        {
          Value = product.LoaiId.ToString(),
          Text = product.Name,
        });

      ViewBag.Dstheloai = dstheloai;

      if (id == 0)
      {
        return View(product);
      }
      else
      {
        product = _db.Products.Include("Category").FirstOrDefault(product => product.IdProduct == id);
        return View(product);
      }
    }

    [HttpPost]
    public IActionResult Upsert(Product product)
    {
      if (ModelState.IsValid)
      {
        if (product.IdProduct == 0)
        {
          _db.Products.Add(product);
        }
        else
        {
          _db.Products.Update(product);
        }
        _db.SaveChanges();
        return RedirectToAction("AdminProducts");
      }
      return View();
    }

    public IActionResult Delete(int id)
    {
      var product = _db.Products.FirstOrDefault(product => product.IdProduct == id);
      if (product == null)
      {
        return NotFound();
      }
      _db.Products.Remove(product);
      _db.SaveChanges();

      return RedirectToAction("AdminProducts");
    }

    [HttpGet]
    public IActionResult ListUser(int page = 1)
    {
      IEnumerable<ApplicationUser> users = _db.ApplicationUser;
      const int pageSize = 6;
      page = page < 1 ? 1 : page;
      int recsCount = users.Count();
      var pager = new Pager(recsCount, page, pageSize);
      int recSkip = (page - 1) * pageSize;
      var data = users.Skip(recSkip).Take(pager.PageSize).ToList();
      this.ViewBag.Pager = pager;
      return View(data);
    }

    public IActionResult Login()
    {
      return View();
    }

    public IActionResult Register()
    {
      return View();
    }

    public IActionResult Button()
    {
      return View();
    }

    public IActionResult Alerts()
    {
      return View();
    }

    public IActionResult Card()
    {
      return View();
    }

    public IActionResult Forms()
    {
      return View();
    }

    public IActionResult Typography()
    {
      return View();
    }

    public IActionResult Icons()
    {
      return View();
    }

    public IActionResult Sample_Page()
    {
      return View();
    }

    //public IActionResult PhanLoai(int loaiSanpham, int page = 1)
    //{
    //  IEnumerable<Product> products;
    //  if (loaiSanpham != 0)
    //  {
    //    products = _db.Products.Include("Category")
    //                           .Where(x => x.LoaiId == loaiSanpham)
    //                           .ToList();
    //  }
    //  else
    //  {
    //    products = _db.Products.Include("Category")
    //                           .ToList();
    //  }

    //  const int pageSize = 12;
    //  page = page < 1 ? 1 : page;
    //  int recsCount = products.Count();
    //  var pager = new Pager(recsCount, page, pageSize);
    //  int recSkip = (page - 1) * pageSize;
    //  var data = products.Skip(recSkip).Take(pager.PageSize).ToList();
    //  this.ViewBag.Pager = pager;
    //  return View(data);
    //}

    public IActionResult AdminProducts(int page = 1)
    {
      IEnumerable<Product> products = _db.Products.Include("Category").ToList();
      const int pageSize = 12;
      page = page < 1 ? 1 : page;
      int recsCount = products.Count();
      var pager = new Pager(recsCount, page, pageSize);
      int recSkip = (page - 1) * pageSize;
      var data = products.Skip(recSkip).Take(pager.PageSize).ToList();
      this.ViewBag.Pager = pager;
      return View(data);
    }

    public IActionResult ViewLoaiSanPham()
    {
      var loaiSP = _db.Categories.ToList();
      ViewBag.LoaiSP = loaiSP;
      return View(loaiSP);
    }

    [HttpGet]
    public IActionResult LoaiSanPham(int id)
    {
      Category loaiSanPham = new Category();
      IEnumerable<SelectListItem> dstheloai = _db.Categories.Select(
        loaiSanPham => new SelectListItem
        {
          Value = loaiSanPham.LoaiId.ToString(),
          Text = loaiSanPham.Name,
        });

      ViewBag.Dstheloai = dstheloai;

      if (id == 0)
      {
        return View(loaiSanPham);
      }
      else
      {
        loaiSanPham = _db.Categories.FirstOrDefault(loaiSanPham => loaiSanPham.LoaiId == id);
        return View(loaiSanPham);
      }
    }

    [HttpPost]
    public IActionResult LoaiSanPham(Category category)
    {
      if (ModelState.IsValid)
      {
        if (category.LoaiId == 0)
        {
          _db.Categories.Add(category);
        }
        else
        {
          _db.Categories.Update(category);
        }
        _db.SaveChanges();
        return RedirectToAction("ViewLoaiSanPham");
      }
      return View();
    }

    public IActionResult DeleteLoaiSanPham(int id)
    {
      var category = _db.Categories.FirstOrDefault(c => c.LoaiId == id);
      if (category == null)
      {
        return NotFound();
      }
      _db.Categories.Remove(category);
      _db.SaveChanges();

      return RedirectToAction("ViewLoaiSanPham");
    }

    [HttpGet]
    public IActionResult Product_Details(int productId)
    {
      Cart cart = new Cart()
      {
        ProductId = productId,
        Product = _db.Products.Include("Category").FirstOrDefault(sp => sp.IdProduct == productId),
        Quantity = 1,
      };
      return View(cart);
    }

    [HttpGet]
    public IActionResult TimKiemSanPham(string name = "", int page = 1)
    {
      var products = from b in _db.Products select b;

      if (!string.IsNullOrEmpty(name))
      {
        products = products.Where(x => x.Name.Contains(name));
      }

      const int pageSize = 12;
      page = page < 1 ? 1 : page;
      int recsCount = products.Count();
      var pager = new Pager(recsCount, page, pageSize);
      int recSkip = (page - 1) * pageSize;
      var data = products.Skip(recSkip).Take(pager.PageSize).ToList();
      ViewBag.SearchString = name;
      this.ViewBag.Pager = pager;
      return View(data);
    }

    [HttpGet]
    public IActionResult EditUser(string id)
    {
      var user = _db.ApplicationUser.FirstOrDefault(x => x.Id == id);
      return View(user);
    }

    [HttpPost]
    public IActionResult EditUser(ApplicationUser updatedUser)
    {
      var idUser = updatedUser.Id;
      var user = _db.ApplicationUser.FirstOrDefault(x => x.Id == idUser);

      if (user != null)
      {
        user.Id = idUser;
        user.Name = updatedUser.Name;
        user.Email = updatedUser.Email;
        user.PhoneNumber = updatedUser.PhoneNumber;
        user.Address = updatedUser.Address;
        _db.SaveChanges();
        return RedirectToAction("ListUser"); // Redirect to the profile page

      };
      return NotFound();
    }

    public IActionResult DeleteUser(string id)
    {
      var user = _db.ApplicationUser.FirstOrDefault(x => x.Id == id);
      if (user == null)
      {
        return NotFound();
      }
      _db.ApplicationUser.Remove(user);
      _db.SaveChanges();

      return RedirectToAction("ListUser");
    }

    // Tong doanh thu //
    //public double GetTotalRevenue()
    //{
    //  // Access the HoaDon data (replace with your actual data access method)
    //  var hoaDons = _db.HoaDons.GetAllHoaDons();

    //  // Calculate total revenue by summing the TotalPrice of all HoaDons
    //  double totalRevenue = hoaDons.Sum(hd => hd.TotalPrice);

    //  return totalRevenue;
    //}

    // Tổng số lg đơn hàng
    [HttpGet]
    public int CalculateTotalQuantityOfOrders()
    {
      // Calculate total quantity of orders
      int totalQuantity = _db.HoaDons.Select(hd => hd.Id).Distinct().Count();

      return totalQuantity;
    }

    // Tổng sản phẩm
    [HttpGet]
    public int CalculateTotalQuantityOfProducts()
    {
      // Calculate total quantity of products
      int totalQuantity = _db.Products.Select(p => p.IdProduct).Distinct().Count(); ;
      ViewBag.TotalQuantity = totalQuantity;

      return totalQuantity;
    }

    // Tổng loai sản phẩm
    [HttpGet]
    public int CalculateTotalQuantityOfCategories()
    {
      // Calculate total quantity of categories
      int totalQuantity = _db.Categories.Select(p => p.LoaiId).Distinct().Count(); ;
      ViewBag.TotalQuantity = totalQuantity;

      return totalQuantity;
    }

    // Tổng đơn hàng theo ngày
    public int CalculateNumberOfOrdersPerDay(DateTime selectedDate)
    {
      // Tính số lượng đơn hàng cho ngày được chọn
      int ordersForSelectedDate = _db.HoaDons
          .Count(hd => hd.OrderDate.Date == selectedDate.Date);

      return ordersForSelectedDate;
    }

    // Tháng
    public Dictionary<string, int> CalculateNumberOfOrdersPerMonth()
    {
      // Lấy dữ liệu từ cơ sở dữ liệu
      var orders = _db.HoaDons.ToList();

      // Thực hiện group và tính toán địa phương
      var ordersPerMonth = orders
          .GroupBy(hd => new { hd.OrderDate.Year, hd.OrderDate.Month })
          .Select(group => new
          {
            Month = $"{group.Key.Year}-{group.Key.Month}",
            NumberOfOrders = group.Count()
          })
          .ToDictionary(item => item.Month, item => item.NumberOfOrders);

      ViewBag.OrdersPerMonth = ordersPerMonth;

      return ordersPerMonth;
    }


    // Năm
    public Dictionary<string, int> CalculateNumberOfOrdersPerYear()
    {
      // Lấy dữ liệu từ cơ sở dữ liệu
      var orders = _db.HoaDons.ToList();

      // Thực hiện group và tính toán địa phương
      var ordersPerMonth = orders
          .GroupBy(hd => hd.OrderDate.Year + "-" + hd.OrderDate.Month.ToString("00")) // Group by year and month
          .Select(group => new
          {
            Month = group.Key,
            NumberOfOrders = group.Count()
          })
          .ToDictionary(item => item.Month, item => item.NumberOfOrders);

      ViewBag.OrdersPerMonth = ordersPerMonth;

      return ordersPerMonth;
    }

    // Tong doanh thu theo thang

    [HttpGet]
    public IActionResult MonthlySales()
    {
      // Truy vấn các hóa đơn từ cơ sở dữ liệu
      var invoices = _db.HoaDons.ToList();

      // Gom nhóm các hóa đơn theo tháng và tính tổng doanh thu cho mỗi tháng
      var monthlySales = invoices.GroupBy(i => new { i.OrderDate.Year, i.OrderDate.Month })
                                 .Select(group => new
                                 {
                                   Month = group.Key.Month,
                                   Year = group.Key.Year,
                                   TotalRevenue = group.Sum(i => i.TotalPrice),
                                   // Tính lợi nhuận bằng cách trừ đi chi phí vận chuyển từ tổng doanh thu
                                   TotalProfit = group.Sum(i => i.TotalPrice) - group.Sum(i => i.ShippingCost)
                                 })
                                 .OrderBy(s => s.Year)
                                 .ThenBy(s => s.Month)
                                 .ToList();

      // Trả về dữ liệu dưới dạng Json
      return Json(monthlySales);
    }

    //[HttpGet]
    //public IActionResult Charts()
    //{
    //  // Call the MonthlySales action to get the data
    //  var monthlySalesData = MonthlySales();
    //  // Log the monthlySalesData
    //  _logger.LogInformation("MonthlySales lYLY: {0}", monthlySalesData.ToString());

    //  // Check if the MonthlySales action returned a JsonResult
    //  if (monthlySalesData is JsonResult jsonResult)
    //  {
    //    // Extract the data from the JsonResult
    //    var monthlySalesList = jsonResult.Value as List<object>;

    //    // Pass the data to the view
    //    return View(monthlySalesList);
    //  }
    //  else
    //  {
    //    // Handle the case where MonthlySales didn't return a JsonResult
    //    // You may return an error view or handle it as appropriate for your application
    //    return RedirectToAction("Error", "Home");
    //  }
    //}
    [HttpGet]
    public IActionResult Charts()
    {
      // Call the MonthlySales action to get the data
      var monthlySalesData = MonthlySales();

      // Log the monthlySalesData
      //_logger.LogInformation("MonthlySales data: {@MonthlySalesData}", monthlySalesData.Value);

      // Check if the MonthlySales action returned a JsonResult
      if (monthlySalesData is JsonResult jsonResult)
      {
        // Extract the data from the JsonResult
        var monthlySalesList = jsonResult.Value as List<object>;

        // Pass the data to the view
        return View(monthlySalesList);
      }
      else
      {
        // Handle the case where MonthlySales didn't return a JsonResult
        // You may return an error view or handle it as appropriate for your application
        return RedirectToAction("Error", "Home");
      }
    }



    public IActionResult SalesRevenueByPeriod(RevenuePeriod period)
    {
      var salesQuery = _db.ChiTietHoaDons
          .Include(ct => ct.HoaDon)
          .Where(ct => ct.HoaDon.OrderStatus == "Completed" && ct.HoaDon.DeliveryDate != null);

      switch (period)
      {
        case RevenuePeriod.Day:
          return CalculateRevenueByDay(salesQuery);
        case RevenuePeriod.Week:
          return CalculateRevenueByWeek(salesQuery);
        case RevenuePeriod.Month:
          return CalculateRevenueByMonth(salesQuery);
        case RevenuePeriod.Year:
          return CalculateRevenueByYear(salesQuery);
        default:
          return BadRequest();
      }
    }

    private IActionResult CalculateRevenueByDay(IQueryable<ChiTietHoaDon> salesQuery)
    {
      var revenueData = salesQuery
          .GroupBy(ct => ct.HoaDon.DeliveryDate.Date)
          .Select(g => new RevenueData
          {
            Period = g.Key.ToShortDateString(),
            TotalRevenue = g.Sum(ct => ct.Quantity * ct.ProductPrice)
          }).ToList();

      return Ok(revenueData);
    }

    private IActionResult CalculateRevenueByWeek(IQueryable<ChiTietHoaDon> salesQuery)
    {
      var revenueData = salesQuery
          .GroupBy(ct => CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(ct.HoaDon.DeliveryDate, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday))
          .Select(g => new RevenueData
          {
            Period = "Week " + g.Key,
            TotalRevenue = g.Sum(ct => ct.Quantity * ct.ProductPrice)
          }).ToList();

      return Ok(revenueData);
    }

    private IActionResult CalculateRevenueByMonth(IQueryable<ChiTietHoaDon> salesQuery)
    {
      var revenueData = salesQuery
          .GroupBy(ct => new { ct.HoaDon.DeliveryDate.Year, ct.HoaDon.DeliveryDate.Month })
          .Select(g => new RevenueData
          {
            Period = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(g.Key.Month) + " " + g.Key.Year,
            TotalRevenue = g.Sum(ct => ct.Quantity * ct.ProductPrice)
          }).ToList();

      return Ok(revenueData);
    }

    private IActionResult CalculateRevenueByYear(IQueryable<ChiTietHoaDon> salesQuery)
    {
      var revenueData = salesQuery
          .GroupBy(ct => ct.HoaDon.DeliveryDate.Year)
          .Select(g => new RevenueData
          {
            Period = g.Key.ToString(),
            TotalRevenue = g.Sum(ct => ct.Quantity * ct.ProductPrice)
          }).ToList();

      return Ok(revenueData);
    }

  }
}

