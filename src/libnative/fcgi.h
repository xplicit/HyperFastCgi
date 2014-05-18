#ifndef __HFC_FCGI_H_
#define __HFC_FCGI_H_

#include <glib.h>

/*
 * Number of bytes in a FCGI_Header.  Future versions of the protocol
 * will not reduce this number.
 */
#define FCGI_HEADER_SIZE 8

/*
 * Maximal number of bytes FCGI Record can handle
 */
#define FCGI_MAX_BODY_SIZE 65535

/*
 * Maximal number of bytes FCGI Record should have
 */
#define FCGI_SUGGESTED_BODY_SIZE 65528

/*
 * Value for version component of FCGI_Header
 */
#define FCGI_VERSION_1           1

/*
 * Values for type component of FCGI_Header
 */
#define FCGI_BEGIN_REQUEST       1
#define FCGI_ABORT_REQUEST       2
#define FCGI_END_REQUEST         3
#define FCGI_PARAMS              4
#define FCGI_STDIN               5
#define FCGI_STDOUT              6
#define FCGI_STDERR              7
#define FCGI_DATA                8
#define FCGI_GET_VALUES          9
#define FCGI_GET_VALUES_RESULT  10
#define FCGI_UNKNOWN_TYPE       11
#define FCGI_MAXTYPE (FCGI_UNKNOWN_TYPE)

#pragma pack(push,1)
typedef struct {
    guint8 version;
    guint8 type;
    guint16 requestId;
    guint16 contentLength;
    guint8 paddingLength;
    guint8 reserved;
} FCGI_Header;

typedef struct {
    guint16 role;
    guint8 flags;
    guint8 reserved1;
    guint8 reserved2;
    guint8 reserved3;
    guint8 reserved4;
    guint8 reserved5;
} FCGI_BeginRequestBody;

typedef struct {
    gint32 appStatus;
    guint8 protocolStatus;
    guint8 reserved1;
    guint8 reserved2;
    guint8 reserved3;
} FCGI_EndRequestBody;
#pragma pack(pop)

/*
 * Mask for flags component of FCGI_BeginRequestBody
 */
#define FCGI_KEEP_CONN  1

/*
 * Values for role component of FCGI_BeginRequestBody
 */
#define FCGI_RESPONDER  1
#define FCGI_AUTHORIZER 2
#define FCGI_FILTER     3

/*
 * Values for protocolStatus component of FCGI_EndRequestBody
 */
#define FCGI_REQUEST_COMPLETE 0
#define FCGI_CANT_MPX_CONN    1
#define FCGI_OVERLOADED       2
#define FCGI_UNKNOWN_ROLE     3

/*
 * Variable names for FCGI_GET_VALUES / FCGI_GET_VALUES_RESULT records
 */
#define FCGI_MAX_CONNS  "FCGI_MAX_CONNS"
#define FCGI_MAX_REQS   "FCGI_MAX_REQS"
#define FCGI_MPXS_CONNS "FCGI_MPXS_CONNS"

guint16
fcgi_get_content_len (FCGI_Header* header);

guint16
fcgi_get_request_id (FCGI_Header* header);

void
fcgi_set_content_len (FCGI_Header* header, guint16 len);

void
fcgi_set_request_id (FCGI_Header* header, guint16 requestId);

void
fcgi_set_app_status (FCGI_EndRequestBody* body, gint32 app_status);

void
fcgi_header_from_bytes (FCGI_Header* header, guint8* bytes);

guint16
fcgi_get_role (FCGI_BeginRequestBody* body);

#endif /* __HFC_FCGI_H_ */
