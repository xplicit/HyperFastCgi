#!/bin/sh

if [ -z "$USERSITE" ]; then
 USERSITE=$SITEDOMAIN
fi

if [ -z "$USERLOCATION" ]; then
 USERLOCATION=$SITELOCATION
fi

if [ -z "$USERHFCPORT" ]; then
 USERHFCPORT=$HFCPORT
fi

USERSITE=$(echo $USERSITE | sed -e 's/[\/&]/\\&/g')
USERLOCATION=$(echo $USERLOCATION | sed -e 's/[\/&]/\\&/g')
USERHFCPORT=$(echo $USERHFCPORT | sed -e 's/[\/&]/\\&/g')

cat hfc-install/config/hello-app.conf.tpl | sed -e "s/\${SITENAME}/$USERSITE/" -e "s/\${HFCPORT}/$USERHFCPORT/" -e "s/\${SITELOCATION}/$USERLOCATION/" > hfc-install/config/$USERSITE.conf
cat hfc-install/config/hfc.config.tpl | sed -e "s/\${SITENAME}/$USERSITE/" -e "s/\${HFCPORT}/$USERHFCPORT/" -e "s/\${SITELOCATION}/$USERLOCATION/" > hfc-install/config/hfc.config

#cat /home/hfc-install/config/hfc.config

echo "Configure site"
cd /home/hfc-install
cp config/$USERSITE.conf /etc/nginx/sites-available/
ln -s /etc/nginx/sites-available/$USERSITE.conf /etc/nginx/sites-enabled/
echo "Disable default nginx site"
rm /etc/nginx/sites-enabled/default     
echo "Update /etc/nginx/fastcgi_params"
sed -e "s/\(fastcgi_param[[:space:]]*SCRIPT_FILENAME\)/#\1/" -e "s/\(fastcgi_param[[:space:]]*PATH_INFO\)/#\1/" /etc/nginx/fastcgi_params > config/fastcgi_params
cp config/fastcgi_params /etc/nginx/fastcgi_params


mkdir -p /etc/hyperfastcgi
mkdir -p /var/log/hyperfastcgi
chown -R www-data:www-data /var/log/hyperfastcgi
cp config/hfc.config /etc/hyperfastcgi

cat /etc/nginx/sites-available/$USERSITE.conf
/etc/init.d/nginx restart
hyperfastcgi4 /config=/etc/hyperfastcgi/hfc.config
