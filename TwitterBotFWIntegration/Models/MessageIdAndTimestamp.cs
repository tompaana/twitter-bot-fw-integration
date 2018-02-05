using System;

namespace TwitterBotFWIntegration.Models
{
    /// <summary>
    /// Contains a Direct Line message ID (used to match a sent message to the bot with
    /// an incoming reply from the bot) and a timestamp.
    /// 
    /// Note that all comparisons are done using the message ID only!
    /// </summary>
    public class MessageIdAndTimestamp : IEquatable<MessageIdAndTimestamp>
    {
        public MessageIdAndTimestamp(string messageId)
        {
            if (string.IsNullOrEmpty(messageId))
            {
                throw new ArgumentNullException("Message ID cannot be null or empty");
            }

            DirectLineMessageId = messageId;
            Timestamp = DateTime.Now;
        }

        public string DirectLineMessageId
        {
            get;
            private set;
        }

        public DateTime Timestamp
        {
            get;
            private set;
        }

        public bool Equals(MessageIdAndTimestamp other)
        {
            return (other.DirectLineMessageId.Equals(DirectLineMessageId));
        }

        public override int GetHashCode()
        {
            int result = 0;

            for (int i = 0; i < DirectLineMessageId.Length; ++i)
            {
                result += DirectLineMessageId[i];
            }

            return result;
        }
    }
}
