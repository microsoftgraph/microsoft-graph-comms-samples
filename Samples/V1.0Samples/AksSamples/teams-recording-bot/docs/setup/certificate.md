# Setting up SSL Certificate

## Generate SSL Certificate

### Requirements

- Follow [Ngrok installation and configuration guide](./ngrok.md)
- Follow [Certbot installation guide for Windows](https://certbot.eff.org/lets-encrypt/windows-other.html) or [Certbot installation guide for Ubuntu](https://certbot.eff.org/lets-encrypt/ubuntufocal-other)
- Follow [OpenSSL installation guide](./openssl.md)

### Instructions

For this example, I'm using my Ngrok reserved domain of jodogrok. Go get your own!

This will connect Ngrok, set up SSL and save it to `/letsencrypt` or the `etc` folder if you run it on Windows.

Please note, there is a limit on how many times you can do LetsEncrypt (like maybe 5 a week!) so save your `letsencrypt` folder.

If the `letsencrypt` folder exists, it will use these certs instead (will copy them to the right place in the container). If you change your ngrok domain name, you will have to delete this folder first as the certs will not work.

- Go to [Ngrok](https://dashboard.ngrok.com/get-started) and login. You will need a pro plan for this
- Reserve your name (I did jordogrok)
- Edit `config.ini` and replace with your email and your domain name (`jordogrok.ngrok.io` was mine. Note, the example on Ngrok site has "au" in it - leave this out)
- Edit `config.sh` and replace
  - `SUBDOMAIN=jodogrok`
  - `AUTHTOKEN=get from Ngrok dash under (3) Connect your account`
  - `CERTIFICATEPASSWORD=password used when saving certificate.pfx`
- Edit `ngrok.yaml` and replace `SUBDOMAIN` with your subdomain.

Open a Windows Terminal, run `./host.sh` and you're off to the races! Access your domain to see the site that you're redirecting to.

Make sure your browser tells you the cert is working.

You may need to change the host networking type in `.devcontainer/docker-compose.yaml` if you are not seeing results of the forwarding. 

## Installing URL ACL and Certificate Bindings

Once you have finished [Setting up Ngrok](https://github.com/microsoft/netcoreteamsrecordingbot/blob/master/docs/setup/ngrok.md) , lets generate our own signed SSL certificates using our newly reserved domains.

1. Follow the instructions in [Generate SSL Certificate](##generate-ssl-certificate) on this page to configure and run [`host.sh`](../../scripts/config.sh) script. This will produce a SSL certificate we can then use for this project. Make sure when you configure the project to use the `RESERVE_DOMAIN` you created earlier.
2. To install your newly created certificate, hit `WIN+R` on your keyboard and type `mmc`.
3. `File -> Add/Remove Snap In...`
4. Add `Certificates`. You'll see a popup. Make sure you select `Computer account` and `Local computer` is selected before clicking `Finish`.
5. Next, expand `Certificates (Local Computer)` -> `Personal` and click on `Certificates`.
6. You should see a bunch of certificates. Right click -> `All Tasks` -> `Import...`
7. Browse for your `certificate.pfx`. Make sure you change the file extension to `Personal Information Exchange...`. Click next, enter your certificate's password, and click through until the certificate is loaded.
8. Now you should see your certificate. Double click on it -> click on `Details` -> scroll down to the bottom and you'll see `Thumbprint`. Copy and paste it somewhere save. We'll refer to this as `THUMBPRINT`.

Once you've got your thumbprint...

1. Create a new file in `build/` called `certs.bat`.
2. Copy the contents of [certs.bat-template](../../scripts/certs.bat-template) to `certs.bat`.
3. Replace `YOUR_CERT_THUMBPRINT` in [certs.bat](../../scripts/certs.bat-template#L20-#L21) with `THUMBPRINT`.
4. Run the bat file in a new command prompt with administrator privileges.

**NOTE:** if your certificate expires, you'll need to regenerate it and repeat all the steps again, including running `certs.bat` with the new `THUMBPRINT`. You'll also need to update `AzureSettings__CertificateThumbprint` in your `.env` file.

### Troubleshooting

#### Issues related to bash commands

Make sure line endings are in unix format. Use `dos2unix` if Windows `git` checked out files in with incompatible line endings.
