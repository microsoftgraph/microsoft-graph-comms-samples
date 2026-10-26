# Install OpenSSL

## Download and Install OpenSSL

1. Choose the version that applies to your OS from [here](https://slproweb.com/products/Win32OpenSSL.html). As example, I chose the Win64 OpenSSL v1.1.1g MSI (not the light version).
2. Run the EXE or MSI with default settings till completion and that should take care of installing OpenSSL!

## Add OpenSSL to your PATH

Why do we want to do this? First off, it’s not a necessity, it just makes it more convenient to use OpenSSL from the command line in the directory of your choice. After the initial install, the openssl.exe is only available from the directory where it resides, namely: `C:\Program Files\OpenSSL-Win64\bin`

## Test OpenSSL Installation

Let’s verify that OpenSSL is now accessible from outside its own directory by opening a Command Prompt in an arbitrary location and executing the following command:

```cmd
openssl version
```
