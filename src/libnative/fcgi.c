#include <glib.h>
#include <string.h>
#include "fcgi.h"

guint16
fcgi_get_content_len (FCGI_Header* header)
{
    return g_ntohs (header->contentLength);
}

guint16
fcgi_get_request_id (FCGI_Header* header)
{
    return g_ntohs (header->requestId);
}

void
fcgi_set_content_len (FCGI_Header* header, guint16 len)
{
    header->contentLength = g_htons (len);
}

void
fcgi_set_request_id (FCGI_Header* header, guint16 requestId)
{
    header->requestId = g_htons (requestId);
}

void
fcgi_set_app_status (FCGI_EndRequestBody* body, gint32 app_status)
{
    body->appStatus = g_htonl (app_status);
}

guint16
fcgi_get_role (FCGI_BeginRequestBody* body)
{
    return g_ntohs (body->role);
}



void
fcgi_header_from_bytes (FCGI_Header* header, guint8* bytes)
{
    memmove (header,bytes,FCGI_HEADER_SIZE);
}
