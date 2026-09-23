namespace HotelBooking.Infrastructure.Outbox;

public enum OutboxMessageStatus : byte
{
    Pending = 0,
    Processed = 1,
    DeadLetter = 2,
}