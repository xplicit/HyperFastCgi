[![Flattr this git repo](http://api.flattr.com/button/flattr-badge-large.png)](https://flattr.com/submit/auto?user_id=xplicit-ru&url=https://github.com/xplicit/HyperFastCgi&title=HyperFastCgi&language=&tags=github&category=software)
# HyperFastCgi

HyperFastCgi hosts mono web applications with nginx. It's a primary replacement of mono-server-fastcgi for linux platform.

Key features:
* Does not leak memory
* Serves requests much faster. [See performance comparison](http://forcedtoadmin.blogspot.ru/2013/11/servicestack-performance-in-mono-p2.html#stat)

Latest stable version https://github.com/xplicit/HyperFastCgi/tree/v0.3_stable

## Installation

Download the source and perform commands:

    ./autogen.sh --prefix=/usr
     make
     sudo make install

## Run

    hyperfastcgi4 /config=<configfile> [arguments]

### Arguments

* `/config=<configfile>` Path to configuration file, which holds general settings like listener configuration (Managed or Native, their protocol, address and port), Application Host configuration (AspNet or Custom). The samples of config files can be found in `./samples` directory. This option is required and was introduced in HyperFastCgi v0.4

Most of the arguments are the same as in mono-server-fastcgi. Some additional arguments were added

* `/addtrailingslash=[true|false]` Adds trailing slash if path to directory does not end with '/'. Default is 'false'. This option were added for compatibility with mono-fastcgi-server. For performance reasons it's recommended to use nginx 'rewrite' command instead, i. e.
    rewrite ^([^.]*[^/])$ $1/ permanent;

### Config file parameters

Samples of config files your can find in `samples` directory.

Configuration file is an XML config, which consists of four sections `server` `listener` `apphost` `web-applications`. 

    <configuration>
    	<server>...General server settings is here...</server>
    	<listener>... Address, port and related stuff here...</listener>
    	<apphost>... Settings for application host...</apphost>
    	<web-applications>
    		<web-application>...Web App1 settings...</web-application>
    		<web-application>...Web App2 settings...</web-application>
    		....
    		<web-application>...Web AppN settings...</web-application>
    	</web-applications>
    </configuration>

Each section except of `web-application` has the attribute `type` which represents fully-qualified CLR type of the class, which will proceed the section. These types provide various behaviour described below and can be written by developer (see Developer section) 

	<server type="HyperFastCgi.ApplicationServers.SimpleApplicationServer">
		..server parameters goes here..
	</server>

All existing types are described in this manual.

#### <server> element

* `type` attribute. Currently can only be set to `HyperFastCgi.ApplicationServers.SimpleApplicationServer` or user-defined type.

* `<host-factory>` element. Type name of the factory, which creates application hosts. Factories can choose how to create application hosts, for example they can create apphost in own domain or in main application domain. Currently there are only one factory `HyperFastCgi.HostFactories.SystemWebHostFactory` which creates application host is their own domain (ASP.NET old-school style)  

* `<threads>` element. This element has four attrubutes, which defines how many threads will be created at the start
	* `min-worker` minimal number of worker threads
	* `max-worker` maximal number of worker threads
	* `min-io` minimum number of IO completion threads
	* `max-io` maximal number of IO completion threads

* `root-dir` element. Sets the root directory for the applications.

#### <listener> element

<listener> describes behaviour of how HyperFastCgi will listen and proceed incoming requests. Currently there are two listeners, which process FastCgi requests, but one can write it's own to process HTTP requests, for example. 

* `type` attribute. Fully-qualified CLR type name. There are two predefined types `HyperFastCgi.Listeners.NativeListener` and `HyperFastCgi.Listeners.ManagedFastCgiListener`.
	NativeListener is a FastCgi libevent-based listener written in unmanaged code. It provides faster request processing time and allows you to use multithreading or single-threading request processing (like Node.Js)
	ManagedFastCgiListener is a FastCgi listener written in asynchroneous sockets managed code. It works slower and can't process requests in Node.Js-like single-threading style, but if you don't want to deal with unmanaged code at all it can be a solution.

* `listener-transport` element. Transport which sends requests from listener to app host. See transports section for more details.
	`type` attribute. Fully-quilified CLR type name. There are two predefined listeners transports, which can be used with managed listener (NativeListener has it's own in native code and does not require to define listener transport)     	         	 
	    `HyperFastCgi.Transports.ManagedFastCgiListenerTransport` - listener transport was written in managed code. It uses cross-domain calls when working with `SystemWebHostFactory`. Cross-domain calls in mono are very slow, so use this transport only if you don't need good performance or want to deal with managed code only. 
	    `HyperFastCgi.Transports.CombinedFastCgiListenerTransport` - this transport uses native calls to pass data fast to another domain. Speed of calls are similar to speed of calls to the methods located in one domain.

* `apphost-transport` element. Transport which recieves requests in the application host and sends response from it to listener. 
	`type` attribute. Fully-quilified CLR type name. There are three predefined apphost transport.
		`HyperFastCgi.Transports.ManagedAppHostTransport` must be used in pair with `HyperFastCgi.Transports.ManagedFastCgiListenerTransport` for managed listener.
		`HyperFastCgi.Transports.CombinedAppHostTransport` must be used in pair with `HyperFastCgi.Transports.CombinedFastCgiListenerTransport` for managed listener.
		`HyperFastCgi.Transports.NativeTransport` must be used with NativeListener only.
	`multithreading` element. Defines how requests will be processed in multithreading. Can hold one of three values: `ThreadPool`, `Task` and `Single`. `ThreadPool` uses ThreadPool.QueueUserWorkItem method for processing requests, `Task` uses TPL, and `Single` processes requests directly. Default is `ThreadPool`

* `protocol` element. Defines which protocol will be used for opening sockets. Allowed values `InterNetwork` for IPv4, `InterNetwork6' for IPv6 and `Unix` for unix file sockets.
* `address` element. Defines the address on which will listen to. For unix-sockets it's a path to file.
* `port` element. Defines the port on which will listen to. Does not used for unix sockets. 
	     
### Nginx configuration

See the [wiki page for examples of how to configure Nginx](https://github.com/xplicit/HyperFastCgi/wiki/Nginx-configuration)

## Additional Info

For more information read the blog
http://forcedtoadmin.blogspot.com
