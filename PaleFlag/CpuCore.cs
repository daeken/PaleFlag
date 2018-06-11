using System;
using System.Diagnostics;
using HypervisorSharp;
using static System.Console;

namespace PaleFlag {
	public unsafe class CpuCore {
		const uint PagetableAddr = 0xF0000000;
		const uint GdtAddr = 0xF0500000;
		
		readonly HvMac Hv = new HvMac();
		readonly HvMacVcpu Cpu;
		readonly byte* PagetableBase, GdtBase;
		
		public uint this[HvReg reg] {
			get => (uint) Cpu[reg];
			set => Cpu[reg] = value;
		}
		
		public CpuCore() {
			Cpu = Hv.CreateVcpu();

			ulong Cap2Ctrl(ulong cap, uint ctrl) => (ctrl | (cap & 0xffffffff)) & (cap >> 32);

			void SetSegment(HvVmcsField seg, HvVmcsField ar, HvVmcsField limit, HvVmcsField @base) {
				Cpu[seg] = seg == HvVmcsField.GUEST_CS ? 1U << 3 : 2U << 3;
				Cpu[ar] = 0xC093;
				Cpu[limit] = 0xFFFFFFFFU;
				Cpu[@base] = 0;
			}
			
			var VMCS_PRI_PROC_BASED_CTLS_HLT = 1U << 7;
			var VMCS_PRI_PROC_BASED_CTLS_CR8_LOAD = 1U << 19;
			var VMCS_PRI_PROC_BASED_CTLS_CR8_STORE = 1U << 20;

			Cpu[HvVmcsField.CTRL_PIN_BASED] = Cap2Ctrl(Hv[HvVmxCapability.PINBASED], 0);
			Cpu[HvVmcsField.CTRL_CPU_BASED] = Cap2Ctrl(Hv[HvVmxCapability.PROCBASED], VMCS_PRI_PROC_BASED_CTLS_HLT | VMCS_PRI_PROC_BASED_CTLS_CR8_LOAD | VMCS_PRI_PROC_BASED_CTLS_CR8_STORE);
			Cpu[HvVmcsField.CTRL_CPU_BASED2] = Cap2Ctrl(Hv[HvVmxCapability.PROCBASED2], 0);
			Cpu[HvVmcsField.CTRL_VMENTRY_CONTROLS] = Cap2Ctrl(Hv[HvVmxCapability.ENTRY], 0);

			Cpu[HvVmcsField.CTRL_EXC_BITMAP] = 0xFFFFFFFF;
			Cpu[HvVmcsField.CTRL_CR0_MASK] = 0xFFFFFFFF;
			Cpu[HvVmcsField.CTRL_CR0_SHADOW] = 0xFFFFFFFF;
			Cpu[HvVmcsField.CTRL_CR4_MASK] = 0xFFFFFFFF;
			Cpu[HvVmcsField.CTRL_CR4_SHADOW] = 0xFFFFFFFF;
			
			SetSegment(HvVmcsField.GUEST_CS, HvVmcsField.GUEST_CS_AR, HvVmcsField.GUEST_CS_LIMIT, HvVmcsField.GUEST_CS_BASE);
			SetSegment(HvVmcsField.GUEST_DS, HvVmcsField.GUEST_DS_AR, HvVmcsField.GUEST_DS_LIMIT, HvVmcsField.GUEST_DS_BASE);
			SetSegment(HvVmcsField.GUEST_ES, HvVmcsField.GUEST_ES_AR, HvVmcsField.GUEST_ES_LIMIT, HvVmcsField.GUEST_ES_BASE);
			SetSegment(HvVmcsField.GUEST_FS, HvVmcsField.GUEST_FS_AR, HvVmcsField.GUEST_FS_LIMIT, HvVmcsField.GUEST_FS_BASE);
			SetSegment(HvVmcsField.GUEST_GS, HvVmcsField.GUEST_GS_AR, HvVmcsField.GUEST_GS_LIMIT, HvVmcsField.GUEST_GS_BASE);
			SetSegment(HvVmcsField.GUEST_SS, HvVmcsField.GUEST_SS_AR, HvVmcsField.GUEST_SS_LIMIT, HvVmcsField.GUEST_SS_BASE);

			Cpu[HvVmcsField.GUEST_LDTR] = Cpu[HvVmcsField.GUEST_TR] = 0;
			Cpu[HvVmcsField.GUEST_LDTR_LIMIT] = Cpu[HvVmcsField.GUEST_TR_LIMIT] = 0;
			Cpu[HvVmcsField.GUEST_LDTR_BASE] = Cpu[HvVmcsField.GUEST_TR_BASE] = 0;
			Cpu[HvVmcsField.GUEST_LDTR_AR] = 0x10000;
			Cpu[HvVmcsField.GUEST_TR_AR] = 0x83;

			PagetableBase = Hv.Map(PagetableAddr, 4 * 1024 * 1024 + 4 * 1024, HvMemoryFlags.RW);
			GdtBase = Hv.Map(GdtAddr, 64 * 1024, HvMemoryFlags.RW);
			
			SetupPagetable();
		}
		
		void SetupPagetable() {
			void GdtEncode(int entry, uint @base, uint limit, byte type) {
				var gdt = GdtBase + entry * 8;
				if(limit > 65536) {
					limit >>= 12;
					gdt[6] = 0xc0;
				} else
					gdt[6] = 0x40;

				gdt[0] = (byte) (limit & 0xFF);
				gdt[1] = (byte) ((limit >> 8) & 0xFF);
				gdt[6] |= (byte) ((limit >> 16) & 0xF);

				gdt[2] = (byte) (@base & 0xFF);
				gdt[3] = (byte) ((@base >> 8) & 0xFF);
				gdt[4] = (byte) ((@base >> 16) & 0xFF);
				gdt[7] = (byte) ((@base >> 24) & 0xFF);

				gdt[5] = type;
			}
			
			var dir = (uint*) PagetableBase;
			for(var i = 0; i < 1024; ++i) {
				dir[i] = (uint) ((PagetableAddr + 4096 * (i + 1)) | 7);
				var table = (uint*) (PagetableBase + 4096 * (i + 1));
				for(var j = 0; j < 1024; ++j)
					table[j] = 0;
			}

			Cpu[HvReg.CR3] = PagetableAddr;
			Cpu[HvReg.CR0] = 0x80000000 | 0x20 | 0x01;

			Cpu[HvReg.GDT_BASE] = GdtAddr;
			Cpu[HvReg.GDT_LIMIT] = 0xFFFF;
			
			GdtEncode(0, 0, 0, 0);
			GdtEncode(1, 0, 0xFFFFFFFF, 0x9A);
			GdtEncode(2, 0, 0xFFFFFFFF, 0x92);

			Cpu[HvReg.CR4] = 0x2000;
		}

		public byte* CreatePhysicalPages(uint addr, int count) {
			return Hv.Map(addr, (ulong) (count * 16 * 1024 * 1024), HvMemoryFlags.RWX);
		}

		public void MapPages(uint virt, uint phys, uint count, bool present) {
			Debug.Assert(Cpu[HvReg.CR3] == PagetableAddr);
			var dir = (uint*) PagetableBase;
			for(var i = 0; i < count; ++i) {
				var tableOff = dir[virt >> 22] & 0xFFFFF000;
				Debug.Assert(tableOff > PagetableAddr && tableOff < PagetableAddr + 5 * 1024 * 1024);
				var table = (uint*) (PagetableBase + (tableOff - PagetableAddr));
				table[(virt >> 12) & 0x3FF] = phys | 0x6U | (present ? 1U : 0U);
				virt += 4096;
				phys += 4096;
			}
			Cpu.InvalidateTlb();
		}

		public bool IsMapped(uint addr) {
			Debug.Assert(Cpu[HvReg.CR3] == PagetableAddr);
			var dir = (uint*) PagetableBase;
			var tableOff = dir[addr >> 22] & 0xFFFFF000;
			var table = (uint*) (PagetableBase + (tableOff - PagetableAddr));
			return (table[(addr >> 12) & 0x3FF] & 1) == 1;
		}

		public uint Virt2Phys(uint addr) {
			Debug.Assert(Cpu[HvReg.CR3] == PagetableAddr);
			var dir = (uint*) PagetableBase;
			var tableOff = dir[addr >> 22] & 0xFFFFF000;
			var table = (uint*) (PagetableBase + (tableOff - PagetableAddr));
			return (table[(addr >> 12) & 0x3FF] & 0xFFFFF000) | (addr & 0xFFF);
		}
		
		public void Run() {
			while(true)
				Enter();
		}

		void Enter() {
			WriteLine($"Entering {Cpu[HvReg.RIP]:X}");
			
			Cpu.Enter();

			var _reason = Cpu[HvVmcsField.RO_EXIT_REASON];
			if((_reason & 0x80000000) != 0)
				throw new Exception($"Failed to enter: {_reason:X8}");

			var reason = (HvExitReason) _reason;
			WriteLine($"Exited with {reason}");

			switch(reason) {
				case HvExitReason.EXC_NMI:
					Environment.Exit(0);
					break;
			}
			
			WriteLine($"Exiting {Cpu[HvReg.RIP]:X}");
		}
	}
}