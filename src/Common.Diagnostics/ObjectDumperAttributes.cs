using System;

namespace Common.Diagnostics
{
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
	public class DumperIgnoreAttribute : Attribute
	{
	}
}
