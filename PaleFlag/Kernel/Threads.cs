using System;

namespace PaleFlag.XboxKernel {
	public partial class Kernel {
		[Export(0xFF)]
		NtStatus PsCreateSystemThreadEx(
			GuestMemory<uint> threadHandle, uint threadExtraSize, uint kernelStackSize, uint tlsDataSize, GuestMemory<uint> threadId, 
			uint startContext1, uint startContext2, bool createSuspended, bool debugStack, uint startRoutine
		) {
			var sp = Box.MemoryAllocator.Allocate(32768) + 32768;
			sp -= 12;
			new GuestMemory<uint>(sp) {
				[0] = 0xDEADBEEFU,
				[1] = startContext1,
				[2] = startContext2
			};
			var thread = Box.ThreadManager.Add(startRoutine, sp);
			thread.Ebp = sp + 4;
			threadHandle.Value = Box.HandleManager.Add(thread);
			if(threadId.GuestAddr != 0) threadId.Value = thread.Id;
			
			Tib.Create(Box, thread);

			return NtStatus.Success;
		}
	}
}