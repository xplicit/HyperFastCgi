#include <sys/socket.h>
#include <sys/un.h>
#include <arpa/inet.h>
#include <stdio.h>
#include <string.h>
#ifdef __FreeBSD__
#include <netinet/in.h> //Required include for the sock_addr structs in FreeBSD
#endif
#include <unistd.h>
#include "socket-helper.h"


void init_socket_addr(sa_family_t family, struct sockaddr_storage *sock_addr, const char *addr, int port)
{
    switch(family)
    {
        case AF_INET:
            memset(sock_addr, 0, sizeof(struct sockaddr_in));
            ((struct sockaddr_in *)sock_addr)->sin_family = family;
            ((struct sockaddr_in *)sock_addr)->sin_port = htons(port);
            inet_aton(addr, &((struct sockaddr_in *)sock_addr)->sin_addr);
            break;
        case AF_INET6:
            memset(sock_addr, 0, sizeof(struct sockaddr_in6));
            ((struct sockaddr_in6 *)sock_addr)->sin6_family = family;
            ((struct sockaddr_in6 *)sock_addr)->sin6_port = htons(port);
            inet_pton(family, addr, &((struct sockaddr_in6 *)sock_addr)->sin6_addr);
            break;
        case AF_UNIX:
            ((struct sockaddr_un *)sock_addr)->sun_family = family;
            strncpy(((struct sockaddr_un *)sock_addr)->sun_path, addr, sizeof(((struct sockaddr_un *)sock_addr)->sun_path));
            break;
        default:
            //ERROR_OUT("Unsupported socket address family: %hu", family);
            break;
    }
}

size_t get_sock_addr_len(sa_family_t family, struct sockaddr_storage* sock_addr)
{
    switch(family)
    {
        case AF_INET:
            return sizeof(struct sockaddr_in);
        case AF_INET6:
            return sizeof(struct sockaddr_in6);
        case AF_UNIX:
            return SUN_LEN((struct sockaddr_un*)sock_addr);
    }

    return 0;
}

sa_family_t address_family_to_sa_family(unsigned short int address_family)
{
    switch(address_family)
    {
        case 1:
            return AF_UNIX;
        case 2:
            return AF_INET;
        case 23:
            return AF_INET6;
        default:
            return AF_UNSPEC;
    }
}

int close_listening_socket(int listenfd, sa_family_t family, struct sockaddr_storage* sock_addr)
{
    if (close(listenfd))
        return -1;

    //delete file if protocol is UNIX
    if (family == AF_UNIX)
    {
        return remove(((struct sockaddr_un*)sock_addr)->sun_path);
    }

    return 0;
}

