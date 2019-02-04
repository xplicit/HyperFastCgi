FROM ubuntu:16.04
MAINTAINER Sergey Zhukov svg@ngs.ru

#installing mono, nginx and hyperfastcgi
RUN apt-get -qqq update \
    && apt-get install -y -q apt-transport-https \
    && apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF \
    && (echo "deb https://download.mono-project.com/repo/ubuntu stable-xenial main" | tee /etc/apt/sources.list.d/mono-official-stable.list) \
    && echo "Updating repositories..." \
    && apt-get -qqq update \
    && echo "Installing mono..." \
    && apt-get install -y -q mono-complete referenceassemblies-pcl libmonosgen-2.0-dev \
    && echo "Installing nginx..." \
    && apt-get install -y nginx \
    && echo "Installing HyperFastCGI" \
    && mkdir -p /home/hfc-install && cd /home/hfc-install && mkdir -p config \
    && apt-get install -y git autoconf automake libtool libtool-bin make libglib2.0-dev libevent-dev \
    && git clone https://github.com/xplicit/hyperfastcgi \
    && (cd /home/hfc-install/hyperfastcgi && ./autogen.sh --prefix=/usr && make && make install)

COPY config/ /home/hfc-install/config/
COPY entrypoint.sh /home/entrypoint.sh

EXPOSE 80
VOLUME ["/var/www"]

ENV USERHFCPORT=9000 USERSITE=www.test.com USERLOCATION=/var/www/hello-app
WORKDIR "/home"
ENTRYPOINT "/home/entrypoint.sh"
