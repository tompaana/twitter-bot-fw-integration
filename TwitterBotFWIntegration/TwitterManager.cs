using System;

namespace TwitterBotFWIntegration
{
    public class TwitterManager : IDisposable
    {
        /// <summary>
        /// True, if the Twitter stream is ready (started). False otherwise.
        /// </summary>
        public bool IsReady
        {
            get;
            private set;
        }

        /// <summary>
        /// Fired when a tweet (message) is received.
        /// </summary>
        public event EventHandler<Tweetinvi.Events.MessageEventArgs> TweetReceived;

        private Tweetinvi.Streaming.IUserStream _userStream;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="consumerKey">The Twitter consumer key.</param>
        /// <param name="consumerSecret">The Twitter consumer secret.</param>
        /// <param name="accessToken">The Twitter app access token.</param>
        /// <param name="accessTokenSecret">The Twitter app secret.</param>
        public TwitterManager(string consumerKey, string consumerSecret, string accessToken = null, string accessTokenSecret = null)
        {
            if (string.IsNullOrEmpty(consumerKey) || string.IsNullOrEmpty(consumerSecret))
            {
                throw new ArgumentNullException("Both consumer key and secret must be valid");
            }

            if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(accessTokenSecret))
            {
                Tweetinvi.Auth.SetApplicationOnlyCredentials(consumerKey, consumerSecret);
            }
            else
            {
                Tweetinvi.Auth.SetUserCredentials(consumerKey, consumerSecret, accessToken, accessTokenSecret);
            }
        }

        public void Dispose()
        {
            if (_userStream != null)
            {
                _userStream.StreamIsReady -= OnStreamIsReady;
                _userStream.MessageSent -= OnMessageSent;
                _userStream.MessageReceived -= OnMessageReceived;
                _userStream.TweetFavouritedByMe -= OnTweetFavouritedByMe;
                _userStream.StopStream();
                _userStream = null;
            }
        }

        public void StartStream()
        {
            if (_userStream == null)
            {
                _userStream = Tweetinvi.Stream.CreateUserStream();
                _userStream.StreamIsReady += OnStreamIsReady;
                _userStream.MessageSent += OnMessageSent;
                _userStream.MessageReceived += OnMessageReceived;
                _userStream.TweetFavouritedByMe += OnTweetFavouritedByMe;
                _userStream.StartStream();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Twitter stream already started");
            }
            
        }

        /// <summary>
        /// Sends the given message (text) to the user matching the given IDs.
        /// </summary>
        /// <param name="messageText">The message to send.</param>
        /// <param name="recipientId">The Twitter recipient ID.</param>
        /// <param name="recipientScreenName">The Twitter recipient screen name.</param>
        public void SendMessage(string messageText, long recipientId = 0, string recipientScreenName = null)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Sending message to {(string.IsNullOrEmpty(recipientScreenName) ? "user" : recipientScreenName)} with ID '{recipientId.ToString()}'");

            Tweetinvi.Models.IUserIdentifier userIdentifier = new Tweetinvi.Models.UserIdentifier(recipientId)
            {
                ScreenName = recipientScreenName
            };

            Tweetinvi.Message.PublishMessage(messageText, userIdentifier);
        }

        private void OnStreamIsReady(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Twitter stream ready");
            IsReady = true;
        }

        private void OnMessageSent(object sender, Tweetinvi.Events.MessageEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Twitter message sent");
        }

        private void OnMessageReceived(object sender, Tweetinvi.Events.MessageEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Twitter message received");
            TweetReceived?.Invoke(this, e);
        }

        private void OnTweetFavouritedByMe(object sender, Tweetinvi.Events.TweetFavouritedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Tweet favourited by 'me'");
        }
    }
}
