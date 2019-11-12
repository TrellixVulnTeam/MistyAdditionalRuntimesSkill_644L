using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Windows.System;

namespace AdditionalRuntimesSkill.Runtime
{
	internal class ExternalProcess : IDisposable
	{
		private readonly InMemoryRandomAccessStream _inputStream;
		private readonly EncodedOutputStreamWatcher _standardOutWatcher;
		private readonly EncodedOutputStreamWatcher _standardErrWatcher;

		private readonly CancellationTokenSource _cancellationSource;

		private readonly string _command;
		private readonly string _args;
		private readonly string _workingDirectory;
		private Task _processExecutionTask;

		public bool IsRunning => !(_processExecutionTask?.IsCompleted ?? false);

		public event EventHandler<string> StandardOutputAvailable;

		public event EventHandler<string> StandardErrorAvailable;

		public event EventHandler<uint> Completed;

		public event EventHandler<Exception> Faulted;

		internal ExternalProcess(string command, string args, string workingDirectory)
		{
			_command = command;
			_args = args;
			_workingDirectory = workingDirectory;

			_inputStream = new InMemoryRandomAccessStream();
			_standardOutWatcher = new EncodedOutputStreamWatcher();
			_standardErrWatcher = new EncodedOutputStreamWatcher();

			_standardOutWatcher.StringAvailable += OnStandardOut;
			_standardErrWatcher.StringAvailable += OnStandardErr;

			_cancellationSource = new CancellationTokenSource();
		}

		public void Start()
		{
			if (_processExecutionTask != null)
			{
				return;
			}

			_processExecutionTask = ProcessLauncher
				.RunToCompletionAsync(_command, _args, new ProcessLauncherOptions
				{
					StandardInput = _inputStream,
					StandardOutput = _standardOutWatcher,
					StandardError = _standardErrWatcher,
					WorkingDirectory = _workingDirectory,
				})
				.AsTask(_cancellationSource.Token)
				.ContinueWith(OnProcessCompleted, TaskContinuationOptions.OnlyOnRanToCompletion)
				.ContinueWith(OnProcessFaulted, TaskContinuationOptions.OnlyOnFaulted);
		}

		public Task WaitForCompletionAsync()
		{
			return _processExecutionTask;
		}

		public void Cancel()
		{
			_cancellationSource.Cancel();
		}

		#region Internal Event Handlers
		private void OnProcessCompleted(Task<ProcessLauncherResult> task)
		{
			Completed?.Invoke(this, task.Result.ExitCode);
		}

		private void OnProcessFaulted(Task task)
		{
			Faulted?.Invoke(this, task.Exception);
		}

		private void OnStandardOut(object sender, string e)
		{
			StandardOutputAvailable?.Invoke(sender, e);
		}

		private void OnStandardErr(object sender, string e)
		{
			StandardErrorAvailable?.Invoke(sender, e);
		}
		#endregion

		#region IDisposable Support
		private bool _isDisposed = false;

		protected virtual void Dispose(bool disposing)
		{
			if (!_isDisposed)
			{
				if (disposing)
				{
					if (IsRunning)
					{
						_cancellationSource.Cancel();
					}

					_inputStream.Dispose();
					_standardOutWatcher.Dispose();
					_standardErrWatcher.Dispose();
					_cancellationSource.Dispose();
				}

				_standardOutWatcher.StringAvailable -= OnStandardOut;
				_standardErrWatcher.StringAvailable -= OnStandardErr;

				_isDisposed = true;
			}
		}

		public void Dispose()
		{
			Dispose(true);
		}
		#endregion
	}
}
