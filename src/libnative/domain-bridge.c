#include <glib.h>
#include <memory.h>
#include <mono/metadata/object.h>
#include <mono/metadata/appdomain.h>
#include "libev.h"
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

HostInfo app;

void
domain_bridge_register_host (MonoObject* host, MonoString *virtual_path, MonoString *path)
{
//    MONO_OBJECT_SETREF(&app,host,host);
    //app.host = host;
    //mono_gc_wbarrier_generic_store(&app.host,host);
    app.host = mono_gchandle_get_target(mono_gchandle_new(host,TRUE));
    app.virtual_path = virtual_path;
    app.path = path;
    INFO_OUT("registering host %ls %p", mono_string_chars(virtual_path), app.host);
}

HostInfo *
domain_bridge_find_host_by_path (MonoObject *transport, MonoString* vpath)
{
    return &app;
}

void
domain_bridge_register_transport(MonoObject *thisObj, MonoReflectionType *apphost_transport_type)
{
    bridge_register_transport(apphost_transport_type, apphost_transport_methods);

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
    domain_bridge_create_request_func(host->host,requestId, request_num, &ex);
    //TODO: handle exception
    mono_domain_set(current,FALSE);
    if (ex)
        ERROR_OUT("exception! %s","add_server_variable");
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



void domain_bridge_register_icall ()
{
    mono_add_internal_call ("Mono.WebServer.HyperFastCgi.Transport.CombinedListenerTransport::GetRoute",domain_bridge_find_host_by_path);

    mono_add_internal_call ("Mono.WebServer.HyperFastCgi.Transport.CombinedListenerTransport::RegisterTransport",domain_bridge_register_transport);
    mono_add_internal_call ("Mono.WebServer.HyperFastCgi.Transport.CombinedListenerTransport::AppHostTransportCreateRequest",domain_bridge_create_request);
    mono_add_internal_call ("Mono.WebServer.HyperFastCgi.Transport.CombinedListenerTransport::AppHostTransportAddHeader",domain_bridge_add_header);
    mono_add_internal_call ("Mono.WebServer.HyperFastCgi.Transport.CombinedListenerTransport::AppHostTransportAddServerVariable",domain_bridge_add_server_variable);
    mono_add_internal_call ("Mono.WebServer.HyperFastCgi.Transport.CombinedListenerTransport::AppHostTransportHeadersSent",domain_bridge_headers_sent);
    mono_add_internal_call ("Mono.WebServer.HyperFastCgi.Transport.CombinedListenerTransport::AppHostTransportAddBodyPart",domain_bridge_add_body_part);
    mono_add_internal_call ("Mono.WebServer.HyperFastCgi.Transport.CombinedListenerTransport::AppHostTransportProcess",domain_bridge_process);

    mono_add_internal_call ("Mono.WebServer.HyperFastCgi.Transport.CombinedAppHostTransport::RegisterHost",domain_bridge_register_host);
    mono_add_internal_call ("Mono.WebServer.HyperFastCgi.Transport.CombinedAppHostTransport::SendOutput",domain_bridge_send_output);
    mono_add_internal_call ("Mono.WebServer.HyperFastCgi.Transport.CombinedAppHostTransport::EndRequest",domain_bridge_end_request);

}

