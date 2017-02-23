Name:           hyperfastcgi
Url:            https://github.com/xplicit/HyperFastCgi
License:        X11/MIT
Group:          Productivity/Networking/Web/Servers
Version:        0.4
Release:        1
Summary:        Mono WebServer HyperFastCgi
Source:         https://github.com/xplicit/HyperFastCgi/archive/master.zip
Requires:       mono, glib2, libevent, libmonosgen-2_0-1, systemd
BuildRequires:  mono-devel, glib2-devel, libevent-devel, libmonosgen-2_0-devel, autoconf, automake, libtool

# Build from spec file as root user
# yum install rpm-build redhat-rpm-config rpmdevtools yum-utils
# mkdir -p ~/rpmbuild/{BUILD,RPMS,SOURCES,SPECS,SRPMS}
# curl -o ~/rpmbuild/SPECS/hyperfastcgi.spec https://raw.githubusercontent.com/xplicit/HyperFastCgi/master/hyperfastcgi.spec
#
# spectool -g -R  ~/rpmbuild/SPECS/hyperfastcgi.spec
# yum-builddep ~/rpmbuild/SPECS/hyperfastcgi.spec
# rpmbuild -ba  ~/rpmbuild/SPECS/hyperfastcgi.spec

%description
Performant nginx to mono fastcgi server

%define debug_package %{nil}

%prep
%setup -q -n HyperFastCgi-master

%build

./autogen.sh --prefix=%{_prefix}
make

%install
make DESTDIR=%{buildroot} install
mkdir -p %{buildroot}%{_datadir}/%{name}/samples
cp samples/*.config %{buildroot}%{_datadir}/%{name}/samples
mkdir -p %{buildroot}%{_sysconfdir}/hyperfastcgi
cp samples/server.config %{buildroot}%{_sysconfdir}/%{name}/hfc.config
mkdir -p %{buildroot}%{_unitdir}
cp samples/ubuntu-startup/systemd/hyperfastcgi.service %{buildroot}%{_unitdir}

%post
/sbin/ldconfig
/usr/bin/gacutil -package 4.5  -i %{_prefix}/lib/%{name}/4.0/HyperFastCgi.exe
install -m 777 -d /var/log/%{name}

%postun
/usr/bin/gacutil -package 4.5  -u %{_prefix}/lib/%{name}/4.0/HyperFastCgi.exe
/sbin/ldconfig

%clean
rm -rf %{buildroot}

%files
%{_bindir}/*
%{_prefix}/lib/*
%config(noreplace) %{_sysconfdir}/*
%{_datadir}/*

%changelog
