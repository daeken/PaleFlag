using PaleFlag.XboxKernel;

namespace PaleFlag.Devices {
	public class CdromDeviceFile : IFileHandle {
		public uint Handle { get; set; }
		public void Close() { }

		public string Name => "CDROM0:";

		public NtStatus Ioctl(uint code, byte[] ibuf, byte[] obuf) => NtStatus.NoMediaInDevice;

		public int Read(byte[] buf, int count) => throw new System.NotImplementedException();
		public void Write(byte[] buf, int count) => throw new System.NotImplementedException();
	}

	public class CdromDeviceNode : VfsDeviceNode {
		public CdromDeviceNode(string fullPath) : base(fullPath) {}

		public override IFileHandle OpenFile() => new CdromDeviceFile();
	}
}