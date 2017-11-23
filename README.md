Twitter Bot Framework Integration
=================================


## Running and testing the sample ##

### Prerequisites ###

The assumption here is that you already have a bot created. If not, go to the
[Bot Framework developer portal](https://dev.botframework.com) to learn how to create one. Note
that you need to have **Direct Line** enabled and you will need the **Direct Line secret**.

* You need to have a Twitter account. If you don't have one, sign up at twitter.com
* Create a Twitter app:

    1. Navigate to the [Twitter Application Management portal](https://apps.twitter.com)
    2. Select **Create New App**
    3. Fill in the details and click **Create your Twitter application**
    4. Once your application is created, navigate to the **Permissions** tab, select **Read, Write and Access direct messages** and click **Update Settings**
    5. Navigate to the **Keys and Access Tokens** tab
        * Since we changed the permissions, we need to regenerate the secrets - so click **Regenerate Consumer Key and Secret**
        * Under **Token Actions** click **Create my access token**
        * Now collect the following credentials (we will need these later):
            * Consumer Key (API key)
            * Consumer Secret (API Secret)
            * Access Token
            * Access Token Secret

### Running the solution ###

Now you should have the following details at hand:

* Bot Framework
    * Direct Line secret
* Twitter
    * Consumer Key (API key)
    * Consumer Secret (API Secret)
    * Access Token
    * Access Token Secret

1. Open the solution ([TwitterBotSample.sln](/TwitterBotSample.sln)) in Visual Studio, locate the
   [Secrets.config](/TwitterBotSample/Secrets.config) file and insert the aforementioned keys and
   secrets there.

    ```xml
    <appSettings>
      <add key="directLineSecret" value="BOT DIRECT LINE SECRET HERE"/>
      <add key="consumerKey" value="TWITTER CONSUMER KEY HERE"/>
      <add key="consumerSecret" value="TWITTER CONSUMER SECRET HERE"/>
      <add key="accessToken" value="TWITTER ACCESS TOKEN HERE"/>
      <add key="accessTokenSecret" value="TWITTER ACCESS TOKEN SECRET HERE"/>
    </appSettings>
    ```

2. Run the solution (make sure that `TwitterBotSample` is set as the start-up project.
3. Send a **Direct Message** in Twitter to the Twitter user/account associated with your Twitter app.
    * You can verify the user/account in
      [Twitter Application Management portal](https://apps.twitter.com)
      on **Keys and Access Tokens** tab under **Your Access Token** (see **Owner**).
4. That's it! Now you should be receiving replies from your bot.

