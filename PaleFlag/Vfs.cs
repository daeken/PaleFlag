namespace PaleFlag {
	public interface IFileHandle : IHandle {
		string Name { get; }
		bool Ioctl(uint code, byte[] ibuf, byte[] obuf);
		int Read(byte[] buf, int count);
		void Write(byte[] buf, int count);
	}
	
	public class Vfs {
		
	}
}