#!/bin/sh
sudo cp hyperfastcgi.conf /etc/init/hyperfastcgi.conf
sudo ln -s /etc/init/hyperfastcgi.conf /etc/init.d/hyperfastcgi.conf
sudo initctl reload-configuration
