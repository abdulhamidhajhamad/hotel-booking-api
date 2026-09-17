namespace HotelBooking.Api.IntegrationTests.Infrastructure;

[CollectionDefinition(Name)]
public sealed class IntegrationTestCollection : ICollectionFixture<HotelBookingApiFactory>
{
    public const string Name = "Integration";
}