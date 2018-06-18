using System;
using System.Runtime.InteropServices;
using HypervisorSharp;
using PaleFlag.Devices;
using PaleFlag.XboxKernel;
using static System.Console;
using static PaleFlag.Globals;

namespace PaleFlag {
	public class Xbox {
		public const uint KernelCallsBase = 0xFC000000U;
		
		public readonly CpuCore Cpu;
		public readonly Kernel Kernel;
		public readonly PageManager PageManager;
		public readonly HandleManager HandleManager = new HandleManager();
		public readonly ThreadManager ThreadManager;
		public readonly MemoryAllocator MemoryAllocator;
		public readonly MmioManager MmioManager;
		public readonly Vfs Vfs;

		public readonly (uint TlsStart, uint TlsEnd, uint TlsZerofill, uint TlsIndexAddr) Tls;
		
		public Xbox(string fn, bool debug) {
			Cpu = new CpuCore(this);
			Kernel = new Kernel(this);
			ThreadManager = new ThreadManager(this);
			MemoryAllocator = new MemoryAllocator(this);
			PageManager = new PageManager(Cpu);
			MmioManager = new MmioManager();

			Vfs = new Vfs(this);
			
			var xbe = new Xbe(fn);
			var (ep, tlsStart, tlsEnd, tlsZerofill, tlsIndexAddr) = xbe.Load(Cpu);
			Tls = (tlsStart, tlsEnd, tlsZerofill, tlsIndexAddr);

			ThreadManager.Add(ep, MemoryAllocator.Allocate(32768) + 32768);

			SetupKernelThunk();
			SetupHack();
			
			guest<ushort>(0x6f5e7).Value = 0xfeeb;

			if(debug)
				Cpu.SetupDebugger();
		}

		void SetupHack() {
			// Some XBEs appear to try to patch kernel stuff,
			// but this hack is enough to terminate that safely

			var phys = PageManager.AllocPhysPages(17);
			var virt = PageManager.AllocVirtPages(17, at: 0x80000000);
			Cpu.MapPages(virt, phys, 17, true);

			var hack = new GuestMemory<uint>(virt + 0x10000);
			var hack2 = MemoryAllocator.Allocate(0x20);
			hack[0x3c / 4] = unchecked(hack2 + 0x7FFF0000);
		}

		unsafe void SetupKernelThunk() {
			var ptr = (uint*) Cpu.CreatePhysicalPages(KernelCallsBase, 1);
			for(var i = 0; i < 400; ++i)
				ptr[i] = 0xccc1010f; // vmcall; int 3 // latter should never be hit
			var launchDataPage = MemoryAllocator.Allocate(0x3400);
			WriteLine($"Launch data page at {launchDataPage:X}");
			ptr[0xa4] = launchDataPage;
			Cpu.MapPages(KernelCallsBase, KernelCallsBase, 1, true);
			ptr[0x142] = 0xDEADBEEF; // XboxHardwareInfo
			var imageName = "xboxdash.xbe";
			// XXX: this should be 0x146.  Wtf? 0002CA8B in dash
			ptr[0x147] = MemoryAllocator.Allocate((uint) Extensions.SizeOf(typeof(AnsiString)) + (uint) imageName.Length + 1); // XeImageFileName
			var str = new GuestMemory<AnsiString>(ptr[0x147]);
			str.Value = new AnsiString {
				Buffer = ptr[0x147] + (uint) Extensions.SizeOf(typeof(AnsiString)), 
				Length = (ushort) imageName.Length, 
				MaxLength = (ushort) (imageName.Length + 1)
			};
			var gm = new GuestMemory<byte>(str.Value.Buffer);
			for(var i = 0; i < imageName.Length; ++i)
				gm[i] = (byte) imageName[i];
			gm[imageName.Length] = 0;
		}

		public void Start() {
			ThreadManager.Next();
			Cpu.Run();
		}

		public GuestMemory<T> New<T>() where T : struct =>
			new GuestMemory<T>(MemoryAllocator.Allocate((uint) Marshal.SizeOf<T>()));
		public GuestMemory<T> New<T>(T value) where T : struct =>
			new GuestMemory<T>(MemoryAllocator.Allocate((uint) Marshal.SizeOf<T>())) { Value = value };
	}
}