# $(call emit-deploy-wrapper,wrapper-variable-name,wrapper-sourcefile,x)
# assumes that for a wrapper foo.pc its source template is foo.pc.in
# if $3 is non-empty then wrapper is marked exec
define emit-deploy-wrapper
$($1): $2
	mkdir -p '$$(shell dirname '$$@')'
	cp '$$<' '$$@'
	$(if $3,chmod +x '$$@')
endef

#mono-server-hyperfastcgi4: 

$(eval $(call emit-deploy-wrapper,MONO_WEBSERVER_HYPERFASTCGI4,mono-server-hyperfastcgi4,x))

$(eval $(call emit-deploy-wrapper,MONO_WEBSERVER_HYPERFASTCGI2,mono-server-hyperfastcgi2,x))

binaries = 

if NET_2_0
MONO_WEBSERVER_HYPERFASTCGI2 = 
endif

if NET_4_0
MONO_WEBSERVER_HYPERFASTCGI4 = 
endif


BINARIES = $(MONO_WEBSERVER_HYPERFASTCGI2) $(MONO_WEBSERVER_HYPERFASTCGI4)

CLEANFILES = $(BINARIES) 

bin_SCRIPTS = mono-server-hyperfastcgi2 mono-server-hyperfastcgi4

all: $(BINARIES)
	echo -e 'bin' $(BINARIES)
	echo -e 'scripts' $(bin_SCRIPTS)