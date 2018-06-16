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
			out uint threadHandle, uint threadExtraSize, uint kernelStackSize, uint tlsDataSize, GuestMemory<uint> threadId, 
			uint startContext1, uint startContext2, bool createSuspended, bool debugStack, uint startRoutine
		) {
			var sp = Box.MemoryAllocator.Allocate(32768) + 32768;
			Console.WriteLine($"Creating new thread with stack top at {sp:X}");
			sp -= 12;
			new GuestMemory<uint>(sp) {
				[0] = 0xDEADBEEFU,
				[1] = startContext1,
				[2] = startContext2
			};
			var thread = Box.ThreadManager.Add(startRoutine, sp);
			thread.Ebp = sp + 4;
			threadHandle = Box.HandleManager.Add(thread);
			if(threadId.GuestAddr != 0) threadId.Value = thread.Id;
			
			Tib.Create(Box, thread);

			return NtStatus.Success;
		}

		[Export(0x123)]
		NtStatus RtlInitializeCriticalSection(ref RtlCriticalSection crit) {
			if(crit.Unknown[0] == 0xDEADBEEF)
				return NtStatus.Success;
			crit = new RtlCriticalSection {
				Unknown = new[] { 0xDEADBEEFU, 0U, 0U, 0U }, 
				LockCount = -1, 
				RecursionCount = 0, 
				OwningThread = Box.ThreadManager.Current.Id
			};
			return NtStatus.Success;
		}

		[Export(0x115)]
		NtStatus RtlEnterCriticalSection(ref RtlCriticalSection crit) {
			return NtStatus.Success;
		}
		
		[Export(0x126)]
		NtStatus RtlLeaveCriticalSection(ref RtlCriticalSection crit) {
			return NtStatus.Success;
		}
	}
}