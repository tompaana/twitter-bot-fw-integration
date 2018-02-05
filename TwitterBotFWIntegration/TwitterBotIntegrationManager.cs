using Microsoft.Bot.Connector.DirectLine;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TwitterBotFWIntegration.Cache;
using TwitterBotFWIntegration.Models;

namespace TwitterBotFWIntegration
{
    /// <summary>
    /// The main class and API of the library.
    /// </summary>
    public class TwitterBotIntegrationManager : IDisposable
    {
        protected DirectLineManager _directLineManager;
        protected TwitterManager _twitterManager;
        protected IMessageAndUserIdCache _messageAndUserIdCache;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="directLineSecret">The Direct Line secret associated with the bot.</param>
        /// <param name="consumerKey">The Twitter consumer key.</param>
        /// <param name="consumerSecret">The Twitter consumer secret.</param>
        /// <param name="accessToken">The Twitter app access token.</param>
        /// <param name="accessTokenSecret">The Twitter app secret.</param>
        /// <param name="messageAndUserIdCache">A message and user ID cache implementation.</param>
        public TwitterBotIntegrationManager(
            string directLineSecret,
            string consumerKey, string consumerSecret,
            string accessToken = null, string accessTokenSecret = null,
            IMessageAndUserIdCache messageAndUserIdCache = null)
        {
            _directLineManager = new DirectLineManager(directLineSecret);
            _twitterManager = new TwitterManager(consumerKey, consumerSecret, accessToken, accessTokenSecret);
            _messageAndUserIdCache = messageAndUserIdCache ?? new InMemoryMessageAndUserIdCache();

            _directLineManager.ActivitiesReceived += OnActivitiesReceived;
            _twitterManager.TweetReceived += OnTweetReceivedAsync;
        }

        /// <summary>
        /// Starts the manager (starts listening for incoming tweets).
        /// </summary>
        public void Start()
        {
            _twitterManager.StartStream();
        }

        public void Dispose()
        {
            _directLineManager.ActivitiesReceived -= OnActivitiesReceived;
            _twitterManager.TweetReceived -= OnTweetReceivedAsync;

            _directLineManager.Dispose();
            _twitterManager.Dispose();
        }

        /// <summary>
        /// Sends the message in the given activity to the user in Twitter with the given identity.
        /// Both the activity and the Twitter user identifier are removed from the collections.
        /// </summary>
        /// <param name="activity">The activity containing the message.</param>
        /// <param name="twitterUserIdentifier">The IDs of the Twitter user to reply to.</param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        protected virtual void ReplyInTwitter(Activity activity, TwitterUserIdentifier twitterUserIdentifier)
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

            _twitterManager.SendMessage(activity.Text, twitterUserIdentifier.TwitterUserId, twitterUserIdentifier.ScreenName);
        }

        /// <summary>
        /// Checks the list of received activities for message IDs matching the previously sent
        /// Direct Line messages. If we have a match, we know it's the bot's reply to the Twitter user.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="activities">A list of activities sent by the bot.</param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        protected virtual void OnActivitiesReceived(object sender, IList<Activity> activities)
        {
            foreach (Activity activity in activities)
            {
                string messageId = activity.ReplyToId;

                if (!string.IsNullOrEmpty(messageId))
                {
                    MessageIdAndTimestamp messageIdAndTimestamp = new MessageIdAndTimestamp(messageId);

                    TwitterUserIdentifier twitterUserIdentifier =
                        _messageAndUserIdCache.GetTwitterUserWaitingForReply(messageIdAndTimestamp);

                    if (twitterUserIdentifier != null)
                    {
                        ReplyInTwitter(activity, twitterUserIdentifier);
                    }
                    else
                    {
                        // Looks like we received the reply activity before we got back
                        // the response from sending the original message to the bot
                        _messageAndUserIdCache.AddPendingReplyFromBotToTwitterUser(messageIdAndTimestamp, activity);

                        System.Diagnostics.Debug.WriteLine($"Stored activity with message ID '{messageId}'");
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
        protected virtual async void OnTweetReceivedAsync(object sender, Tweetinvi.Events.MessageEventArgs messageEventArgs)
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
                    TwitterUserId = messageEventArgs.Message.SenderId,
                    ScreenName = messageEventArgs.Message.SenderScreenName
                };

                // Store the Twitter user details so that we know who to reply to
                _messageAndUserIdCache.AddTwitterUserWaitingForReply(messageIdAndTimestamp, twitterUserIdentifier);

                _directLineManager.StartPolling();
            }

            // Check for pending activities
            foreach (ActivityForTwitterUserBundle pendingMessage
                in _messageAndUserIdCache.GetPendingRepliesToTwitterUsers())
            {
                ReplyInTwitter(
                    pendingMessage.ActivityForTwitterUser,
                    pendingMessage.TwitterUserIdentifier);
            }
        }
    }
}
