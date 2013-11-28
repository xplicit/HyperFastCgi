
EXTRA_DIST =  

# Warning: This is an automatically generated file, do not edit!

if ENABLE_DEBUG_X86
ASSEMBLY_COMPILER_COMMAND = dmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize- -debug "-define:DEBUG;"
ASSEMBLY = bin/Debug/Mono.WebServer.HyperFastCgi.exe
ASSEMBLY_MDB = $(ASSEMBLY).mdb
COMPILE_TARGET = exe
PROJECT_REFERENCES = 
BUILD_DIR = bin/Debug

MONO_WEBSERVER_HYPERFASTCGI_EXE_MDB_SOURCE=bin/Debug/Mono.WebServer.HyperFastCgi.exe.mdb
MONO_WEBSERVER_HYPERFASTCGI_EXE_MDB=$(BUILD_DIR)/Mono.WebServer.HyperFastCgi.exe.mdb

endif

if ENABLE_RELEASE_X86
ASSEMBLY_COMPILER_COMMAND = dmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize+
ASSEMBLY = bin/Release/Mono.WebServer.HyperFastCgi.exe
ASSEMBLY_MDB = 
COMPILE_TARGET = exe
PROJECT_REFERENCES = 
BUILD_DIR = bin/Release

MONO_WEBSERVER_HYPERFASTCGI_EXE_MDB=

endif

AL=al
SATELLITE_ASSEMBLY_NAME=$(notdir $(basename $(ASSEMBLY))).resources.dll

PROGRAMFILES = \
	$(MONO_WEBSERVER_HYPERFASTCGI_EXE_MDB)  

BINARIES = \
	$(MONO_WEBSERVER_HYPERFASTCGI)  


RESGEN=resgen2
	
all: $(ASSEMBLY) $(PROGRAMFILES) $(BINARIES) 

FILES = \
	Program.cs \
	Properties/AssemblyInfo.cs \
	Strings.cs \
	Server.cs \
	NetworkConnector.cs \
	Request.cs \
	TestResponse.cs \
	ApplicationHost.cs \
	WorkerRequest.cs \
	WebSource.cs \
	Main.cs \
	FastCgiProtocol/BeginRequestBody.cs \
	FastCgiProtocol/EndRequestBody.cs \
	FastCgiProtocol/Record.cs \
	FastCgiProtocol/UnknownTypeBody.cs \
	Sockets/TcpSocket.cs \
	Sockets/UnixSocket.cs \
	Sockets/GeneralSocket.cs \
	Config/ConfigurationManager.cs \
	Logging/Logger.cs \
	Logging/LogLevel.cs \
	Sockets/GeneralSocketType.cs 

DATA_FILES = 

RESOURCES = \
	Config/ConfigurationManager.xml 

EXTRAS = \
	FastCgiProtocol \
	Sockets \
	Config \
	Logging \
	mono.webserver.hyperfastcgi.in 

REFERENCES =  \
	System \
	Mono.Posix \
	System.Web \
	System.Configuration \
	System.Xml \
	$(XSP_4_LIBS)

DLL_REFERENCES = 

CLEANFILES = $(PROGRAMFILES) $(BINARIES) 

include $(top_srcdir)/Makefile.include

MONO_WEBSERVER_HYPERFASTCGI = $(BUILD_DIR)/mono.webserver.hyperfastcgi

$(eval $(call emit-deploy-wrapper,MONO_WEBSERVER_HYPERFASTCGI,mono.webserver.hyperfastcgi,x))


$(eval $(call emit_resgen_targets))
$(build_xamlg_list): %.xaml.g.cs: %.xaml
	xamlg '$<'

$(ASSEMBLY_MDB): $(ASSEMBLY)

$(ASSEMBLY): $(build_sources) $(build_resources) $(build_datafiles) $(DLL_REFERENCES) $(PROJECT_REFERENCES) $(build_xamlg_list) $(build_satellite_assembly_list)
	mkdir -p $(shell dirname $(ASSEMBLY))
	$(ASSEMBLY_COMPILER_COMMAND) $(ASSEMBLY_COMPILER_FLAGS) -out:$(ASSEMBLY) -target:$(COMPILE_TARGET) $(build_sources_embed) $(build_resources_embed) $(build_references_ref)
