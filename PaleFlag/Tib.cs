using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
#pragma warning disable 414
#pragma warning disable 169
#pragma warning disable 649

namespace PaleFlag {
	[StructLayout(LayoutKind.Sequential)]
	struct Kthread {
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x28)]
		byte[] UnknownA;

		public uint TlsData;

		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 0xE4)]
		byte[] UnknownB;
	}

	[StructLayout(LayoutKind.Sequential)]
	struct Ethread {
		public Kthread Kthread;
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x110)]
		byte[] UnknownA;
		public uint UniqueThread;
	}
	
	[StructLayout(LayoutKind.Sequential)]
	struct NtTib {
		public uint ExceptionList, StackBase, StackLimit, SubSystemTib;
		public uint FiberData;
		public uint ArbitraryUserPointer;
		public uint Self;
	}

	[StructLayout(LayoutKind.Sequential)]
	struct Kprcb {
		public GuestMemory<Ethread> CurrentThread, NextThread, IdleThread;
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x250)]
		byte[] Unknown;
	}
	
	[StructLayout(LayoutKind.Sequential)]
	struct Kpcr {
		public NtTib NtTib;
		public uint Self;
		public GuestMemory<Kprcb> Prcb;
		public uint Irql;
		public Kprcb PrcbData;
	}
	
	public static class Tib {
		public static void Create(Xbox box, Thread thread) {
			var copy = box.Tls.TlsEnd - box.Tls.TlsStart;
			var tlsAddr = box.MemoryAllocator.Allocate(copy + box.Tls.TlsZerofill + 0xF) + 4;
			while((tlsAddr & 0xF) != 0)
				tlsAddr++;
			tlsAddr -= 4;
			var tls = new GuestMemory<byte>(tlsAddr);
			var gtls = new GuestMemory<byte>(box.Tls.TlsStart);
			for(var i = 0; i < copy; ++i)
				tls[i] = gtls[i];
			for(var i = 0; i < box.Tls.TlsZerofill; ++i)
				tls[(int) copy + i] = 0;
			
			var ethread = new GuestMemory<Ethread>(box.MemoryAllocator.Allocate((uint) Marshal.SizeOf<Ethread>())) {
				Value = new Ethread { UniqueThread = thread.Id, Kthread = new Kthread { TlsData = tlsAddr } }
			};
			var tib = new GuestMemory<Kpcr>(box.MemoryAllocator.Allocate((uint) Marshal.SizeOf<Kpcr>()));
			tib.Value = new Kpcr {
				NtTib = new NtTib { StackBase = /*thread.Esp - 32768*/ tlsAddr, Self = tib }, 
				Self = tib, 
				Prcb = tib + 0x28, 
				PrcbData = new Kprcb {
					CurrentThread = ethread
				}
			};
			Debug.Assert(tib.Value.NtTib.Self == tib);

			thread.Tib = tib;
		}
	}
}