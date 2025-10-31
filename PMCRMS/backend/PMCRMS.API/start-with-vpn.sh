#!/bin/bash

# Start OpenVPN in background if config exists
if [ -f "/etc/openvpn/client.conf" ]; then
    echo "Starting OpenVPN connection..."
    openvpn --config /etc/openvpn/client.conf --daemon --log /app/logs/openvpn.log
    
    # Wait for VPN connection to establish
    echo "Waiting for VPN connection to establish..."
    sleep 10
    
    # Check if VPN is connected
    if ip addr show tun0 > /dev/null 2>&1; then
        echo "VPN connection established successfully"
        ip addr show tun0
    else
        echo "WARNING: VPN connection may not be established"
    fi
else
    echo "No OpenVPN config found at /etc/openvpn/client.conf - skipping VPN"
fi

# Start the .NET application
echo "Starting PMCRMS API..."
exec dotnet PMCRMS.API.dll
