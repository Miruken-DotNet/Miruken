namespace Miruken.Mediator.Schedule
{
    public class Scheduled : IRequest<ScheduleResult>
    {
        public object[] Requests { get; set; }
    }

    public class ScheduleResult
    {
        public object[] Responses { get; set; }
    }
}
