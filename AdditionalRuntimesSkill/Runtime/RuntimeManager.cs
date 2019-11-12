using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.Storage;

namespace AdditionalRuntimesSkill.Runtime
{
	internal class RuntimeManager
	{
		private readonly string _rootWorkingDirectory;
		private readonly List<Runtime> _runtimes = new List<Runtime>();

		public event EventHandler<(string, string)> StandardOut;

		public event EventHandler<(string, string)> StandardErr;

		public event EventHandler<(string, uint)> RuntimeCompleted;

		public event EventHandler<(string, Exception)> RuntimeFaulted;

		public event EventHandler<string> RuntimeCancelled;

		public RuntimeManager()
		{
			_rootWorkingDirectory = ApplicationData.Current.LocalFolder.Path;
			_runtimes = new List<Runtime>();

			AddRuntime("Node", "node.exe", ".js");
			AddRuntime("Python", "python.exe", ".py");
		}

		public IReadOnlyList<string> ListAvailableRuntimes()
		{
			return _runtimes.Select(r => r.Name).ToList();
		}

		public bool HasRuntime(string name)
		{
			return _runtimes.Any(r => r.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
		}

		public void StartRuntime(string name, string subDirectory, string entryPoint)
		{
			Runtime runtime = GetRuntime(name);

			if (runtime is null)
			{
				throw new KeyNotFoundException($"Could not find runtime with name '{name}'");
			}

			if (!runtime.CanExecuteFile(entryPoint))
			{
				throw new InvalidOperationException($"Runtime {name} cannot start file with extenion {runtime.ExecutableExtension}");
			}

			string executionWorkingDirectory = Path.Combine(_rootWorkingDirectory, subDirectory);
			if (!Directory.Exists(executionWorkingDirectory))
			{
				throw new FileNotFoundException($"Could not find application directory {subDirectory} in {_rootWorkingDirectory}");
			}

			runtime.Start(entryPoint, executionWorkingDirectory);
		}

		private void AddRuntime(string name, string exe, string ext)
		{
			var runtime = new Runtime(name, exe, ext);

			runtime.StandardOut += Runtime_StandardOut;
			runtime.StandardErr += Runtime_StandardErr;
			runtime.ExecutionComplete += Runtime_ExecutionComplete;
			runtime.ExecutionFaulted += Runtime_ExecutionFaulted;
			runtime.ExecutionCancelled += Runtime_ExecutionCancelled;

			_runtimes.Add(runtime);
		}

		private void RemoveRuntime(Runtime runtime)
		{
			if (_runtimes.Contains(runtime))
			{
				_runtimes.Remove(runtime);

				runtime.StandardOut -= Runtime_StandardOut;
				runtime.StandardErr -= Runtime_StandardErr;
				runtime.ExecutionComplete -= Runtime_ExecutionComplete;
				runtime.ExecutionFaulted -= Runtime_ExecutionFaulted;
				runtime.ExecutionCancelled -= Runtime_ExecutionCancelled;
				
				//TODO: runtime.Dispose?
			}
		}

		private void Runtime_ExecutionCancelled(object sender, EventArgs e)
		{
			if (sender is Runtime runtime)
			{
				RuntimeCancelled?.Invoke(sender, runtime.Name);
			}
			else
			{
				RuntimeCancelled?.Invoke(sender, "Unknown");
			}
		}

		private void Runtime_ExecutionFaulted(object sender, Exception e)
		{
			if (sender is Runtime runtime)
			{
				RuntimeFaulted?.Invoke(sender, (runtime.Name, e));
			}
			else
			{
				RuntimeFaulted?.Invoke(sender, ("Unknown", e));
			}
		}

		private void Runtime_ExecutionComplete(object sender, uint e)
		{
			if (sender is Runtime runtime)
			{
				RuntimeCompleted?.Invoke(sender, (runtime.Name, e));
			}
			else
			{
				RuntimeCompleted?.Invoke(sender, ("Unknown", e));
			}
		}

		private void Runtime_StandardErr(object sender, string e)
		{
			if (sender is Runtime runtime)
			{
				StandardErr?.Invoke(sender, (runtime.Name, e));
			}
			else
			{
				StandardErr?.Invoke(sender, ("Unknown", e));
			}
		}

		private void Runtime_StandardOut(object sender, string e)
		{
			if (sender is Runtime runtime)
			{
				StandardOut?.Invoke(sender, (runtime.Name, e));
			}
			else
			{
				StandardOut?.Invoke(sender, ("Unknown", e));
			}
		}

		private Runtime GetRuntime(string name)
		{
			return _runtimes.SingleOrDefault(r => r.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
		}
	}
}
