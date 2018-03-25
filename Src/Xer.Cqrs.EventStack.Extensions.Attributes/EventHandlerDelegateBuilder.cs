using System;
using System.Threading;
using System.Threading.Tasks;
using Xer.Delegator;

namespace Xer.Cqrs.EventStack.Extensions.Attributes
{
    internal class EventHandlerDelegateBuilder
    {
        #region From Delegate

        internal static MessageHandlerDelegate FromDelegate<TAttributed, TEvent>(Func<object> attributedObjectFactory, 
                                                                                 Func<TAttributed, TEvent, Task> asyncAction)
                                                                                 where TAttributed : class
                                                                                 where TEvent : class
        {
            if (attributedObjectFactory == null)
            {
                throw new ArgumentNullException(nameof(attributedObjectFactory));
            }

            if (asyncAction == null)
            {
                throw new ArgumentNullException(nameof(asyncAction));
            }

            return (inputEvent, ct) =>
            {
                if (!TryGetExpectedInstanceFromFactory(attributedObjectFactory, out TAttributed instance, out Exception exception))
                {
                    // Exception occurred or null is returned by factory.
                    return TaskUtility.FromException(exception);
                }
                
                // Check for correct event type.
                if (inputEvent is TEvent e)
                {
                    return asyncAction.Invoke(instance, e);
                }
                
                return TaskUtility.FromException(new ArgumentException($"Invalid event. Expected event of type {typeof(TEvent).Name} but {inputEvent.GetType().Name} found.", nameof(inputEvent)));
            };
        }

        internal static MessageHandlerDelegate FromDelegate<TAttributed, TEvent>(Func<object> attributedObjectFactory, 
                                                                                 Func<TAttributed, TEvent, CancellationToken, Task> cancellableAsyncAction)
                                                                                 where TAttributed : class
                                                                                 where TEvent : class
        {
            if (attributedObjectFactory == null)
            {
                throw new ArgumentNullException(nameof(attributedObjectFactory));
            }

            if (cancellableAsyncAction == null)
            {
                throw new ArgumentNullException(nameof(cancellableAsyncAction));
            }

            return (inputEvent, ct) =>
            {
                if (!TryGetExpectedInstanceFromFactory(attributedObjectFactory, out TAttributed instance, out Exception exception))
                {
                    // Exception occurred or null is returned by factory.
                    return TaskUtility.FromException(exception);
                }
                
                // Check for correct event type.
                if (inputEvent is TEvent e)
                {
                    return cancellableAsyncAction.Invoke(instance, e, ct);
                }

                return TaskUtility.FromException(new ArgumentException($"Invalid event. Expected event of type {typeof(TEvent).Name} but {inputEvent.GetType().Name} was found.", nameof(inputEvent)));
            };
        }

        internal static MessageHandlerDelegate FromDelegate<TAttributed, TEvent>(Func<object> attributedObjectFactory, 
                                                                                 Action<TAttributed, TEvent> action,
                                                                                 bool yieldSynchronousExecution = false)
                                                                                 where TAttributed : class
                                                                                 where TEvent : class
        {
            if (attributedObjectFactory == null)
            {
                throw new ArgumentNullException(nameof(attributedObjectFactory));
            }

            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (yieldSynchronousExecution)
            {
                return async (inputEvent, ct) =>
                {
                    // Yield so the sync handler will be scheduled to execute asynchronously.
                    // This will allow other handlers to start execution.
                    await Task.Yield();

                    if (!TryGetExpectedInstanceFromFactory(attributedObjectFactory, out TAttributed instance, out Exception exception))
                    {
                        // Exception occurred or null is returned by factory.
                        throw exception;
                    }

                    // Check for correct event type.
                    if (inputEvent is TEvent e)
                    {
                        action.Invoke(instance, e);
                    }

                    throw new ArgumentException($"Invalid event. Expected event of type {typeof(TEvent).Name} but {inputEvent.GetType().Name} was found.", nameof(inputEvent));
                };
            }

            return (inputEvent, ct) =>
            {
                try
                {
                    if (!TryGetExpectedInstanceFromFactory(attributedObjectFactory, out TAttributed instance, out Exception exception))
                    {
                        // Exception occurred or null is returned by factory.
                        return TaskUtility.FromException(exception);
                    }
                    
                    // Check for correct event type.
                    if (inputEvent is TEvent e)
                    {
                        action.Invoke(instance, e);
                        return TaskUtility.CompletedTask;
                    }

                    return TaskUtility.FromException(new ArgumentException($"Invalid event. Expected event of type {typeof(TEvent).Name} but {inputEvent.GetType().Name} was found.", nameof(inputEvent)));
                }
                catch(Exception ex)
                {
                    return TaskUtility.FromException(ex);
                }
            };
        }

        #endregion From Delegate

        #region Functions

        private static bool TryGetInstanceFromFactory<T>(Func<T> factory, out T instance, out Exception exception) 
            where T : class
        {
            // Locals.
            instance = null;
            exception = null;

            try
            {
                instance = factory.Invoke();
                if (instance == null)
                {
                    // Factory returned null, no exception actually occurred.
                    exception = FailedToRetrieveInstanceFromFactoryDelegateException<T>();
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                // Wrap inner exception.
                exception = FailedToRetrieveInstanceFromFactoryDelegateException<T>(ex);
                return false;
            }
        }

        private static bool TryGetExpectedInstanceFromFactory<TExpectedInstance>(Func<object> factory, out TExpectedInstance instance, out Exception exception) 
            where TExpectedInstance : class
        {
            // Locals.
            instance = null;
            exception = null;

            if (TryGetInstanceFromFactory(factory, out var factoryInstance, out exception))
            {
                instance = factoryInstance as TExpectedInstance;
                if (instance == null)
                {
                    exception = InvalidInstanceFromFactoryDelegateException(typeof(TExpectedInstance), factoryInstance.GetType());
                    return false;
                }

                return true;
            }

            return false;
        }

        private static InvalidOperationException FailedToRetrieveInstanceFromFactoryDelegateException<T>(Exception ex = null)
        {
            return new InvalidOperationException($"Failed to retrieve an instance of {typeof(T).Name} from the instance factory delegate. Please check registration configuration.", ex);
        }

        private static InvalidOperationException InvalidInstanceFromFactoryDelegateException(Type expected, Type actual, Exception ex = null)
        {
            return new InvalidOperationException($"Invalid instance provided by factory delegate. Expected instnece is of {expected.Name} but was given {actual.Name}.", ex);
        }

        #endregion Functions
    }
}