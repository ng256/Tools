using System.Collections.Generic;

namespace System.Management.WMI
{
	public class NetworkAdapterConfigurationInfo
	{
		public object ArpAlwaysSourceRoute
		{
			get;
			set;
		}

		public object ArpUseEtherSNAP
		{
			get;
			set;
		}

		public string Caption
		{
			get;
			set;
		}

		public string DatabasePath
		{
			get;
			set;
		}

		public object DeadGWDetectEnabled
		{
			get;
			set;
		}

		public string[] DefaultIPGateway
		{
			get;
			set;
		}

		public object DefaultTOS
		{
			get;
			set;
		}

		public object DefaultTTL
		{
			get;
			set;
		}

		public string Description
		{
			get;
			set;
		}

		public bool DHCPEnabled
		{
			get;
			set;
		}

		public object DHCPLeaseExpires
		{
			get;
			set;
		}

		public object DHCPLeaseObtained
		{
			get;
			set;
		}

		public object DHCPServer
		{
			get;
			set;
		}

		public object DNSDomain
		{
			get;
			set;
		}

		public string[] DNSDomainSuffixSearchOrder
		{
			get;
			set;
		}

		public bool DNSEnabledForWINSResolution
		{
			get;
			set;
		}

		public string DNSHostName
		{
			get;
			set;
		}

		public string[] DNSServerSearchOrder
		{
			get;
			set;
		}

		public bool DomainDNSRegistrationEnabled
		{
			get;
			set;
		}

		public object ForwardBufferMemory
		{
			get;
			set;
		}

		public bool FullDNSRegistrationEnabled
		{
			get;
			set;
		}

		public ushort[] GatewayCostMetric
		{
			get;
			set;
		}

		public object IGMPLevel
		{
			get;
			set;
		}

		public uint Index
		{
			get;
			set;
		}

		public uint InterfaceIndex
		{
			get;
			set;
		}

		public string[] IPAddress
		{
			get;
			set;
		}

		public uint IPConnectionMetric
		{
			get;
			set;
		}

		public bool IPEnabled
		{
			get;
			set;
		}

		public bool IPFilterSecurityEnabled
		{
			get;
			set;
		}

		public object IPPortSecurityEnabled
		{
			get;
			set;
		}

		public string[] IPSecPermitIPProtocols
		{
			get;
			set;
		}

		public string[] IPSecPermitTCPPorts
		{
			get;
			set;
		}

		public string[] IPSecPermitUDPPorts
		{
			get;
			set;
		}

		public string[] IPSubnet
		{
			get;
			set;
		}

		public object IPUseZeroBroadcast
		{
			get;
			set;
		}

		public object IPXAddress
		{
			get;
			set;
		}

		public object IPXEnabled
		{
			get;
			set;
		}

		public object IPXFrameType
		{
			get;
			set;
		}

		public object IPXMediaType
		{
			get;
			set;
		}

		public object IPXNetworkNumber
		{
			get;
			set;
		}

		public object IPXVirtualNetNumber
		{
			get;
			set;
		}

		public object KeepAliveInterval
		{
			get;
			set;
		}

		public object KeepAliveTime
		{
			get;
			set;
		}

		public string MACAddress
		{
			get;
			set;
		}

		public object MTU
		{
			get;
			set;
		}

		public object NumForwardPackets
		{
			get;
			set;
		}

		public object PMTUBHDetectEnabled
		{
			get;
			set;
		}

		public object PMTUDiscoveryEnabled
		{
			get;
			set;
		}

		public string ServiceName
		{
			get;
			set;
		}

		public string SettingID
		{
			get;
			set;
		}

		public uint TcpipNetbiosOptions
		{
			get;
			set;
		}

		public object TcpMaxConnectRetransmissions
		{
			get;
			set;
		}

		public object TcpMaxDataRetransmissions
		{
			get;
			set;
		}

		public object TcpNumConnections
		{
			get;
			set;
		}

		public object TcpUseRFC1122UrgentPointer
		{
			get;
			set;
		}

		public object TcpWindowSize
		{
			get;
			set;
		}

		public bool WINSEnableLMHostsLookup
		{
			get;
			set;
		}

		public object WINSHostLookupFile
		{
			get;
			set;
		}

		public object WINSPrimaryServer
		{
			get;
			set;
		}

		public string WINSScopeID
		{
			get;
			set;
		}

		public object WINSSecondaryServer
		{
			get;
			set;
		}

		private NetworkAdapterConfigurationInfo()
		{
		}

		private static T GetPropertyValue<T>(ManagementObject wmi, string propertyName)
		{
			try
			{
				return (T)(wmi.GetPropertyValue(propertyName) ?? ((object)default(T)));
			}
			catch
			{
				return default(T);
			}
		}

		public static NetworkAdapterConfigurationInfo[] GetNetworkAdapterConfigurationInfo()
		{
			ManagementClass managementClass = new ManagementClass(new ManagementPath("Win32_NetworkAdapterConfiguration"));
			List<NetworkAdapterConfigurationInfo> list = new List<NetworkAdapterConfigurationInfo>();
			foreach (ManagementObject item in managementClass.GetInstances())
			{
				list.Add(new NetworkAdapterConfigurationInfo
				{
					ArpAlwaysSourceRoute = GetPropertyValue<object>(item, "ArpAlwaysSourceRoute"),
					ArpUseEtherSNAP = GetPropertyValue<object>(item, "ArpUseEtherSNAP"),
					Caption = GetPropertyValue<string>(item, "Caption"),
					DatabasePath = GetPropertyValue<string>(item, "DatabasePath"),
					DeadGWDetectEnabled = GetPropertyValue<object>(item, "DeadGWDetectEnabled"),
					DefaultIPGateway = GetPropertyValue<string[]>(item, "DefaultIPGateway"),
					DefaultTOS = GetPropertyValue<object>(item, "DefaultTOS"),
					DefaultTTL = GetPropertyValue<object>(item, "DefaultTTL"),
					Description = GetPropertyValue<string>(item, "Description"),
					DHCPEnabled = GetPropertyValue<bool>(item, "DHCPEnabled"),
					DHCPLeaseExpires = GetPropertyValue<object>(item, "DHCPLeaseExpires"),
					DHCPLeaseObtained = GetPropertyValue<object>(item, "DHCPLeaseObtained"),
					DHCPServer = GetPropertyValue<object>(item, "DHCPServer"),
					DNSDomain = GetPropertyValue<object>(item, "DNSDomain"),
					DNSDomainSuffixSearchOrder = GetPropertyValue<string[]>(item, "DNSDomainSuffixSearchOrder"),
					DNSEnabledForWINSResolution = GetPropertyValue<bool>(item, "DNSEnabledForWINSResolution"),
					DNSHostName = GetPropertyValue<string>(item, "DNSHostName"),
					DNSServerSearchOrder = GetPropertyValue<string[]>(item, "DNSServerSearchOrder"),
					DomainDNSRegistrationEnabled = GetPropertyValue<bool>(item, "DomainDNSRegistrationEnabled"),
					ForwardBufferMemory = GetPropertyValue<object>(item, "ForwardBufferMemory"),
					FullDNSRegistrationEnabled = GetPropertyValue<bool>(item, "FullDNSRegistrationEnabled"),
					GatewayCostMetric = GetPropertyValue<ushort[]>(item, "GatewayCostMetric"),
					IGMPLevel = GetPropertyValue<object>(item, "IGMPLevel"),
					Index = GetPropertyValue<uint>(item, "Index"),
					InterfaceIndex = GetPropertyValue<uint>(item, "InterfaceIndex"),
					IPAddress = GetPropertyValue<string[]>(item, "IPAddress"),
					IPConnectionMetric = GetPropertyValue<uint>(item, "IPConnectionMetric"),
					IPEnabled = GetPropertyValue<bool>(item, "IPEnabled"),
					IPFilterSecurityEnabled = GetPropertyValue<bool>(item, "IPFilterSecurityEnabled"),
					IPPortSecurityEnabled = GetPropertyValue<object>(item, "IPPortSecurityEnabled"),
					IPSecPermitIPProtocols = GetPropertyValue<string[]>(item, "IPSecPermitIPProtocols"),
					IPSecPermitTCPPorts = GetPropertyValue<string[]>(item, "IPSecPermitTCPPorts"),
					IPSecPermitUDPPorts = GetPropertyValue<string[]>(item, "IPSecPermitUDPPorts"),
					IPSubnet = GetPropertyValue<string[]>(item, "IPSubnet"),
					IPUseZeroBroadcast = GetPropertyValue<object>(item, "IPUseZeroBroadcast"),
					IPXAddress = GetPropertyValue<object>(item, "IPXAddress"),
					IPXEnabled = GetPropertyValue<object>(item, "IPXEnabled"),
					IPXFrameType = GetPropertyValue<object>(item, "IPXFrameType"),
					IPXMediaType = GetPropertyValue<object>(item, "IPXMediaType"),
					IPXNetworkNumber = GetPropertyValue<object>(item, "IPXNetworkNumber"),
					IPXVirtualNetNumber = GetPropertyValue<object>(item, "IPXVirtualNetNumber"),
					KeepAliveInterval = GetPropertyValue<object>(item, "KeepAliveInterval"),
					KeepAliveTime = GetPropertyValue<object>(item, "KeepAliveTime"),
					MACAddress = GetPropertyValue<string>(item, "MACAddress"),
					MTU = GetPropertyValue<object>(item, "MTU"),
					NumForwardPackets = GetPropertyValue<object>(item, "NumForwardPackets"),
					PMTUBHDetectEnabled = GetPropertyValue<object>(item, "PMTUBHDetectEnabled"),
					PMTUDiscoveryEnabled = GetPropertyValue<object>(item, "PMTUDiscoveryEnabled"),
					ServiceName = GetPropertyValue<string>(item, "ServiceName"),
					SettingID = GetPropertyValue<string>(item, "SettingID"),
					TcpipNetbiosOptions = GetPropertyValue<uint>(item, "TcpipNetbiosOptions"),
					TcpMaxConnectRetransmissions = GetPropertyValue<object>(item, "TcpMaxConnectRetransmissions"),
					TcpMaxDataRetransmissions = GetPropertyValue<object>(item, "TcpMaxDataRetransmissions"),
					TcpNumConnections = GetPropertyValue<object>(item, "TcpNumConnections"),
					TcpUseRFC1122UrgentPointer = GetPropertyValue<object>(item, "TcpUseRFC1122UrgentPointer"),
					TcpWindowSize = GetPropertyValue<object>(item, "TcpWindowSize"),
					WINSEnableLMHostsLookup = GetPropertyValue<bool>(item, "WINSEnableLMHostsLookup"),
					WINSHostLookupFile = GetPropertyValue<object>(item, "WINSHostLookupFile"),
					WINSPrimaryServer = GetPropertyValue<object>(item, "WINSPrimaryServer"),
					WINSScopeID = GetPropertyValue<string>(item, "WINSScopeID"),
					WINSSecondaryServer = GetPropertyValue<object>(item, "WINSSecondaryServer")
				});
				item.Dispose();
			}
			return list.ToArray();
		}
	}
}
