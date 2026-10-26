# Bot Registration

1. Follow the steps [Register your bot in the Azure Bot Service](https://microsoftgraph.github.io/microsoft-graph-comms-samples/docs/articles/calls/register-calling-bot.html#register-your-bot-in-the-azure-bot-service). Save the bot name, bot app id and bot secret for configuration. We'll refer to the bot name as `BOT_NAME`, app ID as `BOT_ID` and secret as `BOT_SECRET`.

    * For the calling webhook, by default the notification will go to https://{RESERVED_DOMAIN}/api/calling. This URL prefix is configured with the `CallSignalingRoutePrefix` in [HttpRouteConstants.cs](../../src/RecordingBot.Model/Constants/HttpRouteConstants.cs#L21).

    * Ignore the guidance to [Register bot in Microsoft Teams](https://microsoftgraph.github.io/microsoft-graph-comms-samples/docs/articles/calls/register-calling-bot.html#register-bot-in-microsoft-teams). The recording bot won't be called directly. These bots are related to the policies discussed below, and are "attached" to users, and will be automatically invited to the call.

2. Add the [Add Microsoft Graph permissions for calling to your bot](https://microsoftgraph.github.io/microsoft-graph-comms-samples/docs/articles/calls/register-calling-bot.html#add-microsoft-graph-permissions-for-calling-to-your-bot) - not all these permissions _may_ be required, but this is the exact set that we have tested with the bot with so far:

    * Calls.AccessMedia.All
    * Calls.JoinGroupCall.All
    * Calls.JoinGroupCallAsGuest.All

3. Set Redirect URI (It can be any website. Take a note of the URI input)
    * In Azure portal: Your bot (Bot Channels Registration) > Settings > Manage > Authentication > Add a platform > Web: Configure your redirect URI of the application
    > **ToDo**: Add a sample URI redirect!

    > **ToDo**: Add the instructions for either the implicit grant flow must be enabled or left untouched.

4. The Office 365 Tenant Administrator needs give consent (permission) so the bot can join Teams and access media from that meeting
    * Ensure that you have the `TENANT_ID` for the Office 365 (not Azure AD) tenant that the bot will be using to join calls.
    * Go to `https://login.microsoftonline.com/{TENANT_ID}/adminconsent?client_id={BOT_ID}&state=<any_number>&redirect_uri=<any_callback_url>` using tenant admin to sign-in, then consent for the whole tenant. After hitting Accept, you should be redirected to your redirect URI.

      ```text
      https://login.microsoftonline.com/xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx/adminconsent?client_id=xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx&state=0&redirect_uri=https://mybot.requestcatcher.com/
      ```
