using Microsoft.Bot.Connector.DirectLine;
using System;
using System.Collections.Generic;
using TwitterBotFWIntegration.Models;

namespace TwitterBotFWIntegration.Cache
{
    /// <summary>
    /// An IN-MEMORY implementation of IMessageAndUserIdCache interface.
    /// 
    /// This class is OK to be used for prototyping and testing, but a cloud storage based
    /// implementation is recommended to ensure reliable service in production enviroment.
    /// </summary>
    public class InMemoryMessageAndUserIdCache : IMessageAndUserIdCache
    {
        protected const int DefaultMinCacheExpiryInSeconds = 30;
        protected Dictionary<MessageIdAndTimestamp, TwitterUserIdentifier> _twitterUsersWaitingForReply;
        protected Dictionary<MessageIdAndTimestamp, Activity> _pendingRepliesFromBotToTwitterUser;
        protected int _minCacheExpiryInSeconds;

        public IList<ActivityForTwitterUserBundle> GetPendingRepliesToTwitterUsers()
        {
            RemoveExpiredData(); // Lazy clean-up

            IList<ActivityForTwitterUserBundle> messageToTwitterUserBundles =
                new List<ActivityForTwitterUserBundle>();

            if (_twitterUsersWaitingForReply.Count > 0)
            {
                foreach (MessageIdAndTimestamp messageIdAndTimestamp
                    in _pendingRepliesFromBotToTwitterUser.Keys)
                {
                    if (_twitterUsersWaitingForReply.ContainsKey(messageIdAndTimestamp))
                    {
                        messageToTwitterUserBundles.Add(
                            new ActivityForTwitterUserBundle(
                                _pendingRepliesFromBotToTwitterUser[messageIdAndTimestamp],
                                _twitterUsersWaitingForReply[messageIdAndTimestamp]));
                    }
                }
            }

            return messageToTwitterUserBundles;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="minCacheExpiryInSeconds">The minimum cache expiry time in seconds.
        /// If not provided, the default value is used.</param>
        public InMemoryMessageAndUserIdCache(int minCacheExpiryInSeconds = DefaultMinCacheExpiryInSeconds)
        {
            _twitterUsersWaitingForReply = new Dictionary<MessageIdAndTimestamp, TwitterUserIdentifier>();
            _pendingRepliesFromBotToTwitterUser = new Dictionary<MessageIdAndTimestamp, Activity>();
            _minCacheExpiryInSeconds = minCacheExpiryInSeconds;
        }

        public TwitterUserIdentifier GetTwitterUserWaitingForReply(MessageIdAndTimestamp messageIdAndTimestamp)
        {
            if (messageIdAndTimestamp != null
                && _twitterUsersWaitingForReply.ContainsKey(messageIdAndTimestamp))
            {
                return _twitterUsersWaitingForReply[messageIdAndTimestamp];
            }

            return null;
        }

        public bool AddTwitterUserWaitingForReply(
            MessageIdAndTimestamp messageIdAndTimestamp, TwitterUserIdentifier twitterUserIdentifier)
        {
            if (messageIdAndTimestamp != null
                && twitterUserIdentifier != null
                && !_twitterUsersWaitingForReply.ContainsKey(messageIdAndTimestamp))
            {
                _twitterUsersWaitingForReply.Add(messageIdAndTimestamp, twitterUserIdentifier);
                return true;
            }

            return false;
        }

        public bool RemoveTwitterUserWaitingForReply(MessageIdAndTimestamp messageIdAndTimestamp)
        {
            return _twitterUsersWaitingForReply.Remove(messageIdAndTimestamp);
        }

        public Activity GetPendingReplyFromBotToTwitterUser(MessageIdAndTimestamp messageIdAndTimestamp)
        {
            if (messageIdAndTimestamp != null
                && _pendingRepliesFromBotToTwitterUser.ContainsKey(messageIdAndTimestamp))
            {
                return _pendingRepliesFromBotToTwitterUser[messageIdAndTimestamp];
            }

            return null;
        }

        public bool AddPendingReplyFromBotToTwitterUser(
            MessageIdAndTimestamp messageIdAndTimestamp, Activity pendingReplyActivity)
        {
            if (messageIdAndTimestamp != null
                && !string.IsNullOrEmpty(messageIdAndTimestamp.DirectLineMessageId)
                && pendingReplyActivity != null
                && !_pendingRepliesFromBotToTwitterUser.ContainsKey(messageIdAndTimestamp))
            {
                _pendingRepliesFromBotToTwitterUser.Add(messageIdAndTimestamp, pendingReplyActivity);
                return true;
            }

            return false;
        }

        public bool RemovePendingReplyFromBotToTwitterUser(MessageIdAndTimestamp messageIdAndTimestamp)
        {
            return _pendingRepliesFromBotToTwitterUser.Remove(messageIdAndTimestamp);
        }

        /// <summary>
        /// Clears any records (pending replies and Twitter user identifiers) where the timestamp
        /// (in MessageIdAndTimestamp) is expired.
        /// 
        /// TODO: While the current implementation works, make it nicer e.g. with Linq.
        /// </summary>
        protected virtual void RemoveExpiredData()
        {
            DateTime dateTimeNow = DateTime.Now;
            bool wasRemoved = true;

            while (wasRemoved)
            {
                wasRemoved = false;

                foreach (MessageIdAndTimestamp messageIdAndTimestamp in _pendingRepliesFromBotToTwitterUser.Keys)
                {
                    if (messageIdAndTimestamp.Timestamp.AddSeconds(_minCacheExpiryInSeconds) < dateTimeNow)
                    {
                        _pendingRepliesFromBotToTwitterUser.Remove(messageIdAndTimestamp);
                        wasRemoved = true;
                        break;
                    }
                }

                foreach (MessageIdAndTimestamp messageIdAndTimestamp in _twitterUsersWaitingForReply.Keys)
                {
                    if (messageIdAndTimestamp.Timestamp.AddSeconds(_minCacheExpiryInSeconds) < dateTimeNow)
                    {
                        _twitterUsersWaitingForReply.Remove(messageIdAndTimestamp);
                        wasRemoved = true;
                        break;
                    }
                }
            }
        }
    }
}
