using System.Collections.Generic;
using PaleFlag.XboxKernel;

namespace PaleFlag {
	public interface IFileHandle : IHandle {
		string Name { get; }
		NtStatus Ioctl(uint code, byte[] ibuf, byte[] obuf);
		int Read(byte[] buf, int count);
		void Write(byte[] buf, int count);
	}
	
	public class Vfs {
		readonly Xbox Box;
		readonly Dictionary<string, IFileHandle> DeviceFiles = new Dictionary<string, IFileHandle>();

		public Vfs(Xbox box) => Box = box;

		public void AddDeviceFile(string name, IFileHandle handle) => DeviceFiles[name] = handle;
		
		public IFileHandle Open(string name) {
			return Box.HandleManager.Register(DeviceFiles[name]);
		}
	}
}