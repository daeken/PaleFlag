using System;
using System.Collections.Generic;
using System.Linq;
using HypervisorSharp;

namespace PaleFlag {
	public class Thread : IHandle {
		public uint Id;
		public uint Eip, Eflags = 2, Eax, Ecx, Edx, Ebx, Esi, Edi, Esp, Ebp;
		//public ushort Cs = 1 << 3, Ss = 2 << 3, Ds = 2 << 3, Es = 2 << 3, Fs = 2 << 3, Gs = 2 << 3;
		public uint Tib;

		public void Close() {
		}
	}
	
	public class ThreadManager {
		readonly Xbox Box;
		readonly CpuCore Cpu;
		readonly Queue<Thread> Running = new Queue<Thread>();
		public Thread Current;
		uint ThreadIter;

		public Dictionary<uint, string> Threads => Running.Concat(new[] { Current }).Where(x => x != null).ToDictionary(x => x.Id, x => "Running");

		public ThreadManager(Xbox box) {
			Box = box;
			Cpu = Box.Cpu;
		}

		public Thread Add(uint ep, uint sp) {
			var thread = new Thread { Eip = ep, Esp = sp, Id = ++ThreadIter };
			lock(this) {
				Running.Enqueue(thread);
				return thread;
			}
		}

		public void Next() {
			lock(this) {
				if(Running.Count == 0) return;

				if(Current != null) {
					Freeze(Current);
					Running.Enqueue(Current);
				}

				Console.WriteLine($"Switching threads");
				Thaw(Current = Running.Dequeue());
			}
		}

		void Thaw(Thread thread) {
			Cpu[HvReg.RIP] = thread.Eip;
			Cpu[HvReg.RFLAGS] = thread.Eflags;
			Cpu[HvReg.RAX] = thread.Eax;
			Cpu[HvReg.RBX] = thread.Ebx;
			Cpu[HvReg.RCX] = thread.Ecx;
			Cpu[HvReg.RDX] = thread.Edx;
			Cpu[HvReg.RSI] = thread.Esi;
			Cpu[HvReg.RDI] = thread.Edi;
			Cpu[HvReg.RSP] = thread.Esp;
			Cpu[HvReg.RBP] = thread.Ebp;
			Cpu[HvVmcsField.GUEST_FS_BASE] = thread.Tib;
		}

		void Freeze(Thread thread) {
			thread.Eip = Cpu[HvReg.RIP];
			thread.Eflags = Cpu[HvReg.RFLAGS];
			thread.Eax = Cpu[HvReg.RAX];
			thread.Ebx = Cpu[HvReg.RBX];
			thread.Ecx = Cpu[HvReg.RCX];
			thread.Edx = Cpu[HvReg.RDX];
			thread.Esi = Cpu[HvReg.RSI];
			thread.Edi = Cpu[HvReg.RDI];
			thread.Esp = Cpu[HvReg.RSP];
			thread.Ebp = Cpu[HvReg.RBP];
			thread.Tib = Cpu[HvVmcsField.GUEST_FS_BASE];
		}
	}
}