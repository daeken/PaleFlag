using HypervisorSharp;
using static System.Console;

namespace PaleFlag {
	public class Xbox {
		public readonly CpuCore Cpu;
		public readonly PageManager PageManager;
		
		public Xbox(string fn) {
			Cpu = new CpuCore();
			PageManager = new PageManager(Cpu);

			var pcbase = PageManager.AllocPhysPages(1);
			var vcbase = PageManager.AllocVirtPages(1);
			WriteLine($"Allocated pages at {pcbase:X} phys {vcbase:X} virt");
			Cpu.MapPages(vcbase, pcbase, 1, true);
			
			PageManager.Write(vcbase, 0xCC90909090909090U);
			WriteLine($"Foo? {PageManager.Read<ulong>(vcbase):X}");
			
			Cpu[HvReg.RIP] = vcbase;
			Cpu[HvReg.RFLAGS] = 2;
			
			var xbe = new Xbe(fn);
		}

		public void Start() => Cpu.Run();
	}
}