using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
		readonly XbeTls Tls;
		
		public Xbe(string fn) {
			Data = File.ReadAllBytes(fn);
			Header = Data.ToStruct<XbeHeader>();
			Debug.Assert(Header.Magic == 0x48454258);
			
			Sections = new XbeSection[Header.NumSects];
			for(var i = 0; i < Header.NumSects; ++i)
				Sections[i] = Data.ToStruct<XbeSection>((int) (Header.SectHdrs - Header.Base + i * Marshal.SizeOf<XbeSection>()));

			Tls = Data.ToStruct<XbeTls>((int) (Header.Tls - Header.Base));
		}

		public (uint EntryPoint, uint TlsStart, uint TlsEnd, uint TlsZerofill) Load(CpuCore cpu) {
			var highest = new[] { Header.Base + (uint) Data.Length }.Concat(Sections.Select(x => x.VAddr + x.VSize)).Aggregate(Math.Max);
			if((highest & 0xFFF) != 0)
				highest = (highest & 0xFFFFF000) + 0x1000;

			var tmp = Header.Oep ^ 0xA8FC57ABU;
			var retail = tmp >= Header.Base && tmp < highest;
			
			var pages = (int) (highest / 4096);
			var phys = PageManager.Instance.AllocPhysPages(pages);
			var virt = PageManager.Instance.AllocVirtPages(pages, at: Header.Base);
			cpu.MapPages(virt, phys, pages, true);
			WriteLine($"Physical at {phys:X} virt at {virt:X}");
			PageManager.Instance.Write(Header.Base, Data);
			WriteLine($"File at {Header.Base:X} - {Header.Base + Data.Length:X}");
			WriteLine($"Actual virtual top is {highest:X}");
			foreach(var section in Sections) {
				var name = Encoding.ASCII.GetString(Data, (int) (section.NameAddr - Header.Base), 24).Split('\0')[0];
				WriteLine($"- {name} {section.VAddr:X} {section.VSize:X} -- {section.RAddr:X} {section.RSize:X}");
				PageManager.Instance.Write(section.VAddr, Data.Skip((int) section.RAddr).Take((int) section.RSize).ToArray());
			}

			var thunk = Header.Thunk ^ (retail ? 0x5B6D40B6U : 0xEFB1F152U);
			for(var i = 0U; i < 400; ++i) {
				var addr = new GuestMemory<uint>(thunk + i * 4);
				var cur = addr.Value;
				if(cur == 0)
					break;
				addr.Value = Xbox.KernelCallsBase + (cur & 0x1FF) * 4;
			}
			
			return (Header.Oep ^ (retail ? 0xA8FC57ABU : 0x94859D4B), Tls.DataStart, Tls.DataEnd, Tls.ZeroFill);
		}
	}
}