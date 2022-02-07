// Mscorlib resources.
ResourceSet _mscorlib = null;

// Gets mscorlib internal error message.
string GetResourceString(string name)
{
	if (_mscorlib == null)
	{
		var assembly = Assembly.GetAssembly(typeof(object));
		var name = assembly.GetName().Name;
		var manager = new ResourceManager(name, assembly);
		_mscorlib = manager.GetResourceSet(CultureInfo.CurrentUICulture, true, true);
	}
	return _mscorlib.GetString(name);
}

// Gets parametrized mscorlib internal error message.
string GetResourceString(string name, params object[] args)
{
	return string.Format(GetResourceString(name) ?? throw new ArgumentNullException(nameof(name)), args);
}
