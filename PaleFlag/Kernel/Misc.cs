namespace PaleFlag.XboxKernel {
	public partial class Kernel {
		[Export(0xBB)]
		NtStatus NtClose(uint handle) {
			Box.HandleManager.Close(handle);
			return NtStatus.Success;
		}
	}
}