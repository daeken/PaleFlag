using System;
using System.Runtime.InteropServices;

namespace PaleFlag.XboxKernel {
	public partial class Kernel {
		[StructLayout(LayoutKind.Sequential)]
		struct RtlCriticalSection {
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
			public uint[] Unknown;

			public int LockCount, RecursionCount;
			public uint OwningThread;
		}
		
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

		[Export(0x123)]
		NtStatus RtlInitializeCriticalSection(GuestMemory<RtlCriticalSection> crit) {
			if(crit.Value.Unknown[0] == 0xDEADBEEF)
				return NtStatus.Success;
			crit.Value = new RtlCriticalSection {
				Unknown = new[] { 0xDEADBEEFU, 0U, 0U, 0U }, 
				LockCount = -1, 
				RecursionCount = 0, 
				OwningThread = Box.ThreadManager.Current.Id
			};
			return NtStatus.Success;
		}

		[Export(0x115)]
		NtStatus RtlEnterCriticalSection(GuestMemory<RtlCriticalSection> crit) {
			return NtStatus.Success;
		}
		
		[Export(0x126)]
		NtStatus RtlLeaveCriticalSection(GuestMemory<RtlCriticalSection> crit) {
			return NtStatus.Success;
		}
	}
}