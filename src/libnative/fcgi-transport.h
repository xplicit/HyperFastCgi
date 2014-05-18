#include <glib.h>
#include "fcgi.h"

/*
 * Makes required resources allocations
 */
void
transport_init();

/*
 * Deallocates resources
 */
void
transport_finalize();

/*
 * Processes FCGI record
 */
void
process_record(int fd, FCGI_Header *header, guint8 *body);

/*
 * Sends request output
 */
void
send_output (guint64 requestId, int request_num, guint8 *data, int len);

/*
 * Ends the request
 */
void
end_request (guint64 requestId, int request_num, int app_status, int protocol_status);


