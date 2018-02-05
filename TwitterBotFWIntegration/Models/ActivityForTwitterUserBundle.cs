using Microsoft.Bot.Connector.DirectLine;
using System;

namespace TwitterBotFWIntegration.Models
{
    /// <summary>
    /// Contains a message (Activity) and its recipient (TwitterUserIdentifier).
    /// </summary>
    public class ActivityForTwitterUserBundle
    {
        public Activity ActivityForTwitterUser
        {
            get;
            protected set;
        }

        public TwitterUserIdentifier TwitterUserIdentifier
        {
            get;
            protected set;
        }

        public ActivityForTwitterUserBundle(Activity activityForTwitterUser, TwitterUserIdentifier twitterUserIdentifier)
        {
            if (activityForTwitterUser == null || twitterUserIdentifier == null)
            {
                throw new ArgumentNullException("Activity or Twitter user identifier is null");
            }

            ActivityForTwitterUser = activityForTwitterUser;
            TwitterUserIdentifier = twitterUserIdentifier;
        }
    }
}
