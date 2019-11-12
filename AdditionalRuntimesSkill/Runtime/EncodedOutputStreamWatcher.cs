using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage.Streams;

namespace AdditionalRuntimesSkill.Runtime
{
	public sealed class EncodedOutputStreamWatcher : IOutputStream
	{
		private const int BufferSize = 2048;

		private readonly Encoding _encoding;
		
		public event EventHandler<string> StringAvailable;

		public EncodedOutputStreamWatcher()
		{
			//TODO: allow the runtime to set it's encoding
			_encoding = Encoding.ASCII;
		}

		public IAsyncOperationWithProgress<uint, uint> WriteAsync(IBuffer buffer)
		{
			// intercept the write request and raise an event
			return AsyncInfo.Run<uint, uint>((ct, prog) =>
			{
				if (buffer.Length > 0)
				{
					// read the buffer into a byte array
					byte[] bytes = buffer.ToArray();
					
					// convert the byte buffer to characters
					char[] chars = _encoding.GetChars(bytes);

					// convert the characters to a string
					string str = new string(chars);

					// raise an event with the newly constructed string
					StringAvailable?.Invoke(null, str);
				}

				return Task.FromResult(buffer.Length);
			});
		}

		public IAsyncOperation<bool> FlushAsync()
		{
			// since we are intercepting the write requests, there is nothing to flush
			return AsyncInfo.Run<bool>(ct => Task.FromResult(true));
		}

		public void Dispose()
		{
			// NO-OP, nothing to clean up
		}
	}
}
