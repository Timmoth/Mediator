namespace Mediator.Tests;

public sealed class StructResponseTests
{
    public readonly struct Response
    {
        public readonly Guid Id;
        public Response(Guid id) => Id = id;
    }

    public sealed record Request(Guid Id) : IRequest<Response>;
    public sealed record Command(Guid Id) : ICommand<Response>;
    public sealed record Query(Guid Id) : IQuery<Response>;

    public sealed class Handler :
        IRequestHandler<Request, Response>,
        ICommandHandler<Command, Response>,
        IQueryHandler<Query, Response>
    {
        public ValueTask<Response> Handle(Request request, CancellationToken cancellationToken) =>
            new ValueTask<Response>(new Response(request.Id));

        public ValueTask<Response> Handle(Command command, CancellationToken cancellationToken) =>
            new ValueTask<Response>(new Response(command.Id));

        public ValueTask<Response> Handle(Query query, CancellationToken cancellationToken) =>
            new ValueTask<Response>(new Response(query.Id));
    }

    [Fact]
    public async Task Test_Request()
    {
        var (_, mediator) = Fixture.GetMediator();

        var id = Guid.NewGuid();
        var message = new Request(id);

        var response = await mediator.Send(message);
        Assert.Equal(id, response.Id);
    }

    [Fact]
    public async Task Test_Command()
    {
        var (_, mediator) = Fixture.GetMediator();

        var id = Guid.NewGuid();
        var message = new Command(id);

        var response = await mediator.Send(message);
        Assert.Equal(id, response.Id);
    }

    [Fact]
    public async Task Test_Query()
    {
        var (_, mediator) = Fixture.GetMediator();

        var id = Guid.NewGuid();
        var message = new Query(id);

        var response = await mediator.Send(message);
        Assert.Equal(id, response.Id);
    }

    [Fact]
    public async Task Test_Request_As_Object()
    {
        var (_, mediator) = Fixture.GetMediator();

        var id = Guid.NewGuid();
        object message = new Request(id);

        var response = await mediator.Send(message);
        Assert.Equal(id, ((Response)response!).Id);
    }

    [Fact]
    public async Task Test_Command_Object()
    {
        var (_, mediator) = Fixture.GetMediator();

        var id = Guid.NewGuid();
        object message = new Command(id);

        var response = await mediator.Send(message);
        Assert.Equal(id, ((Response)response!).Id);
    }

    [Fact]
    public async Task Test_Query_Object()
    {
        var (_, mediator) = Fixture.GetMediator();

        var id = Guid.NewGuid();
        object message = new Query(id);

        var response = await mediator.Send(message);
        Assert.Equal(id, ((Response)response!).Id);
    }
}
