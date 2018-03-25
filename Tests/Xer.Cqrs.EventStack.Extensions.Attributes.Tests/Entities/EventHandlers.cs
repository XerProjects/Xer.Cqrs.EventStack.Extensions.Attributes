using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Xer.Cqrs.EventStack.Extensions.Attributes.Tests.Entities
{
    #region Attribute Event Handlers

    public class TestAttributedEventHandler
    {
        private readonly List<object> _handledEvents = new List<object>();
        private readonly ITestOutputHelper _outputHelper;

        public IReadOnlyCollection<object> HandledEvents => _handledEvents.AsReadOnly();

        public TestAttributedEventHandler(ITestOutputHelper testOutputHelper)
        {
            _outputHelper = testOutputHelper;
        }
        
        [EventHandler]
        public void HandleTestEvent1(TestEvent1 @event)
        {
            BaseHandle(@event);
        }

        [EventHandler]
        public void HandleTestEvent2(TestEvent2 @event)
        {
            BaseHandle(@event);
        }

        [EventHandler]
        public void HandleTestEvent3(TestEvent3 @event)
        {
            BaseHandle(@event);
        }

        [EventHandler]
        public Task HandleTestEvent1Async(TestEvent1 @event)
        {
            BaseHandle(@event);
            return Task.CompletedTask;
        }

        [EventHandler]
        public Task HandleTestEvent2Async(TestEvent2 @event)
        {
            BaseHandle(@event);
            return Task.CompletedTask;
        }

        [EventHandler]
        public Task HandleTestEvent3Async(TestEvent3 @event)
        {
            BaseHandle(@event);
            return Task.CompletedTask;
        }

        [EventHandler]
        public Task HandleLongRunningEventAsync(LongRunningEvent @event, CancellationToken cancellationToken)
        {
            BaseHandle(@event);
            return Task.Delay(@event.DurationInMilliseconds, cancellationToken);
        }

        public bool HasHandledEvent<TEvent>()
        {
            return _handledEvents.Any(e => e is TEvent);
        }

        protected void BaseHandle<TEvent>(TEvent @event) where TEvent : class
        {
            if (@event == null)
            {
                throw new ArgumentNullException(nameof(@event));
            }

            _outputHelper.WriteLine($"{DateTime.Now}: {GetType().Name} handled {@event.GetType().Name} event.");
            _handledEvents.Add(@event);
        }

        public static int GetEventHandlerAttributeCountFor<TEvent>() => 
            typeof(TestAttributedEventHandler)
                .GetTypeInfo()
                .DeclaredMethods
                .Count(m => m.GetCustomAttributes(typeof(EventHandlerAttribute), true).Any() &&
                            m.GetParameters().Any(p => p.ParameterType == typeof(TEvent)));
    }

    #endregion Attribute Event Handlers

    public class TestEventHandlerException : Exception
    {
        public TestEventHandlerException() { }
        public TestEventHandlerException(string message) : base(message) { }
    }
}
