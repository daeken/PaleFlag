using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using static System.Console;

namespace PaleFlag {
	[StructLayout(LayoutKind.Sequential)]
	public struct XbeHeader {
		public uint Magic;
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
		public byte[] Signature;

		public uint Base, SoH, SoI, SoIH, Timedate, CertAddr, NumSects, SectHdrs, Flags;
		public uint Oep, Tls, StackCommit, HeapReserve, HeapCommit, PeBase, PeSoI, PeCsum, PeTimedate;
		public uint DebugPathname, DebugFilename, DebugUFilename, Thunk, Imports, NumVers, LibVers, KVers, XapiVers;
		public uint LogoAddr, LogoSize;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct XbeSection {
		public uint Flags, VAddr, VSize, RAddr, RSize;
		public uint NameAddr, NameRef, HeadRef, TailRef;
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
		public byte[] Digest;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct XbeTls {
		public uint DataStart, DataEnd, Index, Callback;
		public uint ZeroFill, Characteristics;
	}
	
	public class Xbe {
		readonly byte[] Data;
		readonly XbeHeader Header;
		readonly XbeSection[] Sections;
		
		public Xbe(string fn) {
			Data = File.ReadAllBytes(fn);
			Header = Data.ToStruct<XbeHeader>();
			Debug.Assert(Header.Magic == 0x48454258);
			
			Sections = new XbeSection[Header.NumSects];
			for(var i = 0; i < Header.NumSects; ++i) {
				Sections[i] = Data.ToStruct<XbeSection>((int) (Header.SectHdrs - Header.Base + i * Marshal.SizeOf<XbeSection>()));
			}
		}

		public void Load(CpuCore cpu) {
			for(var i = 0; i < Sections.Length; ++i) {
				var name = Encoding.ASCII.GetString(Data, (int) (Sections[i].NameAddr - Header.Base), 8);
				WriteLine($"");
			}
		}
	}
}