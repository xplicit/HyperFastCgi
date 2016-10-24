upstream fastcgi_backend {
    server 127.0.0.1:${HFCPORT};
    keepalive 80;
}
 
server {
         listen   80;
         server_name  ${SITENAME};
         access_log   /var/log/nginx/${SITENAME}.log;

         location ~ /\.  { deny all; }

         location / {
                 root ${SITELOCATION};
                 index index.html index.htm default.aspx Default.aspx;
                 #fastcgi_index index.aspx;
		 fastcgi_keep_conn on;
                 fastcgi_pass fastcgi_backend;
                 include /etc/nginx/fastcgi_params;
                 
                 fastcgi_split_path_info ^((?U).+\.as.x)(/?.+)$;
		 fastcgi_param PATH_INFO $fastcgi_path_info;
		 fastcgi_param SCRIPT_FILENAME $document_root$fastcgi_script_name;
         }

}
