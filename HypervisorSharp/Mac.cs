using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace HypervisorSharp {
	internal enum HvReturn : uint {
		Success = 0, 
		Error = 0xFAE94001, 
		Busy = 0xFAE94002, 
		BadArgument = 0xFAE94003, 
		NoResources = 0xFAE94005, 
		NoDevice = 0xFAE94006, 
		Unsupported = 0xFAE9400F
	}

	[Flags]
	internal enum HvVmOptions : ulong {
		Default = 0
	}

	[Flags]
	internal enum HvVcpuOptions : ulong {
		Default = 0
	}

	[Flags]
	public enum HvMemoryFlags : ulong {
		Read = 1 << 0, 
		Write = 1 << 1, 
		Exec = 1 << 2, 
		RW = Read | Write, 
		RX = Read | Exec, 
		RWX = Read | Write | Exec
	}

	public enum HvReg {
		RIP,
		RFLAGS,
		RAX,
		RCX,
		RDX,
		RBX,
		RSI,
		RDI,
		RSP,
		RBP,
		R8,
		R9,
		R10,
		R11,
		R12,
		R13,
		R14,
		R15,
		CS,
		SS,
		DS,
		ES,
		FS,
		GS,
		IDT_BASE,
		IDT_LIMIT,
		GDT_BASE,
		GDT_LIMIT,
		LDTR,
		LDT_BASE,
		LDT_LIMIT,
		LDT_AR,
		TR,
		TSS_BASE,
		TSS_LIMIT,
		TSS_AR,
		CR0,
		CR1,
		CR2,
		CR3,
		CR4,
		DR0,
		DR1,
		DR2,
		DR3,
		DR4,
		DR5,
		DR6,
		DR7,
		TPR,
		XCR0
	}

	public enum HvVmxCapability {
		PINBASED = 0,         // pin-based VMX capabilities
		PROCBASED = 1,        // primary proc.-based VMX capabilities
		PROCBASED2 = 2,       // second. proc.-based VMX capabilities
		ENTRY = 3,            // VM-entry VMX capabilities
		EXIT = 4,             // VM-exit VMX capabilities
		PREEMPTION_TIMER = 32 // VMX preemption timer frequency
	}

	public enum HvVmxReason {
		EXC_NMI					= 0,
		IRQ						= 1,
		TRIPLE_FAULT			= 2,
		INIT					= 3,
		SIPI					= 4,
		IO_SMI					= 5,
		OTHER_SMI				= 6,
		IRQ_WND					= 7,
		VIRTUAL_NMI_WND			= 8,
		TASK					= 9,
		CPUID					= 10,
		GETSEC					= 11,
		HLT						= 12,
		INVD					= 13,
		INVLPG					= 14,
		RDPMC					= 15,
		RDTSC					= 16,
		RSM						= 17,
		VMCALL					= 18,
		VMCLEAR					= 19,
		VMLAUNCH				= 20,
		VMPTRLD					= 21,
		VMPTRST					= 22,
		VMREAD					= 23,
		VMRESUME				= 24,
		VMWRITE					= 25,
		VMOFF					= 26,
		VMON					= 27,
		MOV_CR					= 28,
		MOV_DR					= 29,
		IO						= 30,
		RDMSR					= 31,
		WRMSR					= 32,
		VMENTRY_GUEST			= 33,
		VMENTRY_MSR				= 34,
		MWAIT					= 36,
		MTF						= 37,
		MONITOR					= 39,
		PAUSE					= 40,
		VMENTRY_MC				= 41,
		TPR_THRESHOLD			= 43,
		APIC_ACCESS				= 44,
		VIRTUALIZED_EOI			= 45,
		GDTR_IDTR				= 46,
		LDTR_TR					= 47,
		EPT_VIOLATION			= 48,
		EPT_MISCONFIG			= 49,
		EPT_INVEPT				= 50,
		RDTSCP					= 51,
		VMX_TIMER_EXPIRED		= 52,
		INVVPID					= 53,
		WBINVD					= 54,
		XSETBV					= 55,
		APIC_WRITE				= 56,
		RDRAND					= 57,
		INVPCID					= 58,
		VMFUNC					= 59,
		RDSEED					= 61,
		XSAVES					= 63,
		XRSTORS					= 64
	}

	public enum HvVmcsField {
		VPID = 0x00000000,
		CTRL_POSTED_INT_N_VECTOR = 0x00000002,
		CTRL_EPTP_INDEX = 0x00000004,
		GUEST_ES = 0x00000800,
		GUEST_CS = 0x00000802,
		GUEST_SS = 0x00000804,
		GUEST_DS = 0x00000806,
		GUEST_FS = 0x00000808,
		GUEST_GS = 0x0000080a,
		GUEST_LDTR = 0x0000080c,
		GUEST_TR = 0x0000080e,
		GUEST_INT_STATUS = 0x00000810,
		HOST_ES = 0x00000c00,
		HOST_CS = 0x00000c02,
		HOST_SS = 0x00000c04,
		HOST_DS = 0x00000c06,
		HOST_FS = 0x00000c08,
		HOST_GS = 0x00000c0a,
		HOST_TR = 0x00000c0c,
		CTRL_IO_BITMAP_A = 0x00002000,
		CTRL_IO_BITMAP_B = 0x00002002,
		CTRL_MSR_BITMAPS = 0x00002004,
		CTRL_VMEXIT_MSR_STORE_ADDR = 0x00002006,
		CTRL_VMEXIT_MSR_LOAD_ADDR = 0x00002008,
		CTRL_VMENTRY_MSR_LOAD_ADDR = 0x0000200a,
		PTR = 0x0000200c,
		CTRL_TSC_OFFSET = 0x00002010,
		CTRL_VIRTUAL_APIC = 0x00002012,
		CTRL_APIC_ACCESS = 0x00002014,
		CTRL_POSTED_INT_DESC_ADDR = 0x00002016,
		CTRL_VMFUNC_CTRL = 0x00002018,
		CTRL_EPTP = 0x0000201a,
		CTRL_EOI_EXIT_BITMAP_0 = 0x0000201c,
		CTRL_EOI_EXIT_BITMAP_1 = 0x0000201e,
		CTRL_EOI_EXIT_BITMAP_2 = 0x00002020,
		CTRL_EOI_EXIT_BITMAP_3 = 0x00002022,
		CTRL_EPTP_LIST_ADDR = 0x00002024,
		CTRL_VMREAD_BITMAP_ADDR = 0x00002026,
		CTRL_VMWRITE_BITMAP_ADDR = 0x00002028,
		CTRL_VIRT_EXC_INFO_ADDR = 0x0000202a,
		CTRL_XSS_EXITING_BITMAP = 0x0000202c,
		GUEST_PHYSICAL_ADDRESS = 0x00002400,
		GUEST_LINK_POINTER = 0x00002800,
		GUEST_IA32_DEBUGCTL = 0x00002802,
		GUEST_IA32_PAT = 0x00002804,
		GUEST_IA32_EFER = 0x00002806,
		GUEST_IA32_PERF_GLOBAL_CTRL = 0x00002808,
		GUEST_PDPTE0 = 0x0000280a,
		GUEST_PDPTE1 = 0x0000280c,
		GUEST_PDPTE2 = 0x0000280e,
		GUEST_PDPTE3 = 0x00002810,
		HOST_IA32_PAT = 0x00002c00,
		HOST_IA32_EFER = 0x00002c02,
		HOST_IA32_PERF_GLOBAL_CTRL = 0x00002c04,
		CTRL_PIN_BASED = 0x00004000,
		CTRL_CPU_BASED = 0x00004002,
		CTRL_EXC_BITMAP = 0x00004004,
		CTRL_PF_ERROR_MASK = 0x00004006,
		CTRL_PF_ERROR_MATCH = 0x00004008,
		CTRL_CR3_COUNT = 0x0000400a,
		CTRL_VMEXIT_CONTROLS = 0x0000400c,
		CTRL_VMEXIT_MSR_STORE_COUNT = 0x0000400e,
		CTRL_VMEXIT_MSR_LOAD_COUNT = 0x00004010,
		CTRL_VMENTRY_CONTROLS = 0x00004012,
		CTRL_VMENTRY_MSR_LOAD_COUNT = 0x00004014,
		CTRL_VMENTRY_IRQ_INFO = 0x00004016,
		CTRL_VMENTRY_EXC_ERROR = 0x00004018,
		CTRL_VMENTRY_INSTR_LEN = 0x0000401a,
		CTRL_TPR_THRESHOLD = 0x0000401c,
		CTRL_CPU_BASED2 = 0x0000401e,
		CTRL_PLE_GAP = 0x00004020,
		CTRL_PLE_WINDOW = 0x00004022,
		RO_INSTR_ERROR = 0x00004400,
		RO_EXIT_REASON = 0x00004402,
		RO_VMEXIT_IRQ_INFO = 0x00004404,
		RO_VMEXIT_IRQ_ERROR = 0x00004406,
		RO_IDT_VECTOR_INFO = 0x00004408,
		RO_IDT_VECTOR_ERROR = 0x0000440a,
		RO_VMEXIT_INSTR_LEN = 0x0000440c,
		RO_VMX_INSTR_INFO = 0x0000440e,
		GUEST_ES_LIMIT = 0x00004800,
		GUEST_CS_LIMIT = 0x00004802,
		GUEST_SS_LIMIT = 0x00004804,
		GUEST_DS_LIMIT = 0x00004806,
		GUEST_FS_LIMIT = 0x00004808,
		GUEST_GS_LIMIT = 0x0000480a,
		GUEST_LDTR_LIMIT = 0x0000480c,
		GUEST_TR_LIMIT = 0x0000480e,
		GUEST_GDTR_LIMIT = 0x00004810,
		GUEST_IDTR_LIMIT = 0x00004812,
		GUEST_ES_AR = 0x00004814,
		GUEST_CS_AR = 0x00004816,
		GUEST_SS_AR = 0x00004818,
		GUEST_DS_AR = 0x0000481a,
		GUEST_FS_AR = 0x0000481c,
		GUEST_GS_AR = 0x0000481e,
		GUEST_LDTR_AR = 0x00004820,
		GUEST_TR_AR = 0x00004822,
		GUEST_IGNORE_IRQ = 0x00004824,
		GUEST_ACTIVITY_STATE = 0x00004826,
		GUEST_SMBASE = 0x00004828,
		GUEST_IA32_SYSENTER_CS = 0x0000482a,
		GUEST_VMX_TIMER_VALUE = 0x0000482e,
		HOST_IA32_SYSENTER_CS = 0x00004c00,
		CTRL_CR0_MASK = 0x00006000,
		CTRL_CR4_MASK = 0x00006002,
		CTRL_CR0_SHADOW = 0x00006004,
		CTRL_CR4_SHADOW = 0x00006006,
		CTRL_CR3_VALUE0 = 0x00006008,
		CTRL_CR3_VALUE1 = 0x0000600a,
		CTRL_CR3_VALUE2 = 0x0000600c,
		CTRL_CR3_VALUE3 = 0x0000600e,
		RO_EXIT_QUALIFIC = 0x00006400,
		RO_IO_RCX = 0x00006402,
		RO_IO_RSI = 0x00006404,
		RO_IO_RDI = 0x00006406,
		RO_IO_RIP = 0x00006408,
		RO_GUEST_LIN_ADDR = 0x0000640a,
		GUEST_CR0 = 0x00006800,
		GUEST_CR3 = 0x00006802,
		GUEST_CR4 = 0x00006804,
		GUEST_ES_BASE = 0x00006806,
		GUEST_CS_BASE = 0x00006808,
		GUEST_SS_BASE = 0x0000680a,
		GUEST_DS_BASE = 0x0000680c,
		GUEST_FS_BASE = 0x0000680e,
		GUEST_GS_BASE = 0x00006810,
		GUEST_LDTR_BASE = 0x00006812,
		GUEST_TR_BASE = 0x00006814,
		GUEST_GDTR_BASE = 0x00006816,
		GUEST_IDTR_BASE = 0x00006818,
		GUEST_DR7 = 0x0000681a,
		GUEST_RSP = 0x0000681c,
		GUEST_RIP = 0x0000681e,
		GUEST_RFLAGS = 0x00006820,
		GUEST_DEBUG_EXC = 0x00006822,
		GUEST_SYSENTER_ESP = 0x00006824,
		GUEST_SYSENTER_EIP = 0x00006826,
		HOST_CR0 = 0x00006c00,
		HOST_CR3 = 0x00006c02,
		HOST_CR4 = 0x00006c04,
		HOST_FS_BASE = 0x00006c06,
		HOST_GS_BASE = 0x00006c08,
		HOST_TR_BASE = 0x00006c0a,
		HOST_GDTR_BASE = 0x00006c0c,
		HOST_IDTR_BASE = 0x00006c0e,
		HOST_IA32_SYSENTER_ESP = 0x00006c10,
		HOST_IA32_SYSENTER_EIP = 0x00006c12,
		HOST_RSP = 0x00006c14,
		HOST_RIP = 0x00006c16
	}

	public enum HvExitReason {
		EXC_NMI = 0,
		IRQ = 1,
		TRIPLE_FAULT = 2,
		INIT = 3,
		SIPI = 4,
		IO_SMI = 5,
		OTHER_SMI = 6,
		IRQ_WND = 7,
		VIRTUAL_NMI_WND = 8,
		TASK = 9,
		CPUID = 10,
		GETSEC = 11,
		HLT = 12,
		INVD = 13,
		INVLPG = 14,
		RDPMC = 15,
		RDTSC = 16,
		RSM = 17,
		VMCALL = 18,
		VMCLEAR = 19,
		VMLAUNCH = 20,
		VMPTRLD = 21,
		VMPTRST = 22,
		VMREAD = 23,
		VMRESUME = 24,
		VMWRITE = 25,
		VMOFF = 26,
		VMON = 27,
		MOV_CR = 28,
		MOV_DR = 29,
		IO = 30,
		RDMSR = 31,
		WRMSR = 32,
		VMENTRY_GUEST = 33,
		VMENTRY_MSR = 34,
		MWAIT = 36,
		MTF = 37,
		MONITOR = 39,
		PAUSE = 40,
		VMENTRY_MC = 41,
		TPR_THRESHOLD = 43,
		APIC_ACCESS = 44,
		VIRTUALIZED_EOI = 45,
		GDTR_IDTR = 46,
		LDTR_TR = 47,
		EPT_VIOLATION = 48,
		EPT_MISCONFIG = 49,
		EPT_INVEPT = 50,
		RDTSCP = 51,
		VMX_TIMER_EXPIRED = 52,
		INVVPID = 53,
		WBINVD = 54,
		XSETBV = 55,
		APIC_WRITE = 56,
		RDRAND = 57,
		INVPCID = 58,
		VMFUNC = 59,
		RDSEED = 61,
		XSAVES = 63,
		XRSTORS = 64
	}

	public class MacHvException : Exception {
		readonly string Reason;
		public MacHvException(string reason) => Reason = reason;
		public override string ToString() => Reason;
	}
	
	internal static class MacWrapper {
		const string FrameworkPath = "/System/Library/Frameworks/Hypervisor.framework/Hypervisor";

		internal static void HandleError(HvReturn ret) {
			switch(ret) {
				case HvReturn.Success:
					return;
				case HvReturn.BadArgument:
					throw new MacHvException("Bad Argument");
				case HvReturn.Busy:
					throw new MacHvException("Busy");
				case HvReturn.Error:
					throw new MacHvException("Unhelpful Error");
				case HvReturn.NoDevice:
					throw new MacHvException("No Device");
				case HvReturn.NoResources:
					throw new MacHvException("No Resources");
				case HvReturn.Unsupported:
					throw new MacHvException("Unsupported");
				default:
					throw new MacHvException("Unknown");
			}
		}
		
		[DllImport(FrameworkPath)]
		internal static extern HvReturn hv_vm_create(HvVmOptions flags);
		
		[DllImport(FrameworkPath)]
		internal static extern HvReturn hv_vm_destroy();
		
		[DllImport(FrameworkPath)]
		internal static extern HvReturn hv_vm_map(UIntPtr uva, ulong gpa, UIntPtr size, HvMemoryFlags flags);
		
		[DllImport(FrameworkPath)]
		internal static extern HvReturn hv_vm_unmap(ulong gpa, UIntPtr size);
		
		[DllImport(FrameworkPath)]
		internal static extern HvReturn hv_vm_protect(ulong gpa, UIntPtr size, HvMemoryFlags flags);
		
		[DllImport(FrameworkPath)]
		internal static extern HvReturn hv_vm_sync_tsc(ulong tsc);
		
		[DllImport(FrameworkPath)]
		internal static extern HvReturn hv_vcpu_create(out uint vcpu, HvVcpuOptions flags);
		
		[DllImport(FrameworkPath)]
		internal static extern HvReturn hv_vcpu_destroy(uint vcpu);
		
		[DllImport(FrameworkPath)]
		internal static extern HvReturn hv_vcpu_read_register(uint vcpu, HvReg reg, out ulong value);
		
		[DllImport(FrameworkPath)]
		internal static extern HvReturn hv_vcpu_write_register(uint vcpu, HvReg reg, ulong value);
		
		[DllImport(FrameworkPath)]
		internal static extern HvReturn hv_vcpu_read_fpstate(uint vcpu, byte[] buffer, UIntPtr size);
		
		[DllImport(FrameworkPath)]
		internal static extern HvReturn hv_vcpu_write_fpstate(uint vcpu, byte[] buffer, UIntPtr size);
		
		[DllImport(FrameworkPath)]
		internal static extern HvReturn hv_vcpu_enable_native_msr(uint vcpu, uint msr, bool enable);
		
		[DllImport(FrameworkPath)]
		internal static extern HvReturn hv_vcpu_read_msr(uint vcpu, uint msr, out ulong value);
		
		[DllImport(FrameworkPath)]
		internal static extern HvReturn hv_vcpu_write_msr(uint vcpu, uint msr, ulong value);
		
		[DllImport(FrameworkPath)]
		internal static extern HvReturn hv_vcpu_flush(uint vcpu);
		
		[DllImport(FrameworkPath)]
		internal static extern HvReturn hv_vcpu_invalidate_tlb(uint vcpu);
		
		[DllImport(FrameworkPath)]
		internal static extern HvReturn hv_vcpu_run(uint vcpu);
		
		[DllImport(FrameworkPath)]
		internal static extern HvReturn hv_vcpu_interrupt(uint[] vcpus, uint vcpu_count);
		
		[DllImport(FrameworkPath)]
		internal static extern HvReturn hv_vcpu_get_exec_time(uint vcpu, out ulong time);
		
		[DllImport(FrameworkPath)]
		internal static extern HvReturn hv_vmx_vcpu_read_vmcs(uint vcpu, HvVmcsField field, out ulong value);
		
		[DllImport(FrameworkPath)]
		internal static extern HvReturn hv_vmx_vcpu_write_vmcs(uint vcpu, HvVmcsField field, ulong value);
		
		[DllImport(FrameworkPath)]
		internal static extern HvReturn hv_vmx_read_capability(HvVmxCapability field, out ulong value);
		
		[DllImport(FrameworkPath)]
		internal static extern HvReturn hv_vmx_vcpu_set_apic_address(uint vcpu, ulong gpa);
	}

	public class HvMac {
		public HvMac() => MacWrapper.HandleError(MacWrapper.hv_vm_create(HvVmOptions.Default));
		~HvMac() => MacWrapper.HandleError(MacWrapper.hv_vm_destroy());

		unsafe byte* AllocateAligned(ulong size) {
			var ptr = (ulong) Marshal.AllocHGlobal((IntPtr) size + 4095);
			if((ptr & 0xFFF) != 0)
				ptr += 0x1000 - (ptr & 0xFFF);
			/*var tmp = (uint*) ptr;
			for(var i = 0; i < (uint) size / 4; i++)
				tmp[i] = 0x90c1010fU;*/
			return (byte*) ptr;
		}

		public unsafe byte* Map(ulong address, ulong size, HvMemoryFlags flags) {
			Debug.Assert(size < 0x80000000);
			var addr = AllocateAligned(size);
			MacWrapper.HandleError(MacWrapper.hv_vm_map((UIntPtr) addr, address, (UIntPtr) size, flags));
			return addr;
		}

		public void Unmap(ulong address, ulong size) => MacWrapper.HandleError(MacWrapper.hv_vm_unmap(address, (UIntPtr) size));

		public void Protect(ulong address, ulong size, HvMemoryFlags flags) => MacWrapper.HandleError(MacWrapper.hv_vm_protect(address, (UIntPtr) size, flags));

		public void SyncTsc(ulong tsc) => MacWrapper.HandleError(MacWrapper.hv_vm_sync_tsc(tsc));

		public HvMacVcpu CreateVcpu() => new HvMacVcpu();

		public ulong this[HvVmxCapability cap] {
			get {
				MacWrapper.HandleError(MacWrapper.hv_vmx_read_capability(cap, out var value));
				return value;
			}
		}
	}

	public class HvMacVcpu {
		readonly uint Vcpu;

		internal HvMacVcpu() => MacWrapper.HandleError(MacWrapper.hv_vcpu_create(out Vcpu, HvVcpuOptions.Default));
		~HvMacVcpu() => MacWrapper.HandleError(MacWrapper.hv_vcpu_destroy(Vcpu));
		
		public ulong this[HvReg reg] {
			get {
				MacWrapper.HandleError(MacWrapper.hv_vcpu_read_register(Vcpu, reg, out var value));
				return value;
			}
			set => MacWrapper.HandleError(MacWrapper.hv_vcpu_write_register(Vcpu, reg, value));
		}

		public ulong this[HvVmcsField field] {
			get {
				MacWrapper.HandleError(MacWrapper.hv_vmx_vcpu_read_vmcs(Vcpu, field, out var value));
				return value;
			}
			set => MacWrapper.HandleError(MacWrapper.hv_vmx_vcpu_write_vmcs(Vcpu, field, value));
		}
		
		

		public void Enter() => MacWrapper.HandleError(MacWrapper.hv_vcpu_run(Vcpu));
		public void Flush() => MacWrapper.HandleError(MacWrapper.hv_vcpu_flush(Vcpu));
		public void InvalidateTlb() => MacWrapper.HandleError(MacWrapper.hv_vcpu_invalidate_tlb(Vcpu));
	}
}