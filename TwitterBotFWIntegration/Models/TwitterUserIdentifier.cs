using System;

namespace TwitterBotFWIntegration.Models
{
    public class TwitterUserIdentifier : IEquatable<TwitterUserIdentifier>
    {
        public long TwitterUserId
        {
            get;
            set;
        }
        public string ScreenName
        {
            get;
            set;
        }

        public bool Equals(TwitterUserIdentifier other)
        {
            return (other.TwitterUserId == TwitterUserId
                && other.ScreenName.Equals(ScreenName));
        }
    }
}
