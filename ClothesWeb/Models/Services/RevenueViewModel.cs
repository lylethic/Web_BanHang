using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClothesWeb.Models.Services
{
  public enum RevenuePeriod
  {
    Day,
    Week,
    Month,
    Year
  }

  public class RevenueData
  {
    public string Period { get; set; }
    public double TotalRevenue { get; set; }
  }


  public class Sale
  {
    [Key]
    public int Id { get; set; } 

    public int ProductId { get; set; }
    [ForeignKey("ProductId")]
    public Product Product { get; set; }

    public int Quantity { get; set; }

    public decimal TotalAmount { get; set; }

    public DateTime SaleDate { get; set; }

  }
}
