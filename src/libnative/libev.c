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
static struct cmdsocket cmd_listhead = { .next = NULL };
static struct cmdsocket * const socketlist = &cmd_listhead;
/*
static void quit_func(struct cmdsocket *cmdsocket, struct command *command, const char *params)
{
	INFO_OUT("%s %s\n", command->name, params);
	shutdown_cmdsocket(cmdsocket);
}

static void kill_func(struct cmdsocket *cmdsocket, struct command *command, const char *params)
{
	INFO_OUT("%s %s\n", command->name, params);

	INFO_OUT("Shutting down server.\n");
	if(event_base_loopexit(cmdsocket->evloop, NULL)) {
		ERROR_OUT("Error shutting down server\n");
	}

	shutdown_cmdsocket(cmdsocket);
}
*/
static void add_cmdsocket(struct cmdsocket *cmdsocket)
{
	cmdsocket->prev = socketlist;
	cmdsocket->next = socketlist->next;
	if(socketlist->next != NULL) {
		socketlist->next->prev = cmdsocket;
	}
	socketlist->next = cmdsocket;
}

cmdsocket* find_cmdsocket(int fd)
{
    cmdsocket* cur=socketlist;

    while (cur != NULL && cur->fd != fd) cur = cur->next;

    return cur;
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

static void free_cmdsocket(struct cmdsocket *cmdsocket)
{
	if(CHECK_NULL(cmdsocket)) {
		abort();
	}

	// Remove socket info from list of sockets
	if(cmdsocket->prev->next == cmdsocket) {
		cmdsocket->prev->next = cmdsocket->next;
	} else {
		ERROR_OUT("BUG: Socket list is inconsistent: cmdsocket->prev->next != cmdsocket!\n");
	}
	if(cmdsocket->next != NULL) {
		if(cmdsocket->next->prev == cmdsocket) {
			cmdsocket->next->prev = cmdsocket->prev;
		} else {
			ERROR_OUT("BUG: Socket list is inconsistent: cmdsocket->next->prev != cmdsocket!\n");
		}
	}

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

	if (evbuffer_get_length(output) == 0) {
	    //shutdown_cmdsocket(cmdsocket);
	    free_cmdsocket(cmdsocket);
	} else {
	    bufferevent_enable(cmdsocket->buf_event, EV_WRITE);
	    bufferevent_setcb(cmdsocket->buf_event, NULL, close_connection,NULL,cmdsocket);
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

static void cmd_read(struct bufferevent *buf_event, void *arg)
{
	struct cmdsocket *cmdsocket = (struct cmdsocket *)arg;
	char *cmdline;
	size_t len;
//	int i;

	// Process up to 10 commands at a time
//	for(i = 0; i < 10 && !cmdsocket->shutdown; i++) {
//		cmdline = evbuffer_readline(buf_event->input);
//		if(cmdline == NULL) {
//			// No data, or data has arrived, but no end-of-line was found
//			break;
//		}
//		len = strlen(cmdline);
//
//		INFO_OUT("Read a line of length %zd from client on fd %d: %s\n", len, cmdsocket->fd, cmdline);
//		process_command(len, cmdline, cmdsocket);
//		free(cmdline);
//	}
    cmdline = evbuffer_readline(buf_event->input);
    len = strlen(cmdline);
    process_http(len, cmdline, cmdsocket);

	// Send the results to the client
	flush_cmdsocket(cmdsocket);
	//to http
	//shutdown_cmdsocket(cmdsocket);
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

// Used only by signal handler
static struct event_base *server_loop;

static void sighandler(int signal)
{
	INFO_OUT("Received signal %d: %s.  Shutting down.\n", signal, strsignal(signal));

	if(event_base_loopexit(server_loop, NULL)) {
		ERROR_OUT("Error shutting down server\n");
	}
}



//int main(int argc, char *argv[])
int Listen(unsigned short int address_family, const char *addr, guint16 listen_port)
{
    sa_family_t family = address_family_to_sa_family(address_family);
    //const char *addr = "127.0.0.1";
	//unsigned short listen_port = 9000;

    struct event_base *evloop;
	struct event connect_event;

	struct sockaddr_storage listen_addr;
	size_t listen_addr_len;
	int listenfd;

	// Set signal handlers
	sigset_t sigset;
	sigemptyset(&sigset);
	struct sigaction siginfo = {
		.sa_handler = sighandler,
		.sa_mask = sigset,
		.sa_flags = SA_RESTART,
	};
	sigaction(SIGINT, &siginfo, NULL);
	sigaction(SIGTERM, &siginfo, NULL);

	transport_init();

	// Initialize libevent
	INFO_OUT("libevent version: %s\n", event_get_version());

    #ifdef MT
	if (evthread_use_pthreads() < 0) {
		ERROR_OUT("Error initializing multithreading in events.\n");
		return -1;
	}
	#endif

	evloop = event_base_new();
	if(CHECK_NULL(evloop)) {
		ERROR_OUT("Error initializing event loop.\n");
		return -1;
	}
	server_loop = evloop;
	INFO_OUT("libevent is using %s for events.\n", event_base_get_method(evloop));

	// Initialize socket address
	init_socket_addr(family, &listen_addr, addr, listen_port);
	listen_addr_len = get_sock_addr_len (family, &listen_addr);

	// Begin listening for connections
	listenfd = socket(family, SOCK_STREAM, 0);
	if(listenfd == -1) {
		ERRNO_OUT("Error creating listening socket");
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
	event_set(&connect_event, listenfd, EV_READ | EV_PERSIST, cmd_connect, evloop);
	event_base_set(evloop, &connect_event);
	if(event_add(&connect_event, NULL)) {
		ERROR_OUT("Error scheduling connection event on the event loop.\n");
	}


	// Start the event loop
	if(event_base_dispatch(evloop)) {
		ERROR_OUT("Error running event loop.\n");
	}

	INFO_OUT("Server is shutting down.\n");
	transport_finalize();


	// Clean up and close open connections
	while(socketlist->next != NULL) {
		free_cmdsocket(socketlist->next);
	}

	// Clean up libevent
	if(event_del(&connect_event)) {
		ERROR_OUT("Error removing connection event from the event loop.\n");
	}
	event_base_free(evloop);
	if(close(listenfd)) {
		ERRNO_OUT("Error closing listening socket");
	}

	INFO_OUT("Goodbye.\n");

	return 0;
}


