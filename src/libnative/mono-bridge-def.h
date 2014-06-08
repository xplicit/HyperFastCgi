#ifndef __MONO_BRIDGE_DEF_H_
#define __MONO_BRIDGE_DEF_H_
#include <glib.h>
#include <mono/metadata/object.h>

typedef struct {
    char *name;
    void *func;
} MethodCalls;


typedef void (*CreateRequestType)(MonoObject *obj, guint64 requestId, int request_num, MonoException** ex);
typedef void (*AddServerVariableType)(MonoObject *obj, guint64 requestId, int request_num,
                                        MonoString *name, MonoString *value, MonoException** ex);
typedef void (*AddHeaderType)(MonoObject *obj, guint64 requestId, int request_num,
                                        MonoString *name, MonoString *value, MonoException** ex);
typedef void (*HeadersSentType)(MonoObject *obj, guint64 requestId, int request_num, MonoException** ex);
typedef void (*AddBodyPartType)(MonoObject *obj, guint64 requestId, int request_num,
                                        MonoArray *body, MonoBoolean final, MonoException** ex);
typedef void (*ProcessType)(MonoObject *obj, guint64 requestId, int request_num,
                                        MonoException** ex);
typedef void (*SendOutputType)(MonoObject *obj, guint64 requestId, int request_num, MonoArray *byte_array, int len,
                                        MonoException** ex);

typedef void (*EndRequestType)(MonoObject *obj, guint64 requestId, int request_num, int status,
                                        MonoException** ex);

#define METHOD(methodname) struct methodname ## _intr { methodname ## Type Invoke; const char *MethodName;} methodname


typedef struct BridgeClass {
    struct vtable {

    } vt;
    METHOD(CreateRequest);
    METHOD(AddServerVariable);
    METHOD(AddHeader);
    METHOD(HeadersSent);
    METHOD(AddBodyPart);
    METHOD(Process);
} BridgeClass;

void
bridge_register_transport (MonoReflectionType *transport_type, MethodCalls *methods);


#endif
