[![Flattr this git repo](http://api.flattr.com/button/flattr-badge-large.png)](https://flattr.com/submit/auto?user_id=xplicit-ru&url=https://github.com/xplicit/HyperFastCgi&title=HyperFastCgi&language=&tags=github&category=software)
# HyperFastCgi

Performant nginx to mono fastcgi server  

## Installation

Download the source and perform commands:

    ./autogen.sh --prefix=/usr
     make
     sudo make install

## Run

    mono-server-hyperfastcgi4 /config=<configfile> [arguments]

### Arguments

`/config=<configfile>` Path to configuration file, which holds general settings like listener configuration (Managed or Native, their protocol, address and port), Application Host configuration (AspNet or Custom). The samples of config files can be found in `./samples` directory. This option is required and was introduced in HyperFastCgi v0.4

Most of the arguments are the same as in mono-server-fastcgi. Some additional arguments were added

`/minthreads=[nw,nio]` Sets the minimum number of threads in threadpool.  nw - number of working threads. nio - number of IO threads

`/maxthreads=[nw,nio]` Sets the maximum number of threads in threadpool.  nw - number of working threads. nio - number of IO threads

`/usethreadpool=[true|false]` Use or not use threadpool for processing requests. Default value is 'true'

`/keepalive=[true|false]` Sets the keepalive feature. Default value is 'true'. Obsolete in v0.4

`/addtrailingslash=[true|false]` Adds trailing slash if path to directory does not end with '/'. Default is 'false'. This option were added for compatibility with mono-fastcgi-server. For performance reasons it's recommended to use nginx 'rewrite' command instead, i. e.
    rewrite ^([^.]*[^/])$ $1/ permanent;

### Nginx configuration

See the [wiki page for examples of how to configure Nginx](https://github.com/xplicit/HyperFastCgi/wiki/Nginx-configuration)

## Additional Info

For more information read the blog
http://forcedtoadmin.blogspot.com
