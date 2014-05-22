AM_CFLAGS = $(GLIB_CFLAGS) $(LIBEVENT_CFLAGS) $(LIBEVENT_THREADS_CFLAGS) $(MONO_MODULE_CFLAGS)
lib_LTLIBRARIES = libhfc-native.la

libhfc_native_la_SOURCES = $(sources)
libhfc_native_la_LDFLAGS = -version-info 1:0:0 $(GLIB_LIBS) $(LIBEVENT_LIBS) $(LIBEVENT_PTHREADS_LIBS) $(MONO_MODULE_LIBS)
libhfc_native_la_CFLAGS = $(AM_CFLAGS)

sources = \
	libev.h			\
	libev.c			\
	fcgi.h			\
	fcgi.c			\
	fcgi-transport.h	\
	fcgi-transport.c	\
	socket-helper.h		\
	socket-helper.c		\
	mono-bridge.h		\
	mono-bridge.c
