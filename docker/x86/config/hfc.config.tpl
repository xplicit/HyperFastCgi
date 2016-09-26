<configuration>
    <server type="HyperFastCgi.ApplicationServers.SimpleApplicationServer">
        <host-factory>HyperFastCgi.HostFactories.SystemWebHostFactory</host-factory>
        <threads min-worker="80" max-worker="0" min-io="4" max-io="0" />
    </server>

<listener type="HyperFastCgi.Listeners.NativeListener">
    <apphost-transport type="HyperFastCgi.Transports.NativeTransport">
        <multithreading>ThreadPool</multithreading>
    </apphost-transport>
        <protocol>InterNetwork</protocol>
        <address>127.0.0.1</address>
        <port>${HFCPORT}</port>
    </listener>

    <apphost type="HyperFastCgi.AppHosts.AspNet.AspNetApplicationHost">
        <log level="Debug" write-to-console="true" />
        <add-trailing-slash>false</add-trailing-slash>
    </apphost>
    <web-applications>
        <web-application>
            <name>${SITENAME}</name>
            <vhost>${SITENAME}</vhost>
            <vport>80</vport>
            <vpath>/</vpath>
            <path>${SITELOCATION}</path>
        </web-application>
    </web-applications>
</configuration>
