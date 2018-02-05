using System;
using System.Threading;
using Microsoft.Bot.Connector.DirectLine;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TwitterBotFWIntegration.Cache;
using TwitterBotFWIntegration.Models;

namespace TwitterBotFWIntegration.Tests
{
    [TestClass]
    public class InMemoryMessageAndUserIdCacheTests
    {
        /// <summary>
        /// The cache should purge expired records the lazy way, every time
        /// GetPendingRepliesToTwitterUsers() is called.
        /// </summary>
        [TestMethod]
        public void LazyCleanupTest()
        {
            int minCacheExpiryInSeconds = 5;
            int numberOfDualRecordsToCreate = 4;

            Assert.AreEqual(true, minCacheExpiryInSeconds > numberOfDualRecordsToCreate); // Sanity check

            IMessageAndUserIdCache cache = new InMemoryMessageAndUserIdCache(minCacheExpiryInSeconds);

            DateTime recordCreationStartedTime = DateTime.Now;

            for (int i = 0; i < numberOfDualRecordsToCreate; ++i)
            {
                DateTime dateTimeStart = DateTime.Now;

                MessageIdAndTimestamp messageIdAndTimestamp = new MessageIdAndTimestamp(i.ToString());

                bool recordAddedSuccessfully = cache.AddPendingReplyFromBotToTwitterUser(
                    messageIdAndTimestamp,
                    new Activity("message", $"{i}", dateTimeStart, dateTimeStart, $"serviceUrl{i}", $"channelId{i}"));

                Assert.AreEqual(true, recordAddedSuccessfully);

                recordAddedSuccessfully = cache.AddTwitterUserWaitingForReply(
                    messageIdAndTimestamp,
                    new TwitterUserIdentifier() { ScreenName = $"screenName{i}", TwitterUserId = i });

                Assert.AreEqual(true, recordAddedSuccessfully);

                System.Diagnostics.Debug.WriteLine($"Total of {(i + 1) * 2} records created");

                DateTime dateTimeEnd = DateTime.Now;
                TimeSpan timeElapsed = dateTimeEnd - dateTimeStart;
                long millisecondsPassed = timeElapsed.Milliseconds;

                if (millisecondsPassed < 1000)
                {
                    Thread.Sleep(1000 - (int)millisecondsPassed);
                }
            }

            // (numberOfDualRecordsToCreate) seconds should have passed now

            int numberOfDualRecords = cache.GetPendingRepliesToTwitterUsers().Count;

            Assert.AreEqual(numberOfDualRecordsToCreate, numberOfDualRecords);

            bool timeToEnd = false;
            int millisecondsExpectedUntilAllRecordsExpired =
                minCacheExpiryInSeconds * 1000 + numberOfDualRecordsToCreate * 1000 + 1000;

            while (!timeToEnd)
            {
                cache.GetPendingRepliesToTwitterUsers(); // This should execute lazy purge

                Assert.AreEqual(true, cache.GetPendingRepliesToTwitterUsers().Count <= numberOfDualRecords);
                numberOfDualRecords = cache.GetPendingRepliesToTwitterUsers().Count;

                System.Diagnostics.Debug.WriteLine($"Number of dual records is now {numberOfDualRecords}");

                Thread.Sleep(900);
                timeToEnd = ((DateTime.Now - recordCreationStartedTime).TotalMilliseconds
                    > millisecondsExpectedUntilAllRecordsExpired);
            }

            Assert.AreEqual(0, numberOfDualRecords);
        }
    }
}
