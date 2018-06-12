using System;

namespace PaleFlag.Kernel {
	public class Hal : IKernel {
		[Kernel(0x31)]
		static void HalReturnToFirmware() {
			Console.WriteLine("HalReturnToFirmware");
			Environment.Exit(0);
		}
	}
}