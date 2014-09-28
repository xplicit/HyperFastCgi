#ifndef __SOCKET_HELPER_H_
#define __SOCKET_HELPER_H_
#include <sys/socket.h>

/*
 * Fills sockaddr_storage structure accordingly socket family
 * @family Socket family
 * @sock_addr pointer to structure will be set up
 * @addr protocol-dependend address like '127.0.0.1' from AF_INET or '/tmp/path-to-socket' for AF_UNIX family
 * @port port number for AF_INET and AF_INET6 family
 */
void init_socket_addr(sa_family_t family, struct sockaddr_storage *sock_addr, const char *addr, int port);

/*
 * Returns size of the sockaddr_storage structure depending on the socket family type
 */
size_t get_sock_addr_len(sa_family_t family, struct sockaddr_storage* sock_addr);

/*
 * Converts .NET AddressFamily enum values to <socket.h> families
 */
sa_family_t address_family_to_sa_family(unsigned short int address_family);

/*
 * Closes listening socket
 *
 * Calls close() on listenfd and deletes the file for UNIX protocol
 *
 * Returns 0 if success, -1 if error. errno is set appropriate
 *
 * @listenfd listening socket descriptor
 * @family Socket family
 * @sock_addr pointer to listening socket structure was set to by init_socket_addr()
 */
int close_listening_socket(int listenfd, sa_family_t family, struct sockaddr_storage* sock_addr);

#endif


