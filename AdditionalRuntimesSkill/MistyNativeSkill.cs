using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MistyRobotics.Common;
using MistyRobotics.SDK.Commands;
using MistyRobotics.Common.Data;
using MistyRobotics.SDK.Events;
using MistyRobotics.SDK.Responses;
using MistyRobotics.Common.Types;
using MistyRobotics.SDK;
using MistyRobotics.SDK.Messengers;
using System.IO;
using Windows.ApplicationModel;
using Windows.Storage;
using AdditionalRuntimesSkill.Runtime;

namespace AdditionalRuntimesSkill
{
	internal class MistyNativeSkill : IMistySkill
	{
		private readonly RuntimeManager _runtimeManager = new RuntimeManager();

		private IRobotMessenger _misty;

		/// <summary>
		/// Skill details for the robot
		/// 
		/// There are other parameters you can set if you want:
		///   Description - a description of your skill
		///   TimeoutInSeconds - timeout of skill in seconds
		///   StartupRules - a list of options to indicate if a skill should start immediately upon startup
		///   BroadcastMode - different modes can be set to share different levels of information from the robot using the 'SkillData' websocket
		///   AllowedCleanupTimeInMs - How long to wait after calling OnCancel before denying messages from the skill and performing final cleanup  
		/// </summary>
		public INativeRobotSkill Skill { get; private set; } = new NativeRobotSkill("AdditionalRuntimesSkill", "0ff16d00-7f5c-4dad-9b6e-e832a406c117");

		public MistyNativeSkill()
		{
			_runtimeManager.StandardOut += Runtime_StandardOut;
			_runtimeManager.StandardErr += Runtime_StandardErr;
			_runtimeManager.RuntimeCompleted += Runtime_Completed;
			_runtimeManager.RuntimeCancelled += Runtime_Cancelled;
			_runtimeManager.RuntimeFaulted += Runtime_Faulted;
		}

		private void Runtime_Cancelled(object sender, string e)
		{
			if (_misty != null)
			{
				_misty.SendDebugMessage($"Runtime '{e}' was cancelled.", null);
			}
		}

		private void Runtime_Faulted(object sender, (string, Exception) e)
		{
			if (_misty != null)
			{
				_misty.SendDebugMessage($"Runtime '{e.Item1}' faulted: {e.Item2}\n{e.Item2.StackTrace}", null);
			}
		}

		private void Runtime_Completed(object sender, (string, uint) e)
		{
			if (_misty != null)
			{
				_misty.SendDebugMessage($"Runtime '{e.Item1}' completed with exit code {e.Item2}", null);
			}
		}

		private void Runtime_StandardErr(object sender, (string, string) e)
		{
			if (_misty != null)
			{
				_misty.SendDebugMessage($"[{e.Item1.ToUpper()}][ERR] {e.Item2}", null);
			}
		}

		private void Runtime_StandardOut(object sender, (string, string) e)
		{
			if (_misty != null)
			{
				_misty.SendDebugMessage($"[{e.Item1.ToUpper()}][OUT] {e.Item2}", null);
			}
		}

		/// <summary>
		///	This method is called by the wrapper to set your robot interface
		///	You need to save this off in the local variable commented on above as you are going use it to call the robot
		/// </summary>
		/// <param name="robotInterface"></param>
		public void LoadRobotConnection(IRobotMessenger robotInterface)
		{
			_misty = robotInterface;
		}

		/// <summary>
		/// This event handler is called when the robot/user sends a start message
		/// The parameters can be set in the Skill Runner (or as json) and used in the skill if desired
		/// </summary>
		/// <param name="parameters"></param>
		public void OnStart(object sender, IDictionary<string, object> parameters)
		{
			HandleStartRuntimeRequest(new Dictionary<string, object>
			{
				{ "RuntimeName", "Python" },
				{ "Directory", "pytest" },
				{ "EntryPoint", "justins_test.py" }
			});
		}

		private void Misty_UserEventReceived(object sender, IUserEvent e)
		{
			if (e.EventName == "Runtime.Start")
			{
				HandleStartRuntimeRequest(e.Data);
			}
		}

		/// <summary>
		/// This event handler is called when Pause is called on the skill
		/// User can save the skill status/data to be retrieved when Resume is called
		/// Infrastructure to help support this still under development, but users can implement this themselves as needed for now 
		/// </summary>
		/// <param name="parameters"></param>
		public void OnPause(object sender, IDictionary<string, object> parameters)
		{
			//In this template, Pause is not implemented by default
		}

		/// <summary>
		/// This event handler is called when Resume is called on the skill
		/// User can restore any skill status/data and continue from Paused location
		/// Infrastructure to help support this still under development, but users can implement this themselves as needed for now 
		/// </summary>
		/// <param name="parameters"></param>
		public void OnResume(object sender, IDictionary<string, object> parameters)
		{
			//TODO Put your code here and update the summary above
		}
		
		/// <summary>
		/// This event handler is called when the cancel command is issued from the robot/user
		/// You currently have a few seconds to do cleanup and robot resets before the skill is shut down... 
		/// Events will be unregistered for you 
		/// </summary>
		public void OnCancel(object sender, IDictionary<string, object> parameters)
		{
			//TODO Put your code here and update the summary above
		}

		/// <summary>
		/// This event handler is called when the skill timeouts
		/// You currently have a few seconds to do cleanup and robot resets before the skill is shut down... 
		/// Events will be unregistered for you 
		/// </summary>
		public void OnTimeout(object sender, IDictionary<string, object> parameters)
		{
			//TODO Put your code here and update the summary above
		}

		public void OnResponse(IRobotCommandResponse response)
		{
			Debug.WriteLine("Response: " + response.ResponseType.ToString());
		}

		private async void HandleStartRuntimeRequest(IDictionary<string, object> dict)
		{
			IReadOnlyList<string> validationErrors = StartRuntimeParameters.Validate(dict);
			if (validationErrors.Count > 0)
			{
				string message = "StartRuntime request failed validation: \n" + string.Join("\n", validationErrors);
				await _misty.SendDebugMessageAsync(message);
			}
			else
			{
				StartRuntimeParameters parameters = StartRuntimeParameters.Extract(dict);
				try
				{
					_runtimeManager.StartRuntime(parameters.RuntimeName, parameters.Directory, parameters.EntryPoint);
				}
				catch (Exception ex)
				{
					SendDebugExceptionAsync("Unexpected exception occurred while trying to start runtime.", ex);
				}
			}
		}

		private async void SendDebugExceptionAsync(string message, Exception ex)
		{
			await _misty.SendDebugMessageAsync(message);
			await _misty.SendDebugMessageAsync($"{ex}\n{ex.StackTrace}");
		}

		#region IDisposable Support
		private bool _isDisposed = false;

		private void Dispose(bool disposing)
		{
			if (!_isDisposed)
			{
				if (disposing)
				{
					// TODO: dispose managed state (managed objects).
				}

				// TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
				// TODO: set large fields to null.

				_isDisposed = true;
			}
		}

		// TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
		// ~MistyNativeSkill() {
		//   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
		//   Dispose(false);
		// }

		// This code added to correctly implement the disposable pattern.
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(true);
			// TODO: uncomment the following line if the finalizer is overridden above.
			// GC.SuppressFinalize(this);
		}
		#endregion
	}
}
