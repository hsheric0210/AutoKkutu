using System;

namespace AutoKkutuLib.Modules;

/// <summary>
/// Marker annotation for module dependencies
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class ModuleDependencyAttribute : Attribute
{
	public Type[] Dependencies
	{
		get;
	}

	public ModuleDependencyAttribute(params Type[] dependencies) => Dependencies = dependencies;
}
