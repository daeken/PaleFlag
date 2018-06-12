using System;
using HypervisorSharp;
using static System.Console;

namespace PaleFlag {
	public class Xbox {
		public const uint KernelCallsBase = 0xFF000000U;
		
		public readonly CpuCore Cpu;
		public readonly PageManager PageManager;
		
		public Xbox(string fn) {
			Cpu = new CpuCore { [HvReg.RFLAGS] = 2 };
			PageManager = new PageManager(Cpu);

			var xbe = new Xbe(fn);
			Cpu[HvReg.RIP] = xbe.Load(Cpu);

			var sp = PageManager.AllocVirtPages(8);
			Cpu.MapPages(sp, PageManager.AllocPhysPages(8), 8, true);
			Cpu[HvReg.RSP] = sp + 8 * 4096;

			SetupKernelThunk();
		}

		unsafe void SetupKernelThunk() {
			var ptr = (uint*) Cpu.CreatePhysicalPages(KernelCallsBase, 1);
			for(var i = 0; i < 400; ++i)
				ptr[i] = 0xccc1010f; // vmcall; int 3 // latter should never be hit
			Cpu.MapPages(KernelCallsBase, KernelCallsBase, 1, true);
		}

		public void Start() => Cpu.Run();
	}
}