#ifndef __HFC_LIBEV_H_
#define __HFC_LIBEV_H_

#include <event.h>
#include "fcgi.h"

// Behaves similarly to printf(...), but adds file, line, and function
// information.  I omit do ... while(0) because I always use curly braces in my
// if statements.
#define INFO_OUT(...) {\
	printf("%s:%d: %s():\t", __FILE__, __LINE__, __FUNCTION__);\
	printf(__VA_ARGS__);\
}

#ifdef TRACE
    #define TRACE_OUT(...) {\
        /* printf("%s:%d: %s():\t", __FILE__, __LINE__, __FUNCTION__); */\
        printf(__VA_ARGS__);\
    }
#else
    #define TRACE_OUT(...)
#endif

// Behaves similarly to fprintf(stderr, ...), but adds file, line, and function
// information.
#define ERROR_OUT(...) {\
	fprintf(stderr, "\e[0;1m%s:%d: %s():\t", __FILE__, __LINE__, __FUNCTION__);\
	fprintf(stderr, __VA_ARGS__);\
	fprintf(stderr, "\e[0m");\
}

// Behaves similarly to perror(...), but supports printf formatting and prints
// file, line, and function information.
#define ERRNO_OUT(...) {\
	fprintf(stderr, "\e[0;1m%s:%d: %s():\t", __FILE__, __LINE__, __FUNCTION__);\
	fprintf(stderr, __VA_ARGS__);\
	fprintf(stderr, ": %d (%s)\e[0m\n", errno, strerror(errno));\
}

typedef enum {HEADER, BODY, PADDING} FCGI_State;

typedef struct cmdsocket {
	// The file descriptor for this client's socket
	int fd;

	// Whether this socket has been shut down
	int shutdown;

	// The client's socket address
	struct sockaddr_storage addr;

	// The server's event loop
	struct event_base *evloop;

	// The client's buffered I/O event
	struct bufferevent *buf_event;

	// The client's output buffer (commands should write to this buffer,
	// which is flushed at the end of each command processing loop)
	struct evbuffer *buffer;

	// Doubly-linked list (so removal is fast) for cleaning up at shutdown
	struct cmdsocket *prev, *next;

	//FCGI header and body
	FCGI_Header header;
    guint8* body;

    //current parsing state
    FCGI_State state;
    size_t bytes_read;
} cmdsocket;

typedef struct connect_arg {
    sa_family_t family;
    struct event_base *evloop;
} connect_arg;

cmdsocket *
find_cmdsocket(int fd);

void
flush_cmdsocket(cmdsocket *cmdsocket);


#endif
