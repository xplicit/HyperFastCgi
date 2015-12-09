SUBDIRS = scripts

EXTRA_DIST = $(FILES) $(RESOURCES)

# Warning: This is an automatically generated file, do not edit!

if ENABLE_DEBUG_X86
ASSEMBLY_COMPILER_COMMAND = dmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize- -debug "-define:DEBUG; -keyfile:$(top_srcdir)/src/mono.snk"
ASSEMBLY = bin/Debug/HyperFastCgi.exe
ASSEMBLY_MDB = $(ASSEMBLY).mdb
COMPILE_TARGET = exe
PROJECT_REFERENCES = 
BUILD_DIR = bin/Debug

MONO_WEBSERVER_HYPERFASTCGI_EXE_MDB_SOURCE=bin/Debug/HyperFastCgi.exe.mdb
MONO_WEBSERVER_HYPERFASTCGI_EXE_MDB=$(BUILD_DIR)/HyperFastCgi.exe.mdb

endif

if ENABLE_RELEASE_X86
ASSEMBLY_COMPILER_COMMAND = dmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize+ -keyfile:$(top_srcdir)/src/mono.snk
ASSEMBLY = bin/Release/HyperFastCgi.exe
ASSEMBLY_MDB = 
COMPILE_TARGET = exe
PROJECT_REFERENCES = 
BUILD_DIR = bin/Release

MONO_WEBSERVER_HYPERFASTCGI_EXE_MDB=

endif

if NET_2_0
ASSEMBLY2 = 2.0/$(ASSEMBLY)
GACUTIL2 = $(GACUTIL) -package 2.0
endif
if NET_4_0
ASSEMBLY4 = 4.0/$(ASSEMBLY)
GACUTIL4 = $(GACUTIL) -package 4.5
endif

if NET_2_0
MONO_WEBSERVER_HYPERFASTCGI2  = 2.0/$(BUILD_DIR)/mono-webserver-hyperfastcgi2
endif

if NET_4_0
MONO_WEBSERVER_HYPERFASTCGI4  = 4.0/$(BUILD_DIR)/mono-webserver-hyperfastcgi4
endif

AL=al
SATELLITE_ASSEMBLY_NAME=$(notdir $(basename $(ASSEMBLY))).resources.dll

PROGRAMFILES = \
	$(MONO_WEBSERVER_HYPERFASTCGI_EXE_MDB)  

#BINARIES = \
#	$(MONO_WEBSERVER_HYPERFASTCGI2) \
#	$(MONO_WEBSERVER_HYPERFASTCGI4)  

RESGEN=resgen2
	
all: $(ASSEMBLY2) $(ASSEMBLY4) $(PROGRAMFILES) $(BINARIES) 

FILES =  \
	AppHosts/AppHostBase.cs \
	AppHosts/AspNet/AspNetApplicationHost.cs \
	AppHosts/AspNet/AspNetHostConfig.cs \
	AppHosts/AspNet/AspNetNativeWebRequest.cs \
	AppHosts/AspNet/MonoWorkerRequest.cs \
	AppHosts/Raw/BaseRawRequest.cs \
	AppHosts/Raw/HelloWorldRequest.cs \
	AppHosts/Raw/RawHost.cs \
	AppHosts/Raw/RawHostConfig.cs \
	ApplicationServers/HostInfo.cs \
	ApplicationServers/SimpleApplicationServer.cs \
	Configuration/AppHostConfig.cs \
	Configuration/AppServerConfig.cs \
	Configuration/ConfigAttribute.cs \
	Configuration/ConfigInfo.cs \
	Configuration/ConfigurationManager.cs \
	Configuration/ConfigUtils.cs \
	Configuration/ListenerConfig.cs \
	Configuration/LogConfig.cs \
	Configuration/MultiThreadingOption.cs \
	Configuration/ThreadPoolConfig.cs \
	Configuration/TransportConfig.cs \
	Configuration/WebAppConfig.cs \
	Helpers/FastCgiProtocol/BeginRequestBody.cs \
	Helpers/FastCgiProtocol/EndRequestBody.cs \
	Helpers/FastCgiProtocol/FcgiUtils.cs \
	Helpers/FastCgiProtocol/Record.cs \
	Helpers/FastCgiProtocol/UnknownTypeBody.cs \
	Helpers/Logging/Logger.cs \
	Helpers/Logging/LogLevel.cs \
	Helpers/Mono.WebServer/Events/DomainReloadEventArgs.cs \
	Helpers/Mono.WebServer/Events/MapPathEventArgs.cs \
	Helpers/Mono.WebServer/Events/MapPathEventHandler.cs \
	Helpers/Mono.WebServer/HttpErrors.cs \
	Helpers/Mono.WebServer/VPathToHost.cs \
	Helpers/Sockets/GeneralSocket.cs \
	Helpers/Sockets/GeneralSocketType.cs \
	Helpers/Sockets/TcpSocket.cs \
	Helpers/Sockets/UnixSocket.cs \
	Helpers/Strings.cs \
	Helpers/TestResponse.cs \
	Helpers/ThreadHelper.cs \
	HostFactories/SystemWebHostFactory.cs \
	Interfaces/Events/HostUnloadEventArgs.cs \
	Interfaces/IApplicationHost.cs \
	Interfaces/IApplicationHostFactory.cs \
	Interfaces/IApplicationHostTransport.cs \
	Interfaces/IApplicationServer.cs \
	Interfaces/IListenerTransport.cs \
	Interfaces/IWebListener.cs \
	Interfaces/IWebRequest.cs \
	Interfaces/IWebResponse.cs \
	Listeners/FastCgiNetworkConnector.cs \
	Listeners/ManagedFastCgiListener.cs \
	Listeners/NativeListener.cs \
	Main.cs \
	Properties/AssemblyInfo.cs \
	Transports/BaseAppHostTransport.cs \
	Transports/BaseManagedListenerTransport.cs \
	Transports/CombinedAppHostTransport.cs \
	Transports/CombinedFastCgiListenerTransport.cs \
	Transports/KeyValuePair.cs \
	Transports/ManagedAppHostTransport.cs \
	Transports/ManagedFastCgiListenerTransport.cs \
	Transports/NativeTransport.cs \
	Transports/TransportRequest.cs 

DATA_FILES = 

RESOURCES = Configuration/ConfigurationManager.xml 

EXTRAS = \
	FastCgiProtocol \
	Sockets \
	Config \
	Logging \
	hyperfastcgi.in 

REFERENCES =  \
	Mono.Posix \
	System \
	System.Configuration \
	System.Core \
	System.Web \
	System.Xml

DLL_REFERENCES = 

CLEANFILES = $(PROGRAMFILES) $(BINARIES) 

#include $(top_srcdir)/Makefile.include
# $(call emit-deploy-wrapper,wrapper-variable-name,wrapper-sourcefile,x)
# assumes that for a wrapper foo.pc its source template is foo.pc.in
# if $3 is non-empty then wrapper is marked exec
define emit-deploy-wrapper
$($1): $2
	mkdir -p '$$(shell dirname '$$@')'
	cp '$$<' '$$@'
	$(if $3,chmod +x '$$@')
endef

if NET_4_0
programfiles4dir=$(pkglibdir)/4.0
programfiles4_DATA=$(ASSEMBLY4)
endif
if NET_2_0
programfiles2dir=$(pkglibdir)/2.0
programfiles2_DATA=$(ASSEMBLY2)
endif

$(eval $(call emit_resgen_targets))
$(build_xamlg_list): %.xaml.g.cs: %.xaml
	xamlg '$<'

s2q=$(subst \ ,?,$1)
q2s=$(subst ?,\ ,$1)

build_sources= $(FILES)

build_resources= $(RESOURCES)

references2=$(REFERENCES)

references4=$(REFERENCES)

build_references_ref2 = $(call q2s,$(foreach ref, $(call s2q,$(references2)), $(if $(filter -pkg:%, $(ref)), $(ref), $(if $(filter -r:%, $(ref)), $(ref), -r:$(ref)))))
build_references_ref2 += $(call q2s,$(foreach ref, $(call s2q,$(DLL_REFERENCES)), -r:$(ref)))
build_references_ref2 += $(call q2s,$(foreach ref, $(call s2q,$(PROJECT_REFERENCES)), -r:$(ref)))

build_references_ref4 = $(call q2s,$(foreach ref, $(call s2q,$(references4)), $(if $(filter -pkg:%, $(ref)), $(ref), $(if $(filter -r:%, $(ref)), $(ref), -r:$(ref)))))
build_references_ref4 += $(call q2s,$(foreach ref, $(call s2q,$(DLL_REFERENCES)), -r:$(ref)))
build_references_ref4 += $(call q2s,$(foreach ref, $(call s2q,$(PROJECT_REFERENCES)), -r:$(ref)))


$(ASSEMBLY_MDB): $(ASSEMBLY)

if NET_2_0
$(ASSEMBLY2): $(build_sources) $(build_resources) $(build_datafiles) $(DLL_REFERENCES) $(PROJECT_REFERENCES) $(build_xamlg_list) $(build_satellite_assembly_list)
	$(MKDIR_P) $(shell dirname 2.0/$(ASSEMBLY))
	$(GMCS) -d:NET_2_0 $(ASSEMBLY_COMPILER_FLAGS) -out:$(ASSEMBLY2) -target:$(COMPILE_TARGET) $(build_sources) $(build_resources:%=-resource:%) $(build_references_ref4)
	$(SN) -q -R $(builddir)/$@ $(srcdir)/../mono.snk
endif

if NET_4_0
$(ASSEMBLY4): $(build_sources) $(build_resources) $(build_datafiles) $(DLL_REFERENCES) $(PROJECT_REFERENCES) $(build_xamlg_list) $(build_satellite_assembly_list)
	$(MKDIR_P) $(shell dirname 4.0/$(ASSEMBLY))
	$(DMCS) $(ASSEMBLY_COMPILER_FLAGS) -out:$(ASSEMBLY4) -target:$(COMPILE_TARGET) $(build_sources) $(build_resources:%=-resource:%) $(build_references_ref4)
	$(SN) -q -R $(builddir)/$@ $(srcdir)/../mono.snk
endif


install-data-local:
if NET_2_0
	$(GACUTIL2) $(GACUTIL_FLAGS) -i $(ASSEMBLY2)
endif
if NET_4_0
	$(GACUTIL4) $(GACUTIL_FLAGS) -i $(ASSEMBLY4)
endif

uninstall-local:
	-for i in HyperFastCgi ; do \
		$(GACUTIL) $(GACUTIL_FLAGS) -u $$(basename $$i .exe) ; \
	done

clean-local:
	rm -rf 2.0 4.0
