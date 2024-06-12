#!/bin/bash

. ./config.sh

ngrok authtoken $AUTHTOKEN

ROOT=$PWD/etc
if [ ! -d "$ROOT" ]; then
    mkdir $ROOT
fi

CERTNAME="$SUBDOMAIN.pfx"

if [[ "$OSTYPE" == "linux-gnu"* ]]; then
    FULLPATHCERTS=/etc/letsencrypt

    DIR=/workspace/letsencrypt/renewal

    if test -d "$DIR"; then
        echo "Certs exist, copying"
        if [ ! -d $FULLPATHCERTS ]; then
            mkdir $FULLPATHCERTS
        fi
        cp -r ./letsencrypt $ROOT
    else
        ngrok http -host-header="$SUBDOMAIN.ngrok.io" -subdomain="$SUBDOMAIN" 80 > /dev/null &
        #wait for ngrok
        sleep 5s
        certbot certonly --config config.ini --standalone --preferred-challenges http
        cp -r $FULLPATHCERTS ./
        openssl pkcs12 -export \
            -out $ROOT/$CERTNAME \
            -inkey ./letsencrypt/archive/$SUBDOMAIN.ngrok.io/privkey1.pem \
            -in ./letsencrypt/archive/$SUBDOMAIN.ngrok.io/cert1.pem \
            -certfile ./letsencrypt/archive/$SUBDOMAIN.ngrok.io/chain1.pem \
            -passout pass:$CERTIFICATEPASSWORD
    fi

elif [[ "$OSTYPE" == "msys"* ]]; then

    CERTBOTDIR=C:/Certbot/live/$SUBDOMAIN.ngrok.io

    if test -f "$ROOT/$CERTNAME"; then
        echo "Certs exist, exiting"
    else
        ngrok http -host-header="$SUBDOMAIN.ngrok.io" -subdomain="$SUBDOMAIN" 80 > /dev/null &
        #wait for ngrok
        sleep 5s
        certbot certonly --config config.ini --standalone --preferred-challenges http
        openssl pkcs12 -export \
            -out $ROOT/$CERTNAME \
            -inkey $CERTBOTDIR/privkey1.pem \
            -in $CERTBOTDIR/cert1.pem \
            -certfile $CERTBOTDIR/chain1.pem \
            -passout pass:$CERTIFICATEPASSWORD
        echo "A new certificate has been created and found here: $ROOT/$CERTNAME"
    fi
else
    echo "Current OS: $OSTYPE. This operating system is not supported."
fi


