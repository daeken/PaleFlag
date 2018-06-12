using System;

namespace PaleFlag.XboxKernel {
	public partial class Kernel {
		[Export(0x31)]
		void HalReturnToFirmware() {
			Console.WriteLine("HalReturnToFirmware");
			Environment.Exit(0);
		}
	}
}