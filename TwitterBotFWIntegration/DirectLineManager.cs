using Microsoft.Bot.Connector.DirectLine;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TwitterBotFWIntegration
{
    public class DirectLineManager : IDisposable
    {
        /// <summary>
        /// An event that is fired when new messages are received.
        /// </summary>
        public event EventHandler<IList<Activity>> ActivitiesReceived;

        private const int DefaultPollingIntervalInMilliseconds = 2000; 

        private BackgroundWorker _backgroundWorker;
        private SynchronizationContext _synchronizationContext;
        private Conversation _conversation;
        private string _directLineSecret;
        private string _watermark;
        private int _pollingIntervalInMilliseconds;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="directLineSecret">The Direct Line secret associated with the bot.</param>
        public DirectLineManager(string directLineSecret)
        {
            if (string.IsNullOrEmpty(directLineSecret))
            {
                throw new ArgumentNullException("Direct Line secret is null or empty");
            }

            _backgroundWorker = new BackgroundWorker();
            _backgroundWorker.DoWork += new DoWorkEventHandler(RunPollMessagesLoopAsync);
            _backgroundWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(BackgroundWorkerDone);

            _directLineSecret = directLineSecret;
        }

        public void Dispose()
        {
            _backgroundWorker.DoWork -= new DoWorkEventHandler(RunPollMessagesLoopAsync);
            _backgroundWorker.RunWorkerCompleted -= new RunWorkerCompletedEventHandler(BackgroundWorkerDone);
            _backgroundWorker.CancelAsync();
            _backgroundWorker.Dispose();
        }

        /// <summary>
        /// Sends the given message to the bot.
        /// </summary>
        /// <param name="messageText">The message to send.</param>
        /// <param name="senderId">The sender ID.</param>
        /// <param name="senderName">The sender name.</param>
        /// <returns>Message ID if successful. Null otherwise.</returns>
        public async Task<string> SendMessageAsync(string messageText, string senderId = null, string senderName = null)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Sending DL message from {(string.IsNullOrEmpty(senderName) ? "sender" : senderName)}, ID '{senderId}'");

            Activity activityToSend = new Activity
            {
                From = new ChannelAccount(senderId, senderName),
                Type = ActivityTypes.Message,
                Text = messageText
            };

            ResourceResponse resourceResponse = await PostActivityAsync(activityToSend);

            if (resourceResponse != null)
            {
                StartPolling();
                return resourceResponse.Id;
            }

            return null;
        }

        /// <summary>
        /// Polls for new messages (activities).
        /// </summary>
        /// <param name="conversationId">The ID of the conversation.</param>
        /// <returns></returns>
        public async Task PollMessagesAsync(string conversationId = null)
        {
            if (!string.IsNullOrEmpty(conversationId) || !string.IsNullOrEmpty(_conversation?.ConversationId))
            {
                conversationId = string.IsNullOrEmpty(conversationId) ? _conversation.ConversationId : conversationId;
                ActivitySet activitySet = null;

                using (DirectLineClient directLineClient = new DirectLineClient(_directLineSecret))
                {
                    directLineClient.Conversations.ReconnectToConversation(conversationId);
                    activitySet = await directLineClient.Conversations.GetActivitiesAsync(conversationId, _watermark);
                }

                if (activitySet != null)
                {
#if DEBUG
                    if (activitySet.Activities?.Count > 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"{activitySet.Activities?.Count} activity/activities received");
                    }
#endif

                    _watermark = activitySet?.Watermark;

                    var activities = (from activity in activitySet.Activities
                                      select activity)
                                      .ToList();

                    if (_synchronizationContext != null)
                    {
                        _synchronizationContext.Post((o) => ActivitiesReceived?.Invoke(this, activities), null);
                    }
                    else
                    {
                        ActivitiesReceived?.Invoke(this, activities);
                    }
                }
            }
        }

        /// <summary>
        /// Starts polling for the messages.
        /// </summary>
        /// <param name="pollingIntervalInMilliseconds">The polling interval in milliseconds.</param>
        /// <returns>True, if polling was started. False otherwise (e.g. if already running).</returns>
        public bool StartPolling(int pollingIntervalInMilliseconds = DefaultPollingIntervalInMilliseconds)
        {
            if (_backgroundWorker.IsBusy)
            {
                System.Diagnostics.Debug.WriteLine("Already polling");
                return false;
            }

            _synchronizationContext = SynchronizationContext.Current;
            _pollingIntervalInMilliseconds = pollingIntervalInMilliseconds;
            _backgroundWorker.RunWorkerAsync();
            return true;
        }

        /// <summary>
        /// Stops polling for the messages.
        /// </summary>
        public void StopPolling()
        {
            try
            {
                _backgroundWorker.CancelAsync();
            }
            catch (InvalidOperationException e)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to stop polling: {e.Message}");
            }
        }

        /// <summary>
        /// Posts the given activity to the bot using Direct Line client.
        /// </summary>
        /// <param name="activity">The activity to send.</param>
        /// <returns>The resoure response.</returns>
        private async Task<ResourceResponse> PostActivityAsync(Activity activity)
        {
            ResourceResponse resourceResponse = null;

            using (DirectLineClient directLineClient = new DirectLineClient(_directLineSecret))
            {
                if (_conversation == null)
                {
                    _conversation = directLineClient.Conversations.StartConversation();
                }
                else
                {
                    directLineClient.Conversations.ReconnectToConversation(_conversation.ConversationId);
                }

                resourceResponse = await directLineClient.Conversations.PostActivityAsync(_conversation.ConversationId, activity);
            }

            return resourceResponse;
        }

        private async void RunPollMessagesLoopAsync(object sender, DoWorkEventArgs e)
        {
            while (!e.Cancel)
            {
                await PollMessagesAsync();
                Thread.Sleep(_pollingIntervalInMilliseconds);
            }
        }

        private void BackgroundWorkerDone(object sender, RunWorkerCompletedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Background worker finished");
        }
    }
}
