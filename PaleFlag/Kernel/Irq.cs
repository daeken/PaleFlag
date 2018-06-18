using System.Runtime.InteropServices;

namespace PaleFlag.XboxKernel {
	public partial class Kernel {
		[Export(0x81)]
		byte KeRaiseIrqlToDpcLevel() {
			return 0;
		}

		[Export(0xA1, CallingConvention.Fastcall)]
		void KfLowerIrql(byte newIrql) {
			// XXX: Implement
		}

		enum InterruptMode {
			LevelSensitive, 
			Latched
		}

		struct Kinterrupt {
			public uint ServiceRoutine, ServiceContext;
			public uint BusInterruptLevel, Irql;
			public bool Connected, ShareVector;
			public InterruptMode Mode;
			public uint ServiceCount;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 22)]
			public uint[] DispatchCode;
		}

		[Export(0x6D)]
		void KeInitializeInterrupt(out Kinterrupt interrupt, uint serviceRoutine, uint serviceContext, uint vector, byte irql, InterruptMode interruptMode, bool shareVector) {
			interrupt = new Kinterrupt {
				ServiceRoutine = serviceRoutine, 
				ServiceContext = serviceContext, 
				Irql = irql, 
				Connected = false, 
				Mode = interruptMode
			};
		}

		[Export(0x62)]
		void KeConnectInterrupt(ref Kinterrupt interrupt) {
			// XXX: Actually implement
		}
	}
}