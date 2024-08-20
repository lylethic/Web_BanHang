namespace ClothesWeb.Models
{
  public class VnPaymentResponseModel
  {
    public bool Success { get; set; }
    public string PaymentMethod { get; set; }
    public string OrderDescription { get; set; }
    public string OrderId { get; set; }
    public string PaymentId { get; set; }
    public string TransactionId { get; set; }
    public string Token { get; set; }
    public string VnPayResponseCode { get; set; }
    //
    public string UserId { get; set; }
    public string Note { get; set; }
    public string Address { get; set; }
    public string OrderStatus { get; set; }
    public DateTime CreatedDate { get; set; }
    public string PayMentMethod { get; set; }
    public string ShippingUnit { get; set; }
    public double ShippingCost { get; set; }
  }

  public class VnPaymentRequestModel
  {
    public int OrderId { get; set; }
    public string FullName { get; set; }
    public string Description { get; set; }
    public double Amount { get; set; } // SL
    public DateTime CreatedDate { get; set; }
    //
    //public string UserId { get; set; }
    //public string Note { get; set; }
    //public string Address { get; set; }
    //public string OrderStatus { get; set; }
    //public string PayMentMethod { get; set; }
    //public string ShippingUnit { get; set; }
    //public double ShippingCost { get; set; }
  }
}
