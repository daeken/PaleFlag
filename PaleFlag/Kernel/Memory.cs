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
		NtStatus NtAllocateVirtualMemory(GuestMemory<uint> baseAddress, GuestMemory<uint> zeroBits,
			GuestMemory<uint> regionSize, AllocationType allocationType, uint protect) {
			var addr = baseAddress.Value &= 0xFFFFF000;
			var size = regionSize.Value;
			if((size & 0xFFF) != 0) size = (size & 0xFFFFF000) + 4096;
			regionSize.Value = size;
			Console.WriteLine($"Allocating 0x{size:X} bytes at {addr:X} ({allocationType})");

			if(allocationType.HasFlag(AllocationType.Commit) || allocationType.HasFlag(AllocationType.Reserve)) {
				if(addr == 0)
					baseAddress.Value = Box.MemoryAllocator.Allocate(size / 4096);
				else {
					if(Box.Cpu.IsMapped(addr))
						return NtStatus.Success;
					var virt = Box.PageManager.AllocVirtPages((int) size / 4096, at: addr);
					var phys = Box.PageManager.AllocPhysPages((int) size / 4096);
					Box.Cpu.MapPages(virt, phys, (int) size / 4096, true);
				}
			} else
				throw new Exception($"Unsupported allocation type {allocationType}");

			return NtStatus.Success;
		}
	}
}