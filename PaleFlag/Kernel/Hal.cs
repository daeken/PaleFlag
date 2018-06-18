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

		[Export(0x09)]
		void HalReadSMCTrayState(GuestMemory<uint> count, out uint state) {
			if(count) count.Value = 1;
			state = 16; // Tray open
		}

		[Export(0x2C)]
		uint HalGetInterruptVector(uint busInterruptLevel, out byte irql) {
			irql = 0;
			return 0;
		}
	}
}