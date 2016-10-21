[![Build Status](https://travis-ci.org/xplicit/HyperFastCgi.svg?branch=master)](https://travis-ci.org/xplicit/HyperFastCgi)
# HyperFastCgi

HyperFastCgi hosts mono web applications with nginx. It's a primary replacement of mono-server-fastcgi for linux platform.

Key features:
* Does not leak memory
* Serves requests much faster. [See performance comparison](http://forcedtoadmin.blogspot.ru/2013/11/servicestack-performance-in-mono-p2.html#stat)
* Open architecture allows developers to write their own extensions or replacements of HyperFastCgi components

Latest stable version https://github.com/xplicit/HyperFastCgi/tree/v0.3_stable

## Installation

Prerequisites:

     sudo apt-get install autoconf automake libtool make libglib2.0-dev libevent-dev

For Debian 8 you additionally need to install `libtool-bin` package

Download the source and perform commands:

    ./autogen.sh --prefix=/usr
     make
     sudo make install

## Installation on FreeBSD

Prerequisites:

     pkg install mono autoconf automake libtool gmake libevent2 pkgconf
     
If you want to use the buildin clang instead of gcc you can make a symlink to fool autoconf:
     
     ln -s /usr/bin/cc /usr/bin/gcc

Download the source and perform commands:

    ./autogen.sh --prefix=/usr/local
     gmake
     gmake install

## Run

    hyperfastcgi4 /config=<configfile> [arguments]

### Arguments

* `/config=<configfile>` Path to configuration XML file, which holds general settings like listener configuration (Managed or Native, their protocol, address and port), Application Host configuration (AspNet or Custom). The samples of config files can be found in `./samples` directory. This option is required and was introduced in HyperFastCgi v0.4

* `/applications` Adds applications from a comma separated list of virtual and physical directory pairs. The pairs are separated by colons and optionally include the virtual host name and port to use: `[hostname:[port:]]VPath:realpath,...`

* `/appconfigfile` Adds application definitions from an XML configuration file, typically with the ".webapp" extension. See sample configuration file that comes with the server.

* `/appconfigdir` Adds application definitions from all XML files found in the specified directory DIR. Files must have the ".webapp" extension.

* `/logfile` Specifies a file to log events to.

* `/loglevels` Specifies what log levels to log. It can be any of the following values, or multiple if comma separated:
			`Debug`, `Notice`, `Warning`, `Error`, `Standard` (Notice,Warning,Error), `All` (Debug,Standard)

* `/printlog` Prints log messages to the console.

* `/stopable` Allows the user to stop the server by if "Enter" is pressed. This should not be used when the server has no controlling terminal

* `/version` Displays version information and exits.

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

#### `<server>` element

* `type` attribute. Currently can only be set to `HyperFastCgi.ApplicationServers.SimpleApplicationServer` or user-defined type.

* `<host-factory>` element. Type name of the factory, which creates application hosts. Factories can choose how to create application hosts, for example they can create apphost in own domain or in main application domain. Currently there are only one factory `HyperFastCgi.HostFactories.SystemWebHostFactory` which creates application host is their own domain (ASP.NET old-school style)  

* `<threads>` element. This element has four attrubutes, which defines how many threads will be created at the start
	* `min-worker` minimal number of worker threads
	* `max-worker` maximal number of worker threads
	* `min-io` minimum number of IO completion threads
	* `max-io` maximal number of IO completion threads

* `<root-dir>` element. Sets the root directory for the applications.

        <server type="HyperFastCgi.ApplicationServers.SimpleApplicationServer">
            <!-- Host factory defines how host will be created. SystemWebHostFactory creates host in AppDomain in standard ASP.NET way -->
            <host-factory>HyperFastCgi.HostFactories.SystemWebHostFactory</host-factory>
            <!-- <threads> creates threads at startup. Value "0" means default value --> 
            <threads min-worker="40" max-worker="0" min-io="4" max-io="0" />
            <!--- Sets the application host root directory -->
            <!-- <root-dir>/path/to/your/dir</root-dir> -->
        </server>

#### `<listener>` element

`<listener>` describes behaviour of how HyperFastCgi will listen and proceed incoming requests. Currently there are two listeners, which process FastCgi requests, but one can write it's own to process HTTP requests, for example. 

* `type` attribute. Fully-qualified CLR type name. There are two predefined types `HyperFastCgi.Listeners.NativeListener` and `HyperFastCgi.Listeners.ManagedFastCgiListener`.
	* NativeListener is a FastCgi libevent-based listener written in unmanaged code. It provides faster request processing time and allows you to use multithreading or single-threading request processing (like Node.Js)
	* ManagedFastCgiListener is a FastCgi listener written in asynchroneous sockets managed code. It works slower and can't process requests in Node.Js-like single-threading style, but if you don't want to deal with unmanaged code at all it can be a solution.

* `<listener-transport>` element. Transport which sends requests from listener to app host. See transports section for more details.
	* `type` attribute. Fully-quilified CLR type name. There are two predefined listeners transports, which can be used with managed listener (NativeListener has it's own in native code and does not require to define listener transport)     	         	 
	   * `HyperFastCgi.Transports.ManagedFastCgiListenerTransport` - listener transport was written in managed code. It uses cross-domain calls when working with `SystemWebHostFactory`. Cross-domain calls in mono are very slow, so use this transport only if you don't need good performance or want to deal with managed code only. 
	   * `HyperFastCgi.Transports.CombinedFastCgiListenerTransport` - this transport uses native calls to pass data fast to another domain. Speed of calls are similar to speed of calls to the methods located in one domain.

* `<apphost-transport>` element. Transport which recieves requests in the application host and sends response from it to listener. 
	* `type` attribute. Fully-quilified CLR type name. There are three predefined apphost transport.
		* `HyperFastCgi.Transports.ManagedAppHostTransport` must be used in pair with `HyperFastCgi.Transports.ManagedFastCgiListenerTransport` for managed listener.
		* `HyperFastCgi.Transports.CombinedAppHostTransport` must be used in pair with `HyperFastCgi.Transports.CombinedFastCgiListenerTransport` for managed listener.
		* `HyperFastCgi.Transports.NativeTransport` must be used with NativeListener only.
	* `<multithreading>` element. Defines how requests will be processed in multithreading. Can hold one of three values: `ThreadPool`, `Task` and `Single`. `ThreadPool` uses ThreadPool.QueueUserWorkItem method for processing requests, `Task` uses TPL, and `Single` processes requests directly. Default is `ThreadPool`

* `<protocol>` element. Defines which protocol will be used for opening sockets. Allowed values `InterNetwork` for IPv4, `InterNetwork6` for IPv6 and `Unix` for unix file sockets.
* `<address>` element. Defines the address on which will listen to. For unix-sockets it's a path to file.
* `<port>` element. Defines the port on which will listen to. Is not used for unix sockets.

        <listener type="HyperFastCgi.Listeners.ManagedFastCgiListener">
            <listener-transport type="HyperFastCgi.Transports.CombinedFastCgiListenerTransport" />
            <apphost-transport type="HyperFastCgi.Transports.CombinedAppHostTransport" />
            <protocol>InterNetwork</protocol>
            <address>127.0.0.1</address>
            <port>9000</port>
        </listener>
        
#### `<apphost>` element.

`<apphost>` defines how requests in web application will be processed. HyperFastCgi has two apphosts: AspNet for running ASP.NET applications and Raw for directly working with HTTP request data.

* `type` attribute. Fully-qualified CLR type name. There are two types: 
	* `HyperFastCgi.AppHosts.AspNet.AspNetApplicationHost` hosts standard ASP.NET sites using System.Web. 
	* `HyperFastCgi.AppHosts.Raw.RawHost` provides methods for working with raw request - headers and data. It much faster than ASP.NET host (~2.5x-3.5x) but requires low-level manipulating request data. How to write web-application which works with RawHost see in the "Writing RawHost Application" chapter
* `<log>` element. 
		* `level` attribute. Defines log level in AppHost. There are `Error`, `Debug`, `Standard`, `All` values.
		* `write-to-console` attribute. Defines when logger must write debug info to console. Allowed values are `true` and `false`
* `<add-trailing-slash>` element. Allowed values `true` or `false`. Adds trailing slash if path to directory does not end with '/'. Default is 'false'. This option were added for compatibility with mono-fastcgi-server. For performance reasons it's recommended to use nginx 'rewrite' command instead, i. e. `rewrite ^([^.]*[^/])$ $1/ permanent;`
* `<request-type>` element. Used only by RawHost. User-defined fully-qualified CLR type name which will be used for processing requests. See "Writing RawHost Application" chapter. 

        <apphost type="HyperFastCgi.AppHosts.AspNet.AspNetApplicationHost">
          <log level="Debug" write-to-console="true" />
          <add-trailing-slash>false</add-trailing-slash>
        </apphost>

#### `<web-applications>` element.

`<web-applications>` represents collection of `<web-application>` elements each of them defines web application will be hosted by the server.

`<web-application>` element.
* `<name>` element. Provides name of the web application. Is not used yet.
* `<vhost>` element. Host name of virtual host. For example: www.myserver.com
* `<vport>` element. Port of virtual host.
* `<vpath>` element. Virtual path to the host. Generally `/`
* `<path>` element. Physical path to the host files location. For example, `/var/www/myserver`
	     
### Nginx configuration

See the [wiki page for examples of how to configure Nginx](https://github.com/xplicit/HyperFastCgi/wiki/Nginx-configuration)

## Writing RawHost Application

HyperFastCgi allows your to write fast web-request processing routines using C#. To do it you should do the following steps

1. Create new C# library project and add HyperFastCgi as reference to the project.
2. Create your own class derived from the class HyperFastCgi.AppHosts.Raw.BaseRawRequest.
3. Override method Process and write here your own logic. See the sample

        public class HelloWorldRequest : BaseRawRequest
        {
            public override void Process(IWebResponse response)
            {
                Status = 200;
                StatusDescription = "OK";
                ResponseHeaders.Add("Content-Type","text/html; charset=utf-8");
                response.Send(Encoding.ASCII.GetBytes("Hello, World!"));
                response.CompleteResponse ();
            }
        }

4. Get the `samples/hello-world.config` and replace `<request-type>` element value with the type name of your class. You should get something like this 

        <request-type>YourNameSpace.HelloWorldRequest, YourAssemblyName</request-type> 

5. You're possible have to place your assembly into the GAC or put it under `bin` folder of your web-application, otherwise web-server won't find it. If your web application is located under `/var/www/yourapp` you should place the assembly to `/var/www/yourapp/bin` 

## Package Build Recipe Using [FPM-Cookery](https://github.com/bernd/fpm-cookery)

Here is a fpm build recipe for HyperFastCgi

https://github.com/sepulworld/fpm-hyperfastcgi

## Additional Info

For more information read the blog
http://forcedtoadmin.blogspot.com
