# Xer.Cqrs.EventStack.Extensions.Attributes
Attribute registration extension for Xer.Cqrs.EventStack

# Build
| Branch | Status |
|--------|--------|
| Master | [![Build status](https://ci.appveyor.com/api/projects/status/91k3s51cguc7qcrb?svg=true)](https://ci.appveyor.com/project/XerProjects25246/xer-cqrs-eventstack-extensions-attributes) |
| Dev | [![Build status](https://ci.appveyor.com/api/projects/status/91k3s51cguc7qcrb/branch/dev?svg=true)](https://ci.appveyor.com/project/XerProjects25246/xer-cqrs-eventstack-extensions-attributes/branch/dev) |

# Nuget
[![NuGet](https://img.shields.io/nuget/vpre/xer.cqrs.eventstack.extensions.attributes.svg)](https://www.nuget.org/packages/Xer.Cqrs.EventStack.Extensions.Attributes/)


## Installation
You can simply clone this repository, build the source, reference the dll from the project, and code away!

Xer.Cqrs.EventStack.Extensions.Attributes library is available as a Nuget package: 

[![NuGet](https://img.shields.io/nuget/v/Xer.Cqrs.EventStack.Extensions.Attributes.svg)](https://www.nuget.org/packages/Xer.Cqrs.EventStack.Extensions.Attributes/)

To install Nuget packages:
1. Open command prompt
2. Go to project directory
3. Add the packages to the project:
    ```csharp
    dotnet add package Xer.Cqrs.EventStack.Extensions.Attributes
    ```
4. Restore the packages:
    ```csharp
    dotnet restore
    ```

### Event Handler Attribute Registration

```csharp
public class ProductRegisteredEvent
{
    public int ProductId { get; }
    public string ProductName { get; }

    public ProductRegisteredEvent(int productId, string productName)
    {
        ProductId = productId;
        ProductName = productName;
    }
}
```
```csharp
public EventDelegator RegisterEventHandlers()
{
    // MultiMessageHandlerRegistration allows registration of a multiple message handlers per message type.
    var registration = new MultiMessageHandlerRegistration();

    // Register all methods with [EventHandler] attribute.
    registration.RegisterEventHandlerAttributes(() => new ProductRegisteredEventHandlers(new ProductRepository());

    // Build the delegator.
    var resolver = registration.BuildMessageHandlerResolver();
    return new EventDelegator(resolver);
}
```
```csharp
public class ProductRegisteredEventHandlers : IEventHandler<ProductRegisteredEvent>
{
    // Sync event handler.
    [EventHandler]
    public void Handle(ProductRegisteredEvent @event)
    {
        System.Console.WriteLine($"ProductRegisteredEventHandler handled {@event.GetType()}.");
    }
    
    // Async event handler.
    [EventHandler]
    public Task SendEmailNotificationAsync(ProductRegisteredEvent @event, CancellationToken ct)
    {
        System.Console.WriteLine($"Sending email notification...");
        return Task.CompletedTask;
    }
}
```
