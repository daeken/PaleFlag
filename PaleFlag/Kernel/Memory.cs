using System;

namespace PaleFlag.XboxKernel {
	public partial class Kernel {
		[Flags]
		enum AllocationType {
			Commit = 0x1000, 
			Reserve = 0x2000, 
			Decommit = 0x4000, 
			Release = 0x8000, 
			Free = 0x10000, 
			Private = 0x20000, 
			Mapped = 0x40000, 
			Reset = 0x80000, 
			TopDown = 0x100000
		}

		[Export(0xB8)]
		NtStatus NtAllocateVirtualMemory(ref uint baseAddress, GuestMemory<uint> zeroBits,
			ref uint regionSize, AllocationType allocationType, uint protect) {
			baseAddress &= 0xFFFFF000;
			if((regionSize & 0xFFF) != 0) regionSize = (regionSize & 0xFFFFF000) + 4096;
			Console.WriteLine($"Allocating 0x{regionSize:X} bytes at {baseAddress:X} ({allocationType})");
			
			if(allocationType.HasFlag(AllocationType.Commit) || allocationType.HasFlag(AllocationType.Reserve)) {
				if(baseAddress != 0 && Box.Cpu.IsMapped(baseAddress))
					return NtStatus.Success;
				var virt = Box.PageManager.AllocVirtPages((int) regionSize / 4096, at: baseAddress != 0 ? (uint?) baseAddress : null);
				var phys = Box.PageManager.AllocPhysPages((int) regionSize / 4096);
				Box.Cpu.MapPages(virt, phys, (int) regionSize / 4096, true);
				baseAddress = virt;
			} else
				throw new Exception($"Unsupported allocation type {allocationType}");
			
			Console.WriteLine($"Output address is {baseAddress:X}");

			return NtStatus.Success;
		}

		[Export(0x0F)]
		uint ExAllocatePoolWithTag(uint numberOfBytes, uint tag) {
			Console.WriteLine($"ExAllocatePoolWithTag 0x{numberOfBytes:X} bytes");
			var addr = Box.MemoryAllocator.Allocate(numberOfBytes); // XXX: Actually implement.
			Console.WriteLine($"Pool at {addr:X}");
			return addr;
		}

		[Export(0xA5)]
		uint MmAllocateContiguousMemory(uint numberOfBytes) {
			if((numberOfBytes & 0xFFF) != 0)
				numberOfBytes = (numberOfBytes & 0xFFFFF000) + 0x1000;
			var count = (int) (numberOfBytes / 4096);
			var phys = Box.PageManager.AllocPhysPages(count);
			var virt = Box.PageManager.AllocVirtPages(count);
			Box.Cpu.MapPages(virt, phys, count, true);
			return virt;
		}

		[Export(0xAF)]
		void MmLockUnlockBufferPages(uint baseAddress, uint numberOfBytes, bool unlockPages) {
			// XXX: Actually implement.
		}

		[Export(0xAD)]
		uint MmGetPhysicalAddress(uint virt) => Box.Cpu.Virt2Phys(virt);
	}
}