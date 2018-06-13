using System;
using System.Runtime.InteropServices;
#pragma warning disable 414
#pragma warning disable 169

namespace PaleFlag.XboxKernel {
	public partial class Kernel {
		struct Kdpc {
			public ushort Type;
			public byte Number, Importance;
			public ListEntry DpcListEntry;
			public uint DeferredRoutine, DeferredContext;
			public uint SystemArgument1, SystemArgument2;
		}
		
		[Export(0x6B)]
		void KeInitializeDpc(GuestMemory<Kdpc> dpc, uint deferredRoutine, uint deferredContext) {
			dpc.Value = new Kdpc {
				Number = 0, 
				Type = (byte) Kobject.DpcObject, 
				DeferredRoutine = deferredRoutine, 
				DeferredContext = deferredContext
			};
		}
	}
}