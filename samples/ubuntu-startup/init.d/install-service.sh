#!/bin/bash
#before run place your server config script into /ect/hyperfastcgi/hfc.config file
sudo mkdir -p /var/log/hyperfastcgi
sudo chown www-data:www-data /var/log/hyperfastcgi
sudo cp init.d/hyperfastcgi4 /etc/init.d/hyperfastcgi4
sudo update-rc.d hyperfastcgi4 start 90 2 3 4 5 . stop 20 0 1 6 .
