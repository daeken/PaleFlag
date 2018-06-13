using System;
using HypervisorSharp;
using PaleFlag.XboxKernel;
using static System.Console;
using static PaleFlag.Globals;

namespace PaleFlag {
	public class Xbox {
		public const uint KernelCallsBase = 0xFF000000U;
		
		public readonly CpuCore Cpu;
		public readonly Kernel Kernel;
		public readonly PageManager PageManager;
		public readonly HandleManager HandleManager = new HandleManager();
		public readonly ThreadManager ThreadManager;
		public readonly MemoryAllocator MemoryAllocator;

		public readonly (uint TlsStart, uint TlsEnd, uint TlsZerofill) Tls;
		
		public Xbox(string fn) {
			Cpu = new CpuCore(this);
			Kernel = new Kernel(this);
			ThreadManager = new ThreadManager(this);
			MemoryAllocator = new MemoryAllocator(this);
			PageManager = new PageManager(Cpu);

			var xbe = new Xbe(fn);
			var (ep, tlsStart, tlsEnd, tlsZerofill) = xbe.Load(Cpu);
			Tls = (tlsStart, tlsEnd, tlsZerofill);

			ThreadManager.Add(ep, MemoryAllocator.Allocate(32768) + 32768);

			SetupKernelThunk();
			
			guest<ushort>(0x6f5e7).Value = 0xfeeb;
			//guest<byte>(0x74A2F).Value = 0xcc;
			//guest<uint>(0x6F577).Value = 0xcc01010f;
		}

		unsafe void SetupKernelThunk() {
			var ptr = (uint*) Cpu.CreatePhysicalPages(KernelCallsBase, 1);
			for(var i = 0; i < 400; ++i)
				ptr[i] = 0xccc1010f; // vmcall; int 3 // latter should never be hit
			Cpu.MapPages(KernelCallsBase, KernelCallsBase, 1, true);
		}

		public void Start() {
			ThreadManager.Next();
			Cpu.Run();
		}
	}
}