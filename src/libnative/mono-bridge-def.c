#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/reflection.h>
#include <string.h>
#include "libev.h"
#include "mono-bridge-def.h"

//TODO: add search by method_name and signature
static MonoMethod *
find_method_by_name(MonoClass *klass, gchar *method_name)
{
    MonoClass *search_klass = klass;
    MonoMethod *method;
    void *iter;

    do {
        iter = NULL;
        while ((method = mono_class_get_methods(search_klass, &iter))) {
            if (strcmp(mono_method_get_name(method), method_name) == 0) {
                return method;
            }
        }
    } while ((search_klass = mono_class_get_parent(search_klass)));

    return NULL;
}

MonoBoolean
bridge_register_transport (MonoReflectionType *transport_type, MethodCalls *methods)
{
    MonoType *type = mono_reflection_type_get_type(transport_type);
    MonoClass *klass = mono_class_from_mono_type(type);
    MonoMethod* method;

    int i=0;
    while (methods[i].name)
    {
        method = find_method_by_name(klass, methods[i].name);
        if (!method) {
            ERROR_OUT("Can't find method %s in class %s:%s\n", methods[i].name, mono_class_get_namespace(klass), mono_class_get_name(klass));
            return FALSE;
        }
        *(uintptr_t *)methods[i].func = (uintptr_t)mono_method_get_unmanaged_thunk (method);
        if (!methods[i].func) {
            ERROR_OUT("Can't create wrapper for method %s\n", mono_method_full_name(method, TRUE));
            return FALSE;
        }
        i++;
    }

    return TRUE;
}


