[![GitHub Workflow Status](https://img.shields.io/github/workflow/status/martinothamar/Mediator/Build)](https://github.com/martinothamar/Mediator/actions)<br/>
[![Abstractions NuGet current](https://img.shields.io/nuget/v/Mediator.Abstractions?label=Mediator.Abstractions)](https://www.nuget.org/packages/Mediator.Abstractions)
[![SourceGenerator NuGet current](https://img.shields.io/nuget/v/Mediator.SourceGenerator?label=Mediator.SourceGenerator)](https://www.nuget.org/packages/Mediator.SourceGenerator)<br/>
[![Abstractions NuGet prerelease](https://img.shields.io/nuget/vpre/Mediator.Abstractions?label=Mediator.Abstractions)](https://www.nuget.org/packages/Mediator.Abstractions)
[![SourceGenerator NuGet prerelease](https://img.shields.io/nuget/vpre/Mediator.SourceGenerator?label=Mediator.SourceGenerator)](https://www.nuget.org/packages/Mediator.SourceGenerator)

# Mediator

This is a high performance .NET implementation of the Mediator pattern using the [source generators](https://devblogs.microsoft.com/dotnet/introducing-c-source-generators/) feature introduced in .NET 5.
The API and usage is mostly based on the great [MediatR](https://github.com/jbogard/MediatR) library, with some deviations to allow for better performance.
Packages are .NET Standard 2.1 compatible.

The mediator pattern is great for implementing cross cutting concern (logging, metrics, etc) and avoiding "fat" constructors due to lots of injected services.

Goals for this library
* High performance
  * Runtime performance can be the same for both runtime reflection and source generator based approaches, but it's easier to optimize in the latter case
* AOT friendly
  * MS are investing time in various AOT scenarios, and for example iOS requirees AOT compilation
* Build time errors instead of runtime errors
  * The generator includes diagnostics, i.e. if a handler is not defined for a request, a warning is emitted

In particular, source generators in this library is used to
* Generate code for DI registration
* Generate code for `IMediator` implementation
  * Request/Command/Query `Send` methods are monomorphized (1 method per T), the generic `ISender.Send` methods rely on these
  * You can use both `IMediator` and `Mediator`, the latter allows for better performance
* Generate diagnostics related messages and message handlers

- [Mediator](#mediator)
  - [2. Benchmarks](#2-benchmarks)
  - [3. Usage and abstractions](#3-usage-and-abstractions)
    - [3.1. Message types](#31-message-types)
    - [3.2. Handler types](#32-handler-types)
    - [3.3. Pipeline types](#33-pipeline-types)
    - [3.4. Configuration](#34-configuration)
  - [4. Getting started](#4-getting-started)
    - [4.1. Add package](#41-add-package)
    - [4.2. Add Mediator to DI container](#42-add-mediator-to-di-container)
    - [4.3. Create `IRequest<>` type](#43-create-irequest-type)
    - [4.4. Use pipeline behaviors](#44-use-pipeline-behaviors)
    - [4.5. Constrain `IPipelineBehavior<,>` message with open generics](#45-constrain-ipipelinebehavior-message-with-open-generics)
    - [4.6. Use notifications](#46-use-notifications)
    - [4.7. Polymorphic dispatch with notification handlers](#47-polymorphic-dispatch-with-notification-handlers)
    - [4.8. Notification handlers also support open generics](#48-notification-handlers-also-support-open-generics)
    - [4.9. Use streaming messages](#49-use-streaming-messages)
  - [5. Diagnostics](#5-diagnostics)
  - [6. Differences from MediatR](#6-differences-from-mediatr)

## 2. Benchmarks

This benchmark exposes the perf overhead of the libraries.
Mediator (this library) and MediatR methods show the overhead of the respective mediator implementations.
I've also included the [MessagePipe](https://github.com/Cysharp/MessagePipe) library as it also has great performance.

* `<SendRequest | Stream>_Baseline`: simple method call into the handler class
* `<SendRequest | Stream>_Mediator`: the concrete `Mediator` class generated by this library
* `<SendRequest | Stream>_MessagePipe`: the [MessagePipe](https://github.com/Cysharp/MessagePipe) library
* `<SendRequest | Stream>_IMediator`: call through the `IMediator` interface in this library
* `<SendRequest | Stream>_MediatR`: the [MediatR](https://github.com/jbogard/MediatR) library

See [benchmarks code](/benchmarks/Mediator.Benchmarks/Request) for more details on the measurement.

![Requests benchmark](/img/request_benchmark.png "Requests benchmark")

![Stream benchmark](/img/stream_benchmark.png "Stream benchmark")

## 3. Usage and abstractions

There are two NuGet packages needed to use this library
* Mediator.SourceGenerator
  * To generate the `IMediator` implementation and dependency injection setup.
* Mediator
  * Message types (`IRequest<,>`, `INotification`), handler types (`IRequestHandler<,>`, `INotificationHandler<>`), pipeline types (`IPipelineBehavior`)

You install the source generator package into your edge/outermost project (i.e. ASP.NET Core application, Background worker project),
and then use the `Mediator` package wherever you define message types and handlers.
Standard message handlers are automatically picked up and added to the DI container in the generated `AddMediator` method.
Pipeline behaviors need to be added manually.

For example implementations, see the [/samples](/samples) folder.
See the [ASP.NET sample](/samples/ASPNET_CleanArchitecture) for a more real world setup.

### 3.1. Message types

* `IMessage` - marker interface
* `IStreamMessage` - marker interface
* `IBaseRequest` - market interface for requests
* `IRequest` - a request message, no return value (`ValueTask<Unit>`)
* `IRequest<out TResponse>` - a request message with a response (`ValueTask<TResponse>`)
* `IStreamRequest<out TResponse>` - a request message with a streaming response (`IAsyncEnumerable<TResponse>`)
* `IBaseCommand` - marker interface for commands
* `ICommand` - a command message, no return value (`ValueTask<Unit>`)
* `ICommand<out TResponse>` - a command message with a response (`ValueTask<TResponse>`)
* `IStreamCommand<out TResponse>` - a command message with a streaming response (`IAsyncEnumerable<TResponse>`)
* `IBaseQuery` - marker interface for queries
* `IQuery<out TResponse>` - a query message with a response (`ValueTask<TResponse>`)
* `IStreamQuery<out TResponse>` - a query message with a streaming response (`IAsyncEnumerable<TResponse>`)
* `INotification` - a notification message, no return value (`ValueTask`)

As you can see, you can achieve the exact same thing with requests, commands and queries. But I find the distinction in naming useful if you for example use the CQRS pattern or for some reason have a preference on naming in your application.

### 3.2. Handler types

* `IRequestHandler<in TRequest>`
* `IRequestHandler<in TRequest, TResponse>`
* `IStreamRequestHandler<in TRequest, out TResponse>`
* `ICommandHandler<in TCommand>`
* `ICommandHandler<in TCommand, TResponse>`
* `IStreamCommandHandler<in TCommand, out TResponse>`
* `IQueryHandler<in TQuery, TResponse>`
* `IStreamQueryHandler<in TQuery, out TResponse>`
* `INotificationHandler<in TNotification>`

These types are used in correlation with the message types above.

### 3.3. Pipeline types

* `IPipelineBehavior<TMessage, TResponse>`
* `IStreamPipelineBehavior<TMessage, TResponse>`

```csharp
public sealed class GenericHandler<TMessage, TResponse> : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
{
    public ValueTask<TResponse> Handle(TMessage message, CancellationToken cancellationToken, MessageHandlerDelegate<TMessage, TResponse> next)
    {
        // ...
        return next(message, cancellationToken);
    }
}

public sealed class GenericStreamHandler<TMessage, TResponse> : IStreamPipelineBehavior<TMessage, TResponse>
    where TMessage : IStreamMessage
{
    public IAsyncEnumerable<TResponse> Handle(TMessage message, CancellationToken cancellationToken, StreamHandlerDelegate<TMessage, TResponse> next)
    {
        // ...
        return next(message, cancellationToken);
    }
}
```

### 3.4. Configuration

There is an assembly level attribute for configuration: `MediatorOptionsAttribute`.
Declare the attribute in the project where the source generator is installed.

* `Namespace` - where the `IMediator` implementation is generated
* `DefaultServiceLifetime` - the DI service lifetime
  * `Singleton` - (default value) everything registered as singletons, minimal allocations
  * `Transient` - handlers registered as transient, `IMediator`/`Mediator`/`ISender`/`IPublisher` still singleton
  * `Scoped`    - mediator and handlers registered as scoped

## 4. Getting started

In this section we will get started with Mediator and go through a sample
illustrating the various ways the Mediator pattern can be used in an application.

See the full runnable sample code in the [SimpleEndToEnd sample](/samples/SimpleEndToEnd/).

### 4.1. Add package

```pwsh
dotnet add package Mediator.SourceGenerator --version 1.0.*
dotnet add package Mediator.Abstractions --version 1.0.*
```
or
```xml
<PackageReference Include="Mediator.SourceGenerator" Version="1.0.*">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
</PackageReference>
<PackageReference Include="Mediator.Abstractions" Version="1.0.*" />
```

### 4.2. Add Mediator to DI container

In `ConfigureServices` or equivalent, call `AddMediator` (unless `MediatorOptions` is configured, default namespace is `Mediator`).
This registers your handler below.

```csharp
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using System;

var services = new ServiceCollection(); // Most likely IServiceCollection comes from IHostBuilder/Generic host abstraction in Microsoft.Extensions.Hosting

services.AddMediator();
var serviceProvider = services.BuildServiceProvider();
```

### 4.3. Create `IRequest<>` type

```csharp
var mediator = serviceProvider.GetRequiredService<IMediator>();
var ping = new Ping(Guid.NewGuid());
var pong = await mediator.Send(ping);
Debug.Assert(ping.Id == pong.Id);

// ...

public sealed record Ping(Guid Id) : IRequest<Pong>;

public sealed record Pong(Guid Id);

public sealed class PingHandler : IRequestHandler<Ping, Pong>
{
    public ValueTask<Pong> Handle(Ping request, CancellationToken cancellationToken)
    {
        return new ValueTask<Pong>(new Pong(request.Id));
    }
}
```

As soon as you code up message types, the source generator will add DI registrations automatically (inside `AddMediator`).
P.S - You can inspect the code yourself - open `Mediator.g.cs` in VS from Project -> Dependencies -> Analyzers -> Mediator.SourceGenerator -> Mediator.SourceGenerator.MediatorGenerator,
or just F12 through the code.

### 4.4. Use pipeline behaviors

The pipeline behavior below validates all incoming `Ping` messages.
Pipeline behaviors currently must be added manually.

```csharp
services.AddMediator();
services.AddSingleton<IPipelineBehavior<Ping, Pong>, PingValidator>();

public sealed class PingValidator : IPipelineBehavior<Ping, Pong>
{
    public ValueTask<Pong> Handle(Ping request, CancellationToken cancellationToken, MessageHandlerDelegate<Ping, Pong> next)
    {
        if (request is null || request.Id == default)
            throw new ArgumentException("Invalid input");

        return next(request, cancellationToken);
    }
}
```

### 4.5. Constrain `IPipelineBehavior<,>` message with open generics

Add open generic handler to process all or a subset of messages passing through Mediator.
This handler will log any error that is thrown from message handlers (`IRequest`, `ICommand`, `IQuery`).
It also publishes a notification allowing notification handlers to react to errors.

```csharp
services.AddMediator();
services.AddSingleton(typeof(IPipelineBehavior<,>), typeof(ErrorLoggerHandler<,>));

public sealed record ErrorMessage(Exception Exception) : INotification;
public sealed record SuccessfulMessage() : INotification;

public sealed class ErrorLoggerHandler<TMessage, TResponse> : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage // Constrained to IMessage, or constrain to IBaseCommand or any custom interface you've implemented
{
    private readonly ILogger<ErrorLoggerHandler<TMessage, TResponse>> _logger;
    private readonly IMediator _mediator;

    public ErrorLoggerHandler(ILogger<ErrorLoggerHandler<TMessage, TResponse>> logger, IMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
    }

    public async ValueTask<TResponse> Handle(TMessage message, CancellationToken cancellationToken, MessageHandlerDelegate<TMessage, TResponse> next)
    {
        try
        {
            var response = await next(message, cancellationToken);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling message");
            await _mediator.Publish(new ErrorMessage(ex));
            throw;
        }
    }
}
```

### 4.6. Use notifications

We can define a notification handler to catch errors from the above pipeline behavior.

```csharp
// Notification handlers are automatically added to DI container

public sealed class ErrorNotificationHandler : INotificationHandler<ErrorMessage>
{
    public ValueTask Handle(ErrorMessage error, CancellationToken cancellationToken)
    {
        // Could log to application insights or something...
        return default;
    }
}
```

### 4.7. Polymorphic dispatch with notification handlers

We can also define a notification handler that receives all notifications.

```csharp

public sealed class StatsNotificationHandler : INotificationHandler<INotification> // or any other interface deriving from INotification
{
    private long _messageCount;
    private long _messageErrorCount;

    public (long MessageCount, long MessageErrorCount) Stats => (_messageCount, _messageErrorCount);

    public ValueTask Handle(INotification notification, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _messageCount);
        if (notification is ErrorMessage)
            Interlocked.Increment(ref _messageErrorCount);
        return default;
    }
}
```

### 4.8. Notification handlers also support open generics

```csharp
public sealed class GenericNotificationHandler<TNotification> : INotificationHandler<TNotification>
    where TNotification : INotification // Generic notification handlers will be registered as open constrained types automatically
{
    public ValueTask Handle(TNotification notification, CancellationToken cancellationToken)
    {
        return default;
    }
}
```


### 4.9. Use streaming messages

Since version 1.* of this library there is support for streaming using `IAsyncEnumerable`.

```csharp
var mediator = serviceProvider.GetRequiredService<IMediator>();

var ping = new StreamPing(Guid.NewGuid());

await foreach (var pong in mediator.CreateStream(ping))
{
    Debug.Assert(ping.Id == pong.Id);
    Console.WriteLine("Received pong!"); // Should log 5 times
}

// ...

public sealed record StreamPing(Guid Id) : IStreamRequest<Pong>;

public sealed record Pong(Guid Id);

public sealed class PingHandler : IStreamRequestHandler<StreamPing, Pong>
{
    public async IAsyncEnumerable<Pong> Handle(StreamPing request, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        for (int i = 0; i < 5; i++)
        {
            await Task.Delay(1000, cancellationToken);
            yield return new Pong(request.Id);
        }
    }
}
```

## 5. Diagnostics

Since this is a source generator, diagnostics are also included. Examples below

* Missing request handler

![Missing request handler](/img/missing_request_handler.png "Missing request handler")

* Multiple request handlers found

![Multiple request handlers found](/img/multiple_request_handlers.png "Multiple request handlers found")


## 6. Differences from [MediatR](https://github.com/jbogard/MediatR)

This is a work in progress list on the differences between this library and MediatR.

* `RequestHandlerDelegate<TResponse>()` -> `MessageHandlerDelegate<TMessage, TResponse>(TMessage message, CancellationToken cancellationToken)`
  * This is to avoid excessive closure allocations. I thin it's worthwhile when the cost is simply passing along the message and the cancellationtoken.
* No `ServiceFactory`
  * This library relies on the `Microsoft.Extensions.DependencyInjection`, so it only works with DI containers that integrate with those abstractions.
* Singleton service lifetime by default
  * MediatR in combination with `MediatR.Extensions.Microsoft.DependencyInjection` does transient service registration by default, which leads to a lot of allocations. Even if it is configured for singleton lifetime, `IMediator` and `ServiceFactory` services are registered as transient (not configurable).
