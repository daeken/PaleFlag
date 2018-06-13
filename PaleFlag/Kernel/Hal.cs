using System;

namespace PaleFlag.XboxKernel {
	public partial class Kernel {
		[Export(0x31)]
		void HalReturnToFirmware() {
			Console.WriteLine("HalReturnToFirmware");
			Environment.Exit(0);
		}

		[Export(0x2F)]
		void HalRegisterShutdownNotification(uint shutdownRegistration, bool register) {
		}
	}
}