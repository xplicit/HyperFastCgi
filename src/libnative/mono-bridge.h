#ifndef __HFC_MONO_BRIDGE_H_
#define __HFC_MONO_BRIDGE_H_

#include <glib.h>
#include <mono/metadata/object.h>
#include "mono-bridge-def.h"

void
register_transport (MonoReflectionType *transport_type);

void
register_host (MonoObject* host, MonoString *virtual_path, MonoString *path);

HostInfo *
find_host_by_path (gchar* vpath);

void
create_request (HostInfo *host, guint64 requestId, int request_num);

void
add_server_variable (HostInfo *host, guint64 requestId, int request_num, guint8 *name, int name_len, guint8 *value, int value_len);

void
add_header (HostInfo *host, guint64 requestId, int request_num, guint8 *name, int name_len, guint8 *value, int value_len);

void
headers_sent (HostInfo *host, guint64 requestId, int request_num);

void
add_body_part (HostInfo *host, guint64 requestId, int request_num, guint8 *body, int len, gboolean final);

void
process (HostInfo *host, guint64 requestId, int request_num);

void
bridge_send_output(MonoObject *transport, guint64 requestId, int request_num, MonoArray *byte_array, int len);

void
bridge_end_request(MonoObject *transport, guint64 requestId, int request_num, int status);

#endif
