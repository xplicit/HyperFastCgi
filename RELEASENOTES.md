# HyperFastCgi Release Notes

## Version 0.4 alpha

### New features
* Changed architecture of HyperFastCgi. Is was splitted into independent components:
Listener, Transport, ApplicationHost, WebReqest/WebResponse. Each one implements own 
interface.

* Listener moved outside from ApplicationHost AppDomain. This gives the ability to run
several web applications inside one process each on its own AppDomain. Low performance
cross-domain communication was resolved by using native transport.

* Written new native listener based on libevent. All fast-cgi parsing code totally 
rewritten using unmanaged code.

* Native listener allows to process requests in standard multithreaded manner or in single-thread 
similar to Node.js

* Independed components allow developers to write their own high-performance simple web-servers or even
implement existing middleware like OWIN.

* `keepalive` settings is not needed more. HyperFastCgi checks the incoming requests and
determines if nginx front-end using `fcgi_keep_conn on` option.