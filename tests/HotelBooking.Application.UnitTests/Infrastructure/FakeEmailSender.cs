using HotelBooking.Application.Abstractions.Email;

namespace HotelBooking.Api.IntegrationTests.Infrastructure;

public sealed class FakeEmailSender : IEmailSender
{
    private readonly List<EmailMessage> _sent = new();
    private readonly object _gate = new();

    public IReadOnlyList<EmailMessage> Sent
    {
        get
        {
            lock (_gate) return _sent.ToArray();
        }
    }

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        lock (_gate) _sent.Add(message);
        return Task.CompletedTask;
    }

    public void Clear()
    {
        lock (_gate) _sent.Clear();
    }
}