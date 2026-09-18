namespace HotelBooking.Application.Common.Messaging;

public interface ICommandBase;
public interface ICommand : ICommandBase;
public interface ICommand<TResponse> : ICommandBase;