using Microsoft.Bot.Connector.DirectLine;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace TwitterBotFWIntegration
{
    /// <summary>
    /// Note that all comparisons are done using the message ID only!
    /// </summary>
    class MessageIdAndTimestamp : IEquatable<MessageIdAndTimestamp>
    {
        public MessageIdAndTimestamp(string messageId)
        {
            if (string.IsNullOrEmpty(messageId))
            {
                throw new ArgumentNullException("Message ID cannot be null or empty");
            }

            MessageId = messageId;
            Timestamp = DateTime.Now;
        }

        public string MessageId
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
            return (other.MessageId.Equals(MessageId));
        }

        public override int GetHashCode()
        {
            int result = 0;

            for (int i = 0; i < MessageId.Length; ++i)
            {
                result += MessageId[i];
            }

            return result;
        }
    }

    class TwitterUserIdentifier : IEquatable<TwitterUserIdentifier>
    {
        public long Id
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
            return (other.Id == Id
                && other.ScreenName.Equals(ScreenName));
        }
    }

    /// <summary>
    /// The main class and API of the library.
    /// </summary>
    public class TwitterBotIntegrationManager : IDisposable
    {
        private DirectLineManager _directLineManager;
        private Twitter _twitter;
        private Dictionary<MessageIdAndTimestamp, TwitterUserIdentifier> _twitterUsersWaitingForReply;
        private Dictionary<MessageIdAndTimestamp, Activity> _pendingReplies;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="directLineSecret">The Direct Line secret associated with the bot.</param>
        /// <param name="consumerKey">The Twitter consumer key.</param>
        /// <param name="consumerSecret">The Twitter consumer secret.</param>
        /// <param name="accessToken">The Twitter app access token.</param>
        /// <param name="accessTokenSecret">The Twitter app secret.</param>
        public TwitterBotIntegrationManager(
            string directLineSecret,
            string consumerKey, string consumerSecret,
            string accessToken = null, string accessTokenSecret = null)
        {
            _directLineManager = new DirectLineManager(directLineSecret);
            _twitter = new Twitter(consumerKey, consumerSecret, accessToken, accessTokenSecret);

            _directLineManager.ActivitiesReceived += OnActivitiesReceived;
            _twitter.TweetReceived += OnTweetReceivedAsync;

            _twitterUsersWaitingForReply = new Dictionary<MessageIdAndTimestamp, TwitterUserIdentifier>();
            _pendingReplies = new Dictionary<MessageIdAndTimestamp, Activity>();
        }

        /// <summary>
        /// Starts the manager (starts listening for incoming tweets).
        /// </summary>
        public void Start()
        {
            _twitter.StartStream();
        }

        public void Dispose()
        {
            _directLineManager.ActivitiesReceived -= OnActivitiesReceived;
            _twitter.TweetReceived -= OnTweetReceivedAsync;

            _directLineManager.Dispose();
            _twitter.Dispose();
        }

        /// <summary>
        /// Sends the message in the given activity to the user in Twitter with the given identity.
        /// Both the activity and the Twitter user identifier are removed from the collections.
        /// </summary>
        /// <param name="activity">The activity containing the message.</param>
        /// <param name="twitterUserIdentifier">The IDs of the Twitter user to reply to.</param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        private void ReplyInTwitter(Activity activity, TwitterUserIdentifier twitterUserIdentifier)
        {
            if (activity == null || twitterUserIdentifier == null)
            {
                throw new ArgumentNullException("Either the activity or the Twitter user identifier is null");
            }

            string messageId = activity.ReplyToId;

            if (string.IsNullOrEmpty(messageId))
            {
                throw new ArgumentNullException("The activity is missing the 'reply to ID'");
            }

            System.Diagnostics.Debug.WriteLine(
                $"Replying to user '{twitterUserIdentifier.ScreenName}' using message in activity with message ID '{messageId}'");

            _twitter.SendMessage(activity.Text, twitterUserIdentifier.Id, twitterUserIdentifier.ScreenName);

            MessageIdAndTimestamp messageIdAndTimestamp = new MessageIdAndTimestamp(messageId);
            _twitterUsersWaitingForReply.Remove(messageIdAndTimestamp);
            _pendingReplies.Remove(messageIdAndTimestamp);
        }

        /// <summary>
        /// Check the message IDs in the pending activities and the Twitter user identifier
        /// dictionary for matches. If a match is found, the message in the activity is sent to
        /// the matching Twitter user.
        /// </summary>
        [MethodImpl(MethodImplOptions.Synchronized)]
        private void CheckForPendingActivities()
        {
            if (_pendingReplies.Count > 0 && _twitterUsersWaitingForReply.Count > 0)
            {
                string messageId = string.Empty;

                while (messageId != null)
                {
                    messageId = null;
                    MessageIdAndTimestamp messageIdAndTimestamp = null;

                    // To consider: Replace the following using LINQ
                    foreach (MessageIdAndTimestamp key in _pendingReplies.Keys)
                    {
                        if (_twitterUsersWaitingForReply.ContainsKey(key))
                        {
                            messageIdAndTimestamp = key;
                            messageId = messageIdAndTimestamp.MessageId;
                            break;
                        }
                    }

                    if (!string.IsNullOrEmpty(messageId))
                    {
                        ReplyInTwitter(
                            _pendingReplies[messageIdAndTimestamp],
                            _twitterUsersWaitingForReply[messageIdAndTimestamp]);
                    }
                }
            }
        }

        /// <summary>
        /// Checks the list of received activities for message IDs matching the previously sent
        /// Direct Line messages. If we have a match, we know it's the bot's reply to the Twitter user.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="activities">A list of activities sent by the bot.</param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        private void OnActivitiesReceived(object sender, IList<Activity> activities)
        {
            foreach (Activity activity in activities)
            {
                string messageId = activity.ReplyToId;

                if (!string.IsNullOrEmpty(messageId))
                {
                    MessageIdAndTimestamp messageIdAndTimestamp = new MessageIdAndTimestamp(messageId);

                    if (_twitterUsersWaitingForReply.ContainsKey(messageIdAndTimestamp))
                    {
                        TwitterUserIdentifier twitterUserIdentifier = _twitterUsersWaitingForReply[messageIdAndTimestamp];

                        if (twitterUserIdentifier != null)
                        {
                            ReplyInTwitter(activity, twitterUserIdentifier);
                        }
                    }
                    else
                    {
                        // Looks like we received the reply activity before we got back
                        // the response from sending the original message to the bot
                        _pendingReplies[messageIdAndTimestamp] = activity;

                        System.Diagnostics.Debug.WriteLine(
                            $"Stored activity with message ID '{messageId}' - the number pending activities is now {_pendingReplies.Count}");
                    }
                }
            }
        }

        /// <summary>
        /// Sends the message in the received tweet to the bot via Direct Line.
        /// If we get a valid response indicating that the message was received by the bot,
        /// we will store the Twitter user identifiers (to be able to reply back).
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="messageEventArgs">Contains the Twitter message details.</param>
        private async void OnTweetReceivedAsync(object sender, Tweetinvi.Events.MessageEventArgs messageEventArgs)
        {
            string messageId = await _directLineManager.SendMessageAsync(
                messageEventArgs.Message.Text,
                messageEventArgs.Message.SenderId.ToString(),
                messageEventArgs.Message.SenderScreenName);

            if (string.IsNullOrEmpty(messageId))
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Failed to send the message from user '{messageEventArgs.Message.SenderScreenName}' to the bot - message text was '{messageEventArgs.Message.Text}'");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Message from user '{messageEventArgs.Message.SenderScreenName}' successfully sent to the bot - message ID is '{messageId}'");

                MessageIdAndTimestamp messageIdAndTimestamp = new MessageIdAndTimestamp(messageId);

                TwitterUserIdentifier twitterUserIdentifier = new TwitterUserIdentifier()
                {
                    Id = messageEventArgs.Message.SenderId,
                    ScreenName = messageEventArgs.Message.SenderScreenName
                };

                _twitterUsersWaitingForReply[messageIdAndTimestamp] = twitterUserIdentifier;

                CheckForPendingActivities();

                _directLineManager.StartPolling();
            }
        }
    }
}
