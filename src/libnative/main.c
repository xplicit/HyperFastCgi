#include <glib.h>
#include <mono/metadata/object.h>
#include <mono/metadata/metadata.h>
#include <mono/metadata/class.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/loader.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/image.h>

typedef MonoString * (*Host_GetString)(MonoObject *obj,MonoException** ex);
Host_GetString host_getString;

typedef gint32 (*GetHashCode) (MonoObject *obj);
GetHashCode func;

MonoObject* host;

MonoString *
GetString (MonoObject* obj)
{
    guint32 hash;
    MonoException* ex;
    MonoDomain* domain=mono_object_get_domain(host);
    MonoDomain* current=mono_domain_get();
    mono_domain_set(domain,FALSE);
    MonoString* ret=host_getString(host,&ex);
    mono_domain_set(current,FALSE);
    return ret;

//    switch (type)
//    {
//        case 0:
//            return mono_string_new(mono_domain_get(),"String from icall");
//        case 1:
//            //hash=func(obj);
//            //return mono_string_new(mono_domain_get(),"Wrong point");
//            return host_getString(obj,&ex);
//    }
//    return mono_string_new(mono_domain_get(),"Wrong point");
}

void SaveHost(MonoObject* obj)
{
    host=obj;
}

void
RegisterIcalls()
{
   mono_add_internal_call ("NativeCommunication.IcallClient::GetString",GetString);
   mono_add_internal_call ("NativeCommunication.Host::SaveHost",SaveHost);

   MonoImage* image=mono_image_loaded("NativeCommunication");
   MonoMethodDesc* mdesc=mono_method_desc_new ("NativeCommunication.Host:GetString", TRUE);
   MonoMethod* method=mono_method_desc_search_in_image(mdesc, image);

   host_getString=mono_method_get_unmanaged_thunk (method);

   MonoImage* image2=mono_image_loaded("mscorlib");
   MonoMethodDesc* mdesc2=mono_method_desc_new ("System.Object:GetHashCode", TRUE);
   MonoMethod* System_Object_GetHashCode_method=mono_method_desc_search_in_image(mdesc2, image2);
   func = mono_method_get_unmanaged_thunk (System_Object_GetHashCode_method);

}

