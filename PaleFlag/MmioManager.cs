using static System.Console;

namespace PaleFlag {
	public class MmioManager {
		public uint Read(uint addr) {
			WriteLine($"Mmio read from {addr:X}");
			return 0;
		}

		public void Write(uint addr, uint value) {
			WriteLine($"Mmio write ({value:X}) to {addr:X}");
		}
	}
}