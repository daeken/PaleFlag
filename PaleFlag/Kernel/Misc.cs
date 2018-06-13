using System.Runtime.InteropServices;

namespace PaleFlag.XboxKernel {
	public partial class Kernel {
		[StructLayout(LayoutKind.Sequential)]
		struct ListEntry {
			public uint Flink, Blink; // ListEntry*
		}
		
		[Export(0xBB)]
		NtStatus NtClose(uint handle) {
			Box.HandleManager.Close(handle);
			return NtStatus.Success;
		}

		[StructLayout(LayoutKind.Sequential)]
		struct DispatcherHeader {
			public byte Type, Absolute, Size, Inserted;
			public int SignalState;
			public ListEntry WaitListHead;
		}
		
		[StructLayout(LayoutKind.Sequential)]
		struct Ktimer {
			public DispatcherHeader Header;
			public ulong DueTime;
			public ListEntry TimerListEntry;
			public GuestMemory<Kdpc> Dpc;
			public int Period;
		}

		enum TimerType : uint {
			NotificationTimer = 0, 
			SynchronizationTimer = 1
		}

		[Export(0x71)]
		void KeInitializeTimerEx(GuestMemory<Ktimer> timer, TimerType type) {
			timer.Value = new Ktimer {
				Header = new DispatcherHeader {
					Type = (byte) (type + 8), 
					Size = 10, 
					WaitListHead = new ListEntry {
						Flink = timer + 8, 
						Blink = timer + 8
					}
				}
			};
		}

		[Export(0x95)]
		bool KeSetTimer(GuestMemory<Ktimer> timer, ulong dueTime, GuestMemory<Kdpc> dpc) {
			return true;
		}

		[Export(0x18)]
		NtStatus ExQueryNonVolatileSetting(uint valueIndex, GuestMemory<uint> type, GuestMemory<uint> value, uint valueLength, GuestMemory<uint> resultLength) {
			if(type)
				type.Value = 4;
			if(value)
				value.Value = 0;
			if(resultLength)
				resultLength.Value = 4;
			return NtStatus.Success;
		}
	}
}