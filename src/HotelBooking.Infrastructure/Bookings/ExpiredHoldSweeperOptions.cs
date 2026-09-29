namespace HotelBooking.Infrastructure.Bookings;

public sealed class ExpiredHoldSweeperOptions
{
    public int SweepIntervalSeconds { get; init; } = 60;
}