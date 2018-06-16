using System.Linq;
using System.Text;

namespace PaleFlag.XboxKernel {
	public struct AnsiString {
		public ushort Length, MaxLength;
		public GuestMemory<byte> Buffer;

		public string GetString() {
			var gm = Buffer;
			return Encoding.ASCII.GetString(Enumerable.Range(0, Length).Select(i => gm[i]).ToArray());
		}
	}
	
	public struct UnicodeString {
		public ushort Length, MaxLength;
		public GuestMemory<byte> Buffer;

		public string GetString() {
			var gm = Buffer;
			return Encoding.Unicode.GetString(Enumerable.Range(0, Length).Select(i => gm[i]).ToArray());
		}
	}
	
	public partial class Kernel {
		int PStrlen(GuestMemory<byte> gm) {
			var len = 0;
			while(gm[len] != 0)
				len++;
			return len;
		}
		
		[Export(0x121)]
		void RtlInitAnsiString(out AnsiString dest, GuestMemory<byte> source) {
			var slen = source == 0 ? 0 : PStrlen(source);
			dest = new AnsiString {
				Buffer = source, 
				Length = (ushort) slen, 
				MaxLength = (ushort) (slen + 1)
			};
		}
	}
}