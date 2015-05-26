using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayerUI.Tools
{
	public abstract class RegistryBase
	{
		private Dictionary<string, object> _internalDictionary;

		private HashSet<RegistryContext> _contextRegistry;

		private RegistryContext _currentContext;

		public RegistryBase()
		{
			Init();
		}

		public void Init()
		{
			_contextRegistry = new HashSet<RegistryContext>();
			_currentContext = null;
		}


		public void SetContext(RegistryContext reg)
		{
			if (ContextRegistered(reg))
				_currentContext = reg;
		}

		public void AddRegistry(RegistryContext reg)
		{
			if (!ContextRegistered(reg))
				_contextRegistry.Add(reg);
		}

		private bool ContextRegistered(RegistryContext reg)
		{
			return _contextRegistry.Contains(reg);
		}

	}

	public class SimpleRegistry : RegistryBase
	{

	}
}
