using System.Collections.Generic;

namespace AdditionalRuntimesSkill.Runtime
{
	internal class StartRuntimeParameters
	{
		public string RuntimeName { get; }

		public string Directory { get; }

		public string EntryPoint { get; }

		public StartRuntimeParameters(string runtimeName, string directory, string entryPoint)
		{
			RuntimeName = runtimeName;
			Directory = directory;
			EntryPoint = entryPoint;
		}

		public static IReadOnlyList<string> Validate(IDictionary<string, object> dict)
		{
			List<string> validationErrors = new List<string>();
			
			ValidateParameter<string>(dict, nameof(RuntimeName), validationErrors);
			ValidateParameter<string>(dict, nameof(Directory), validationErrors);
			ValidateParameter<string>(dict, nameof(EntryPoint), validationErrors);

			return validationErrors;
		}

		public static StartRuntimeParameters Extract(IDictionary<string, object> dict)
		{
			string name = (string)dict[nameof(RuntimeName)];
			string dir = (string)dict[nameof(Directory)];
			string entry = (string)dict[nameof(EntryPoint)];
			return new StartRuntimeParameters(name, dir, entry);
		}

		private static void ValidateParameter<T>(IDictionary<string, object> dict, string name, List<string> errors)
		{
			if (!dict.ContainsKey(name))
			{
				errors.Add($"Missing required parameter '{name}'");
			}
			else if (dict.TryGetValue(name, out object value) && !(value is T))
			{
				errors.Add($"Parameter '{name}' found with type {value.GetType().Name} but expected {typeof(T).Name}");
			}
		}
	}
}
