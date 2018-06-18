using System;

namespace PaleFlag.XboxKernel {
	public enum NtStatus : uint {
		Success = 0, 
		NoMediaInDevice = 0xC0000013, 
		ObjectNameNotFound = 0xC0000034
	}

	public partial class Kernel {
		[Export(0x12D)]
		uint RtlNtStatusToDosError(NtStatus status) {
			switch(status) {
				case NtStatus.Success: return 0;
				case NtStatus.NoMediaInDevice: return 0x00000458;
				default:
					Console.WriteLine($"Unknown NtStatus {status:X} in RtlNtStatusToDosError");
					return 0;
			}
		}
	}
}