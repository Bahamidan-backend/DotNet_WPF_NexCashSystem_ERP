namespace POSSystem.WPF.Data.Repositories
{
    // DTO خفيف لقوائم الطلبات - لا يحمل Navigation Properties
    public record OrderListDto(
        int Id,
        string OrderNumber,
        DateTime CreatedAt,
        decimal Total,
        OrderStatus Status,
        string UserName,
        int? ShiftId,
        string? ShiftName,
        PaymentMethod PaymentMethod,
        int ItemsCount,
        string? CustomerDisplayName = "عميل مباشر",
        string? PaymentMethodDisplay = "",
        string? RiskLabel = "",
        bool IsHighRisk = false
    );

    // DTO لتفاصيل الطلب - يحمل البيانات الكاملة عند الحاجة
    public record OrderDetailDto(
        int Id,
        string OrderNumber,
        DateTime CreatedAt,
        decimal SubTotal,
        decimal ServiceCharge,
        decimal Discount,
        decimal Total,
        OrderStatus Status,
        PaymentMethod PaymentMethod,
        string? Notes,
        int UserId,
        string UserName,
        int? ShiftId,
        string? ShiftName,
        DeliveryDto? Delivery,
        IReadOnlyList<OrderItemDto> Items
    );

    public record OrderItemDto(
        int Id,
        int? ProductId,
        string? ProductName,
        int? AddonId,
        string? AddonName,
        int Quantity,
        decimal UnitPrice,
        decimal TotalPrice,
        string? Size,
        string? Notes,
        IReadOnlyList<AddonDto> LinkedAddons
    );

    public record AddonDto(
        int Id,
        string Name,
        decimal Price
    );

    public record DeliveryDto(
        int DeliveryId,
        string CustomerName,
        string Phone,
        string Address,
        string? DriverName,
        decimal Fee
    );
}