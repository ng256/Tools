using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Ini;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;

namespace System.Resourses
{
	public class ResourceFile : ResourceSet, IInitializer, IResourceReader, IEnumerable, IDisposable, IResourceWriter
	{
		private const ulong MagicBytes = 7498353358uL;

		[NonSerialized]
		protected IResourceWriter Writer;

		protected ResourceFile()
		{
		}

		private bool SafeGenerate(string fileName)
		{
			try
			{
				if (File.Exists(fileName))
				{
					using (Stream stream = File.OpenRead(fileName))
					{
						using (BinaryReader reader = new BinaryReader(stream))
						{
							if (stream.Length > 8 && reader.ReadUInt64() == MagicBytes)
							{
								return true;
							}
						}
					}
				}
				File.Delete(fileName);
				using (ResourceWriter writer = new ResourceWriter(fileName))
				{
					writer.Generate();
				}
			}
			catch
			{
				return false;
			}
			return true;
		}

		private bool SafeGenerate(Stream stream)
		{
			try
			{
				using (BinaryReader reader = new BinaryReader(stream))
				{
					if (stream.Length > 8 && reader.ReadUInt64() == MagicBytes)
					{
						return true;
					}
				}
				stream.Seek(0L, SeekOrigin.Begin);
				using (ResourceWriter writer = new ResourceWriter(stream))
				{
					writer.Generate();
				}
			}
			catch
			{
				return false;
			}
			return true;
		}

		public ResourceFile(string fileName, bool readOnly = false)
		{
			if (fileName == null)
			{
				throw new ArgumentNullException("fileName", AssemblyMessageFormatter.DefaultFormatter.GetMessage("ArgumentNull_FileName"));
			}
			if (fileName == string.Empty)
			{
				throw new ArgumentNullException("fileName", AssemblyMessageFormatter.DefaultFormatter.GetMessage("Argument_EmptyFileName"));
			}
			if (!SafeGenerate(fileName))
			{
				throw new FileNotFoundException(AssemblyMessageFormatter.DefaultFormatter.FormatMessage("IO.FileNotFound_FileName", fileName), fileName);
			}
			Reader = new ResourceReader(fileName);
			ReadResources();
			if (!readOnly)
			{
				Writer = new ResourceWriter(File.Open(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None));
			}
		}

		public ResourceFile(Stream stream, bool readOnly = false)
		{
			if (stream == null)
			{
				throw new ArgumentNullException("stream");
			}
			if (!stream.CanRead)
			{
				throw new InvalidOperationException(AssemblyMessageFormatter.DefaultFormatter.GetMessage("Argument_StreamNotReadable"));
			}
			if (!stream.CanWrite)
			{
				throw new InvalidOperationException(AssemblyMessageFormatter.DefaultFormatter.GetMessage("Argument_StreamNotWritable"));
			}
			if (!SafeGenerate(stream))
			{
				throw new FileNotFoundException(AssemblyMessageFormatter.DefaultFormatter.GetMessage("BadImageFormat_ResourcesHeaderCorrupted"));
			}
			Reader = new ResourceReader(stream);
			ReadResources();
			if (!readOnly)
			{
				Writer = new ResourceWriter(stream);
			}
		}

		public ResourceFile(ResourceReader reader, out Stream stream, bool readOnly = false)
		{
			stream = null;
			Reader = reader ?? throw new ArgumentNullException("reader");
			ReadResources();
			if (!readOnly)
			{
				stream = new MemoryStream(8096);
				Writer = new ResourceWriter(stream);
			}
		}

		private object InternalReadObject(Type type, string name, bool ignoreCase = false, object defaultValue = null, TypeConverter converter = null, CultureInfo culture = null)
		{
			if (type == null)
			{
				return null;
			}
			if (converter == null)
			{
				converter = TypeDescriptor.GetConverter(type);
			}
			if (culture == null)
			{
				culture = CultureInfo.InvariantCulture;
			}
			Type defaultValueType = defaultValue?.GetType();
			IConvertible def;
			if ((def = defaultValue as IConvertible) != null)
			{
				if (type == typeof(string))
				{
					defaultValue = def.ToString(culture);
				}
				else if (type == typeof(int))
				{
					defaultValue = def.ToInt32(culture);
				}
				else if (type == typeof(double))
				{
					defaultValue = def.ToDouble(culture);
				}
				else if (type == typeof(bool))
				{
					defaultValue = def.ToBoolean(culture);
				}
				else if (type == typeof(byte))
				{
					defaultValue = def.ToByte(culture);
				}
				else if (type == typeof(char))
				{
					defaultValue = def.ToChar(culture);
				}
				else if (type == typeof(sbyte))
				{
					defaultValue = def.ToSByte(culture);
				}
				else if (type == typeof(short))
				{
					defaultValue = def.ToInt16(culture);
				}
				else if (type == typeof(ushort))
				{
					defaultValue = def.ToUInt16(culture);
				}
				else if (type == typeof(uint))
				{
					defaultValue = def.ToUInt32(culture);
				}
				else if (type == typeof(long))
				{
					defaultValue = def.ToInt64(culture);
				}
				else if (type == typeof(ulong))
				{
					defaultValue = def.ToUInt64(culture);
				}
				else if (type == typeof(float))
				{
					defaultValue = def.ToSingle(culture);
				}
				else if (type == typeof(decimal))
				{
					defaultValue = def.ToDecimal(culture);
				}
			}
			else if (defaultValueType == null)
			{
				defaultValue = (type.IsValueType ? Activator.CreateInstance(type) : null);
			}
			else if (!defaultValueType.IsAssignableFrom(type) && !defaultValueType.IsSubclassOf(type))
			{
				defaultValue = ((!converter.CanConvertFrom(defaultValueType)) ? (type.IsValueType ? Activator.CreateInstance(type) : null) : converter.ConvertFrom(null, culture, defaultValue));
			}
			object value;
			try
			{
				value = GetObject(name, ignoreCase);
			}
			catch (Exception)
			{
				return defaultValue;
			}
			if (value == null)
			{
				return defaultValue;
			}
			Type valueType = value.GetType();
			if (valueType == type)
			{
				return value;
			}
			if (!converter.CanConvertFrom(valueType))
			{
				return defaultValue;
			}
			return converter.ConvertFrom(null, culture, value);
		}

		private void InternalWriteObject(Type type, string name, object value = null, TypeConverter converter = null, CultureInfo culture = null)
		{
			if (name == null || value == null || type == null)
			{
				return;
			}
			Type type2 = value.GetType();
			if (converter == null)
			{
				converter = TypeDescriptor.GetConverter(type);
			}
			if (culture == null)
			{
				culture = CultureInfo.InvariantCulture;
			}
			IConvertible conv;
			if (type2 == type)
			{
				AddResource(name, value);
			}
			else if ((conv = value as IConvertible) != null)
			{
				if (type == typeof(string))
				{
					value = conv.ToString(culture);
				}
				else if (type == typeof(int))
				{
					value = conv.ToInt32(culture);
				}
				else if (type == typeof(double))
				{
					value = conv.ToDouble(culture);
				}
				else if (type == typeof(bool))
				{
					value = conv.ToBoolean(culture);
				}
				else if (type == typeof(byte))
				{
					value = conv.ToByte(culture);
				}
				else if (type == typeof(char))
				{
					value = conv.ToChar(culture);
				}
				else if (type == typeof(sbyte))
				{
					value = conv.ToSByte(culture);
				}
				else if (type == typeof(short))
				{
					value = conv.ToInt16(culture);
				}
				else if (type == typeof(ushort))
				{
					value = conv.ToUInt16(culture);
				}
				else if (type == typeof(uint))
				{
					value = conv.ToUInt32(culture);
				}
				else if (type == typeof(long))
				{
					value = conv.ToInt64(culture);
				}
				else if (type == typeof(ulong))
				{
					value = conv.ToUInt64(culture);
				}
				else if (type == typeof(float))
				{
					value = conv.ToSingle(culture);
				}
				else if (type == typeof(decimal))
				{
					value = conv.ToDecimal(culture);
				}
				AddResource(name, value);
			}
			else if (converter.CanConvertTo(type))
			{
				AddResource(name, converter.ConvertTo(null, culture, value, type));
			}
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				IResourceWriter writer = Writer;
				Writer = null;
				writer?.Close();
				Table?.Clear();
			}
			Writer = null;
			base.Dispose(disposing);
		}

		protected override void ReadResources()
		{
			base.ReadResources();
			Reader?.Close();
		}

		protected void UpdateTable(object name, object value)
		{
			if (Table.Contains(name))
			{
				Table[name] = value;
			}
			else
			{
				Table.Add(name, value);
			}
		}

		public T GetValue<T>(string name, T defaultValue = default(T), bool ignoreCase = false)
		{
			try
			{
				T obj;
				return ((obj = (T)GetObject(name, ignoreCase)) != null) ? obj : defaultValue;
			}
			catch
			{
				return defaultValue;
			}
		}

		public string GetString(string name, string defaultValue = null, bool ignoreCase = false)
		{
			try
			{
				return GetString(name, ignoreCase);
			}
			catch
			{
				return defaultValue;
			}
		}

		public void AddResource<T>(string name, T value)
		{
			AddResource(name, value);
		}

		public virtual void AddResource(string name, string value)
		{
			UpdateTable(name, value);
			Writer.AddResource(name, value);
		}

		public virtual void AddResource(string name, object value)
		{
			UpdateTable(name, value);
			Writer.AddResource(name, value);
		}

		public virtual void AddResource(string name, byte[] value)
		{
			UpdateTable(name, value);
			Writer.AddResource(name, value);
		}

		public virtual void Generate()
		{
			Writer.Generate();
		}

		public virtual void ReadSettings(Assembly assembly = null)
		{
			if (assembly == null)
			{
				assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
			}
			Type[] types = assembly.GetTypes();
			for (int i = 0; i < types.Length; i++)
			{
				PropertyInfo[] properties = types[i].GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
				foreach (PropertyInfo property in properties)
				{
					ResourceAttribute assemblyMessage;
					if (property.CanWrite && (assemblyMessage = property.GetCustomAttributes(typeof(ResourceAttribute), false).FirstOrDefault() as ResourceAttribute) != null)
					{
						string name = assemblyMessage.Name ?? property.Name;
						object defaultValue = assemblyMessage.DefaultValue;
						try
						{
							Type propertyType = property.PropertyType;
							TypeConverter converter = property.GetPropertyConverter();
							object newPropertyValue = InternalReadObject(propertyType, name, true, defaultValue, converter);
							property.SetPropertyValue(newPropertyValue);
						}
						catch (Exception)
						{
						}
					}
				}
			}
		}

		public virtual void WriteSettings(Assembly assembly = null)
		{
			if (Writer == null)
			{
				return;
			}
			if (assembly == null)
			{
				assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
			}
			Type[] types = assembly.GetTypes();
			for (int i = 0; i < types.Length; i++)
			{
				PropertyInfo[] properties = types[i].GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
				foreach (PropertyInfo property in properties)
				{
					ResourceAttribute assemblyMessage;
					if (!property.CanWrite || (assemblyMessage = property.GetCustomAttributes(typeof(ResourceAttribute), false).FirstOrDefault() as ResourceAttribute) == null)
					{
						continue;
					}
					string name = assemblyMessage.Name ?? property.Name;
					try
					{
						Type propertyType = property.PropertyType;
						TypeConverter converter = property.GetPropertyConverter();
						object value = property.GetPropertyValue();
						string str;
						if ((str = value as string) != null)
						{
							Writer.AddResource(name, str);
						}
						else
						{
							InternalWriteObject(propertyType, name, value, converter);
						}
					}
					catch (Exception)
					{
					}
				}
			}
			Writer.Generate();
		}
	}
}
