# SecureWss
Testing WSS on 4-Series Processor
the C5Debugger.dll reference will need to be manually added in order to compile.

Project works from web browsers as well as TSW's now.

To get to work on TSW, you will need to download the certificate from `/user` and upload it to the TSW's you want to use it on into the `/User/Cert`directory then console into the TSW and type `certif addf {certname} root` then load your UI and it should be able to connect.
