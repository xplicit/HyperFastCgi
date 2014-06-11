#include <glib.h>
#include <string.h>
#include <mono/metadata/object.h>
#include "libev.h"
#include "host-list.h"

//TODO: add pthread rw-lock mutex to apps array
GArray *apps = NULL;
HostInfo lastApp;

static gboolean
match_host (HostInfo *host, gchar *vhost, int vport, gchar *vpath);

static void
dump_hosts();

void
register_host (MonoObject* host, MonoString *virtual_host, int virtual_port, MonoString *virtual_path, MonoString *path)
{
    if (!apps) {
        apps = g_array_new (FALSE, FALSE, sizeof(HostInfo));
    }

    HostInfo app;
//    MONO_OBJECT_SETREF(&app,host,host);
    //app.host = host;
    //mono_gc_wbarrier_generic_store(&app.host,host);
    app.host_gc_handle = mono_gchandle_new(host,TRUE);
    app.host = mono_gchandle_get_target(app.host_gc_handle);
    app.vhost = mono_string_to_utf8(virtual_host);
    app.vport = virtual_port;
    app.vpath = mono_string_to_utf8(virtual_path);
    app.path = mono_string_to_utf8(path);
    lastApp = app;

    g_array_append_val(apps,app);

    INFO_OUT("%s:%i:%s:%s host=%p pinned_host=%p domain=%p\n", app.vhost, app.vport, app.vpath, app.path, host, app.host, mono_object_get_domain(host));
//    dump_hosts();
}

void
unregister_host (MonoObject* host, MonoString *virtual_host, int virtual_port, MonoString *virtual_path)
{
    gchar *vhost = mono_string_to_utf8(virtual_host);
    gchar *vpath = mono_string_to_utf8(virtual_path);
    HostInfo app;
    int i;

    for(i = apps->len - 1; i >= 0; i--) {
        app = g_array_index(apps, HostInfo, i);
        if (strcmp(app.vhost, vhost) == 0
            && strcmp(app.vpath, vpath) == 0
            && app.vport == virtual_port) {
                INFO_OUT("%s:%i:%s:%s\n", app.vhost, app.vport, app.vpath, app.path);
                g_array_remove_index(apps, i);
                g_free(app.vhost);
                g_free(app.vpath);
                g_free(app.path);
                mono_gchandle_free(app.host_gc_handle);
            }
    }
    g_free (vhost);
    g_free (vpath);

//    dump_hosts();
}

HostInfo *
find_host_by_path (gchar* vhost, int vport, gchar* vpath)
{
    int i;

    if (!apps)
        return NULL;

    if (apps->len == 1)
        return &g_array_index(apps,HostInfo,0);

    for (i = 0; i < apps->len; i++) {
        if (match_host(&g_array_index(apps, HostInfo, i), vhost, vport, vpath))
            return &g_array_index(apps, HostInfo, i);
    }

    return NULL;
}

static gboolean
match_host (HostInfo *host, gchar *vhost, int vport, gchar *vpath)
{
    //check matching the port.
    if (host->vport != PORTS_ALL && host->vport != vport)
        return FALSE;

    //check matching host. Simple wildcards "*" for all hosts
    if (strcmp(host->vhost,"*") != 0 &&
        strcmp(host->vhost, vhost) != 0)
        return FALSE;

    //final check, that vpath is prefixed by host->vpath
    int vlen = strlen(host->vpath);
    if (host->vpath[vlen-1]=='/') vlen--;

    return strncmp(host->vpath, vpath, vlen) == 0;
}

static void
dump_hosts()
{
    HostInfo *app;
    int i;

    INFO_OUT("Applications:\n")
    for(i=0; i < apps->len; i++) {
        app = &g_array_index(apps, HostInfo, i);
        INFO_OUT("%s:%i:%s:%s %p\n", app->vhost, app->vport, app->vpath, app->path, app->host);
    }

}

