namespace Xer.Cqrs.EventStack.Extensions.Attributes.Tests.Entities
{
    public class TestEvent1 {}

    public class TestEvent2 {}

    public class TestEvent3 {}
    
    public class ExceptionTriggeringEvent {}

    public class LongRunningEvent
    {
        public int DurationInMilliseconds { get; }

        public LongRunningEvent(int durationInMilliseconds)
        {
            DurationInMilliseconds = durationInMilliseconds;
        }
    }
}
