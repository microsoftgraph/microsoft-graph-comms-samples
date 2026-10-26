
## A list of common issues when debugging locally

### Local Console app

You are running the bot within visual studio (as a console app) and it starts, runs and closes unexpectedly early.

**Possible Solution:**

Run Visual Studio with Administrative privileges (run in Admin mode - indicator in top right of Visual Studio will confirm)

### Call Status

If your Bot Call status is always Establishing and its never Established

The messaginb bot is unable to commmunicate with the media server

**Possible solutions:**

1. Check SSL Certificates
2. Rerun certs.bat
3. Ngrok check that running and is redirecting correctly (no errors or missing responses)
4. Ngrok check local firewall (incoming and outgoing)
5. Check Reserved TCP Address in Ngrok site is setup

(To be confirmed: there has been some mentions of only 0 and 1 subdomains in Ngrok being useful e.g.
1.tcp.ngrok.io:27188)

### Cannot add participant

If your media service cannot connect to your local media processor and returns a *MediaControllerConversationAddParticipantFailed*

**Possible solution:**

1. Confirm that Ngrok is running locally
2. Confirm that your ports match the port Ngrok opened up for you (.env file) - `AzureSettings__InstancePublicPort=RESERVED_PORT`
3. your `CName` points to the right instance of the Ngrok tcp proxy.

### Other things to try

Another common issue is not using the right certificate for your bot.
Confirm the certificate is in the correct place, has not expired, the thumbprint is correct and the the certs.bat rules have been run.
