using System;
using System.Collections.Generic;
using System.Reflection;
using Xer.Cqrs.EventStack.Extensions.Attributes;

namespace Xer.Delegator.Registrations
{
    public static class MultiMessageHandlerRegistrationExtensions
    {
        private static readonly MethodInfo RegisterMessageHandlerDelegateOpenGenericMethodInfo = typeof(MultiMessageHandlerRegistrationExtensions)
                                                                                                    .GetTypeInfo()
                                                                                                    .GetDeclaredMethod(nameof(RegisterMessageHandlerDelegate));

        #region IMessageHandlerRegistration Extensions

        /// <summary>
        /// Register methods marked with the [EventHandler] attribute as event handlers.
        /// <para>Supported signatures for methods marked with [EventHandler] are: (Methods can be named differently)</para>
        /// <para>void HandleEvent(TEvent event);</para>
        /// <para>Task HandleEventAsync(TEvent event);</para>
        /// <para>Task HandleEventAsync(TEvent event, CancellationToken cancellationToken);</para>
        /// </summary>
        /// <typeparam name="TAttributed">Type to search for methods marked with [EventHandler] attribute.</param>
        /// <remarks>
        /// This method will search for the methods marked with [EventHandler] in the type specified in type parameter.
        /// The type parameter should be the actual type that contains [EventHandler] methods.
        /// </remarks>
        /// <param name="registration">Message handler registration.</param>
        /// <param name="attributedObjectFactory">Factory delegate which provides an instance of a class that contains methods marked with [EventHandler] attribute.</param>
        public static void RegisterEventHandlersByAttribute<TAttributed>(this MultiMessageHandlerRegistration registration,
                                                                         Func<TAttributed> attributedObjectFactory)
                                                                         where TAttributed : class
        {
            RegisterEventHandlersByAttribute(registration, EventHandlerAttributeMethod.FromType(attributedObjectFactory));
        }

        /// <summary>
        /// Register methods of the specified type that are marked with the [EventHandler] attribute as event handlers.
        /// <para>Supported signatures for methods marked with [EventHandler] are: (Methods can be named differently)</para>
        /// <para>void HandleEvent(TEvent event);</para>
        /// <para>Task HandleEventAsync(TEvent event);</para>
        /// <para>Task HandleEventAsync(TEvent event, CancellationToken cancellationToken);</para>
        /// </summary>
        /// <param name="registration">Message handler registration.</param>
        /// <param name="type">Type to scan for methods marked with the [CommandHandler] attribute.</param>
        /// <param name="instanceFactory">Factory delegate that provides an instance of the specified type.</param>
        public static void RegisterEventHandlersByAttribute(this MultiMessageHandlerRegistration registration,
                                                            Type type, 
                                                            Func<object> instanceFactory)
        {
            RegisterEventHandlersByAttribute(registration, EventHandlerAttributeMethod.FromType(type, instanceFactory));
        }

        /// <summary>
        /// Register methods of types that are marked with the [EventHandler] attribute as event handlers.
        /// <para>Supported signatures for methods marked with [EventHandler] are: (Methods can be named differently)</para>
        /// <para>void HandleEvent(TEvent event);</para>
        /// <para>Task HandleEventAsync(TEvent event);</para>
        /// <para>Task HandleEventAsync(TEvent event, CancellationToken cancellationToken);</para>
        /// </summary>
        /// <param name="registration">Message handler registration.</param>
        /// <param name="types">Types to scan for methods marked with the [EventHandler] attribute.</param>
        /// <param name="instanceFactory">Factory delegate that provides an instance of a given type.</param>
        public static void RegisterEventHandlersByAttribute(this MultiMessageHandlerRegistration registration,
                                                            IEnumerable<Type> types, 
                                                            Func<Type, object> instanceFactory)
        {
            RegisterEventHandlersByAttribute(registration, EventHandlerAttributeMethod.FromTypes(types, instanceFactory));
        }

        /// <summary>
        /// Register methods of types from the assembly that are marked with the [EventHandler] attribute as event handlers.
        /// <para>Supported signatures for methods marked with [EventHandler] are: (Methods can be named differently)</para>
        /// <para>void HandleEvent(TEvent event);</para>
        /// <para>Task HandleEventAsync(TEvent event);</para>
        /// <para>Task HandleEventAsync(TEvent event, CancellationToken cancellationToken);</para>
        /// </summary>
        /// <param name="registration">Message handler registration.</param>
        /// <param name="assembly">Assembly to scan for methods marked with the [EventHandler] attribute.</param>
        /// <param name="instanceFactory">Factory delegate that provides an instance of a type that has methods marked with [EventHandler] attribute.</param>
        public static void RegisterEventHandlersByAttribute(this MultiMessageHandlerRegistration registration,
                                                            Assembly assembly, 
                                                            Func<Type, object> instanceFactory)
        {
            RegisterEventHandlersByAttribute(registration, EventHandlerAttributeMethod.FromAssembly(assembly, instanceFactory));
        }

        /// <summary>
        /// Register methods of types from the list of assemblies that are marked with the [EventHandler] attribute as event handlers.
        /// <para>Supported signatures for methods marked with [EventHandler] are: (Methods can be named differently)</para>
        /// <para>void HandleEvent(TEvent event);</para>
        /// <para>Task HandleEventAsync(TEvent event);</para>
        /// <para>Task HandleEventAsync(TEvent event, CancellationToken cancellationToken);</para>
        /// </summary>
        /// <param name="registration">Message handler registration.</param>
        /// <param name="assemblies">Assemblies to scan for methods marked with the [EventHandler] attribute.</param>
        /// <param name="instanceFactory">Factory delegate that provides an instance of a type that has methods marked with [EventHandler] attribute.</param>
        public static void RegisterEventHandlersByAttribute(this MultiMessageHandlerRegistration registration,
                                                            IEnumerable<Assembly> assemblies, 
                                                            Func<Type, object> instanceFactory)
        {
            RegisterEventHandlersByAttribute(registration, EventHandlerAttributeMethod.FromAssemblies(assemblies, instanceFactory));
        }

        /// <summary>
        /// Register methods marked with the [EventHandler] attribute as event handlers.
        /// <para>Supported signatures for methods marked with [EventHandler] are: (Methods can be named differently)</para>
        /// <para>void HandleEvent(TEvent event);</para>
        /// <para>Task HandleEventAsync(TEvent event);</para>
        /// <para>Task HandleEventAsync(TEvent event, CancellationToken cancellationToken);</para>
        /// </summary>
        /// <param name="registration">Message handler registration.</param>
        /// <param name="eventHandlerMethods">Objects which represent methods marked with [EventHandler] attribute.</param>
        public static void RegisterEventHandlersByAttribute(this MultiMessageHandlerRegistration registration,
                                                            IEnumerable<EventHandlerAttributeMethod> eventHandlerMethods)
        {
            if (registration == null)
            {
                throw new ArgumentNullException(nameof(registration));
            }

            if (eventHandlerMethods == null)
            {
                throw new ArgumentNullException(nameof(eventHandlerMethods));
            }

            // Get all methods marked with EventHandler attribute and register.
            foreach (EventHandlerAttributeMethod eventHandlerMethod in eventHandlerMethods)
            {
                // Create method and register to registration.
                RegisterMessageHandlerDelegateOpenGenericMethodInfo
                    .MakeGenericMethod(eventHandlerMethod.EventType)
                    // Null because this is static method.
                    .Invoke(null, new object[] 
                    {
                        registration,
                        eventHandlerMethod.CreateEventHandlerDelegate()
                    });
            }
        }

        #endregion IMessageHandlerRegistration Extensions

        #region Functions

        /// <summary>
        /// Create message handler delegate from EventHandlerAttributeMethod and register to MultiMessageHandlerRegistration.
        /// </summary>
        /// <typeparam name="TEvent">Type of event.</typeparam>
        /// <param name="registration">Message handler registration.</param>
        /// <param name="messageHandlerDelegate">Message handler delegate built from a method marked with [EventHandler] attribute.</param>
        private static void RegisterMessageHandlerDelegate<TEvent>(MultiMessageHandlerRegistration registration,
                                                                   MessageHandlerDelegate messageHandlerDelegate)
                                                                   where TEvent : class
        {
            // Create delegate and register.
            registration.Register<TEvent>(messageHandlerDelegate.Invoke);
        }

        #endregion Functions
    }
}