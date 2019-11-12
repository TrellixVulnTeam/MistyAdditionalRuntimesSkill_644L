using System;
using System.IO;

namespace AdditionalRuntimesSkill.Runtime
{
	internal class Runtime
	{
		private readonly string _runtimeExecutablePath;

		private ExternalProcess _currentRuntimeExecution;

		public string Name { get; }

		public string ExecutableExtension { get; }

		public event EventHandler<string> StandardOut;

		public event EventHandler<string> StandardErr;

		public event EventHandler<uint> ExecutionComplete;

		public event EventHandler ExecutionCancelled;

		public event EventHandler<Exception> ExecutionFaulted;

		public Runtime(string name, string exe, string executableExtension)
		{
			if (string.IsNullOrEmpty(name))
			{
				throw new ArgumentNullException(nameof(name));
			}

			if (string.IsNullOrEmpty(exe))
			{
				throw new ArgumentNullException(nameof(exe));
			}

			// this has to be a relative path otherwise Windows will enforce the process launcher whitelist
			_runtimeExecutablePath = $".\\Assets\\{name}\\{exe}";
			
			if (!File.Exists(_runtimeExecutablePath))
			{
				throw new FileNotFoundException($"Could not find runtime executable at {_runtimeExecutablePath}");
			}

			ExecutableExtension = executableExtension;
			Name = name;
		}

		public bool CanExecuteFile(string filename)
		{
			return ExecutableExtension.Equals(Path.GetExtension(filename), StringComparison.OrdinalIgnoreCase);
		}
		
		public void Start(string scriptPath, string workingDirectory)
		{
			if (_currentRuntimeExecution != null)
			{
				throw new InvalidOperationException("Only one execution per runtime is allowed.");
			}

			// TODO: need to support starting with more arguments
			string args = scriptPath;
			
			// create the execution wrapper
			_currentRuntimeExecution = new ExternalProcess(_runtimeExecutablePath, args, workingDirectory);

			// subscribe to all of the events
			_currentRuntimeExecution.StandardOutputAvailable += CurrentRuntimeExecution_StandardOutputAvailable;
			_currentRuntimeExecution.StandardErrorAvailable += CurrentRuntimeExecution_StandardErrorAvailable;
			_currentRuntimeExecution.Completed += CurrentRuntimeExecution_Completed;
			_currentRuntimeExecution.Faulted += CurrentRuntimeExecution_Faulted;

			// start execution of the runtime 
			_currentRuntimeExecution.Start();
		}

		private void CurrentRuntimeExecution_Faulted(object sender, Exception e)
		{
			ExecutionFaulted?.Invoke(this, e);

			DisposeCurrentExecution();
		}

		private void CurrentRuntimeExecution_Completed(object sender, uint e)
		{
			ExecutionComplete?.Invoke(this, e);

			DisposeCurrentExecution();
		}

		private void CurrentRuntimeExecution_StandardErrorAvailable(object sender, string e)
		{
			StandardErr?.Invoke(this, e);
		}

		private void CurrentRuntimeExecution_StandardOutputAvailable(object sender, string e)
		{
			StandardOut?.Invoke(this, e);
		}

		private void DisposeCurrentExecution()
		{
			_currentRuntimeExecution.StandardOutputAvailable -= CurrentRuntimeExecution_StandardOutputAvailable;
			_currentRuntimeExecution.StandardErrorAvailable -= CurrentRuntimeExecution_StandardErrorAvailable;
			_currentRuntimeExecution.Completed -= CurrentRuntimeExecution_Completed;
			_currentRuntimeExecution.Faulted -= CurrentRuntimeExecution_Faulted;

			_currentRuntimeExecution.Dispose();

			_currentRuntimeExecution = null;
		}
	}
}
