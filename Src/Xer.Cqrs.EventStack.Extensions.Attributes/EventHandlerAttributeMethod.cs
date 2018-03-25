using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xer.Delegator;

namespace Xer.Cqrs.EventStack.Extensions.Attributes
{
    /// <summary>
    /// Represents a single method that is marked with an [EventHandler] attribute.
    /// <para>Supported signatures for methods marked with [EventHandler] are: (Methods can be named differently)</para>
    /// <para>- void HandleEvent(TEvent event);</para>
    /// <para>- Task HandleEventAsync(TEvent event);</para>
    /// <para>- Task HandleEventAsync(TEvent event, CancellationToken cancellationToken);</para>
    /// </summary>
    public partial class EventHandlerAttributeMethod
    {
        #region Static Declarations
        
        private static readonly ParameterExpression CancellationTokenParameterExpression = Expression.Parameter(typeof(CancellationToken), "cancellationToken");
        private static readonly MethodInfo BuildWrappedSyncDelegateOpenGenericMethodInfo = typeof(EventHandlerAttributeMethod).GetTypeInfo().GetDeclaredMethod(nameof(BuildWrappedSyncDelegate));
        private static readonly MethodInfo BuildCancellableAsyncDelegateOpenGenericMethodInfo = typeof(EventHandlerAttributeMethod).GetTypeInfo().GetDeclaredMethod(nameof(BuildCancellableAsyncDelegate));
        private static readonly MethodInfo BuildNonCancellableAsyncDelegateOpenGenericMethodInfo = typeof(EventHandlerAttributeMethod).GetTypeInfo().GetDeclaredMethod(nameof(BuildNonCancellableAsyncDelegate));

        #endregion Static Declarations
        
        #region Properties

        /// <summary>
        /// Method's declaring type.
        /// </summary>
        public Type DeclaringType { get; }
        
        /// <summary>
        /// Factory delegate that provides an instance of this method's declaring type.
        /// </summary>
        public Func<object> InstanceFactory { get; }
        
        /// <summary>
        /// Type of event handled by the method.
        /// </summary>
        public Type EventType { get; }

        /// <summary>
        /// Method info.
        /// </summary>
        public MethodInfo MethodInfo { get; }

        /// <summary>
        /// Indicates if method is an asynchronous method.
        /// </summary>
        public bool IsAsync { get; }

        /// <summary>
        /// Indicates if method supports cancellation.
        /// </summary>
        public bool SupportsCancellation { get; }

        /// <summary>
        /// Indicates if execution should yield for the method.
        /// </summary>
        /// <remarks>
        /// This will never be true for async methods (Method whose IsAsync propoerty is true).
        /// </remarks>
        public bool YieldSynchronousExecution { get; }

        #endregion Properties

        #region Constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="methodInfo">Method info.</param>
        /// <param name="eventType">Type of event that is accepted by this method.</param>
        /// <param name="instanceFactory">Factory delegate that provides an instance of the method info's declaring type.</param>
        /// <param name="isAsync">Is method an async method?</param>
        /// <param name="supportsCancellation">Does method supports cancellation?</param>
        /// <param name="yieldSynchronousExecution">Should yield synchronous method execution?</param>
        private EventHandlerAttributeMethod(MethodInfo methodInfo, Type eventType, Func<object> instanceFactory, bool isAsync, bool supportsCancellation, bool yieldSynchronousExecution = false)
        {
            MethodInfo = methodInfo ?? throw new ArgumentNullException(nameof(methodInfo));
            DeclaringType = methodInfo.DeclaringType;
            EventType = eventType ?? throw new ArgumentNullException(nameof(eventType));
            InstanceFactory = instanceFactory;
            IsAsync = isAsync;
            SupportsCancellation = supportsCancellation;
            YieldSynchronousExecution = !isAsync && yieldSynchronousExecution;
        }

        #endregion Constructors

        #region Methods

        /// <summary>
        /// Create a delegate that handles an event that is specified in <see cref="Xer.Delegator.Registrations.EventHandlerAttributeMethod.EventType"/>.
        /// </summary>
        /// <returns>Delegate that handles an event that is specified in <see cref="Xer.Delegator.Registrations.EventHandlerAttributeMethod.EventType"/>.</returns>
        public MessageHandlerDelegate CreateEventHandlerDelegate()
        {
            try
            {
                if (IsAsync)
                {
                    if (SupportsCancellation)
                    {
                        // Invoke BuildCancellableAsyncDelegate<TDeclaringType, TEvent>(attributedObjectFactory)
                        return InvokeDelegateBuilderMethod(BuildCancellableAsyncDelegateOpenGenericMethodInfo);
                    }
                    else
                    {
                        // Invoke BuildNonCancellableAsyncDelegate<TDeclaringType, TEvent>(attributedObjectFactory)
                        return InvokeDelegateBuilderMethod(BuildNonCancellableAsyncDelegateOpenGenericMethodInfo);
                    }
                }
                else
                {
                    // Invoke BuildWrappedSyncDelegate<TDeclaringType, TEvent>(attributedObjectFactory)
                    return InvokeDelegateBuilderMethod(BuildWrappedSyncDelegateOpenGenericMethodInfo);
                }
            }
            catch(Exception ex)
            {
                throw new InvalidOperationException($"Failed to create event handler delegate for {DeclaringType.Name}'s {MethodInfo.ToString()} method.", ex);
            }
        }

        #endregion Methods

        #region Functions

        /// <summary>
        /// Builds a delegate from an asynchronous (cancellable) action.
        /// </summary>
        /// <typeparam name="TAttributed">Type that contains [EventHandler] methods. This should match DeclaringType property.</typeparam>
        /// <typeparam name="TEvent">Type of event that is handled by the EventHandlerAttributeMethod. This should match EventType property.</typeparam>
        /// <param name="attributedObjectFactory">Factory delegate which provides an instance of a class that contains methods marked with [EventHandler] attribute.</param>
        /// <returns>Delegate that handles an event.</returns>
        private MessageHandlerDelegate BuildCancellableAsyncDelegate<TAttributed, TEvent>(Func<object> attributedObjectFactory)
            where TAttributed : class
            where TEvent : class
        {
            // Create an expression that will invoke the event handler method of a given instance.
            ParameterExpression instanceParameterExpression = Expression.Parameter(typeof(TAttributed), "instance");
            ParameterExpression eventParameterExpression = Expression.Parameter(typeof(TEvent), "event");
            MethodCallExpression callExpression = Expression.Call(instanceParameterExpression, MethodInfo, eventParameterExpression, CancellationTokenParameterExpression);

            // Lambda signature:
            // (instance, @event, cancallationToken) => instance.HandleEventAsync(@event, cancellationToken);
            Func<TAttributed, TEvent, CancellationToken, Task> cancellableAsyncDelegate = Expression.Lambda<Func<TAttributed, TEvent, CancellationToken, Task>>(callExpression, new[] 
            {  
                instanceParameterExpression,
                eventParameterExpression,
                CancellationTokenParameterExpression
            }).Compile();

            return EventHandlerDelegateBuilder.FromDelegate(attributedObjectFactory, cancellableAsyncDelegate);
        }

        /// <summary>
        /// Builds a delegate from an asynchronous (non-cancellable) action.
        /// </summary>
        /// <typeparam name="TAttributed">Type that contains [EventHandler] methods. This should match DeclaringType property.</typeparam>
        /// <typeparam name="TEvent">Type of event that is handled by the EventHandlerAttributeMethod. This should match EventType property.</typeparam>
        /// <param name="attributedObjectFactory">Factory delegate which provides an instance of a class that contains methods marked with [EventHandler] attribute.</param>
        /// <returns>Delegate that handles an event.</returns>
        private MessageHandlerDelegate BuildNonCancellableAsyncDelegate<TAttributed, TEvent>(Func<object> attributedObjectFactory)
            where TAttributed : class
            where TEvent : class
        {
            // Create an expression that will invoke the event handler method of a given instance.
            ParameterExpression instanceParameterExpression = Expression.Parameter(typeof(TAttributed), "instance");
            ParameterExpression eventParameterExpression = Expression.Parameter(typeof(TEvent), "event");
            MethodCallExpression callExpression = Expression.Call(instanceParameterExpression, MethodInfo, eventParameterExpression);

            // Lambda signature:
            // (instance, @event) => instance.HandleEventAsync(@event);
            Func<TAttributed, TEvent, Task> nonCancellableAsyncDelegate = Expression.Lambda<Func<TAttributed, TEvent, Task>>(callExpression, new[] 
            {  
                instanceParameterExpression,
                eventParameterExpression
            }).Compile();

            return EventHandlerDelegateBuilder.FromDelegate(attributedObjectFactory, nonCancellableAsyncDelegate);
        }

        /// <summary>
        /// Builds a delegate from a synchronous action.
        /// </summary>
        /// <typeparam name="TAttributed">Type that contains [EventHandler] methods. This should match DeclaringType property.</typeparam>
        /// <typeparam name="TEvent">Type of event that is handled by the EventHandlerAttributeMethod. This should match EventType property.</typeparam>
        /// <param name="attributedObjectFactory">Factory delegate which provides an instance of a class that contains methods marked with [EventHandler] attribute.</param>
        /// <returns>Delegate that handles an event.</returns>
        private MessageHandlerDelegate BuildWrappedSyncDelegate<TAttributed, TEvent>(Func<object> attributedObjectFactory)
            where TAttributed : class
            where TEvent : class
        {
            // Create an expression that will invoke the event handler method of a given instance.
            ParameterExpression instanceParameterExpression = Expression.Parameter(typeof(TAttributed), "instance");
            ParameterExpression eventParameterExpression = Expression.Parameter(typeof(TEvent), "event");
            MethodCallExpression callExpression = Expression.Call(instanceParameterExpression, MethodInfo, eventParameterExpression);

            // Lambda signature:
            // (instance, @event) => instance.HandleEvent(@event);
            Action<TAttributed, TEvent> action = Expression.Lambda<Action<TAttributed, TEvent>>(callExpression, new[] 
            {  
                instanceParameterExpression,
                eventParameterExpression
            }).Compile();
            
            return EventHandlerDelegateBuilder.FromDelegate(attributedObjectFactory, action, yieldSynchronousExecution: YieldSynchronousExecution);
        }

        /// <summary>
        /// Invoke the specified method to build a delegate that can handle this EventHandlerAttributeMethod's event type.
        /// </summary>
        /// <param name="openGenericBuildDelegateMethodInfo">Method to invoke.</param>
        /// <returns>Delegate that can handle this EventHandlerAttributeMethod's event type.</returns>
        private MessageHandlerDelegate InvokeDelegateBuilderMethod(MethodInfo openGenericBuildDelegateMethodInfo)
        {
            return (MessageHandlerDelegate)openGenericBuildDelegateMethodInfo
                .MakeGenericMethod(DeclaringType, EventType)
                .Invoke(this, new[] {  InstanceFactory });
        }

        #endregion Functions
    }
}
