# Ngrok

From the Ngrok documentation – "`ngrok allows you to expose a web server running on your local machine to the internet.`"

## Local debugging

A benefit of using Ngrok is the ability to [debug your channel locally](https://blog.botframework.com/2017/10/19/debug-channel-locally-using-ngrok/).
Specifically Ngrok can be used to forward messages from external channels on the web directly to our local machine to allow debugging, as opposed to the standard messaging endpoint configured in the Bot Framework portal.

Messaging bots use HTTP, but calls and online meeting bots use the lower-level TCP. Ngrok supports TCP tunnels in addition to HTTP tunnels. Since Ngroks public TCP endpoints have fixed URLs you should have a DNS `CNAME` entry for your service that points to these URLs.
This enables the Microsoft media service to connect with the local bot.


More information can be found here:
- [How to develop calling and online meeting bots on your local PC](https://docs.microsoft.com/en-us/microsoftteams/platform/bots/calls-and-meetings/debugging-local-testing-calling-meeting-bots).
- [Tips on debugging your local development environment](..\debug.md)

Notes:
Please note that while local bot debugging can be performed, it is not a method Microsoft officially supports. 
Since Ngrok free accounts don't provide end-to-end encryption you will need to consider a paid Ngrok account for which the installation instructions are as follows:

## Ngrok Installation

### Installing Ngrok on Windows

#### Use the Chocolatey Package Manager

If you use the Chocolatey package manager (highly recommended), installation simply requires the following command from an elevated command prompt:

```powershell
choco install ngrok.portable
```

This will install Ngrok in your PATH so you can run it from any directory.

#### Install Manually

Installing Ngrok manually involves a few more steps:

1. Download the Ngrok ZIP file from this site: https://ngrok.com/download
2. Unzip the `ngrok.exe` file
3. Place the `ngrok.exe` in a folder of your choosing
4. Make sure the folder is in your PATH environment variable

#### Test Your Installation

To test that Ngrok is installed properly, open a new command window (command prompt or PowerShell) and run the following:

```powershell
ngrok version
```

It should print a string like "ngrok version 2.x.x". If you get something like "'ngrok' is not recognized" it probably means you don't have the folder containing ngrok.exe in your PATH environment variable. You may also need to open a new command window.

### Installing Ngrok on Linux (Ubuntu 18/20) or WSL

Installing Ngrok on Linux or WSL, you will need to download the Ngrok ZIP file from this site: https://ngrok.com/download. Make sure to choose the appropriate to your Linux OS the type of Ngrok file from the `More Options` dropdown menu options. For example, for WSL running Linux 64-bit, choose the `Linux` option which will download you the `ngrok-stable-linux-amd64.zip` file.
Run the following commands in your WSL/Linux terminal to install Ngrok:

```bash
cd ~
sudo apt-get unzip
sudo wget https://bin.equinox.io/c/4VmDzA7iaHb/ngrok-stable-linux-amd64.zip
unzip ngrok-stable-linux-amd64.zip
```

Copy the Ngrok file into your `~/.local/bin` folder so it can be accessed from any location.

To test that Ngrok is installed properly, run the following:

```bash
ngrok version
```

## Setting up Ngrok

1. Navigate to [Reserved Domains](https://dashboard.ngrok.com/endpoints/domains) in your Ngrok account and reserve a domain. Make sure you select the US region. We will configure Azure and the bot to point to this domain. We'll refer to this domain as `RESERVED_DOMAIN` in this doc.

2. Now navigate to [TCP Addresses](https://dashboard.ngrok.com/endpoints/tcp-addresses) and reserve a TCP port. Make sure you select the US region. This will be used to push incoming streams to. We'll refer to this port as `RESERVED_PORT` and the full address will be referred to as `RESERVE_FULL_TCP_PORT_ADDRESS`.
