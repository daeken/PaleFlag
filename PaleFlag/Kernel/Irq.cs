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
	}
}