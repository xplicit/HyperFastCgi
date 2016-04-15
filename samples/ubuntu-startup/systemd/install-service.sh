#!/bin/sh
sudo cp hyperfastcgi.service /lib/systemd/system
sudo systemctl daemon-reload
