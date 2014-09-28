/*
 * An event-driven server that handles simple commands from multiple clients.
 * If no command is received for 60 seconds, the client will be disconnected.
 *
 * Note that evbuffer_readline() is a potential source of denial of service, as
 * it does an O(n) scan for a newline character each time it is called.  One
 * solution would be checking the length of the buffer and dropping the
 * connection if the buffer exceeds some limit (dropping the data is less
 * desirable, as the client is clearly not speaking our protocol anyway).
 * Another (more ideal) solution would be starting the newline search at the
 * end of the existing buffer.  The server won't crash with really long lines
 * within the limits of system RAM (tested using lines up to 1GB in length), it
 * just runs slowly.
 *
 * Created Dec. 19-21, 2010 while learning to use libevent 1.4.
 * (C)2010 Mike Bourgeous, licensed under 2-clause BSD
 * Contact: mike on nitrogenlogic (it's a dot com domain)
 *
 * References used:
 * Socket code from previous personal projects
 * http://monkey.org/~provos/libevent/doxygen-1.4.10/
 * http://tupleserver.googlecode.com/svn-history/r7/trunk/tupleserver.c
 * http://abhinavsingh.com/blog/2009/12/how-to-build-a-custom-static-file-serving-http-server-using-libevent-in-c/
 * http://www.wangafu.net/~nickm/libevent-book/Ref6_bufferevent.html
 * http://publib.boulder.ibm.com/infocenter/iseries/v5r3/index.jsp?topic=%2Frzab6%2Frzab6xacceptboth.htm
 *
 * Useful commands for testing:
 * valgrind --leak-check=full --show-reachable=yes --track-fds=yes --track-origins=yes --read-var-info=yes ./cliserver
 * echo "info" | eval "$(for f in `seq 1 100`; do echo -n nc -q 10 localhost 14310 '| '; done; echo nc -q 10 localhost 14310)"
 */
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <errno.h>
#include <signal.h>
#include <unistd.h>
#include <fcntl.h>
#include <sys/types.h>
#include <sys/socket.h>
#include <event.h>
#include <event2/thread.h>
#include <glib.h>
#include "fcgi.h"
#include "fcgi-transport.h"
#include "libev.h"
#include "socket-helper.h"

#define MT


#ifdef MT
    #define BUF_OPT BEV_OPT_THREADSAFE
#else
    #define BUF_OPT 0
#endif

// Prints a message and returns 1 if o is NULL, returns 0 otherwise
#define CHECK_NULL(o) ( (o) == NULL ? ( fprintf(stderr, "\e[0;1m%s is null.\e[0m\n", #o), 1 ) : 0 )
static void shutdown_cmdsocket(struct cmdsocket *cmdsocket);

// List of open connections to be cleaned up at server shutdown
static pthread_mutex_t sockets_lock;
static GHashTable* sockets = NULL;

static void add_cmdsocket(struct cmdsocket *cmdsocket)
{
    struct cmdsocket* prev;

    pthread_mutex_lock(&sockets_lock);
    prev = g_hash_table_lookup(sockets, GINT_TO_POINTER(cmdsocket->fd));
    if (!prev) {
        g_hash_table_insert(sockets, GINT_TO_POINTER(cmdsocket->fd), cmdsocket);
    }
    pthread_mutex_unlock(&sockets_lock);

    if (prev) {
        ERROR_OUT("Trying to add existing socket %i",cmdsocket->fd);
        //TODO: close the socket (previous or new one) and free resources
        pthread_mutex_lock(&sockets_lock);
        g_hash_table_insert(sockets, GINT_TO_POINTER(cmdsocket->fd), cmdsocket);
        pthread_mutex_unlock(&sockets_lock);

    }

}

cmdsocket* find_cmdsocket(int fd)
{
    cmdsocket* ret = NULL;

    if (sockets)
    {
        pthread_mutex_lock(&sockets_lock);
        ret = g_hash_table_lookup(sockets, GINT_TO_POINTER(fd));
        pthread_mutex_unlock(&sockets_lock);
    }

    return ret;
}

static void remove_cmdsocket(struct cmdsocket *cmdsocket)
{
    gboolean res;

    pthread_mutex_lock(&sockets_lock);
    res = g_hash_table_remove(sockets, GINT_TO_POINTER(cmdsocket->fd));
    pthread_mutex_unlock(&sockets_lock);

	if (!res) {
        ERROR_OUT("Trying to remove non-existing socket %i",cmdsocket->fd);
	}
}


static struct cmdsocket *create_cmdsocket(int sockfd, struct sockaddr_storage *remote_addr, struct event_base *evloop)
{
	struct cmdsocket *cmdsocket;

	cmdsocket = calloc(1, sizeof(struct cmdsocket));
	if(cmdsocket == NULL) {
		ERRNO_OUT("Error allocating command handler info");
		close(sockfd);
		return NULL;
	}
	cmdsocket->fd = sockfd;
	cmdsocket->addr = *remote_addr;
	cmdsocket->evloop = evloop;
	cmdsocket->state = HEADER;
	cmdsocket->bytes_read = 0;
	cmdsocket->body = NULL;

	add_cmdsocket(cmdsocket);

	return cmdsocket;
}

static void free_cmdsocket_only(struct cmdsocket *cmdsocket)
{
	// Close socket and free resources
	if (cmdsocket->body != NULL) {
        g_free(cmdsocket->body);
	}
	if(cmdsocket->buf_event != NULL) {
		bufferevent_free(cmdsocket->buf_event);
	}
	if(cmdsocket->buffer != NULL) {
		evbuffer_free(cmdsocket->buffer);
	}
	if(cmdsocket->fd >= 0) {
		shutdown_cmdsocket(cmdsocket);
		if(close(cmdsocket->fd)) {
			ERRNO_OUT("Error closing connection on fd %d", cmdsocket->fd);
		}
	}
	free(cmdsocket);
}

static gboolean free_cmdsocket_from_hash_table(gpointer key, gpointer value, gpointer user_data)
{
    free_cmdsocket_only((struct cmdsocket *)value);

    return TRUE;
}

static void free_cmdsocket(struct cmdsocket *cmdsocket)
{
	if(CHECK_NULL(cmdsocket)) {
		abort();
	}

	// Remove socket info from list of sockets
	remove_cmdsocket(cmdsocket);

	free_cmdsocket_only(cmdsocket);
}

static void shutdown_cmdsocket(struct cmdsocket *cmdsocket)
{
	if(!cmdsocket->shutdown && shutdown(cmdsocket->fd, SHUT_RDWR)) {
		ERRNO_OUT("Error shutting down client connection on fd %d", cmdsocket->fd);
	}
	cmdsocket->shutdown = 1;
}

static int set_nonblock(int fd)
{
	int flags;

	flags = fcntl(fd, F_GETFL);
	if(flags == -1) {
		ERRNO_OUT("Error getting flags on fd %d", fd);
		return -1;
	}
	flags |= O_NONBLOCK;
	if(fcntl(fd, F_SETFL, flags)) {
		ERRNO_OUT("Error setting non-blocking I/O on fd %d", fd);
		return -1;
	}

	return 0;
}

static void close_connection(struct bufferevent *bev, void *ptr)
{
    //shutdown_cmdsocket(ptr);
    free_cmdsocket(ptr);
}

void
flush_cmdsocket(struct cmdsocket *cmdsocket)
{
    struct evbuffer *output;

	if(bufferevent_write_buffer(cmdsocket->buf_event, cmdsocket->buffer)) {
		ERROR_OUT("Error sending data to client on fd %d\n", cmdsocket->fd);
	}

    output=bufferevent_get_output(cmdsocket->buf_event);

    //check that all bytes were sent. If yes, free_cmdsocket closes connection and frees buffers
    //If not, set the callback on write operation completed to close_connection
	evbuffer_lock(output);
	if (evbuffer_get_length(output) == 0) {
	    //shutdown_cmdsocket(cmdsocket);
        evbuffer_unlock(output);
        free_cmdsocket(cmdsocket);
	} else {
	    bufferevent_enable(cmdsocket->buf_event, EV_WRITE);
	    bufferevent_setcb(cmdsocket->buf_event, NULL, close_connection,NULL,cmdsocket);
        evbuffer_unlock(output);
	}
}

static const char* Header="HTTP/1.1 200 OK\r\nContent-Type: text/html; charset=utf-8\r\nContent-Length: 20\r\n\r\n";

static const char* Response="<p>Hello, world!</p>";

static void process_http(size_t len, char *cmdline, struct cmdsocket *cmdsocket)
{
    evbuffer_add_printf(cmdsocket->buffer, "%s%s", Header,Response);
}

//trash for padding reading
static unsigned char trash[256];

static void fcgi_read(struct bufferevent *buf_event, void *arg)
{
	struct cmdsocket *cmdsocket = (struct cmdsocket *)arg;

    while (TRUE)
    {
        //read header
        if (cmdsocket->state == HEADER)
        {
            cmdsocket->bytes_read+=bufferevent_read(buf_event,
                                                    ((guint8 *)&cmdsocket->header) + cmdsocket->bytes_read,
                                                    FCGI_HEADER_SIZE - cmdsocket->bytes_read);
            if (cmdsocket->bytes_read == FCGI_HEADER_SIZE)
            {
                cmdsocket->bytes_read = 0;
                cmdsocket->state = BODY;
                cmdsocket->body = g_malloc(fcgi_get_content_len(&cmdsocket->header));
            }
            else {
                return;
            }
        }
        //read body
        if (cmdsocket->state == BODY)
        {
            unsigned short body_size=fcgi_get_content_len(&cmdsocket->header);

            cmdsocket->bytes_read+=bufferevent_read(buf_event,
                                                    cmdsocket->body + cmdsocket->bytes_read,
                                                    body_size - cmdsocket->bytes_read);
            //body is done
            if (cmdsocket->bytes_read == body_size)
            {
                cmdsocket->state = PADDING;
                cmdsocket->bytes_read = 0;
            }
            else {
                return;
            }
        }
        //skip padding bytes
        if (cmdsocket->state == PADDING)
        {
            if (cmdsocket->header.paddingLength > 0) {
                cmdsocket->bytes_read += bufferevent_read (buf_event,
                                                    trash,
                                                    cmdsocket->header.paddingLength - cmdsocket->bytes_read);
            }

            if (cmdsocket->bytes_read == cmdsocket->header.paddingLength)
            {
                process_record (cmdsocket->fd, &cmdsocket->header, cmdsocket->body);

                g_free(cmdsocket->body);
                cmdsocket->body = NULL;
                cmdsocket->state = HEADER;
                cmdsocket->bytes_read = 0;
            }
            else
            {
                return;
            }
        }
    }
}

static void cmd_error(struct bufferevent *buf_event, short error, void *arg)
{
	struct cmdsocket *cmdsocket = (struct cmdsocket *)arg;

	if(error & EVBUFFER_EOF) {
		INFO_OUT("Remote host disconnected from fd %d.\n", cmdsocket->fd);
		cmdsocket->shutdown = 1;
	} else if(error & EVBUFFER_TIMEOUT) {
		INFO_OUT("Remote host on fd %d timed out.\n", cmdsocket->fd);
	} else {
		ERROR_OUT("A socket error (0x%hx) occurred on fd %d.\n", error, cmdsocket->fd);
	}

	free_cmdsocket(cmdsocket);
}

static void setup_connection(int sockfd, struct sockaddr_storage *remote_addr, struct event_base *evloop)
{
	struct cmdsocket *cmdsocket;

	if(set_nonblock(sockfd)) {
		ERROR_OUT("Error setting non-blocking I/O on an incoming connection.\n");
	}

	// Copy connection info into a command handler info structure
	cmdsocket = create_cmdsocket(sockfd, remote_addr, evloop);
	if(cmdsocket == NULL) {
		close(sockfd);
		return;
	}

	// Initialize a buffered I/O event
	//cmdsocket->buf_event = bufferevent_new(sockfd, fcgi_read, NULL, cmd_error, cmdsocket);
	cmdsocket->buf_event = bufferevent_socket_new(evloop, sockfd, BUF_OPT);
	if(CHECK_NULL(cmdsocket->buf_event)) {
		ERROR_OUT("Error initializing buffered I/O event for fd %d.\n", sockfd);
		free_cmdsocket(cmdsocket);
		return;
	}
    bufferevent_setcb(cmdsocket->buf_event, fcgi_read, NULL, cmd_error, cmdsocket);

	bufferevent_base_set(evloop, cmdsocket->buf_event);
	bufferevent_settimeout(cmdsocket->buf_event, 60, 0);
	if(bufferevent_enable(cmdsocket->buf_event, EV_READ)) {
		ERROR_OUT("Error enabling buffered I/O event for fd %d.\n", sockfd);
		free_cmdsocket(cmdsocket);
		return;
	}

	// Create the outgoing data buffer
	cmdsocket->buffer = evbuffer_new();
	if(CHECK_NULL(cmdsocket->buffer)) {
		ERROR_OUT("Error creating output buffer for fd %d.\n", sockfd);
		free_cmdsocket(cmdsocket);
		return;
	}
	evbuffer_enable_locking(cmdsocket->buffer, NULL);
}

static void cmd_connect(int listenfd, short evtype, void *arg)
{
	struct sockaddr_storage remote_addr;
	socklen_t addrlen = sizeof(remote_addr);
	int sockfd;
	int i;

	if(!(evtype & EV_READ)) {
		ERROR_OUT("Unknown event type in connect callback: 0x%hx\n", evtype);
		return;
	}

	// Accept and configure incoming connections (up to 10 connections in one go)
	for(i = 0; i < 100; i++) {
		sockfd = accept(listenfd, (struct sockaddr *)&remote_addr, &addrlen);
		if(sockfd < 0) {
			if(errno != EWOULDBLOCK && errno != EAGAIN) {
				ERRNO_OUT("Error accepting an incoming connection");
			}
			break;
		}

		setup_connection(sockfd, &remote_addr, (struct event_base *)arg);
	}
}

static struct event_base *server_loop;
static struct event connect_event;
//listening socket
static int listenfd;
static sa_family_t family;
static struct sockaddr_storage listen_addr;

void Shutdown()
{
	if(event_base_loopexit(server_loop, NULL)) {
		ERROR_OUT("Error shutting down server\n");
		return;
	}

	transport_finalize();

	// Clean up and close open connections
	pthread_mutex_lock(&sockets_lock);
	g_hash_table_foreach_remove(sockets, free_cmdsocket_from_hash_table, NULL);
	g_hash_table_destroy(sockets);
	sockets = NULL;
	pthread_mutex_unlock(&sockets_lock);

    pthread_mutex_destroy(&sockets_lock);

	// Clean up libevent
	if(event_del(&connect_event)) {
		ERROR_OUT("Error removing connection event from the event loop.\n");
	}
	event_base_free(server_loop);
	if(close_listening_socket(listenfd, family, &listen_addr)) {
		ERRNO_OUT("Error closing listening socket");
	}

	INFO_OUT("Goodbye.\n");
}

void ProcessLoop()
{
    if(event_base_dispatch(server_loop)) {
		ERROR_OUT("Error running event loop.\n");
		return;
	}
}

int Listen(unsigned short int address_family, const char *addr, guint16 listen_port)
{
	size_t listen_addr_len;

    family = address_family_to_sa_family(address_family);

    if (family == AF_UNSPEC) {
        ERROR_OUT("Unknown address family: %hu\n", address_family);
        return -1;
    }

    //init socket hashtable. passing NULL to use pointer direct hashfunc and equals
    pthread_mutex_init(&sockets_lock, NULL);
    sockets = g_hash_table_new(NULL,NULL);

	transport_init();

	// Initialize libevent
	INFO_OUT("libevent version: %s\n", event_get_version());

    #ifdef MT
	if (evthread_use_pthreads() < 0) {
		ERROR_OUT("Error initializing multithreading in events.\n");
		return -1;
	}
	#endif

	server_loop = event_base_new();
	if(CHECK_NULL(server_loop)) {
		ERROR_OUT("Error initializing event loop.\n");
		return -1;
	}
	INFO_OUT("libevent is using %s for events.\n", event_base_get_method(server_loop));

	// Initialize socket address
	init_socket_addr(family, &listen_addr, addr, listen_port);
	listen_addr_len = get_sock_addr_len (family, &listen_addr);

	// Begin listening for connections
	listenfd = socket(family, SOCK_STREAM, 0);
	if(listenfd == -1) {
		ERRNO_OUT("Error creating listening socket, address_family=%hu",family);
		return -1;
	}
	int tmp_reuse = 1;
	if(setsockopt(listenfd, SOL_SOCKET, SO_REUSEADDR, &tmp_reuse, sizeof(tmp_reuse))) {
		ERRNO_OUT("Error enabling socket address reuse on listening socket");
		return -1;
	}
	if(bind(listenfd, (struct sockaddr *)&listen_addr, listen_addr_len)) {
		ERRNO_OUT("Error binding listening socket");
		return -1;
	}
	if(listen(listenfd, 8)) {
		ERRNO_OUT("Error listening to listening socket");
		return -1;
	}

	// Set socket for non-blocking I/O
	if(set_nonblock(listenfd)) {
		ERROR_OUT("Error setting listening socket to non-blocking I/O.\n");
		return -1;
	}

	// Add an event to wait for connections
	event_set(&connect_event, listenfd, EV_READ | EV_PERSIST, cmd_connect, server_loop);
	event_base_set(server_loop, &connect_event);
	if(event_add(&connect_event, NULL)) {
		ERROR_OUT("Error scheduling connection event on the event loop.\n");
	}

	return 0;
}


