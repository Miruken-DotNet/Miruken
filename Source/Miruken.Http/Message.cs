namespace Miruken.Http;

public class Message
{
    public Message()
    {
    }

    public Message(object payload)
    {
        Payload = payload;
    }

    public object Payload { get; set; }
}