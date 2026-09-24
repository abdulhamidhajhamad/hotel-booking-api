namespace HotelBooking.Domain.Common;
public enum HotelCategory
{
    Standard = 0,
    Budget = 1,
    Boutique = 2,
    Luxury = 3
}

public enum BookingStatus
{
    Pending = 0,
    Confirmed = 1,
    CheckedIn = 2,
    Completed = 3,
    Cancelled = 4
}

public enum PaymentStatus
{
    Pending = 0,
    Succeeded = 1,
    Failed = 2,
    Refunded = 3
}

public enum IdempotencyStatus
{
    Pending = 0,
    Completed = 1
}