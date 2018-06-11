using HypervisorSharp;

namespace PaleFlag {
	public class Xbox {
		public readonly CpuCore Cpu;
		
		public Xbox() {
			Cpu = new CpuCore();
			Cpu.MapPages(0x10000, 0, 1, true);
			var mem = Cpu.Physical<byte>(0);
			for(var i = 0; i < 16; ++i)
				mem[i] = 0x90;
			mem[0x10] = 0xCC;
			Cpu[HvReg.RIP] = 0x10000;
			Cpu[HvReg.RFLAGS] = 2;
		}

		public void Start() => Cpu.Run();
	}
}