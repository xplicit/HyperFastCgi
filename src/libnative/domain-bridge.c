#include <glib.h>
#include <memory.h>
#include <mono/metadata/object.h>
#include <mono/metadata/appdomain.h>
#include "libev.h"
#include "host-list.h"
#include "mono-bridge-def.h"

CreateRequestType domain_bridge_create_request_func;
AddServerVariableType domain_bridge_add_server_variable_func;
AddHeaderType domain_bridge_add_header_func;
HeadersSentType domain_bridge_headers_sent_func;
AddBodyPartType domain_bridge_add_body_part_func;
ProcessType domain_bridge_process_func;

static MethodCalls apphost_transport_methods[]={
    {"CreateRequest",&domain_bridge_create_request_func},
    {"AddHeader",&domain_bridge_add_header_func},
    {"AddServerVariable",&domain_bridge_add_server_variable_func},
    {"HeadersSent",&domain_bridge_headers_sent_func},
    {"AddBodyPart",&domain_bridge_add_body_part_func},
    {"Process",&domain_bridge_process_func},
    {NULL,NULL}
};

SendOutputType domain_bridge_send_output_func;
EndRequestType domain_bridge_end_request_func;

static MethodCalls listener_transport_methods[] = {
    {"SendOutput", &domain_bridge_send_output_func},
    {"EndRequest", &domain_bridge_end_request_func},
    {NULL,NULL}
};

MonoObject *listener_transport;

void
domain_bridge_register_apphost_transport(MonoObject *thisObj, MonoReflectionType *apphost_transport_type)
{
    bridge_register_transport(apphost_transport_type, apphost_transport_methods);
}

void
domain_bridge_register_listener_transport(MonoObject *thisObj)
{

    listener_transport = mono_gchandle_get_target(mono_gchandle_new(thisObj,TRUE));
    MonoReflectionType *listener_transport_type = mono_type_get_object(mono_object_get_domain(thisObj),
                                                        mono_class_get_type(mono_object_get_class(thisObj))
                                                        );

    bridge_register_transport(listener_transport_type, listener_transport_methods);
}

void
domain_bridge_create_request (MonoObject *transport, HostInfo *host, guint64 requestId, int request_num)
{
    MonoException* ex = NULL;
    MonoDomain* domain=mono_object_get_domain(host->host);
    MonoDomain* current=mono_domain_get();
    mono_domain_set(domain,FALSE);
//    if (mono_gchandle_get_target(host->host_gc_handle) != host->host) {
//        ERROR_OUT("target=%p host=%p", mono_gchandle_get_target(host->host_gc_handle), host->host);
//    }

    domain_bridge_create_request_func(host->host,requestId, request_num, &ex);
    //TODO: handle exception
    mono_domain_set(current,FALSE);
    if (ex)
        ERROR_OUT("exception! %s","create_request");
}

void
domain_bridge_add_server_variable (MonoObject *transport, HostInfo *host, guint64 requestId, int request_num, MonoString *name, MonoString *value)
{
    MonoException* ex = NULL;
    MonoDomain* domain=mono_object_get_domain(host->host);
    MonoDomain* current=mono_domain_get();
    mono_domain_set(domain,FALSE);

    domain_bridge_add_server_variable_func(host->host, requestId, request_num,
                            mono_string_new_utf16(domain, mono_string_chars(name), mono_string_length(name)),
                            mono_string_new_utf16(domain, mono_string_chars(value), mono_string_length(value)),
                            &ex);
    //TODO: handle exception
    mono_domain_set(current,FALSE);
    if (ex)
        ERROR_OUT("exception! %s","add_server_variable");
}

void
domain_bridge_add_header (MonoObject *transport, HostInfo *host, guint64 requestId, int request_num, MonoString *name, MonoString *value)
{
    MonoException* ex = NULL;
    MonoDomain* domain=mono_object_get_domain(host->host);
    MonoDomain* current=mono_domain_get();
    mono_domain_set(domain,FALSE);

    domain_bridge_add_header_func(host->host, requestId, request_num,
                            mono_string_new_utf16(domain, mono_string_chars(name), mono_string_length(name)),
                            mono_string_new_utf16(domain, mono_string_chars(value), mono_string_length(value)),
                            &ex);
    //TODO: handle exception
    mono_domain_set(current,FALSE);
    if (ex)
        ERROR_OUT("exception! %s","add_server_variable");

}

void
domain_bridge_headers_sent (MonoObject *transport, HostInfo *host, guint64 requestId, int request_num)
{
    MonoException* ex;
    MonoDomain* domain=mono_object_get_domain(host->host);
    MonoDomain* current=mono_domain_get();
    mono_domain_set(domain,FALSE);

    domain_bridge_headers_sent_func(host->host, requestId, request_num,
                            &ex);
    //TODO: handle exception
    mono_domain_set(current,FALSE);
}

void
domain_bridge_add_body_part (MonoObject *transport, HostInfo *host, guint64 requestId, int request_num, MonoArray *body, gboolean final)
{
    MonoException *ex;
    int len = mono_array_length(body);
    MonoArray *byte_array = NULL;
    MonoClass *byte_class = mono_get_byte_class();
    MonoDomain *domain=mono_object_get_domain(host->host);
    MonoDomain *current=mono_domain_get();
    mono_domain_set(domain,FALSE);

    if (len > 0) {
        byte_array = mono_array_new(domain, byte_class, len);
        int elem_size = mono_class_data_size(byte_class);
        void *dest_addr = mono_array_addr_with_size(byte_array, elem_size, 0);
        void *src_addr = mono_array_addr_with_size(body, elem_size, 0);
        memcpy(dest_addr,src_addr,len);
    }

    domain_bridge_add_body_part_func (host->host, requestId, request_num,
                            byte_array, final, &ex);
    //TODO: handle exception
    mono_domain_set(current,FALSE);
}

void
domain_bridge_process (MonoObject *transport, HostInfo *host, guint64 requestId, int request_num)
{
    MonoException* ex;
    MonoDomain* domain=mono_object_get_domain(host->host);
    MonoDomain* current=mono_domain_get();
    mono_domain_set(domain,FALSE);

    domain_bridge_process_func(host->host, requestId, request_num,&ex);
    //TODO: handle exception
    mono_domain_set(current,FALSE);
}

void
domain_bridge_send_output(MonoObject *apphost_transport, guint64 requestId, int request_num, MonoArray *body, int len)
{
    MonoException *ex;
    MonoArray *byte_array = NULL;
    MonoClass *byte_class = mono_get_byte_class();
    MonoDomain *domain=mono_object_get_domain(listener_transport);
    MonoDomain *current=mono_domain_get();
    mono_domain_set(domain,FALSE);

    if (len > 0) {
        byte_array = mono_array_new(domain, byte_class, len);
        int elem_size = mono_class_data_size(byte_class);
        void *dest_addr = mono_array_addr_with_size(byte_array, elem_size, 0);
        void *src_addr = mono_array_addr_with_size(body, elem_size, 0);
        memcpy(dest_addr,src_addr,len);
    }

    domain_bridge_send_output_func (listener_transport, requestId, request_num,
                            byte_array, len, &ex);
    //TODO: handle exception
    mono_domain_set(current,FALSE);
}

void
domain_bridge_end_request(MonoObject *transport, guint64 requestId, int request_num, int status)
{
    MonoException* ex = NULL;
    MonoDomain *domain=mono_object_get_domain(listener_transport);
    MonoDomain *current=mono_domain_get();

    mono_domain_set(domain,FALSE);
    domain_bridge_end_request_func(listener_transport, requestId, request_num, status, &ex);
    //TODO: handle exception
    mono_domain_set(current,FALSE);
}

HostInfo*
domain_bridge_get_route(MonoObject *transport, MonoString *virtual_host, int virtual_port, MonoString *virtual_path)
{
    HostInfo* app;
    gchar *vhost = mono_string_to_utf8(virtual_host);
    gchar *vpath = mono_string_to_utf8(virtual_path);

    app = find_host_by_path(vhost, virtual_port, vpath);

    g_free(vhost);
    g_free(vpath);

    return app;
}


void domain_bridge_register_icall ()
{
    mono_add_internal_call ("HyperFastCgi.Transports.CombinedFastCgiListenerTransport::GetRoute",domain_bridge_get_route);

    mono_add_internal_call ("HyperFastCgi.Transports.CombinedFastCgiListenerTransport::RegisterTransport",domain_bridge_register_listener_transport);
    mono_add_internal_call ("HyperFastCgi.Transports.CombinedFastCgiListenerTransport::AppHostTransportCreateRequest",domain_bridge_create_request);
    mono_add_internal_call ("HyperFastCgi.Transports.CombinedFastCgiListenerTransport::AppHostTransportAddHeader",domain_bridge_add_header);
    mono_add_internal_call ("HyperFastCgi.Transports.CombinedFastCgiListenerTransport::AppHostTransportAddServerVariable",domain_bridge_add_server_variable);
    mono_add_internal_call ("HyperFastCgi.Transports.CombinedFastCgiListenerTransport::AppHostTransportHeadersSent",domain_bridge_headers_sent);
    mono_add_internal_call ("HyperFastCgi.Transports.CombinedFastCgiListenerTransport::AppHostTransportAddBodyPart",domain_bridge_add_body_part);
    mono_add_internal_call ("HyperFastCgi.Transports.CombinedFastCgiListenerTransport::AppHostTransportProcess",domain_bridge_process);

    mono_add_internal_call ("HyperFastCgi.Transports.CombinedAppHostTransport::RegisterAppHostTransport",domain_bridge_register_apphost_transport);
    mono_add_internal_call ("HyperFastCgi.Transports.CombinedAppHostTransport::RegisterHost",register_host);
    mono_add_internal_call ("HyperFastCgi.Transports.CombinedAppHostTransport::UnregisterHost",unregister_host);
    mono_add_internal_call ("HyperFastCgi.Transports.CombinedAppHostTransport::SendOutput",domain_bridge_send_output);
    mono_add_internal_call ("HyperFastCgi.Transports.CombinedAppHostTransport::EndRequest",domain_bridge_end_request);

}

