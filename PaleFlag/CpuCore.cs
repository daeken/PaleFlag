using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using GdbStub;
using HypervisorSharp;
using PaleFlag.XboxKernel;
using static System.Console;

namespace PaleFlag {
	public unsafe class CpuCore : IDebugTarget {
		const uint PagetableAddr = 0xF0000000;
		const uint MmioPhysPage = 0xFD000000;
		
		readonly HvMac Hv = new HvMac();
		readonly HvMacVcpu Cpu;
		readonly byte* PagetableBase;
		readonly byte* MmioPhysBase;

		Gdb<CpuCore> Gdb;
		readonly Xbox Box;
		
		public uint this[HvReg reg] {
			get => (uint) Cpu[reg];
			set => Cpu[reg] = value;
		}

		public uint this[HvVmcsField field] {
			get => (uint) Cpu[field];
			set => Cpu[field] = value;
		}

		public bool SingleStepFlag {
			get => (Cpu[HvReg.RFLAGS] & 0x0100) != 0;
			set {
				if(value)
					Cpu[HvReg.RFLAGS] |= 0x0100;
				else
					Cpu[HvReg.RFLAGS] &= 0xFFFFFEFF;
			}
		}
		
		public CpuCore(Xbox box) {
			Box = box;
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

			Cpu[HvVmcsField.GUEST_LDTR] = Cpu[HvVmcsField.GUEST_TR] = 0;
			Cpu[HvVmcsField.GUEST_LDTR_LIMIT] = Cpu[HvVmcsField.GUEST_TR_LIMIT] = 0;
			Cpu[HvVmcsField.GUEST_LDTR_BASE] = Cpu[HvVmcsField.GUEST_TR_BASE] = 0;
			Cpu[HvVmcsField.GUEST_LDTR_AR] = 0x10000;
			Cpu[HvVmcsField.GUEST_TR_AR] = 0x83;
			
			Cpu[HvReg.CR4] = 0x2000 | 0x400 | 0x200;

			PagetableBase = Hv.Map(PagetableAddr, 4 * 1024 * 1024 + 4 * 1024, HvMemoryFlags.RWX);
			MmioPhysBase = Hv.Map(MmioPhysPage, 4096, HvMemoryFlags.RWX);
			
			SetupPagetable();
			
			SetSegment(HvVmcsField.GUEST_CS, HvVmcsField.GUEST_CS_AR, HvVmcsField.GUEST_CS_LIMIT, HvVmcsField.GUEST_CS_BASE);
			SetSegment(HvVmcsField.GUEST_DS, HvVmcsField.GUEST_DS_AR, HvVmcsField.GUEST_DS_LIMIT, HvVmcsField.GUEST_DS_BASE);
			SetSegment(HvVmcsField.GUEST_ES, HvVmcsField.GUEST_ES_AR, HvVmcsField.GUEST_ES_LIMIT, HvVmcsField.GUEST_ES_BASE);
			SetSegment(HvVmcsField.GUEST_FS, HvVmcsField.GUEST_FS_AR, HvVmcsField.GUEST_FS_LIMIT, HvVmcsField.GUEST_FS_BASE);
			SetSegment(HvVmcsField.GUEST_GS, HvVmcsField.GUEST_GS_AR, HvVmcsField.GUEST_GS_LIMIT, HvVmcsField.GUEST_GS_BASE);
			SetSegment(HvVmcsField.GUEST_SS, HvVmcsField.GUEST_SS_AR, HvVmcsField.GUEST_SS_LIMIT, HvVmcsField.GUEST_SS_BASE);
			
			
		}

		public void SetupDebugger() => Gdb = new Gdb<CpuCore>(this, new IPEndPoint(IPAddress.Any, 12345));

		void SetupPagetable() {
			var dir = (uint*) PagetableBase;
			for(var i = 0; i < 1024; ++i) {
				dir[i] = (uint) ((PagetableAddr + 4096 * (i + 1)) | 7);
				var table = (uint*) (PagetableBase + 4096 * (i + 1));
				for(var j = 0; j < 1024; ++j)
					table[j] = 0;
			}

			Cpu[HvReg.CR3] = PagetableAddr;
			Cpu[HvReg.CR0] = 0x80000000 | 0x20 | 0x02 | 0x01;
		}

		public byte* CreatePhysicalPages(uint addr, int count) {
			return Hv.Map(addr, (ulong) (count * 16 * 1024 * 1024), HvMemoryFlags.RWX);
		}

		public void MapPages(uint virt, uint phys, int count, bool present) {
			Debug.Assert(Cpu[HvReg.CR3] == PagetableAddr);
			var dir = (uint*) PagetableBase;
			for(var i = 0; i < count; ++i) {
				var tableOff = dir[virt >> 22] & 0xFFFFF000;
				Debug.Assert(tableOff > PagetableAddr && tableOff < PagetableAddr + 5 * 1024 * 1024);
				var table = (uint*) (PagetableBase + (tableOff - PagetableAddr));
				//WriteLine($"Setting {virt:X} to map to {phys:X}");
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

		public void DumpRegs() {
			var regs = new[] { HvReg.RAX, HvReg.RBX, HvReg.RCX, HvReg.RDX, HvReg.RSP, HvReg.RBP, HvReg.RSI, HvReg.RDI, HvReg.RIP, HvReg.RFLAGS };
			foreach(var reg in regs)
				WriteLine($"- {reg} == {(uint) Cpu[reg]:X8}");
		}

		public void Run() {
			if(Gdb == null)
				Enter();
			else
				Gdb.Run();
		}

		uint? InMmio;
		bool MmioWrite;

		void Enter() {
			while(true) {
				//WriteLine($"Entering {Cpu[HvReg.RIP]:X}");

				Cpu.Enter();

				var _reason = Cpu[HvVmcsField.RO_EXIT_REASON];
				if((_reason & 0x80000000) != 0)
					throw new Exception($"Failed to enter: {_reason:X8}");

				var reason = (HvExitReason) _reason;
				if(reason == HvExitReason.IRQ) {
					Box.ThreadManager.Next();
					continue;
				}

				if(reason == HvExitReason.EPT_VIOLATION)
					continue;
				var qual = (uint) Cpu[HvVmcsField.RO_EXIT_QUALIFIC];
				var insnLen = (uint) Cpu[HvVmcsField.RO_VMEXIT_INSTR_LEN];
				//if(reason != HvExitReason.VMCALL)
				//	WriteLine($"Exited with {reason} at {Cpu[HvReg.RIP]:X}");

				switch(reason) {
					case HvExitReason.EXC_NMI:
						var vecVal = Cpu[HvVmcsField.RO_VMEXIT_IRQ_INFO] & 0xFFFFU;
						var errorCode = Cpu[HvVmcsField.RO_VMEXIT_IRQ_ERROR];
						var cont = false;
						switch((vecVal >> 8) & 7) {
							case 6:
								var interrupt = vecVal & 0xFF;
								WriteLine($"Interrupt {interrupt}");
								if(interrupt == 3 && Gdb != null) {
									Trapped?.Invoke(TrapType.Breakpoint);
									return;
								}
								break;
							case 3:
								var exc = vecVal & 0xFF;
								switch(exc) {
									case 1 when InMmio != null:
										var mpage = InMmio.Value & 0xFFFFF000;
										MapPages(mpage, MmioPhysPage, 1, false);
										var mptr = (uint*) (MmioPhysBase + (InMmio.Value - mpage));
										if(MmioWrite)
											Box.MmioManager.Write(InMmio.Value, *mptr);
										InMmio = null;
										cont = true;
										SingleStepFlag = false;
										break;
									case 1 when Gdb != null:
										Trapped?.Invoke(TrapType.SingleStep);
										return;
									case 14:
										var isWrite = (errorCode & 2) == 2;
										if(qual >= 0xFD000000) {
											InMmio = qual;
											var page = qual & 0xFFFFF000;
											MapPages(page, MmioPhysPage, 1, true);
											if(isWrite)
												MmioWrite = true;
											else {
												MmioWrite = false;
												var ret = Box.MmioManager.Read(qual);
												var ptr = (uint*) (MmioPhysBase + (qual - page));
												ptr[0] = ret;
											}

											SingleStepFlag = true;
											cont = true;
										} else
											WriteLine($"Invalid {(isWrite ? "write" : "read")} from {qual:X}");

										break;
								}
								break;
							default:
								WriteLine($"Unknown NMI {vecVal:X}");
								break;
						}

						if(cont) break;

						DumpRegs();

						if(Gdb != null) {
							Trapped?.Invoke(TrapType.Segfault);
							return;
						}

						Environment.Exit(0);
						break;
					case HvExitReason.EPT_VIOLATION:
						break;
					case HvExitReason.VMCALL:
						var sp = (uint) Cpu[HvReg.RSP];
						Cpu[HvReg.RSP] = sp + 4;
						var retAddr = new GuestMemory<uint>(sp).Value;
						var call = (int) (Cpu[HvReg.RIP] - Xbox.KernelCallsBase) / 4;
						WriteLine($"Kernel call to {(KernelExportNames) call} (returning to {retAddr:X})");
						if(!Box.Kernel.Functions.ContainsKey(call)) {
							WriteLine($"Unimplemented kernel function 0x{call:X} - {(KernelExportNames) call}");
							Environment.Exit(0);
						}

						Box.Kernel.Functions[call]();
						Cpu[HvReg.RIP] = retAddr;
						break;
				}
			}
		}


		public event DebugTrap Trapped;
		public int RegisterSize => 32;
		public RegisterSet Registers => X86RegisterSet.Instance;

		public ulong this[string reg] {
			get {
				switch(reg) {
					case "EIP": return Cpu[HvReg.RIP];
					case "EAX": return Cpu[HvReg.RAX];
					case "EBX": return Cpu[HvReg.RBX];
					case "ECX": return Cpu[HvReg.RCX];
					case "EDX": return Cpu[HvReg.RDX];
					case "ESI": return Cpu[HvReg.RSI];
					case "EDI": return Cpu[HvReg.RDI];
					case "ESP": return Cpu[HvReg.RSP];
					case "EBP": return Cpu[HvReg.RBP];
					case "EFLAGS": return Cpu[HvReg.RFLAGS];
					case "CS": return Cpu[HvReg.CS];
					case "DS": return Cpu[HvReg.DS];
					case "ES": return Cpu[HvReg.ES];
					case "FS": return Cpu[HvReg.FS];
					case "GS": return Cpu[HvReg.GS];
					case "SS": return Cpu[HvReg.SS];
				}
				WriteLine($"Unknown register requested: {reg}");
				throw new NotImplementedException();
			}
			set => throw new NotImplementedException();
		}

		public uint ThreadId { get => Box.ThreadManager.Current.Id; set => throw new NotImplementedException(); }
		public Dictionary<uint, string> Threads => Box.ThreadManager.Threads;

		public byte[] ReadMemory(ulong addr, uint size) {
			var gm = new GuestMemory<byte>((uint) addr);
			return Enumerable.Range(0, (int) size).Select(i => gm[i]).ToArray();
		}

		public void WriteMemory(ulong addr, byte[] data) {
			var gm = new GuestMemory<byte>((uint) addr);
			for(var i = 0; i < data.Length; ++i)
				gm[i] = data[i];
		}

		public void AddBreakpoint(BreakpointType type, ulong addr) => throw new NotImplementedException();
		public void RemoveBreakpoint(BreakpointType type, ulong addr) => throw new NotImplementedException();

		bool WasSingleStepping;
		public void SingleStep(uint? threadId = null) {
			if(!WasSingleStepping) {
				WasSingleStepping = true;
				SingleStepFlag = true;
			}

			Enter();
		}

		public void Continue(uint? threadId = null) {
			if(WasSingleStepping) {
				WasSingleStepping = false;
				SingleStepFlag = false;
			}
			Enter();
		}

		public void BreakIn() => throw new NotImplementedException();
	}
}