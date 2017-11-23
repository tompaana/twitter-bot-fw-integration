using System;
using System.Configuration;
using System.Threading;
using TwitterBotFWIntegration;

namespace TwitterBotSample
{
    class Program
    {
        private static TwitterBotIntegrationManager CreateTwitterBotIntegrationManager()
        {
            string directLineSecret = ConfigurationManager.AppSettings["directLineSecret"];
            string consumerKey = ConfigurationManager.AppSettings["consumerKey"];
            string consumerSecret = ConfigurationManager.AppSettings["consumerSecret"];
            string accessToken = ConfigurationManager.AppSettings["accessToken"];
            string accessTokenSecret = ConfigurationManager.AppSettings["accessTokenSecret"];

            return new TwitterBotIntegrationManager(
                directLineSecret, consumerKey, consumerSecret, accessToken, accessTokenSecret);

        }
        static void Main()
        {
            TwitterBotIntegrationManager twitterBotConnection = CreateTwitterBotIntegrationManager();

            twitterBotConnection.Start();

            while (true)
            {
                Thread.Sleep(1000);
            }
        }
    }
}
