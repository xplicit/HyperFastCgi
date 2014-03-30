[![Flattr this git repo](http://api.flattr.com/button/flattr-badge-large.png)](https://flattr.com/submit/auto?user_id=xplicit-ru&url=https://github.com/xplicit/HyperFastCgi&title=HyperFastCgi&language=&tags=github&category=software)
HyperFastCgi
============

Performant nginx to mono fastcgi server  

Installation
------------

Download the source and perform commands:

    ./autogen.sh --prefix=/usr
     make
     sudo make install

Run
------------

    mono-server-hyperfastcgi4 <arguments>

Arguments
------------
Most of the arguments are the same as in mono-server-fastcgi. Some additional arguments were added

**/minthreads=[nw,nio]**

Sets the minimum number of threads in threadpool.  nw - number of working threads. nio - number of IO threads

**/maxthreads=[nw,nio]**

Sets the maximum number of threads in threadpool.  nw - number of working threads. nio - number of IO threads

**/usethreadpool=[true|false]**

Use or not use threadpool for processing requests. Default value is 'true'

**/keepalive=[true|false]**

Sets the keepalive feature. Default value is 'true'

Additional Info
------------

For more information read the blog
http://forcedtoadmin.blogspot.com
