Name:           mono-webserver-hyperfastcgi
Url:            https://github.com/xplicit/HyperFastCgi
License:        X11/MIT
Group:          Productivity/Networking/Web/Servers
Version:        0.3
Release:        1
Summary:        Mono WebServer HyperFastCgi
Source:         %{name}-%{version}-%{release}.tar
BuildRoot:      %{_tmppath}/%{name}-%{version}-build
BuildArch:      noarch
BuildRequires:  mono-devel

# To build the tar, you can use : 
# git archive --format=tar --prefix=mono-webserver-hyperfastcgi-0.3-1/ master > ~/rpmbuild/SOURCES/mono-webserver-hyperfastcgi-0.3-1.tar

%description
Performant nginx to mono fastcgi server

%prep
%setup -q -n %{name}-%{version}-%{release}

%build
sed -i 's#"1.0.*"#"2.0.*"#' src/Mono.WebServer.HyperFastCgi/Properties/AssemblyInfo.cs
xbuild /tv:2.0 /p:Configuration=Release /target:rebuild /p:SignAssembly=true /p:AssemblyOriginatorKeyFile="../mono.snk"
gacutil -i src/Mono.WebServer.HyperFastCgi/bin/Release/Mono.WebServer.HyperFastCgi.exe -package 2.0 -gacdir .%{_prefix}
sed -i 's#"2.0.*"#"4.0.*"#' src/Mono.WebServer.HyperFastCgi/Properties/AssemblyInfo.cs
xbuild /tv:4.0 /p:Configuration=Release /target:rebuild /p:SignAssembly=true /p:AssemblyOriginatorKeyFile="../mono.snk"
gacutil -i src/Mono.WebServer.HyperFastCgi/bin/Release/Mono.WebServer.HyperFastCgi.exe -package 4.0 -gacdir .%{_prefix}

%install
mv .%{_prefix} %{buildroot}%{_prefix}
mkdir -p %{buildroot}%{_bindir}
echo "#!/bin/sh" > %{buildroot}%{_bindir}/mono-server-hyperfastcgi2
echo 'exec %{_bindir}/mono $MONO_OPTIONS "%{_prefix}/lib/mono/2.0/Mono.WebServer.HyperFastCgi.exe" "$@"' >> %{buildroot}%{_bindir}/mono-server-hyperfastcgi2
chmod +x %{buildroot}%{_bindir}/mono-server-hyperfastcgi2
echo "#!/bin/sh" > %{buildroot}%{_bindir}/mono-server-hyperfastcgi4
echo 'exec %{_bindir}/mono $MONO_OPTIONS "%{_prefix}/lib/mono/4.0/Mono.WebServer.HyperFastCgi.exe" "$@"' >> %{buildroot}%{_bindir}/mono-server-hyperfastcgi4
chmod +x %{buildroot}%{_bindir}/mono-server-hyperfastcgi4

%clean
rm -rf %{buildroot}

%files
%defattr(-,root,root)
%{_bindir}/mono-server-hyperfastcgi2
%{_bindir}/mono-server-hyperfastcgi4
%{_prefix}/lib/mono/2.0/Mono.WebServer.HyperFastCgi.exe
%{_prefix}/lib/mono/4.0/Mono.WebServer.HyperFastCgi.exe
%{_prefix}/lib/mono/gac/Mono.WebServer.HyperFastCgi/*

%changelog
