# $(call emit-deploy-wrapper,wrapper-variable-name,wrapper-sourcefile,x)
# assumes that for a wrapper foo.pc its source template is foo.pc.in
# if $3 is non-empty then wrapper is marked exec
define emit-deploy-wrapper
$($1): $2
	mkdir -p '$$(shell dirname '$$@')'
	cp '$$<' '$$@'
	$(if $3,chmod +x '$$@')
endef

$(eval $(call emit-deploy-wrapper,MONO_WEBSERVER_HYPERFASTCGI4,hyperfastcgi4,x))

$(eval $(call emit-deploy-wrapper,MONO_WEBSERVER_HYPERFASTCGI2,hyperfastcgi2,x))

binaries = 

HFC_NAMES = 

if NET_2_0
MONO_WEBSERVER_HYPERFASTCGI2 = 
HFC_NAMES += hyperfastcgi2
endif

if NET_4_0
MONO_WEBSERVER_HYPERFASTCGI4 = 
HFC_NAMES += hyperfastcgi4
endif


BINARIES = $(MONO_WEBSERVER_HYPERFASTCGI2) $(MONO_WEBSERVER_HYPERFASTCGI4)

CLEANFILES = $(BINARIES) 

bin_SCRIPTS = $(HFC_NAMES)

all: $(BINARIES)
	echo -e 'bin' $(BINARIES)
	echo -e 'scripts' $(bin_SCRIPTS)