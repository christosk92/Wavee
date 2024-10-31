namespace Wavee.Models.Remote;

internal class SendRequestReply
{
    public string type { get; set; }
    public string key { get; set; }
    public SendRequestReplyPayload payload { get; set; }

    internal sealed class SendRequestReplyPayload
    {
        public bool success { get; set; }
    }
}