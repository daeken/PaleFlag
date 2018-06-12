using System.Collections.Generic;
using System.Linq;
using static System.Console;

namespace PaleFlag {
	public class MemoryAllocator {
		public readonly Xbox Box;
		
		public readonly Dictionary<uint, uint> FreeList = new Dictionary<uint, uint>();
		public readonly Dictionary<uint, uint> Allocated = new Dictionary<uint, uint>();

		public MemoryAllocator(Xbox box) => Box = box;

		public uint Allocate(uint bytes) {
			lock(this) {
				KeyValuePair<uint, uint> block;
				while(true) {
					block = FreeList.FirstOrDefault(x => x.Value >= bytes);
					if(block.Key == 0)
						Expand();
					else
						break;
				}

				FreeList.Remove(block.Key);
				if(block.Value > bytes)
					Add(block.Key + bytes, block.Value - bytes);

				return block.Key;
			}
		}

		public void Free(uint addr, uint? size = null) {
			if(size == null)
				size = Allocated[addr];

			Allocated.Remove(addr);
			Add(addr, size.Value);
		}

		void Expand() {
			// Expand by 8MB at a time
			var virt = Box.PageManager.AllocVirtPages(2048);
			var phys = Box.PageManager.AllocPhysPages(2048);
			Box.Cpu.MapPages(virt, phys, 4096, true);
			Add(virt, 2048 * 4096);
		}

		void Add(uint addr, uint size) {
			var existing = FreeList.FirstOrDefault(x => x.Key + x.Value == addr);
			if(existing.Key != 0) {
				FreeList.Remove(existing.Key);
				addr = existing.Key;
				size += existing.Value;
			}

			existing = FreeList.FirstOrDefault(x => x.Key == addr + size);
			if(existing.Key != 0) {
				FreeList.Remove(existing.Key);
				size += existing.Value;
			}

			FreeList[addr] = size;
		}
	}
}