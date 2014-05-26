#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/reflection.h>
#include "libev.h"
#include "mono-bridge-def.h"

void
bridge_register_transport (MonoReflectionType *transport_type, MethodCalls *methods)
{
    MonoType *type = mono_reflection_type_get_type(transport_type);
    MonoClass *klass = mono_class_from_mono_type(type);
    MonoImage* image=mono_class_get_image(klass);
    char *fullname = g_strdup_printf("%s.%s",mono_class_get_namespace(klass), mono_class_get_name(klass));
    char *method_name;
    MonoMethodDesc* mdesc;
    MonoMethod* method;

    int i=0;
    while (methods[i].name)
    {
        method_name = g_strdup_printf("%s:%s",fullname,methods[i].name);
        mdesc = mono_method_desc_new (method_name, TRUE);
        method = mono_method_desc_search_in_image(mdesc, image);
        if (!method) {
            ERROR_OUT("Can't find method %s",method_name);
        }
        *(uintptr_t *)methods[i].func = (uintptr_t)mono_method_get_unmanaged_thunk (method);
        mono_method_desc_free(mdesc);
        g_free(method_name);
        i++;
    }

    g_free (fullname);
}


