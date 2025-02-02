using System.Collections.Generic;

namespace System.Management.WMI
{
	public class ProcessorInfo
	{
		public ushort AddressWidth
		{
			get;
			set;
		}

		public ushort Architecture
		{
			get;
			set;
		}

		public ushort Availability
		{
			get;
			set;
		}

		public string Caption
		{
			get;
			set;
		}

		public object ConfigManagerErrorCode
		{
			get;
			set;
		}

		public object ConfigManagerUserConfig
		{
			get;
			set;
		}

		public ushort CpuStatus
		{
			get;
			set;
		}

		public string CreationClassName
		{
			get;
			set;
		}

		public uint CurrentClockSpeed
		{
			get;
			set;
		}

		public ushort CurrentVoltage
		{
			get;
			set;
		}

		public ushort DataWidth
		{
			get;
			set;
		}

		public string Description
		{
			get;
			set;
		}

		public string DeviceID
		{
			get;
			set;
		}

		public object ErrorCleared
		{
			get;
			set;
		}

		public object ErrorDescription
		{
			get;
			set;
		}

		public uint ExtClock
		{
			get;
			set;
		}

		public ushort Family
		{
			get;
			set;
		}

		public object InstallDate
		{
			get;
			set;
		}

		public uint L2CacheSize
		{
			get;
			set;
		}

		public object L2CacheSpeed
		{
			get;
			set;
		}

		public uint L3CacheSize
		{
			get;
			set;
		}

		public uint L3CacheSpeed
		{
			get;
			set;
		}

		public object LastErrorCode
		{
			get;
			set;
		}

		public ushort Level
		{
			get;
			set;
		}

		public ushort LoadPercentage
		{
			get;
			set;
		}

		public string Manufacturer
		{
			get;
			set;
		}

		public uint MaxClockSpeed
		{
			get;
			set;
		}

		public string Name
		{
			get;
			set;
		}

		public uint NumberOfCores
		{
			get;
			set;
		}

		public uint NumberOfLogicalProcessors
		{
			get;
			set;
		}

		public object OtherFamilyDescription
		{
			get;
			set;
		}

		public object PNPDeviceID
		{
			get;
			set;
		}

		public object PowerManagementCapabilities
		{
			get;
			set;
		}

		public bool PowerManagementSupported
		{
			get;
			set;
		}

		public string ProcessorId
		{
			get;
			set;
		}

		public ushort ProcessorType
		{
			get;
			set;
		}

		public ushort Revision
		{
			get;
			set;
		}

		public string Role
		{
			get;
			set;
		}

		public string SocketDesignation
		{
			get;
			set;
		}

		public string Status
		{
			get;
			set;
		}

		public ushort StatusInfo
		{
			get;
			set;
		}

		public object Stepping
		{
			get;
			set;
		}

		public string SystemCreationClassName
		{
			get;
			set;
		}

		public string SystemName
		{
			get;
			set;
		}

		public object UniqueId
		{
			get;
			set;
		}

		public ushort UpgradeMethod
		{
			get;
			set;
		}

		public string Version
		{
			get;
			set;
		}

		public object VoltageCaps
		{
			get;
			set;
		}

		private ProcessorInfo()
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

		public static ProcessorInfo[] GetProcessorInfo()
		{
			ManagementClass managementClass = new ManagementClass(new ManagementPath("Win32_Processor"));
			List<ProcessorInfo> list = new List<ProcessorInfo>();
			foreach (ManagementObject item in managementClass.GetInstances())
			{
				list.Add(new ProcessorInfo
				{
					AddressWidth = GetPropertyValue<ushort>(item, "AddressWidth"),
					Architecture = GetPropertyValue<ushort>(item, "Architecture"),
					Availability = GetPropertyValue<ushort>(item, "Availability"),
					Caption = GetPropertyValue<string>(item, "Caption"),
					ConfigManagerErrorCode = GetPropertyValue<object>(item, "ConfigManagerErrorCode"),
					ConfigManagerUserConfig = GetPropertyValue<object>(item, "ConfigManagerUserConfig"),
					CpuStatus = GetPropertyValue<ushort>(item, "CpuStatus"),
					CreationClassName = GetPropertyValue<string>(item, "CreationClassName"),
					CurrentClockSpeed = GetPropertyValue<uint>(item, "CurrentClockSpeed"),
					CurrentVoltage = GetPropertyValue<ushort>(item, "CurrentVoltage"),
					DataWidth = GetPropertyValue<ushort>(item, "DataWidth"),
					Description = GetPropertyValue<string>(item, "Description"),
					DeviceID = GetPropertyValue<string>(item, "DeviceID"),
					ErrorCleared = GetPropertyValue<object>(item, "ErrorCleared"),
					ErrorDescription = GetPropertyValue<object>(item, "ErrorDescription"),
					ExtClock = GetPropertyValue<uint>(item, "ExtClock"),
					Family = GetPropertyValue<ushort>(item, "Family"),
					InstallDate = GetPropertyValue<object>(item, "InstallDate"),
					L2CacheSize = GetPropertyValue<uint>(item, "L2CacheSize"),
					L2CacheSpeed = GetPropertyValue<object>(item, "L2CacheSpeed"),
					L3CacheSize = GetPropertyValue<uint>(item, "L3CacheSize"),
					L3CacheSpeed = GetPropertyValue<uint>(item, "L3CacheSpeed"),
					LastErrorCode = GetPropertyValue<object>(item, "LastErrorCode"),
					Level = GetPropertyValue<ushort>(item, "Level"),
					LoadPercentage = GetPropertyValue<ushort>(item, "LoadPercentage"),
					Manufacturer = GetPropertyValue<string>(item, "Manufacturer"),
					MaxClockSpeed = GetPropertyValue<uint>(item, "MaxClockSpeed"),
					Name = GetPropertyValue<string>(item, "Name"),
					NumberOfCores = GetPropertyValue<uint>(item, "NumberOfCores"),
					NumberOfLogicalProcessors = GetPropertyValue<uint>(item, "NumberOfLogicalProcessors"),
					OtherFamilyDescription = GetPropertyValue<object>(item, "OtherFamilyDescription"),
					PNPDeviceID = GetPropertyValue<object>(item, "PNPDeviceID"),
					PowerManagementCapabilities = GetPropertyValue<object>(item, "PowerManagementCapabilities"),
					PowerManagementSupported = GetPropertyValue<bool>(item, "PowerManagementSupported"),
					ProcessorId = GetPropertyValue<string>(item, "ProcessorId"),
					ProcessorType = GetPropertyValue<ushort>(item, "ProcessorType"),
					Revision = GetPropertyValue<ushort>(item, "Revision"),
					Role = GetPropertyValue<string>(item, "Role"),
					SocketDesignation = GetPropertyValue<string>(item, "SocketDesignation"),
					Status = GetPropertyValue<string>(item, "Status"),
					StatusInfo = GetPropertyValue<ushort>(item, "StatusInfo"),
					Stepping = GetPropertyValue<object>(item, "Stepping"),
					SystemCreationClassName = GetPropertyValue<string>(item, "SystemCreationClassName"),
					SystemName = GetPropertyValue<string>(item, "SystemName"),
					UniqueId = GetPropertyValue<object>(item, "UniqueId"),
					UpgradeMethod = GetPropertyValue<ushort>(item, "UpgradeMethod"),
					Version = GetPropertyValue<string>(item, "Version"),
					VoltageCaps = GetPropertyValue<object>(item, "VoltageCaps")
				});
				item.Dispose();
			}
			return list.ToArray();
		}
	}
}
