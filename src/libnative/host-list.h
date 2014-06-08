#ifndef __HOST_LIST_H_
#define __HOST_LIST_H_
#include <glib.h>
#include <mono/metadata/object.h>

#define PORTS_ALL -1

typedef struct {
    MonoObject *host;
    uint32_t host_gc_handle;
    gchar *vhost;
    int vport;
    gchar *vpath;
    gchar *path;
} HostInfo;

void
register_host (MonoObject* host, MonoString *virtual_host, int virtual_port, MonoString *virtual_path, MonoString *path);

void
unregister_host (MonoObject* host, MonoString *virtual_host, int virtual_port, MonoString *virtual_path);

HostInfo *
find_host_by_path (gchar* virtual_host, int virtual_port, gchar* virtual_path);

#endif
