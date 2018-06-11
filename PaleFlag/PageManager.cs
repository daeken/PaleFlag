using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using MoreLinq;
using static System.Console;

namespace PaleFlag {
	public unsafe class PageManager {
		readonly CpuCore Cpu;
		readonly byte*[] PhysBlocks = new byte*[240];
		readonly bool[] PhysPages = new bool[960 * 1024], VirtPages = new bool[1024 * 1024];
		readonly Dictionary<uint, int>
			FreePhysPages = new Dictionary<uint, int> { [0x1000] = 960 * 1024 }, 
			FreeVirtPages = new Dictionary<uint, int> { [0x1000] = 1024 * 1024 };

		public PageManager(CpuCore cpu) => Cpu = cpu;

		void AllocPhysBlocks(uint addr, int count) {
			var saddr = addr >> 24;
			void DoAlloc(int off, int pcount) {
				WriteLine($"\tAllocating {pcount} physical blocks for {(saddr + off) << 24:X}");
				var ptr = Cpu.CreatePhysicalPages((uint) (addr + 16 * 1024 * 1024 * off), pcount);
				for(var k = 0; k < pcount; ++k)
					PhysBlocks[saddr + off + k] = ptr + 16 * 1024 * 1024 * k;
			}
			
			var ccount = 0;
			for(var i = 0; i < count; ++i) {
				if(PhysBlocks[i] == (byte*) 0) {
					ccount++;
					continue;
				}
				if(ccount == 0) continue;
				
				DoAlloc(i - ccount, ccount);
				ccount = 0;
			}
			
			if(ccount != 0)
				DoAlloc(count - ccount, ccount);
		}

		public uint AllocPhysPages(int count) {
			var addr = AllocPages(count, PhysPages, FreePhysPages);
			AllocPhysBlocks(addr, (count & 0xFFF) != 0 ? count / 4096 + 1 : count / 4096);
			return addr;
		}

		public uint AllocVirtPages(int count, uint? min = null, uint? max = null, uint? at = null) {
			if(at != null) {
				min = at;
				max = (uint) (at + count * 4096);
			}
			return AllocPages(count, VirtPages, FreeVirtPages, min, max);
		}

		uint AllocPages(int count, bool[] resident, Dictionary<uint, int> free, uint? min = null, uint? max = null) {
			void SetFree(uint addr, int pages) {
				WriteLine($"Adding block at {addr:X8} with {pages} pages");
				free[addr] = pages;
			}
			
			WriteLine($"Finding allocation block in {(resident == VirtPages ? "virtual" : "physical")} memory for {count} pages -- min={min} max={max}");
			var block = free.Where(x => x.Value >= count && (min == null || x.Key >= min) && (max == null || x.Key + count * 4096 <= max)).OrderBy(x => x.Value).First();
			WriteLine($"Found block in which to allocate: {block.Value} pages at {block.Key:X8}");
			free.Remove(block.Key);
			var start = block.Key;
			if(block.Value > count) {
				if(min == null || block.Key == min)
					SetFree((uint) (block.Key + 4096 * count), block.Value - count);
				else {
					var off = (min.Value - block.Key) / 4096;
					if(off + count < block.Value)
						SetFree((uint) (block.Key + (off + count) * 4096), (int) (block.Value - off - count));
					SetFree(block.Key, (int) off);
					start += off * 4096;
				}
			}

			var sbase = start >> 12;
			for(var i = 0; i < count; ++i)
				resident[sbase + i] = true;
			
			return start;
		}

		public void ReleasePhysPages(uint addr, int count) => ReleasePages(addr, count, PhysPages, FreePhysPages);
		public void ReleaseVirtPages(uint addr, int count) => ReleasePages(addr, count, VirtPages, FreeVirtPages);

		void ReleasePages(uint addr, int count, bool[] resident, Dictionary<uint, int> free) {
			void SetFree(uint _addr, int pages) {
				WriteLine($"Adding block at {_addr:X8} with {pages} pages");
				free[_addr] = pages;
			}
			
			WriteLine($"Releasing {count} pages at {addr:X8} in {(resident == VirtPages ? "virtual" : "physical")} memory");
			var adjacent = free.Where(x => x.Key == addr + count * 4096 || x.Key + x.Value * 4096 == addr).ToList();
			adjacent.ForEach(x => free.Remove(x.Key));
			var sbase = addr >> 12;
			for(var i = 0; i < count; ++i)
				resident[sbase + i] = false;
			if(adjacent.Count == 0) {
				SetFree(addr, count);
				return;
			}
			WriteLine($"Found {adjacent.Count} adjacent blocks");
			adjacent.Add(new KeyValuePair<uint, int>(addr, count));
			SetFree(adjacent.Select(x => x.Key).Aggregate(Math.Min), adjacent.Select(x => x.Value).Sum());
		}

		public T Read<T>(uint vaddr) {
			var size = Marshal.SizeOf<T>();
			var data = new byte[size];

			var prevPage = 0xFFFFFFFFU;
			var physPage = 0U;
			for(var i = 0U; i < size; ++i) {
				var page = (vaddr + i) & 0xFFFFF000U;
				if(page != prevPage) {
					prevPage = page;
					physPage = Cpu.Virt2Phys(page);
				}
				data[i] = ReadPhys(physPage + (vaddr + i - page));
			}
			
			var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
			var ret = Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
			handle.Free();
			return ret;
		}

		public void Write<T>(uint vaddr, T value) {
			var size = Marshal.SizeOf<T>();
			var data = new byte[size];

			var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
			Marshal.StructureToPtr(value, handle.AddrOfPinnedObject(), true);
			handle.Free();

			var prevPage = 0xFFFFFFFFU;
			var physPage = 0U;
			for(var i = 0U; i < size; ++i) {
				var page = (vaddr + i) & 0xFFFFF000U;
				if(page != prevPage) {
					prevPage = page;
					physPage = Cpu.Virt2Phys(page);
				}
				WritePhys(physPage + (vaddr + i - page), data[i]);
			}
		}

		public byte ReadPhys(uint paddr) {
			var blockNum = paddr >> 24;
			var blockBase = blockNum << 24;
			Debug.Assert(PhysBlocks[blockNum] != (byte*) 0);
			return PhysBlocks[blockNum][paddr - blockBase];
		}

		public void WritePhys(uint paddr, byte value) {
			WriteLine($"Writing {value:X} to guest physical {paddr:X8}");
			var blockNum = paddr >> 24;
			var blockBase = blockNum << 24;
			Debug.Assert(PhysBlocks[blockNum] != (byte*) 0);
			PhysBlocks[blockNum][paddr - blockBase] = value;
		}
	}
}